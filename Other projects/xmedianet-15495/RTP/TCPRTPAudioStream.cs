/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.Net.Sockets;
using System.Threading;

using SocketServer;

namespace RTP
{
    /// <summary>
    ///  A class for sending an receiving RTPPackets over TCP
    /// </summary>
    public class TCPRTPSocketClient : SocketServer.SocketClient
    {

#if !WINDOWS_PHONE
        public TCPRTPSocketClient(System.Net.Sockets.Socket s, SocketServer.ConnectMgr cmgr)
            : base(s, cmgr)
        {

        }
#else

        public TCPRTPSocketClient(ILogInterface logmgr, string strGuid) :
            base(logmgr, strGuid)
        {
        }

        public TCPRTPSocketClient(System.Net.Sockets.Socket s)
            : base(s)
        {

        }
#endif
        
        /// <summary>
        /// Parse out our packet, then call the handler
        /// </summary>
        /// <param name="arResult"></param>
        protected override void OnRecvData(byte[] bDataReceived, int nLen)
        {
            if (nLen == 0)
            {
                Disconnect();
                return;
            }

            ExtractAndNotifyPacket(bDataReceived);
            DoAsyncRead(); // go read some more
        }

        ByteBuffer ByteBuffer = new ByteBuffer();
        ushort nCurrentLength = 0;

        protected void ExtractAndNotifyPacket(byte[] bData)
        {
                    /// See if we have enough data to examine the rest of our header, if not, wait until the
            /// next time around
            /// 
            ByteBuffer.AppendData(bData);

            while (true)
            {
                if (nCurrentLength == 0)
                {
                    if (ByteBuffer.Size < 2)
                        return;

                    byte [] bLength = ByteBuffer.GetNSamples(2);
                    /// Little Endian for RTP, right?
                    nCurrentLength = (ushort) (bLength[0] | (bLength[1]<<8));
                }
    
                if (ByteBuffer.Size < nCurrentLength)
                    return;
                else
                {
                    byte [] bRTPPacket = ByteBuffer.GetNSamples(nCurrentLength);
                    RTPPacket packet = RTPPacket.BuildPacket(bRTPPacket);
                    ProcessRTPPacket(packet);
                    nCurrentLength = 0;
                }
            }
        }


        protected void ProcessRTPPacket(RTPPacket packet)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("Received packet {0}", packet));
            /// Tell our host we have data
            /// 
            if (OnRTPPacket != null)
                OnRTPPacket(packet);
        }

        public delegate void DelegateRTPPacket(RTPPacket frame);
        public event DelegateRTPPacket OnRTPPacket = null;

    
        public bool SendPacket(RTPPacket packet)
        {
            if (Connected == false)
                return false;

            byte [] bSendingBuffer = packet.GetBytes();

            try
            {
                byte[] bSend = new byte[bSendingBuffer.Length + 2];
                bSend[0] = (byte)(bSendingBuffer.Length & 0xFF);
                bSend[1] = (byte)((bSendingBuffer.Length & 0xFF00) >> 8);
                Array.Copy(bSendingBuffer, 0, bSend, 2, bSendingBuffer.Length);

                Send(bSend);
            }
            catch (Exception)
            {
                OnDisconnect("Socket Closed trying to send");
            }
            return true;
        }


        public override void OnDisconnect(string strReason)
        {
            base.OnDisconnect(strReason);
        }

    }

#if !WINDOWS_PHONE
    public class TCPRTPSocketClientCreator : SocketServer.SocketCreator
    {
        public TCPRTPSocketClientCreator()
        {
        }

        public override SocketServer.SocketClient CreateSocket(System.Net.Sockets.Socket s, SocketServer.ConnectMgr cmgr)
        {
            return new TCPRTPSocketClient(s, cmgr);
        }

        public override SocketServer.SocketClient AcceptSocket(System.Net.Sockets.Socket s, SocketServer.ConnectMgr cmgr)
        {
            TCPRTPSocketClient objNew = new TCPRTPSocketClient(s, cmgr);
            return objNew;
        }
    }
#endif

    

}
