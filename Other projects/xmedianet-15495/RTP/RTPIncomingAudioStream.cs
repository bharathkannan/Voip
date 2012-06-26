/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.Net.Sockets;

using SocketServer;

namespace RTP
{
#if !WINDOWS_PHONE

    /// <summary>
    /// Class for receiving multicasting RTP audio
    /// </summary>
    public class RTPIncomingAudioStream 
    {
        public RTPIncomingAudioStream(IPEndPoint MultiCastAddress)
        {
            this.MulticastAddress = MultiCastAddress;
        }

        public static int MulticastPort = 4010;
        private IPEndPoint m_objMulticastAddress = new IPEndPoint(IPAddress.Parse("239.90.80.70"), MulticastPort);

        public IPEndPoint MulticastAddress
        {
            get { return m_objMulticastAddress; }
            set { m_objMulticastAddress = value; }
        }

        public static BufferPool BufferPool = new BufferPool(4096, 5);
        Socket MultiCastRecvSocket = null;

        object SocketLock = new object();
        public void StartReceiving()
        {
            lock (SocketLock)
            {
                if (MultiCastRecvSocket != null)
                    return;

                /// 
                IPEndPoint LocalEndpoint = new IPEndPoint(IPAddress.Any, MulticastPort);
                MultiCastRecvSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                MultiCastRecvSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                MultiCastRecvSocket.Bind(LocalEndpoint);

                MultiCastRecvSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(MulticastAddress.Address));
                MultiCastRecvSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 4096);
                DoReceive();
            }
        }

        void DoReceive()
        {
            lock (SocketLock)
            {
                if (MultiCastRecvSocket == null)
                    return;

                EndPoint ep = (EndPoint) MulticastAddress;

                try
                {
                    byte [] bBuffer = BufferPool.Checkout();
                   MultiCastRecvSocket.BeginReceiveFrom(bBuffer, 0, bBuffer.Length, SocketFlags.None, ref ep, new AsyncCallback(OnRecvSocket), bBuffer);
                }
                catch(Exception)
                {
                }
            }
        }

        public delegate void DelegateNewPacket(byte [] bCompressedAudio);
        public event DelegateNewPacket OnNewPacket = null;

        void OnRecvSocket(IAsyncResult result)
        {
            byte [] bBuffer = (byte [] ) result.AsyncState;
            try
            {
                
                EndPoint ep = (EndPoint) MulticastAddress;
                int nRecv = MultiCastRecvSocket.EndReceiveFrom(result, ref ep);

                // Notify the man of the incoming data

                if (OnNewPacket != null)
                {
                   byte[] bPacketCopy = new byte[nRecv];
                   Array.Copy(bBuffer, 0, bPacketCopy, 0, nRecv);

                   RTPPacket packet = RTPPacket.BuildPacket(bPacketCopy);
                   OnNewPacket(packet.PayloadData);
                }
            }
            catch(Exception)
            {
            }
            finally
            {
                BufferPool.Checkin(bBuffer);
            }

            DoReceive();
        }

        public void StopReceiving()
        {
            lock (SocketLock)
            {
                if (MultiCastRecvSocket != null)
                {
                    MultiCastRecvSocket.Close();
                    MultiCastRecvSocket.Dispose();
                    MultiCastRecvSocket = null;
                }
            }
        }

    }
#endif
}
