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
    #if !WINDOWS_PHONE

    public class RTPOutgoingVideoFeed
    {
        public RTPOutgoingVideoFeed(byte nPayload, int nFrameRate)
        {
            if (m_objMulticastAddress == null)
            {
                Random rand = new Random();
                string strAddress = string.Format("239.90.80.{0}", rand.Next(200) + 50);
                m_objMulticastAddress = new IPEndPoint(IPAddress.Parse(strAddress), MulticastPort);
            }
            Payload = nPayload;
        }

        private IPEndPoint m_objLocalEndpoint = new IPEndPoint(IPAddress.Any, 0);

        public IPEndPoint LocalEndpoint
        {
           get { return m_objLocalEndpoint; }
           set { m_objLocalEndpoint = value; }
        }

        public static int MulticastPort = 4008;
        private IPEndPoint m_objMulticastAddress = null;

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
                MultiCastSendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                MultiCastSendSocket.Bind(LocalEndpoint);
                MultiCastSendSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(MulticastAddress.Address));
                MultiCastSendSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 5);
                MultiCastSendSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, 64000);
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

        public void SendFrame(int nWidth, int nHeight, byte [] bCompressedFrame)
        {
            //System.Drawing.Imaging.BitmapData data = Frame.LockBits(new System.Drawing.Rectangle(0, 0, Frame.Width, Frame.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite,
            //    System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            //byte[] bRGBFrame = new byte[Frame.Width*3*Frame.Height];
            //unsafe
            //{
            //    byte *pSrc = (byte *) data.Scan0.ToPointer();
            //    fixed (byte* pDest = &bRGBFrame[0])
            //    {
            //        CopyMemory(pDest, pSrc, Frame.Width*Frame.Height*3);
            //    }
            //}
            //Frame.UnlockBits(data);


            lock (SocketLock)
            {
               int nAt = 0;
               /// Send the data packets
               /// 
               int nPacket = 0;
               while (true)
               {
                   int nNextSize = ((bCompressedFrame.Length - nAt) > 60000) ? 60000 : (bCompressedFrame.Length - nAt);
                   byte [] bNextData = new byte[nNextSize];
                   Array.Copy(bCompressedFrame, nAt, bNextData, 0, nNextSize);
                   nAt += nNextSize;
                   
                   RTP.RTPPacket datapacket = FormatNextPacket(bNextData);
                   datapacket.Marker = (nPacket == 0)?true:false;
                   datapacket.TimeStamp = m_nFrame;

                   byte[] bDataPacket = datapacket.GetBytes();

                   if (MultiCastSendSocket != null)
                       MultiCastSendSocket.Send(bDataPacket);

                   if (nAt >= (bCompressedFrame.Length - 1))
                       break;
                   nPacket++;
               }
            }

            m_nFrame++;
        }

     
        private byte m_nPayload = 127;

        public byte Payload
        {
            get { return m_nPayload; }
            set { m_nPayload = value; }
        }

        private int m_nFrameRate = 30;

        public int FrameRate
        {
            get { return m_nFrameRate; }
            set { m_nFrameRate = value; }
        }

        private ushort m_nSequence = 0;

        protected ushort Sequence
        {
            get { return m_nSequence; }
            set { m_nSequence = value; }
        }

        public void Reset()
        {
            m_nSequence = 0;
            m_nFrame = 0;
        }

        uint m_nFrame = 0;

        public RTPPacket FormatNextPacket(byte[] VideoPayload)
        {
            RTPPacket packet = new RTPPacket();
            packet.PayloadData = VideoPayload;
            packet.PayloadType = Payload;
            if (m_nSequence == 0)
                packet.Marker = true;
            else
                packet.Marker = false;
            packet.SequenceNumber = m_nSequence++;
            packet.TimeStamp = (uint) ( m_nSequence * 1000 / FrameRate);


            return packet;
        }
    }

#endif
}
