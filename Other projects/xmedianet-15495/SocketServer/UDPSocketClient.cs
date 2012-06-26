/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;

using System.Collections.Generic;

namespace SocketServer
{
    public class BufferSocketRef
    {
        public BufferSocketRef(UDPSocketClient client)
        {
            Client = client;

            /// See if we are using our pinned-memory protection, if we are use from the pool, if not, just new it
            if (UDPSocketClient.m_BufferPool != null)
                bRecv = UDPSocketClient.m_BufferPool.Checkout();
            else
                bRecv = new byte[UDPSocketClient.BufferSize];
        }

        public UDPSocketClient Client;
        public byte[] bRecv;

        public void CheckInCopy(int nLen)
        {
            /// Copy our buffer to a non-pinned byte array, and release our pinned array back to the pool
            if (UDPSocketClient.m_BufferPool != null)
            {
                byte[] bPassIn = new byte[nLen];
                if (nLen > 0)
                    Array.Copy(bRecv, 0, bPassIn, 0, nLen);

                UDPSocketClient.m_BufferPool.Checkin(bRecv);
                bRecv = bPassIn;
            }
        }

    }

    /// <summary>
    /// Sends and receives datagrams
    /// </summary>
    public class UDPSocketClient
    {
        public UDPSocketClient(IPEndPoint ep) // : base(ep)
        {
            m_ipEp = ep;
            Init(ep);
        }

        void Init(IPEndPoint ep)
        {
            if (ep.AddressFamily == AddressFamily.InterNetworkV6)
                s = new System.Net.Sockets.Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.IP);
            else
                s = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            if (ep.AddressFamily == AddressFamily.InterNetworkV6)
            {
                IPEndPoint sender = new IPEndPoint(IPAddress.IPv6Any, 0);
                m_tempRemoteEP = (System.Net.IPEndPoint)sender;
            }
            else
            {
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                m_tempRemoteEP = (System.Net.IPEndPoint)sender;
            }
            asyncb = new AsyncCallback(OnReceiveUDP);
        }

        public DateTimePrecise DateTimePrecise = new DateTimePrecise();

        /// <summary>
        /// Set to log to another source
        /// </summary>
        public ILogInterface m_Logger = null;

        private string m_strOurGuid = "UDPClient";
        public string OurGuid
        {
            get { return m_strOurGuid; }
            set { m_strOurGuid = value; }
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

        public readonly IPEndPoint m_ipEp;         /// our endpoint
        public System.Net.Sockets.Socket s = null;

        public delegate void DelegateReceivePacket(byte[] bData, int nLength, IPEndPoint epfrom, IPEndPoint epthis, DateTime dtReceived);
        public event DelegateReceivePacket OnReceivePacket = null;
        public event DelegateReceivePacket OnReceiveMessage = null;

        private static int m_nBufferSize = 16 * 1024;
        public static int BufferSize
        {
            get { return UDPSocketClient.m_nBufferSize; }
            set { UDPSocketClient.m_nBufferSize = value; }
        }

        protected IPEndPoint m_tempRemoteEP = null;  /// temp endpoint for receivefrom
        protected System.AsyncCallback asyncb;
        protected bool m_bReceive = true;

        public object SyncRoot = new object();

        //const uint SIO_UDP_CONNRESET = 0x9800000C;
        // 0x9800000C == 2440136844 (uint) == -1744830452 (int) == 0x9800000C
        const int SIO_UDP_CONNRESET = -1744830452;

        public static IBufferPool m_BufferPool = null;

        bool m_bIsBound = false;
        public bool Bind()
        {
            if (m_bIsBound == true)
                return true;

            lock (SyncRoot)
            {
                m_bReceive = true;
                System.Net.EndPoint epBind = (EndPoint)m_ipEp;
                try
                {
                    s.Bind(epBind);
                }
                catch (SocketException e3) /// winso
                {
                    LogError(MessageImportance.High, "EXCEPTION", string.Format("{0} - {1}", e3.ErrorCode, e3.ToString()));
                    return false;
                }
                catch (ObjectDisposedException e4) // socket was closed
                {
                    LogError(MessageImportance.High, "EXCEPTION", e4.ToString());
                    return false;
                }

                m_bIsBound = true;
            }

            return true;
        }

        public bool StartReceiving()
        {
            return StartReceiving(true);
        }

        public bool StartReceiving(bool bBind)
        {
#if !MONO
            /// See http://blog.devstone.com/aaron/archive/2005/02/20/460.aspx
            /// This will stop winsock errors when receiving an ICMP packet 
            /// "Destination unreachable"
            byte[] inValue = new byte[] { 0, 0, 0, 0 };     // == false
            byte[] outValue = new byte[] { 0, 0, 0, 0 };    // initialize to 0
            s.IOControl(SIO_UDP_CONNRESET, inValue, outValue);
#endif

            if ((bBind == true) && (Bind() == false))
                return false;

            lock (SyncRoot)
            {
                m_bReceive = true;
                s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 128000);
                /// have 8 pending receives in the queue at all times

                DoReceive();
#if !MONO
                if (System.Environment.OSVersion.Version.Major >= 6)
                {
                    DoReceive();
                    DoReceive();
                }
#endif
            }
            return true;
        }

