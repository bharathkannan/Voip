/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Net;
using AudioClasses;

namespace RTP
{
    /// <summary>
    ///  Base class for codecs
    /// </summary>
    public class Codec
    { 

        public Codec(string strName)
        {
            Name = strName;
        }

        private string m_strName = "Unknown";

        public string Name
        {
            get { return m_strName; }
            set { m_strName = value; }
        }

        private int m_nReceivePTime = 20;

        public int ReceivePTime
        {
            get { return m_nReceivePTime; }
            set { m_nReceivePTime = value; }
        }

        private int m_nTransmitPTime = 20;

        public int TransmitPTime
        {
            get { return m_nTransmitPTime; }
            set { m_nTransmitPTime = value; }
        }

        private int m_nPayloadType = 0;

        public int PayloadType
        {
            get { return m_nPayloadType; }
            set { m_nPayloadType = value; }
        }

        protected uint m_nPacketsEncoded = 0;
        protected System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        public double AverageEncodeTime
        {
            get
            {
                
                if (m_nPacketsEncoded <= 0)
                    return 0;
                return watch.ElapsedMilliseconds / m_nPacketsEncoded;
            }
        }

        protected AudioFormat m_objAudioFormat = AudioFormat.SixteenByEightThousandMono;
        public virtual AudioFormat AudioFormat
        {
            get
            {
                return m_objAudioFormat;
            }
            protected set
            {
                m_objAudioFormat = value;
            }
        }

        private VideoCaptureRate m_objVideoFormat = null;

        protected virtual VideoCaptureRate VideoFormat
        {
            get { return m_objVideoFormat; }
            set { m_objVideoFormat = value; }
        }

        public virtual RTPPacket[] Encode(short[] sData)
        {
            return null;
        }

        public virtual short[] DecodeToShorts(RTPPacket packet)
        {
            return null;
        }

        public virtual byte[] DecodeToBytes(RTPPacket packet)
        {
            return null;
        }
        
    }
}
