/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Net;

using SocketServer;
using System.Net.Sockets;
using System.Collections.Generic;

#if !WINDOWS_PHONE /// Windows phone doesn't support listening for connections, so we can't user this for file transfer.  Have to use a proxy or inband-byte streams for file transfer

namespace System.Net.XMPP
{
    public enum ServerSessionState
    {
        None,
        WaitingForMethodSelections,
        WaitingForSocksRequestMessage,
        JustExisting,
        Forwarding
    }
    public class SOCKSServerSession : SocketClient
    {
        public SOCKSServerSession(Socket s, SOCKS5Listener parent)
        {
            this.Client = s;
            Parent = parent;
            ConnectClient = new SocketClient();
            ConnectClient.DisconnectHandler += new SocketEventHandler(ConnectClient_DisconnectHandler);
            ConnectClient.ReceiveHandlerBytes += new SocketReceiveHandler(ConnectClient_ReceiveHandlerBytes);
        }

        void ConnectClient_ReceiveHandlerBytes(SocketClient client, byte[] bData, int nLength)
        {
            if (this.Connected == true)
            {
                Send(bData, nLength);
            }
        }

        void ConnectClient_DisconnectHandler(object sender, EventArgs e)
        {
            if (this.Connected == true)
            {
                this.Disconnect();
            }
        }

        public override void OnDisconnect(string strReason)
        {
            base.OnDisconnect(strReason);
            try
            {
                if ((this.ConnectClient != null) && (this.ConnectClient.Connected == true))
                {
                    this.ConnectClient.Disconnect();
                    this.ConnectClient = null;
                }
            }
            catch (Exception)
            {

            }
            finally
            {
                this.ConnectClient = null;
            }
        }




        SOCKS5Listener Parent = null;
        SocketClient ConnectClient = null;


        public void Start()
        {
            ServerSessionState = ServerSessionState.WaitingForMethodSelections;
            DoAsyncRead();
        }

        ByteBuffer ReceiveBuffer = new ByteBuffer();

        private ServerSessionState m_eServerSessionState = ServerSessionState.None;

        public ServerSessionState ServerSessionState
        {
            get { return m_eServerSessionState; }
            set { m_eServerSessionState = value; }
        }