        public void StopReceiving()
        {
            lock (SyncRoot)
            {
                if (m_bReceive == false)
                {
                    return;
                }

                LogMessage(MessageImportance.Lowest, this.OurGuid, string.Format("Called StopReceiving for {0}", s.LocalEndPoint));
                m_bReceive = false;
                // Shutdown not recommended on UDP
                //s.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                s.Close();
            }
        }

        protected void DoReceive()
        {
            lock (SyncRoot)
            {
                if (m_bReceive == false)
                {
                    return;
                }


                System.Net.EndPoint ep = (System.Net.EndPoint)m_tempRemoteEP;
                try
                {
                    if (m_Logger != null)
                        LogMessage(MessageImportance.Lowest, this.OurGuid, string.Format("Called DoReceive for {0}", s.LocalEndPoint));
                    BufferSocketRef objRef = new BufferSocketRef(this);
                    s.BeginReceiveFrom(objRef.bRecv, 0, m_nBufferSize, System.Net.Sockets.SocketFlags.None, ref ep, asyncb, objRef);
                }
                catch (SocketException e3) /// winso
                {
                    LogError(MessageImportance.High, "SocketEXCEPTION", string.Format("{0} - {1}", e3.ErrorCode, e3.ToString()));
                    return;
                }
                catch (ObjectDisposedException e4) // socket was closed
                {
                    LogError(MessageImportance.High, "ObjectDisposedEXCEPTION", e4.ToString());
                    return;
                }
                catch (Exception e5)
                {
                    LogError(MessageImportance.High, "EXCEPTION", e5.ToString());
                    return;
                }
            }
            return;
        }


        protected void OnReceiveUDP(IAsyncResult ar)
        {
            DateTime dtReceive = DateTimePrecise.Now;
            BufferSocketRef objRef = ar.AsyncState as BufferSocketRef;
            System.Net.EndPoint ep = (System.Net.EndPoint)m_tempRemoteEP;
            int nRecv = 0;

            try
            {
                nRecv = s.EndReceiveFrom(ar, ref ep);
                objRef.CheckInCopy(nRecv);
            }
            catch (SocketException e3) /// winso
            {
                objRef.CheckInCopy(nRecv);
                LogError(MessageImportance.High, "EXCEPTION", e3.ToString());

                /// Get 10054 if the other end is not listening (ICMP returned)... fixed above with IOControl
                if (e3.ErrorCode != 10054)
                {
                }
                return;

            }
            catch (ObjectDisposedException e4) // socket was closed
            {
                objRef.CheckInCopy(nRecv);
                this.LogWarning(MessageImportance.Low, "EXCEPTION", e4.ToString());
                return;
            }
            catch (Exception e5)
            {
                objRef.CheckInCopy(nRecv);
                LogError(MessageImportance.High, "EXCEPTION", e5.ToString());
                return;
            }

            System.Net.IPEndPoint ipep = (System.Net.IPEndPoint)ep;
            OnRecv(objRef.bRecv, nRecv, ipep, dtReceive);
        }

