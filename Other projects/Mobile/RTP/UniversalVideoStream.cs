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

    public enum FrameFormat : byte
    {
        PNG = 0,
        JPG = 1,
        H264NALUNITS = 2,
        WebM = 3,
    }

    #if !WINDOWS_PHONE

    /// <summary>
    /// A simple streamed video frame format for desktop sharing and camera sharing on devices that don't support WCF
    /// Format is: (Everything is BigEndian)
    ///     MagicNumber : uint // 0x22777722
    ///     FrameFormat : byte  /// See FrameFormat enum above
    ///     Length :    uint    /// Length of the data in bytes.. .BigEndian
    ///     TimeStamp : uint ///  RTP timestamp using 90000 clock... BigEndian
    ///     x : ushort /// the x coordinate where the image will be placed.  A full frame must have already been sent (for future compatibility)
    ///     y: ushort /// The y coordinate where the image will be placed.
    ///     Data : byte [] /// Video data in specified format, may be encrypted if that was negotiated out of band
    /// </summary>
    public class UniversalVideoFrame
    {
        public UniversalVideoFrame()
        {

        }

        public const uint MagicNumber = 0x22777722;

        private FrameFormat m_eFrameFormat = FrameFormat.PNG;

        public FrameFormat FrameFormat
        {
            get { return m_eFrameFormat; }
            set { m_eFrameFormat = value; }
        }
        private uint m_nLength = 0;

        public uint Length
        {
            get { return m_nLength; }
            private set { m_nLength = value; }
        }
        private uint m_nTimeStamp = 0;

        public uint TimeStamp
        {
            get { return m_nTimeStamp; }
            set { m_nTimeStamp = value; }
        }
        private ushort m_nX = 0;

        public ushort X
        {
            get { return m_nX; }
            set { m_nX = value; }
        }
        private ushort m_nY = 0;

        public ushort Y
        {
            get { return m_nY; }
            set { m_nY = value; }
        }
        private byte[] m_bFrameDataData = new byte[] { };

        public byte[] FrameData
        {
            get { return m_bFrameDataData; }
            set { m_bFrameDataData = value; Length = (uint)FrameData.Length;  }
        }


        public byte[] AllData
        {
            get 
            {
                byte[] bData = new byte[FrameData.Length + 17];
                bData[0] = 0x22; bData[1] = 0x77; bData[2] = 0x77; bData[3] = 0x22;
                bData[4] = (byte) FrameFormat;
                bData[5] = (byte)((Length & 0xFF000000) >> 24);
                bData[6] = (byte)((Length & 0xFF0000) >> 16);
                bData[7] = (byte)((Length & 0xFF00) >> 8);
                bData[8] = (byte) (Length & 0xFF);

                bData[9] = (byte) ((TimeStamp & 0xFF000000) >> 24);
                bData[10] = (byte)((TimeStamp & 0xFF0000) >> 16);
                bData[11] = (byte)((TimeStamp & 0xFF00) >> 8);
                bData[12] = (byte)(TimeStamp & 0xFF);

                bData[13] = (byte)((X & 0xFF00) >> 8);
                bData[14] = (byte) (X & 0xFF);

                bData[15] = (byte)((Y & 0xFF00) >> 8);
                bData[16] = (byte) (Y & 0xFF);

                Array.Copy(FrameData, 0, bData, 17, FrameData.Length);
                return m_bFrameDataData; 
            }
            set 
            {
                if (value.Length < 17)
                    return;

                uint nMagicCookie = (uint)((value[0] << 24) | (value[1] << 16) | (value[2] << 8) | value[3]);
                if (nMagicCookie != MagicNumber)
                    return;

                FrameFormat = (FrameFormat)value[4];
                uint nLength = (uint)((value[5] << 24) | (value[6] << 16) | (value[7] << 8) | value[8]);
                uint nTimeStamp  = (uint)((value[9] << 24) | (value[10] << 16) | (value[11] << 8) | value[12]);
                ushort nX = (ushort)((value[13] << 8) | value[14]);
                ushort nY = (ushort)((value[15] << 8) | value[16]);

                if ((value.Length - 17) != nLength)
                    return;

                Length = nLength;
                TimeStamp = nTimeStamp;
                X = nX;
                Y = nY;
                FrameData = new byte[Length];
                Array.Copy(value, 17, FrameData, 0, nLength);
              
            }
        }

        public bool SetHeader(byte[] value)
        {
            if (value.Length < 17)
                return false;

            uint nMagicCookie = (uint)((value[0] << 24) | (value[1] << 16) | (value[2] << 8) | value[3]);
            if (nMagicCookie != MagicNumber)
                return false;

            FrameFormat = (FrameFormat)value[4];
            uint nLength = (uint)((value[5] << 24) | (value[6] << 16) | (value[7] << 8) | value[8]);
            uint nTimeStamp = (uint)((value[9] << 24) | (value[10] << 16) | (value[11] << 8) | value[12]);
            ushort nX = (ushort)((value[13] << 8) | value[14]);
            ushort nY = (ushort)((value[15] << 8) | value[16]);

            Length = nLength;
            TimeStamp = nTimeStamp;
            X = nX;
            Y = nY;
            return true;
        }


    }

    public class UniversalVideoSocketClient : SocketServer.SocketClient
    {
        public UniversalVideoSocketClient(System.Net.Sockets.Socket s, SocketServer.ConnectMgr cmgr)
            : base(s, cmgr)
        {

        }
        
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

        private UniversalVideoFrame CurrentVideoFrame = null;
        ByteBuffer ByteBuffer = new ByteBuffer();

        protected void ExtractAndNotifyPacket(byte[] bData)
        {
                    /// See if we have enough data to examine the rest of our header, if not, wait until the
            /// next time around
            /// 

            ByteBuffer.AppendData(bData);

            while (true)
            {
                if (ByteBuffer.Size <= 0)
                    break;
                else if (CurrentVideoFrame != null)
                {
                    /// See if we have enough data for the video part of this frame
                    /// 
                    if (ByteBuffer.Size < CurrentVideoFrame.Length)
                        return;

                    CurrentVideoFrame.FrameData = ByteBuffer.GetNSamples(ByteBuffer.Size);
                    ProcessVideoFrame(CurrentVideoFrame);
                    CurrentVideoFrame = null;
                }
                else if (ByteBuffer.Size >= 17)
                {
                    CurrentVideoFrame = new UniversalVideoFrame();
                    bool bValid = CurrentVideoFrame.SetHeader(bData);
                    if (bValid == false)
                    {
                        CurrentVideoFrame = null;
                        //

                        int nAt = ByteBuffer.FindBytes(new byte[] { 0x22, 0x77, 0x77, 0x22 });
                        if (nAt == 0)
                        {
                            ByteBuffer.GetAllSamples(); // flush...nothing but bad data
                        }
                        else
                        {
                           ByteBuffer.GetNSamples(nAt); // found the header at another location... should never happen, may want to abort the stream here instead
                        }
                    }
                }
            }
        }


        protected void ProcessVideoFrame(UniversalVideoFrame vidframe)
        {
            /// Tell our host we have data
            /// 
            if (OnNewVideoFrame != null)
                OnNewVideoFrame(vidframe);
        }

        public delegate void DelegateNewVideoData(UniversalVideoFrame frame);
        public event DelegateNewVideoData OnNewVideoFrame = null;

        bool m_bIsSending = false;
        object SendingLock = new object();
        byte[] bSendingBuffer = null;

        public bool MaybeCanSend
        {
            get
            {
                lock (SendingLock)
                {
                    return !m_bIsSending;
                }
            }
        }
        public bool SendVideoFrame(UniversalVideoFrame frame)
        {
            if (Connected == false)
                return false;

            lock (SendingLock)
            {
                if (m_bIsSending == false)
                {
                    m_bIsSending = true;
                }
                else
                    return false;
            }

            bSendingBuffer = frame.AllData;
            socket.BeginSend(bSendingBuffer, 0, bSendingBuffer.Length, System.Net.Sockets.SocketFlags.None, new AsyncCallback(SendComplete), this);
            return true;
        }

        void SendComplete(IAsyncResult ar)
        {
            try
            {
                int nSend = socket.EndSend(ar);

                lock (SendingLock)
                {
                    m_bIsSending = false;
                }
            }
            catch (Exception)
            {
            }

        }


        public override void OnDisconnect(string strReason)
        {
            base.OnDisconnect(strReason);
        }

    }


    public class UniversalVideoSocketClientCreator : SocketServer.SocketCreator
    {
        public UniversalVideoSocketClientCreator()
        {
        }

        public override SocketServer.SocketClient CreateSocket(System.Net.Sockets.Socket s, SocketServer.ConnectMgr cmgr)
        {
            return new UniversalVideoSocketClient(s, cmgr);
        }

        public override SocketServer.SocketClient AcceptSocket(System.Net.Sockets.Socket s, SocketServer.ConnectMgr cmgr)
        {
            UniversalVideoSocketClient objNew = new UniversalVideoSocketClient(s, cmgr);
            return objNew;
        }
    }

#endif
    

}
