/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;

using System.Collections.Generic;

using System.Net.Security;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.IO;


namespace SocketServer
{
    public delegate void DelegateConnectFinish(SocketClient client, bool bSuccess, string strErrors);

    public class SocketClient
    {
        public delegate void SocketEventHandler(object sender, System.EventArgs e);
        public delegate void SocketReceiveHandler(SocketClient client, byte[] bData, int nLength);

        public SocketClient()
        {
            m_AsyncConnect = new AsyncCallback(OnClientConnected);
            this.Client = null;

            /// Add our SOCKS proxy filter in case we decide we need it
            SOCKStrans = new SOCKSTransform(this);
        }


        public SocketClient(ILogInterface logmgr, string strGuid)
            : this()
        {
            m_Logger = logmgr;
            OurGuid = strGuid;
        }

        public SocketClient(Socket s, ConnectMgr parentnotify)
            : this()
        {
            Init(s, parentnotify);
        }

        protected bool m_bSocketClientDisposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (m_bSocketClientDisposed == true)
                return;
            m_bSocketClientDisposed = true;

            m_AsyncConnect = null;

            OnAsyncConnectFinished = null;
            ReceiveHandler = null;
            ReceiveHandlerBytes = null;
            DisconnectHandler = null;


            m_Logger = null;
            this.TransformList.Clear();
            this.TransformList = null;
            this.Client = null;
            this.m_bData = null;
        }


        public static IBufferPool m_BufferPool = null;


        /// <summary>
        /// set for logging
        /// </summary>
        public ILogInterface m_Logger = null;
        public string OurGuid = "SocketClient";

        public event DelegateConnectFinish OnAsyncConnectFinished = null;
        public event SocketEventHandler ReceiveHandler = null;
        public event SocketReceiveHandler ReceiveHandlerBytes = null;
        public event SocketEventHandler DisconnectHandler = null;

        private AsyncCallback m_AsyncConnect = null;
        protected bool m_bStartReadOnConnect = true;

        protected System.Net.Sockets.Socket Client;
        public System.Net.Sockets.Socket socket
        {
            get
            {
                return Client;
            }
        }

        public bool Connected
        {
            get
            {
                return Client.Connected;
            }
        }



        SOCKSTransform SOCKStrans = null;
        public SslStream SslStream = null;
        NetworkStream NetworkStream = null;
        public bool StartTLS(string strTargetHost)
        {
            if (Connected == false)
                return false;


            SslStream = new SslStream(NetworkStream, false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
            SslStream.AuthenticateAsClient(strTargetHost, null, SslProtocols.Tls, false);

            return true;
        }

        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

            return true;
            // Do not allow this client to communicate with unauthenticated servers.
            //return false;
        }



        public List<IMessageFilter> TransformList = new List<IMessageFilter>();
        public byte[] TransformSendData(byte[] bData)
        {
            if (this.m_bSocketClientDisposed == true)
                return bData;

            if (bData == null)
                return bData;
            byte[] bRet = bData;

            try
            {
                foreach (IMessageFilter nextfilter in TransformList)
                {
                    if (nextfilter == null)
                        continue;

                    if (nextfilter.IsFilterActive == false)
                        continue;

                    bRet = nextfilter.TransformSendData(bData);
                    if ((bRet == null) || (bRet.Length <= 0))
                        return new byte[] { };
                }
            }
            catch (Exception ex)
            {
                if (m_Logger != null)
                    m_Logger.LogError(OurGuid, MessageImportance.Highest, string.Format("Exception transforming data: {0}", ex));
            }

            return bRet;
        }

        // Parsed incoming data into a list of packets
        public List<byte[]> TransformReceiveData(byte[] bData)
        {
            List<byte[]> ReturnList = new List<byte[]>();
            ReturnList.Add(bData);

            if (this.m_bSocketClientDisposed == true)
                return ReturnList;

            if (bData == null)
                return ReturnList;

            try
            {
                foreach (IMessageFilter nextfilter in TransformList)
                {
                    if (nextfilter == null)
                        continue;

                    if (nextfilter.IsFilterActive == false)
                        continue;

                    List<byte[]> TempList = new List<byte[]>();
                    foreach (byte[] bNextData in ReturnList)
                        TempList.AddRange(nextfilter.TransformReceiveData(bNextData));

                    if (TempList.Count <= 0) /// no data was returned... not enough to make a message
                        return new List<byte[]>();

                    ReturnList = TempList;
                }
            }
            catch (Exception ex)
            {
                if (m_Logger != null)
                    this.LogError(MessageImportance.Highest, "", string.Format("Exception transforming data: {0}", ex));
            }


            return ReturnList;
        }

