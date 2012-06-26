/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using System.Text.RegularExpressions;

namespace SocketServer
{
    public delegate void DelegateConnectFinished(SocketClient client, string strErrors);

    public class ConnectMgr
    {
        protected SocketCreator m_socketcreator = null;
        protected int m_nPort = 0;
        public string m_SocketError;

        public ILogInterface m_Logger = null;
        public ILogInterface Logger
        {
            set
            {
                m_Logger = value;
                m_AcceptorManager.Logger = value;
            }
        }

        public string OurGuid = "ConnectMgr";

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

        public ConnectMgr() // default construct assumes is for Listening
        {
            Init();
            m_socketcreator = new SocketCreator();
        }

        public ConnectMgr(SocketCreator screator) // out bound interface
        {
            Init();
            m_socketcreator = screator;
        }

        protected void Init()
        {
            m_AsyncConnect = new AsyncCallback(OnClientConnected);
            m_nPort = 0;
            m_AcceptorManager = new AcceptorManager(this);
            AcceptHandler = null;
            ReceiveHandler = null;
            m_SocketError = "";
            m_socketcreator = null;
        }

        internal AcceptorManager m_AcceptorManager = null;
        public bool Listen(int nPort)
        {
            return m_AcceptorManager.EnableAccept(nPort, this.m_socketcreator);
        }

        public bool ListenIPV6(int nPort)
        {
            return m_AcceptorManager.EnableAcceptIPV6(nPort, this.m_socketcreator);
        }


        public bool Listen(IPEndPoint ep)
        {
            return m_AcceptorManager.EnableAccept(ep, this.m_socketcreator);
        }

        public bool Listen(int nPort, SocketCreator screator)
        {
            return m_AcceptorManager.EnableAccept(nPort, screator);
        }

        public bool ListenIPV6(int nPort, SocketCreator screator)
        {
            return m_AcceptorManager.EnableAcceptIPV6(nPort, screator);
        }


        public bool Listen(IPEndPoint ep, SocketCreator screator)
        {
            return m_AcceptorManager.EnableAccept(ep, screator);
        }

        public bool StopListen(int nPort)
        {
            return m_AcceptorManager.StopListen(nPort);
        }

        public UDPSocketClient CreateUDP()
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            return CreateUDP(ep);
        }

