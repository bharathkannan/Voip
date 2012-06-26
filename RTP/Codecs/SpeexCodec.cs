/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Net;
using NSpeex;
using AudioClasses;

namespace RTP
{
    public class SpeexCodec : Codec
    {

        public SpeexCodec(BandMode mode)
            : base("Speex")
        {
            Mode = mode;
            Encoder = new SpeexEncoder(mode);
            Encoder.VBR = false;
            Encoder.Quality = 10;
            Decoder = new SpeexDecoder(mode);
        }

        SpeexEncoder Encoder = null;
        SpeexDecoder Decoder = null;

        BandMode m_eMode = BandMode.Wide;

        public BandMode Mode
        {
            get { return m_eMode; }
            set { m_eMode = value; }
        }

        public override AudioFormat AudioFormat
        {
            get
            {
                if (Mode == BandMode.Narrow)
                    return AudioFormat.SixteenByEightThousandMono;
                else if (Mode == BandMode.Wide)
                    return AudioFormat.SixteenBySixteenThousandMono;
                else 
                    return AudioFormat.SixteenBySixteenThousandMono; /// TODO, figure out what format this codec wants
            }
        }
        byte[] bEncodeBuffer = new byte[512];
        short[] bDecodeBuffer = new short[1024];

        public override RTPPacket[] Encode(short[] sData)
        {
            if (sData.Length != Encoder.FrameSize)
                throw new Exception("Must provide input data equal to 1 frame size"); // for now, later it can be multiples

            int nRet = 0;

            //try
            //{
                watch.Start();
                nRet = Encoder.Encode(sData, 0, sData.Length, bEncodeBuffer, 0, bEncodeBuffer.Length);
                watch.Stop();
                m_nPacketsEncoded++;
            //}
            //catch (ArgumentNullException)
            //{ }
            //catch (ArgumentOutOfRangeException)
            //{
            //}

            byte[] bRet = new byte[nRet];
            Array.Copy(bEncodeBuffer, 0, bRet, 0, nRet);

            RTPPacket packet = new RTPPacket();
            packet.PayloadData = bRet;

            return new RTPPacket[] {packet};
        }

        public override short[] DecodeToShorts(RTPPacket packet)
        {
            int nDecoded = 0;
            try
            {
                nDecoded = Decoder.Decode(packet.PayloadData, 0, packet.PayloadData.Length, bDecodeBuffer, 0, false);
            }
            catch (ArgumentNullException)
            { }
            catch (ArgumentOutOfRangeException)
            {
            }

            short[] sRet = new short[nDecoded];
            Array.Copy(bDecodeBuffer, 0, sRet, 0, nDecoded);
            return sRet;
        }

        public override byte[] DecodeToBytes(RTPPacket packet)
        {
            short[] sBytes = DecodeToShorts(packet);
            return AudioClasses.Utils.ConvertShortArrayToByteArray(sBytes);
        }

    }
}
