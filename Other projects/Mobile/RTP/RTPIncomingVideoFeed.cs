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
    public interface IVideoPacketReceiver
    {
        void OnNewPacket(byte[] bPacketData);
    }

#if !WINDOWS_PHONE
    public class RTPIncomingVideoFeed : IVideoPacketReceiver
    {
        public RTPIncomingVideoFeed(IPEndPoint MultiCastAddress)
        {
            this.MulticastAddress = MultiCastAddress;
            VideoFrameFragmentor = new VideoFrameFragmentor(this);
        }

        private IPEndPoint m_objLocalEndpoint = new IPEndPoint(IPAddress.Any, 4008);

        public IPEndPoint LocalEndpoint
        {
           get { return m_objLocalEndpoint; }
           set { m_objLocalEndpoint = value; }
        }

        public static int MulticastPort = 4008;
        private IPEndPoint m_objMulticastAddress = new IPEndPoint(IPAddress.Parse("239.90.80.70"), MulticastPort);

        public IPEndPoint MulticastAddress
        {
            get { return m_objMulticastAddress; }
            set { m_objMulticastAddress = value; }
        }

        public static BufferPool BufferPool = new BufferPool(6220800, 5);
        Socket MultiCastRecvSocket = null;

        object SocketLock = new object();
        public void StartReceiving()
        {
            lock (SocketLock)
            {
                if (MultiCastRecvSocket != null)
                    return;

                /// 
                MultiCastRecvSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                MultiCastRecvSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                MultiCastRecvSocket.Bind(LocalEndpoint);

                MultiCastRecvSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(MulticastAddress.Address));
                MultiCastRecvSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 64000);
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

        public delegate void DelegateNewFrame(byte [] bCompressedVideoData);
        public event DelegateNewFrame OnNewFrame = null;

        VideoFrameFragmentor VideoFrameFragmentor = null;
        void OnRecvSocket(IAsyncResult result)
        {
            byte [] bBuffer = (byte [] ) result.AsyncState;
            try
            {
                
                EndPoint ep = (EndPoint) MulticastAddress;
                int nRecv = MultiCastRecvSocket.EndReceiveFrom(result, ref ep);

                // Notify the man of the incoming data

                if (OnNewFrame != null)
                {
                   byte[] bPacketCopy = new byte[nRecv];
                   Array.Copy(bBuffer, 0, bPacketCopy, 0, nRecv);

                   RTPPacket packet = RTPPacket.BuildPacket(bPacketCopy);
                   VideoFrameFragmentor.NewFragmentReceived(packet);
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


        public void OnNewPacket(byte[] bPacketData)
        {
            if ( (bPacketData != null) && (OnNewFrame != null))
            {
                // bVideoFrame is in JPEG format (may change this to png later)..  
                OnNewFrame(bPacketData);
            }
        }
    }

#endif
}