        /// <summary>
        /// Create a new UDP Listen socket and add it to our map
        /// </summary>
        /// <param name="nPort"></param>
        /// <returns></returns>
        public UDPSocketClient CreateUDP(int nPort)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, nPort);
            return CreateUDP(ep);
        }

        public UDPSocketClient CreateUDP(IPEndPoint ep)
        {
            return new UDPSocketClient(ep);
        }

        public UDPSocketClient CreateUDPIPV6()
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.IPv6Any, 0);
            return CreateUDP(ep);
        }

        public UDPSocketClient CreateUDPIPV6(int nPort)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.IPv6Any, nPort);
            return CreateUDP(ep);
        }

        public DelegateConnectFinished OnConnectFinished = null;
        protected void FireConnectFinished(SocketClient client, string strError)
        {
            if (OnConnectFinished != null)
                OnConnectFinished(client, strError);
        }

        public SocketClient CreateClientAsync(string ipaddr, int nport)
        {
            return CreateClientAsync(ipaddr, nport, false);
        }
        /// <summary>
        ///  Creates a tcp connection asyncronously.  The client muust call DoAsyncRead on connection completed
        /// </summary>
        /// <param name="ipaddr"></param>
        /// <param name="nport"></param>
        /// <returns></returns>
        public SocketClient CreateClientAsync(string ipaddr, int nport, bool bIPVersion6)
        {
            IPAddress hostadd = null;
            IPEndPoint EPhost = null;
            try
            {
                if (IsIPAddress(ipaddr) == true)
                {
                    EPhost = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(ipaddr), nport);
                }
                else
                {
                    if (IsIPV6Address(ipaddr) == true)
                    {
                        EPhost = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(ipaddr), nport);
                    }
                    else
                    {
                        hostadd = Resolve(ipaddr);
                        EPhost = new IPEndPoint(hostadd, nport);
                    }
                }
            }
            catch (ArgumentNullException e)
            {
                LogError(MessageImportance.Highest, "EXCEPTION", e.ToString());
                return null;
            }
            catch (SocketException e2)
            {
                LogError(MessageImportance.Highest, "CreateClientAsync", string.Format("Exception Parsing, Winsock Native: {0}, Socket: {1}\n{2}", e2.NativeErrorCode, e2.SocketErrorCode, e2));
                return null;
            }


            //Creates the Socket for sending data over TCP.
            Socket s = null;
            if (bIPVersion6 == false)
                s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            else
                s = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

            SocketClient client = this.m_socketcreator.CreateSocket(s, this);


            // Connects to the host using IPEndPoint.
            try
            {
                s.BeginConnect(EPhost, m_AsyncConnect, client);
            }
            catch (SocketException e2)
            {
                LogError(MessageImportance.Highest, "CreateClientAsync", string.Format("Exception in BeginConnect, Winsock Native: {0}, Socket: {1}\n{2}", e2.NativeErrorCode, e2.SocketErrorCode, e2));
                return null;
            }
            return client;
        }



        private AsyncCallback m_AsyncConnect = null;

        private void OnClientConnected(IAsyncResult ar)
        {
            SocketClient client = ar.AsyncState as SocketClient;

            string strError = "";
            try
            {
                client.socket.EndConnect(ar);
            }
            catch (SocketException e) /// winso
            {
                strError = string.Format("{0} - {1}", e.ErrorCode, e.ToString());
                LogError(MessageImportance.Highest, "OnClientConnected", string.Format("Exception calling EndConnect(), Winsock Native: {0}, Socket: {1}\n{2}", e.NativeErrorCode, e.SocketErrorCode, e));
            }
            catch (ObjectDisposedException e2) // socket was closed
            {
                strError = e2.ToString();
                LogError(MessageImportance.Highest, "EXCEPTION", strError);
            }


            if (client.socket.Connected == false)
            {
                this.FireConnectFinished(client, "Not Connected: " + strError);
                return;
            }

            FireConnectFinished(client, "");
        }

        public SocketClient CreateClient(string ipaddr, int nport, bool bbeginread)
        {
            return CreateClient(ipaddr, nport, bbeginread, false);
        }
        // The first two do DNS.
        // if bbeginread IS false it's caller's job to call DoAsyncRead()
        public SocketClient CreateClient(string ipaddr, int nport, bool bbeginread, bool bIPVersion6)
        {
            IPAddress hostadd = null;
            IPEndPoint EPhost = null;
            try
            {
                if (IsIPAddress(ipaddr))
                {
                    EPhost = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(ipaddr), nport);
                }
                else if (IsIPV6Address(ipaddr) == true)
                {
                    EPhost = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(ipaddr), nport);
                }
                else
                {
                    hostadd = Resolve(ipaddr);
                    EPhost = new IPEndPoint(hostadd, nport);
                }

            }
            catch (ArgumentNullException e)
            {

                LogError(MessageImportance.Highest, "EXCEPTION", e.ToString());
                return null;
            }
            catch (SocketException e2)
            {
                LogError(MessageImportance.Highest, "CreateClient", string.Format("Exception calling Resolve(), Winsock Native: {0}, Socket: {1}\n{2}", e2.NativeErrorCode, e2.SocketErrorCode, e2));
                return null;
            }

            return CreateClient(EPhost, bbeginread, m_socketcreator, bIPVersion6);
        }


        public static string strRegExp = @"^\s*\d{1,3}.\d{1,3}.\d{1,3}.\d{1,3}\s*$";
        public static Regex RegExIPV4 = new Regex(strRegExp);
        public static Regex RegExIPV6 = new Regex(@"^\s*[\d\:a-fA-F]+\s*$");

        public static bool IsIPAddress(string ipaddr)
        {
            if (ipaddr == null)
                return false;
            System.Text.RegularExpressions.Match MatchMan = null;
            MatchMan = RegExIPV4.Match(ipaddr);
            if (MatchMan.Success == false)
            {
                MatchMan = RegExIPV6.Match(ipaddr);
            }
            return MatchMan.Success;
        }

        public static ProtocolType GetIPAddressType(string ipaddr)
        {
            System.Text.RegularExpressions.Match MatchMan = null;
            MatchMan = RegExIPV4.Match(ipaddr);
            if (MatchMan.Success == true)
                return ProtocolType.IPv4;

            MatchMan = RegExIPV6.Match(ipaddr);
            if (MatchMan.Success == true)
                return ProtocolType.IPv6;

            return ProtocolType.Unknown;
        }

        ///  IPV6 has only hexadecimal numbers and colons
        public static string strRegExpV6 = @"^\s*[a-fA-F0-9\:]+\s*$";
        public static System.Text.RegularExpressions.Regex RegExFindV6 = new System.Text.RegularExpressions.Regex(strRegExpV6);
        public static bool IsIPV6Address(string ipaddr)
        {
            System.Text.RegularExpressions.Match MatchMan = null;
            MatchMan = RegExFindV6.Match(ipaddr);
            return MatchMan.Success;
        }

        public SocketClient CreateClient(string ipaddr, int nport, bool bbeginread, SocketCreator creator)
        {
            return CreateClient(ipaddr, nport, bbeginread, creator, false);
        }

        // if bbeginread IS false it's caller's job to call DoAsyncRead()
        public SocketClient CreateClient(string ipaddr, int nport, bool bbeginread, SocketCreator creator, bool bIsIPV6)
        {
            IPAddress hostadd = null;
            IPEndPoint EPhost = null;
            try
            {
                if (IsIPAddress(ipaddr))
                {
                    EPhost = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(ipaddr), nport);
                }
                else if (IsIPV6Address(ipaddr) == true)
                {
                    EPhost = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(ipaddr), nport);
                }
                else
                {
                    hostadd = Resolve(ipaddr);
                    EPhost = new IPEndPoint(hostadd, nport);
                }
            }
            catch (ArgumentNullException e)
            {
                LogError(MessageImportance.Highest, "EXCEPTION", e.ToString());
                return null;
            }
            catch (SocketException e2)
            {
                LogError(MessageImportance.Highest, "CreateClient", string.Format("Exception calling Resolve(), Winsock Native: {0}, Socket: {1}\n{2}", e2.NativeErrorCode, e2.SocketErrorCode, e2));
                return null;
            }

            return CreateClient(EPhost, bbeginread, creator, bIsIPV6);
        }


        public static IPEndPoint GetIPEndpoint(string strHost, int nPort)
        {
            IPAddress hostadd = null;
            IPEndPoint EPhost = null;
            try
            {
                if (IsIPAddress(strHost) == true)
                {
                    EPhost = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(strHost), nPort);
                }
                else
                {
                    hostadd = Resolve(strHost);
                    EPhost = new IPEndPoint(hostadd, nPort);
                }
            }
            catch (ArgumentNullException)
            {
                return null;
            }
            catch (SocketException)
            {
                return null;
            }
            catch (Exception )
            {
                return null;
            }

            return EPhost;
        }

        /// <summary>
        /// Resolves a name to an IP address first using our custom resolver (if non null),
        /// then using DNS.  DNS may throw exceptions
        /// </summary>
        /// <param name="strName"></param>
        /// <returns></returns>
        public static IPAddress Resolve(string strName)
        {
            IPAddress hostadd = null;

            if (hostadd == null)
            {
                hostadd = Dns.Resolve(strName).AddressList[0];
            }
            return hostadd;
        }

        public SocketClient CreateClient(IPEndPoint ep, bool bbeginread, SocketCreator creator)
        {
            return CreateClient(ep, bbeginread, creator, false);
        }

        public SocketClient CreateClient(IPEndPoint ep, bool bbeginread, SocketCreator creator, bool IsIPV6)
        {
            //Creates the Socket for sending data over TCP.
            Socket s = null;
            if (IsIPV6 == false)
                s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            else
                s = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

            // Connects to the host using IPEndPoint.
            try
            {
                s.Connect(ep);
            }
            catch (SocketException e)
            {
                LogError(MessageImportance.Highest, "CreateClient", string.Format("Exception calling Connect, Winsock Native: {0}, Socket: {1}\n{2}", e.NativeErrorCode, e.SocketErrorCode, e));
                return null;
            }

            if (!s.Connected)
            {
                LogError(MessageImportance.Highest, "Connection", "Failed to connect to " + ep.Address.ToString() + " port " + ep.Port);
                return null;
            }

            SocketClient client = creator.CreateSocket(s, this);
            if (bbeginread)
                client.DoAsyncRead();
            return client;
        }

        public SocketClient CreateClient(IPEndPoint localep, IPEndPoint remoteep, bool bbeginread, SocketCreator creator)
        {
            //Creates the Socket for sending data over TCP.
            Socket s = null;
            if (remoteep.Address.AddressFamily != AddressFamily.InterNetworkV6)
                s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            else
                s = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

            // Connects to the host using IPEndPoint.
            try
            {
                LogMessage(MessageImportance.Highest, "EXCEPTION", string.Format("Attempting to create TCP connection from {0} to {1}", localep, remoteep));
                s.Bind(localep);
                s.Connect(remoteep);
            }
            catch (SocketException e)
            {
                LogError(MessageImportance.Highest, "CreateClient", string.Format("Exception calling Bind or Connect, Winsock Native: {0}, Socket: {1}\n{2}", e.NativeErrorCode, e.SocketErrorCode, e));
                return null;
            }

            if (!s.Connected)
            {
                LogError(MessageImportance.Highest, "Connection", "Failed to connect to " + remoteep);
                return null;
            }

            SocketClient client = creator.CreateSocket(s, this);
            if (bbeginread)
                client.DoAsyncRead();
            return client;

        }


        public delegate void OnHandler(object sender, EventArgs e);

        public event OnHandler AcceptHandler;
        public void FireAcceptHandler(SocketClient client)
        {
            if (AcceptHandler != null)
            {
                AcceptHandler(client, new System.EventArgs());
            }
        }
        public event OnHandler ReceiveHandler;
        public void FireReceiveHandler(SocketClient client, System.EventArgs args)
        {
            if (ReceiveHandler != null)
            {
                ReceiveHandler(client, args);
            }
        }

        public event OnHandler DisconnectHandler;
        public void FireDisconnectHandler(SocketClient client)
        {
            if (DisconnectHandler != null)
            {
                DisconnectHandler(client, new System.EventArgs());
            }

        }


    }

}