        public bool StartReadOnConnect
        {
            get
            {
                return m_bStartReadOnConnect;
            }
            set
            {
                m_bStartReadOnConnect = value;
            }
        }


        protected void Init(Socket s, ConnectMgr parentnotify)
        {
            this.Client = s;
            if ((this.Client != null) && (this.Client.Connected == true) )
               NetworkStream = new NetworkStream(this.Client);
        }

        public bool IsIPVersion6 = false;


        public void SetSOCKSProxy(int nSocksVersion, string strSocksHost, int nSocksport, string strUser)
        {
            SOCKStrans.SocksVersion = nSocksVersion;
            SOCKStrans.SocksHost = strSocksHost;
            SOCKStrans.SocksPort = nSocksport;
            SOCKStrans.User = strUser;
            SOCKStrans.IsFilterActive = true;
        }

        public bool Connect(string ipaddr, int nport)
        {
            return Connect(ipaddr, nport, true);
        }

        public bool Connect(string ipaddr, int nport, bool bbeginread)
        {
            UserInitiatedDisconnect = false;
            StartReadOnConnect = bbeginread;

            IPAddress hostadd = null;
            IPEndPoint EPhost = null;
            try
            {
                if (SocketServer.ConnectMgr.IsIPAddress(ipaddr))
                {
                    EPhost = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(ipaddr), nport);
                    IsIPVersion6 = false;
                }
                else
                {
                    if (SocketServer.ConnectMgr.IsIPV6Address(ipaddr) == true)
                    {
                        IsIPVersion6 = true;
                        EPhost = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(ipaddr), nport);
                    }
                    else
                    {
                        hostadd = SocketServer.ConnectMgr.Resolve(ipaddr);
                        EPhost = new IPEndPoint(hostadd, nport);
                    }
                }
            }
            catch (ArgumentNullException e)
            {
                if (m_Logger != null)
                    m_Logger.LogError(ToString(), MessageImportance.Highest, e.ToString());
                return false;
            }
            catch (SocketException e2)
            {
                if (m_Logger != null)
                    m_Logger.LogError(ToString(), MessageImportance.Highest, e2.ToString());
                return false;
            }

            if (Client != null)
            {
                if (Client.Connected == true)
                    Disconnect();
            }


            if ((IsIPVersion6 == true) && (System.Net.Sockets.Socket.OSSupportsIPv6 == false))
            {
                if (m_Logger != null)
                    m_Logger.LogError(ToString(), MessageImportance.Highest, string.Format("IP Version 6 not support, can't connect to {0}", ipaddr));
                return false;
            }

            Socket s = null;

            if (IsIPVersion6 == false)
                s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            else
                s = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

            // Connects to the host using IPEndPoint.
            try
            {
                s.Connect(EPhost);
            }
            catch (SocketException e)
            {
                LogError(MessageImportance.Highest, "", e.ToString());
                return false;
            }

            if (s.Connected == false)
                return false;

            Init(s, null);

            if (m_bStartReadOnConnect)
                DoAsyncRead();

            return true;
        }

        /// <summary>
        ///  Creates a tcp connection asyncronously.  The client muust call DoAsyncRead on connection completed
        /// </summary>
        /// <param name="ipaddr"></param>
        /// <param name="nport"></param>
        /// <returns></returns>
        public bool ConnectAsync(string ipaddr, int nport)
        {
            UserInitiatedDisconnect = false;
            IPAddress hostadd = null;
            IPEndPoint EPhost = null;
            if (ipaddr.Length <= 0)
                return false;

            if (SOCKStrans.IsFilterActive == true)
            {
                /// Set our proxy remote location just in case we've been activated
                SOCKStrans.RemoteHost = ipaddr;
                SOCKStrans.RemotePort = nport;
                ipaddr = SOCKStrans.SocksHost;
                nport = SOCKStrans.SocksPort;
            }


            try
            {
                if (ConnectMgr.IsIPAddress(ipaddr) == true)
                {
                    EPhost = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(ipaddr), nport);
                }
                else
                {
                    hostadd = ConnectMgr.Resolve(ipaddr);
                    EPhost = new IPEndPoint(hostadd, nport);
                }
            }
            catch (SocketException e) /// could not resolve host name
            {
                if (m_Logger != null)
                    m_Logger.LogError(ToString(), MessageImportance.Highest, e.ToString());
                return false;
            }

            //Creates the Socket for sending data over TCP.
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Connects to the host using IPEndPoint.
            try
            {
                s.BeginConnect(EPhost, m_AsyncConnect, s);
            }
            catch (SocketException e2)
            {
                if (m_Logger != null)
                    m_Logger.LogError(ToString(), MessageImportance.Highest, e2.ToString());
                return false;
            }

            return true;
        }

        private void OnClientConnected(IAsyncResult ar)
        {
            Socket s = ar.AsyncState as Socket;

            string strError = "";
            try
            {
                s.EndConnect(ar);
            }
            catch (SocketException e) /// winso
            {
                strError = string.Format("{0} - {1}", e.ErrorCode, e.ToString());
                if (m_Logger != null)
                    m_Logger.LogError(ToString(), MessageImportance.Highest, strError);
            }
            catch (ObjectDisposedException e2) // socket was closed
            {
                strError = e2.ToString();
                if (m_Logger != null)
                    m_Logger.LogError(ToString(), MessageImportance.Highest, strError);
            }

            this.Init(s, null);

            if (s.Connected == false)
            {
                OnConnected(false, strError);
                if (OnAsyncConnectFinished != null)
                    OnAsyncConnectFinished(this, false, strError);
                return;
            }

            if (SOCKStrans.IsFilterActive == true)
            {
                SOCKStrans.Start();
                return;
            }

            NegotiationsFinishedFireConnected(true, "");
        }


        internal void NegotiationsFinishedFireConnected(bool bSuccess, string strErrors)
        {
            if (bSuccess == false)
            {
                try
                {
                    Client.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                }
                catch (Exception)
                {
                }
            }

            OnConnected(bSuccess, strErrors);
            if (OnAsyncConnectFinished != null)
                OnAsyncConnectFinished(this, bSuccess, "");

            if ((m_bStartReadOnConnect) && (this.Connected == true))
                DoAsyncRead();
        }

        protected virtual void OnConnected(bool bSuccess, string strErrors)
        {
        }


        protected void FireReceiveHandler(byte[] bData, int nLen)
        {
            FireReceiveHandler(new SocketEventArgs(bData, nLen));
        }

        protected void FireReceiveHandler(SocketEventArgs args)
        {
            if (ReceiveHandler != null)
            {
                ReceiveHandler(this, args);
            }
            if (ReceiveHandlerBytes != null)
            {
                ReceiveHandlerBytes(this, args.m_data, args.Length);
            }
        }

        protected void FireDisconnectHandler(System.EventArgs args)
        {
            if (DisconnectHandler != null)
                DisconnectHandler(this, args);
        }

        System.Net.Sockets.SocketFlags m_SendFlags = SocketFlags.None;
        public System.Net.Sockets.SocketFlags SendFlags
        {
            get
            {
                return m_SendFlags;
            }
            set
            {
                m_SendFlags = value; /// may specify don't route, et
            }
        }

        public static int DSCPDefault = 0;
        public static int DSCPExpeditedForwarding = 46;
        public static int DSCPAssuredForwardingSIP = 32;  // class 3 med drop

        public void SetTypeOfServiceDSCP(int nDSCP)
        {
            try
            {
                int nValue = (nDSCP & 0x3F) << 2;
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.TypeOfService, nValue);
            }
            catch (Exception ex)
            {
                if (m_Logger != null)
                    m_Logger.LogError(ToString(), MessageImportance.Highest, string.Format("Exception in SetTypeOfServiceDSCP(): {0}", ex));
            }
        }

        public void SetTypeOfService(int nValue)
        {
            try
            {
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.TypeOfService, nValue);
            }
            catch (Exception ex)
            {
                if (m_Logger != null)
                    m_Logger.LogError(ToString(), MessageImportance.Highest, string.Format("Exception in SetTypeOfService(): {0}", ex));
            }
        }

        public virtual int Send(string strLine)
        {
            if (this.m_bSocketClientDisposed == true)
                return -1;

            byte[] sendBytes = System.Text.Encoding.UTF8.GetBytes(strLine);
            int nRet = 0;
            nRet = Send(sendBytes);
            return nRet;
        }

        public virtual int Send(byte[] bData)
        {
            if (this.m_bSocketClientDisposed == true)
                return -1;

            if (bData == null)
                return -1;

            return Send(bData, bData.Length, true);
        }

        public virtual int Send(byte[] bData, int nLength)
        {
            return Send(bData, nLength, true);
        }

        public virtual int Send(byte[] bData, int nLength, bool bTransform)
        {
            if (this.m_bSocketClientDisposed == true)
                return -1;

            int nRet = -1;
            lock (SyncRoot)
            {
                if (Client != null && Client.Connected)
                {
                    try
                    {
                        if (bTransform == true)
                            bData = TransformSendData(bData);
                        if ((bData == null) || (bData.Length <= 0))
                            return 0;

                        if (SslStream != null)
                            SslStream.Write(bData, 0, bData.Length);
                        else
                            NetworkStream.Write(bData, 0, bData.Length);
                        nRet = bData.Length;
                    }
                    catch (System.Net.Sockets.SocketException ex)
                    {
                        if (m_Logger != null)
                            m_Logger.LogError(ToString(), MessageImportance.Highest, ex.ToString());

                        OnDisconnect("Send failed");
                        throw new Exception("Send Failed", ex);
                    }
                    catch (Exception ex2)
                    {
                        if (m_Logger != null)
                            m_Logger.LogError(ToString(), MessageImportance.Highest, ex2.ToString());

                        OnDisconnect("Send failed");
                        throw new Exception("Send Failed", ex2);
                    }
                }
                else
                {
                    OnDisconnect("Client not connected");
                }
            }
            return nRet;
        }

     
        public virtual void OnDisconnect(string strReason)
        {
            lock (SyncRoot)
            {
                try
                {
                    if (Client != null)
                        Client.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                }
                catch (SocketException e) /// winso
                {
                    string strError = string.Format("{0} - {1}", e.ErrorCode, e.ToString());
                    if (m_Logger != null)
                        m_Logger.LogError(ToString(), MessageImportance.Highest, strError);
                }
                catch (ObjectDisposedException e2) // socket was closed
                {
                    string strError = e2.ToString();
                    if (m_Logger != null)
                        m_Logger.LogError(ToString(), MessageImportance.Highest, strError);
                }
                catch (System.Exception)
                { }

                FinalClose();

                FireDisconnectHandler(new System.EventArgs());

            }
        }

        public object SyncRoot = new object();
        bool UserInitiatedDisconnect = false;
        public virtual bool Disconnect()
        {
            UserInitiatedDisconnect = true;
            lock (SyncRoot)  // don't want this to be called by multiple people at the same time
            {
                if (Client == null)
                    return true;

                if (Client.Connected == false)
                {
                    return true;
                }

                try
                {
                    Client.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                }
                catch (SocketException e) /// winso
                {
                    string strError = string.Format("{0} - {1}", e.ErrorCode, e.ToString());
                    if (m_Logger != null)
                        m_Logger.LogError(ToString(), MessageImportance.Highest, strError);
                }
                catch (ObjectDisposedException e2) // socket was closed
                {
                    string strError = e2.ToString();
                    if (m_Logger != null)
                        m_Logger.LogError(ToString(), MessageImportance.Highest, strError);
                }
                catch (Exception ex)
                {
                    string strError = ex.ToString();
                    if (m_Logger != null)
                        m_Logger.LogError(ToString(), MessageImportance.Highest, strError);
                }

            }
            return true;
        }

        private void FinalClose()
        {
            try
            {
                if (Client != null)
                    Client.Close();

                if (SslStream != null)
                    SslStream.Close();

                if (NetworkStream != null)
                    NetworkStream.Close();
            }
            catch (SocketException e)
            {
                if (m_Logger != null)
                    m_Logger.LogError(ToString(), MessageImportance.Highest, e.ToString());
                return;
            }
            catch (Exception)
            {
            }
            finally
            {
                NetworkStream = null;
                SslStream = null;
            }
        }

        private byte[] m_bData = new byte[8192];
        public void DoAsyncRead()
        {
            lock (SyncRoot)  // don't want this to be called by multiple people at the same time
            {
                if (Client != null && Client.Connected)
                {

                    try
                    {
                        byte[] bData = null;
                        if (m_BufferPool != null)
                            bData = m_BufferPool.Checkout();
                        else
                            bData = new byte[4096];

                        if (SslStream != null)
                        {
                            IAsyncResult result = SslStream.BeginRead(bData, 0, bData.Length - 1, new AsyncCallback(OnRecvDataAll), bData);
                            if (result.CompletedSynchronously == true)
                                System.Threading.Thread.Sleep(0);
                        }
                        else
                        {
                            IAsyncResult result = NetworkStream.BeginRead(bData, 0, bData.Length - 1, new AsyncCallback(OnRecvDataAll), bData);
                            if (result.CompletedSynchronously == true)
                                System.Threading.Thread.Sleep(0);
                        }
                        //System.Net.Sockets.NetworkStream networkStream = GetStream();
                        //networkStream.BeginRead(m_bData, 0, m_bData.Length -1, m_AsyncRecv, networkStream);
                    }
                    catch (System.Net.Sockets.SocketException sock)
                    {
                        if (m_Logger != null)
                            m_Logger.LogError(ToString(), MessageImportance.Highest, sock.ToString());
                        System.Threading.Thread.Sleep(0);
                        OnDisconnect(sock.ToString());
                    }
                    catch (System.IO.IOException e)
                    {
                        if (m_Logger != null)
                            m_Logger.LogError(ToString(), MessageImportance.Highest, e.ToString());
                        System.Threading.Thread.Sleep(0);
                        OnDisconnect(e.ToString());
                    }
                    catch (System.ObjectDisposedException e2)
                    {
                        if (m_Logger != null)
                            m_Logger.LogError(ToString(), MessageImportance.Highest, e2.ToString());
                        OnDisconnect(e2.ToString());
                    }
                    catch (System.InvalidOperationException e3)
                    {
                        if (m_Logger != null)
                            m_Logger.LogError(ToString(), MessageImportance.Highest, e3.ToString());
                        OnDisconnect(e3.ToString());
                    }
                    catch (Exception ex)
                    {
                        if (m_Logger != null)
                            m_Logger.LogError(ToString(), MessageImportance.Highest, ex.ToString());
                        OnDisconnect(ex.ToString());
                    }
                }
            }
        }


        /// <summary>
        /// Function is called by our read threads whenever data is received.  This function calls the
        /// OnRecvData function, which is the one that should be overriden by the user.
        /// </summary>
        /// <param name="ar"></param>
        private void OnRecvDataAll(IAsyncResult ar)
        {
            if (Client == null)
            {
                return;
            }

            if (ar == null)
            {
                throw new Exception("IAsyncResult was null");
            }

            byte[] bData = (byte[])ar.AsyncState;

            int nLen = 0;
            try
            {
                if (SslStream != null)
                    nLen = SslStream.EndRead(ar);
                else
                    nLen = NetworkStream.EndRead(ar);
                //nLen = Client.EndReceive(ar);
                // //System.Net.Sockets.NetworkStream networkStream  = ar.AsyncState as System.Net.Sockets.NetworkStream;
                // //nLen = networkStream.EndRead(ar);
            }
            catch (SocketException esock)
            {
                if (m_Logger != null)
                    m_Logger.LogError(ToString(), MessageImportance.Highest, esock.ToString());

                OnDisconnect(esock.ToString());

                /// Check in our buffer, prevent pinning
                if (m_BufferPool != null)
                    m_BufferPool.Checkin(bData);

                return;
            }
            catch (System.IO.IOException e) /// socket was closed
            {
                if (m_Logger != null)
                    m_Logger.LogError(ToString(), MessageImportance.Highest, e.ToString());
                FireDisconnectHandler(new System.EventArgs());
                //OnDisconnect(e.ToString());  // don't call this here, socket is already closed

                /// Check in our buffer, prevent pinning
                if (m_BufferPool != null)
                    m_BufferPool.Checkin(bData);

                return;
            }
            catch (System.ObjectDisposedException e2) // network stream closed
            {
                if (m_Logger != null)
                    m_Logger.LogError(ToString(), MessageImportance.Highest, e2.ToString());
                FireDisconnectHandler(new System.EventArgs());
                //OnDisconnect(e.ToString());  // don't call this here, socket is already closed

                /// Check in our buffer, prevent pinning
                if (m_BufferPool != null)
                    m_BufferPool.Checkin(bData);

                return;
            }
            catch (System.Exception e3)
            {
                if (m_Logger != null)
                    m_Logger.LogError(ToString(), MessageImportance.Highest, e3.ToString());
                OnDisconnect(e3.ToString());

                /// Check in our buffer, prevent pinning
                if (m_BufferPool != null)
                    m_BufferPool.Checkin(bData);

                return;
            }
            HandleReceiveData(bData, nLen);
        }

        void HandleReceiveData(byte [] bData, int nLen)
        {

            if (nLen == 0)
            {
                if (UserInitiatedDisconnect == false)
                    OnDisconnect("Graceful Disconnect");
                return;
            }

            if (bData == null)
            {
                throw new Exception("bData is null in SocketClient.OnRecvDataAll");
            }

            try
            {
                byte[] bPassIn = new byte[nLen];
                Array.Copy(bData, 0, bPassIn, 0, nLen);

                /// Check in our buffer, prevent pinning
                if (m_BufferPool != null)
                    m_BufferPool.Checkin(bData);

                OnRecvData(bPassIn, nLen);
            }
            catch (System.NullReferenceException exnull)
            {
                throw new Exception("Something is null here... not sure what", exnull);
            }

        }

        protected virtual void OnRecvData(byte[] bData, int nLen)
        {
            SocketEventArgs args = new SocketEventArgs(bData, nLen);

            FireReceiveHandler(bData, nLen);


            List<byte[]> ReturnList = TransformReceiveData(bData);
            foreach (byte[] bNextArray in ReturnList)
            {
                if ((bNextArray != null) && (bNextArray.Length > 0))
                    OnMessage(bNextArray);
            }

            DoAsyncRead(); // go read some more
        }

        public event SocketReceiveHandler OnReceiveMessage = null;

        // An individual message (as defined by the transform filter list), has been received and extracted from the array received
        protected virtual void OnMessage(byte[] bMessage)
        {
            if (OnReceiveMessage != null)
                OnReceiveMessage(this, bMessage, bMessage.Length);

        }


        void LogMessage(MessageImportance importance, string strEventName, string strMessage)
        {
            if (m_Logger != null)
            {
                m_Logger.LogMessage(OurGuid, importance, strMessage);
            }
            else
            {
#if !MONO
                System.Diagnostics.Trace.WriteLine(strMessage, strEventName);
#endif
            }
        }

        void LogWarning(MessageImportance importance, string strEventName, string strMessage)
        {
            if (m_Logger != null)
            {
                m_Logger.LogWarning(OurGuid, importance, strMessage);
            }
            else
            {
#if !MONO
                System.Diagnostics.Trace.WriteLine(strMessage, strEventName);
#endif
            }
        }

        void LogError(MessageImportance importance, string strEventName, string strMessage)
        {
            if (m_Logger != null)
            {
                m_Logger.LogError(OurGuid, importance, strMessage);
            }
            else
            {
#if !MONO
                System.Diagnostics.Trace.WriteLine(strMessage, strEventName);
#endif
            }
        }
    }
}
