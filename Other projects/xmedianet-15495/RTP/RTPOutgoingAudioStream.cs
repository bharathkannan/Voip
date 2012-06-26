/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.Net.Sockets;

namespace RTP
{
    /// TODO.. make this work with windows phone
#if !WINDOWS_PHONE
    public class RTPOutgoingAudioStream
    {
        public RTPOutgoingAudioStream(byte nPayload)
        {
            Payload = nPayload;
            SSRC = (uint) ran.Next();
        }
        static Random ran = new Random();

        private uint m_nSSRC = 0;

        public uint SSRC
        {
            get { return m_nSSRC; }
            set { m_nSSRC = value; }
        }
        public static int MulticastPort = 4010;
        private IPEndPoint m_objMulticastAddress = new IPEndPoint(IPAddress.Parse("239.90.80.70"), MulticastPort);

        public IPEndPoint MulticastAddress
        {
            get { return m_objMulticastAddress; }
            set { m_objMulticastAddress = value; }
        }

        Socket MultiCastSendSocket = null;

        object SocketLock = new object();

        public void StartSending()
        {
            lock (SocketLock)
            {
                if (MultiCastSendSocket != null)
                    return;

                /// 
                IPEndPoint LocalEndpoint = new IPEndPoint(IPAddress.Any, 0);
                MultiCastSendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                MultiCastSendSocket.Bind(LocalEndpoint);
                MultiCastSendSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(MulticastAddress.Address));
                MultiCastSendSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 5);
                MultiCastSendSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, 4096);
                MultiCastSendSocket.Connect(MulticastAddress);
            }
        }

        public void StopSending()
        {
            lock (SocketLock)
            {
                if (MultiCastSendSocket != null)
                {
                    MultiCastSendSocket.Close();
                    MultiCastSendSocket.Dispose();
                    MultiCastSendSocket = null;
                }
            }
        }
        [System.Runtime.InteropServices.DllImport("msvcrt.dll", EntryPoint = "memcpy")]
        public unsafe static extern void CopyMemory(byte* pDest, byte* pSrc, int nLength);

        public void SendPacket(byte [] bCompressedAudio)
        {

            lock (SocketLock)
            {
               /// Send the data packets
               /// 
                   
                RTP.RTPPacket datapacket = FormatNextPacket(bCompressedAudio);

                byte[] bDataPacket = datapacket.GetBytes();

                if (MultiCastSendSocket != null)
                    MultiCastSendSocket.Send(bDataPacket);

            }
        }

     
        private byte m_nPayload = 9;

        public byte Payload
        {
            get { return m_nPayload; }
            set { m_nPayload = value; }
        }

        private ushort m_nSequence = 0;

        protected ushort Sequence
        {
            get { return m_nSequence; }
            set { m_nSequence = value; }
        }

        private uint m_nTimeStamp = 0;

        protected uint TimeStamp
        {
            get { return m_nTimeStamp; }
            set { m_nTimeStamp = value; }
        }

        public void Reset()
        {
            m_nSequence = 0;
            m_nTimeStamp = 0;
        }


        public RTPPacket FormatNextPacket(byte[] bCompressedAudio)
        {
            RTP.RTPPacket newpacket = new RTP.RTPPacket();
            newpacket.SSRC = m_nSSRC;
            newpacket.TimeStamp = m_nTimeStamp;
            m_nTimeStamp += 160;

            newpacket.Marker = (m_nSequence == 0) ? true : false;

            newpacket.SequenceNumber = m_nSequence++;
            newpacket.PayloadData = bCompressedAudio;

            return newpacket;
        }
    }

#endif

}