        protected override void OnRecvData(byte[] bData, int nLen)
        {
            if (nLen == 0)
            {
                OnDisconnect("Normal closing");
                return;
            }

            //Console.WriteLine(string.Format("<-- {0}", ByteSize.ByteUtils.HexStringFromByte(bData, true)));

            if (ServerSessionState == ServerSessionState.Forwarding)
            {
                try
                {
                    ConnectClient.Send(bData, nLen);

                    DoAsyncRead();
                }
                catch (Exception)
                { }

                return;
            }

            ReceiveBuffer.AppendData(bData, 0, nLen);

            if (ServerSessionState == ServerSessionState.JustExisting)
            {
                DoAsyncRead(); // go read some more
                return;
            }

            byte[] bCurData = ReceiveBuffer.PeekAllSamples();

            if (bCurData.Length <= 0)
            {
                DoAsyncRead();
                return;
            }

            if (ServerSessionState == ServerSessionState.WaitingForMethodSelections)
            {
                int nVersion = bCurData[0];

                if (nVersion == 5)
                {
                    MethodSelectionsMessage msg = new MethodSelectionsMessage();
                    int nRead = msg.ReadFromBytes(bData, 0);
                    if (nRead > 0)
                    {
                        ReceiveBuffer.GetNSamples(nRead);

                        /// Determine which method we support
                        /// 
                        bool bCanDoNoAuth = false;
                        foreach (SockMethod method in msg.Methods)
                        {
                            if (method == SockMethod.NoAuthenticationRequired)
                            {
                                bCanDoNoAuth = true;
                                break;
                            }
                        }

                        if (bCanDoNoAuth == false)
                        {
                            MethodSelectedMessage retmsg = new MethodSelectedMessage();
                            retmsg.Version = 5;
                            retmsg.SockMethod = SockMethod.NoAcceptableMethods;
                            this.Send(retmsg.GetBytes());
                            this.Disconnect();
                            return;
                        }
                        else
                        {
                            ServerSessionState = ServerSessionState.WaitingForSocksRequestMessage;
                            MethodSelectedMessage retmsg = new MethodSelectedMessage();
                            retmsg.Version = msg.Version;
                            retmsg.SockMethod = SockMethod.NoAuthenticationRequired;
                            this.Send(retmsg.GetBytes());
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Version {0} not supported", nVersion);
                    MethodSelectedMessage retmsg = new MethodSelectedMessage();
                    retmsg.Version = 5;
                    retmsg.SockMethod = SockMethod.NoAcceptableMethods;
                    this.Send(retmsg.GetBytes());
                    this.Disconnect();
                    return;
                }
            }
            else if (ServerSessionState == ServerSessionState.WaitingForSocksRequestMessage)
            {
                /// Read in our SocksRequestMessage
                /// 
                SocksRequestMessage reqmsg = new SocksRequestMessage();
                int nRead = reqmsg.ReadFromBytes(bData, 0);
                if (nRead > 0)
                {
                    ReceiveBuffer.GetNSamples(nRead);

                    if (reqmsg.Version != 0x05)
                        Console.WriteLine("No version 5, client wants version: {0}", reqmsg.Version);


                    //Parent.HandleRequest(reqmsg, this);
                    if (reqmsg.SOCKSCommand == SOCKSCommand.Connect)
                    {
                        /// See what the man wants.  It appears that mozilla immediately starts sending data if we return success here, so let's do it
                        /// 
                        this.ServerSessionState = ServerSessionState.Forwarding;
                        /// Let's try to connect
                        /// 

                        /// Don't connect to a remote host in this version, just check the hash here, if it matches, send back success
                        bool bHashMatched = false;

                        //if (reqmsg.AddressType == AddressType.DomainName)
                        //    bConnected = ConnectClient.Connect(reqmsg.DestinationDomain, reqmsg.DestinationPort, true);
                        //else
                        //    bConnected = ConnectClient.Connect(reqmsg.DestinationAddress.ToString(), reqmsg.DestinationPort, true);

                        SocksReplyMessage reply = new SocksReplyMessage();

                        if (bHashMatched == false)
                        {
                            reply.SOCKSReply = SOCKSReply.ConnectionRefused;
                        }
                        else
                        {
                            reply.SOCKSReply = SOCKSReply.Succeeded;
                        }

                        Send(reply.GetBytes());
                    }
                    else
                    {
                        SocksReplyMessage reply = new SocksReplyMessage();
                        reply.SOCKSReply = SOCKSReply.CommandNotSupported;
                        reply.AddressType = AddressType.IPV4;
                        Send(reply.GetBytes());
                    }
                }
            }


            DoAsyncRead(); // go read some more
        }


        public override int Send(byte[] bData)
        {
            int nRet = 0;
            try
            {
                //Console.WriteLine(string.Format("--> {0}", ByteSize.ByteUtils.HexStringFromByte(bData, true)));
                nRet = base.Send(bData);
            }
            catch (Exception)
            {
                this.Disconnect();
            }
            return nRet;
        }

        public override int Send(byte[] bData, int nLength)
        {
            int nRet = 0;
            try
            {
                //Console.WriteLine(string.Format("--> {0}", ByteSize.ByteUtils.HexStringFromByte(bData, true)));
                nRet = base.Send(bData, nLength);
            }
            catch (Exception)
            {
                this.Disconnect();
            }
            return nRet;
        }

    }



    public class SOCKS5Listener
    {
        public SOCKS5Listener()
        {
            Listener.OnNewConnection += new SocketListener.DelegateNewConnectedSocket(Listener_OnNewConnection);
        }


        private int m_nPort = 8080;

        public int Port
        {
            get { return m_nPort; }
            set { m_nPort = value; }
        }

        SocketListener Listener = new SocketListener();

        public void Start()
        {
            Console.WriteLine("SOCKS server listening on port {0}", Port);
            Listener.EnableAccept(Port);
        }

        public List<SOCKSServerSession> Sessions = new List<SOCKSServerSession>();
        public object SessionLock = new object();


        void Listener_OnNewConnection(System.Net.Sockets.Socket s)
        {
            Console.WriteLine("Session Connecting: {0}", s);
            SOCKSServerSession session = new SOCKSServerSession(s, this);
            lock (SessionLock)
            {
                Sessions.Add(session);
            }

            session.DisconnectHandler += new SocketClient.SocketEventHandler(session_DisconnectHandler);
            session.Start();
        }

        void session_DisconnectHandler(object sender, EventArgs e)
        {
            Console.WriteLine("Session Disconnecting: {0}", sender);
            SOCKSServerSession session = sender as SOCKSServerSession;
            lock (SessionLock)
            {
                if (Sessions.Contains(session) == true)
                    Sessions.Remove(session);
            }
        }



    }

}

#endif