        private void OnRecv(byte[] bRecv, int nRecv, IPEndPoint ipep, DateTime dtReceive)
        {
            if (nRecv > 0)
            {
                if (OnReceivePacket != null)
                {
                    OnReceivePacket(bRecv, nRecv, ipep, this.m_ipEp, dtReceive);
                }
            }

            // Pass the data through our receive transform
            if (OnReceiveMessage != null)
            {
                byte[] bMessage = new byte[nRecv];
                Array.Copy(bRecv, 0, bMessage, 0, nRecv);

                List<byte[]> ReturnList = TransformReceiveData(bMessage);
                foreach (byte[] bNextArray in ReturnList)
                    OnReceiveMessage(bNextArray, bNextArray.Length, ipep, this.m_ipEp, dtReceive);
            }

            if (m_bReceive == true)
                DoReceive();
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

        public int DiffSvcCP = 0;
        /// Can't actaully set the type of service using SetSocketOption, the function returns success but doesn't do anything (you could in XP, but you had to set a registry setting to allow it)
        /// The preferred method now is the QOS API's, which are too complicated and way more than we need.  We'll use raw sockets
        /// if needed.
        public void SetTypeOfServiceDSCP(int nDSCP)
        {
            DiffSvcCP = nDSCP;
        }

        public int SendUDP(byte[] bData, int nLength, System.Net.EndPoint ep)
        {
            lock (SyncRoot)
            {
                if (m_bReceive == false)
                {
                    this.LogError(MessageImportance.Highest, "error", string.Format("Can't call SendUDP, socket not valid or closed"));
                    return 0;
                }

                if (this.m_Logger != null)
                    LogMessage(MessageImportance.Lowest, this.OurGuid, string.Format("SendUDP from {0} to {1}", s.LocalEndPoint, ep));
                return s.SendTo(bData, nLength, SendFlags, ep);
            }
        }


        /// <summary>
        /// Sends a message, passing it through the transform list before sending
        /// </summary>
        /// <param name="bData"></param>
        /// <param name="nLength"></param>
        /// <param name="ep"></param>
        /// <returns></returns>
        public virtual int SendMessage(byte[] bData, int nLength, System.Net.EndPoint ep)
        {
            lock (SyncRoot)
            {
                if (m_bReceive == false)
                {
                    this.LogError(MessageImportance.Highest, "error", string.Format("Can't call SendUDP, socket not valid or closed"));
                    return 0;
                }

                try
                {
                    bData = TransformSendData(bData);
                }
                catch (System.Net.Sockets.SocketException ex)
                {
                    this.LogError(MessageImportance.Highest, "exception", ex.ToString());
                }


                LogMessage(MessageImportance.Lowest, this.OurGuid, string.Format("SendUDP from {0} to {1}", s.LocalEndPoint, ep));
                return s.SendTo(bData, nLength, SendFlags, ep);

            }
        }

        #region ReStarting DoReceive()
        //
        // Only want to start the DoReceive().  The main purpose is to keep ListenOn the local 
        // port while recover the error caused by operation of sending to a unreachable destination(usually un-listened port).
        // Dead computer does not cause error.
        //
        public bool RestartReceivingData()
        {
            for (int i = 0; i < 8; i++)
                DoReceive();
            return true;
        }
        #endregion



        public List<IMessageFilter> TransformList = new List<IMessageFilter>();
        public byte[] TransformSendData(byte[] bData)
        {
            if (this.m_bReceive == false)
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
                    bRet = nextfilter.TransformSendData(bData);
                    if ((bRet == null) || (bRet.Length <= 0))
                        return new byte[] { };
                }
            }
            catch (Exception ex)
            {
                this.LogError(MessageImportance.Highest, "TransformSendData", string.Format("Exception transforming data: {0}", ex));
            }

            return bRet;
        }

        // Parsed incoming data into a list of packets
        public List<byte[]> TransformReceiveData(byte[] bData)
        {
            List<byte[]> ReturnList = new List<byte[]>();
            ReturnList.Add(bData);

            if (this.m_bReceive == false)
                return ReturnList;

            if (bData == null)
                return ReturnList;

            try
            {
                foreach (IMessageFilter nextfilter in TransformList)
                {
                    if (nextfilter == null)
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
                this.LogError(MessageImportance.Highest, "TransformReceiveData", string.Format("Exception transforming data: {0}", ex));
            }

            return ReturnList;
        }

    }


}
