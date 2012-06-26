/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AudioClasses;
using NAudio.Codecs;

namespace RTP
{
    public class G722CodecWrapper : Codec
    {
        public G722CodecWrapper()
            : base("G.722")
        {
        }

        G722CodecState EncodeState = new G722CodecState(64000, G722Flags.None);
        G722CodecState DecodeState = new G722CodecState(64000, G722Flags.None);

        G722Codec Codec = new G722Codec();

        public override AudioClasses.AudioFormat AudioFormat
        {
            get
            {
                return AudioFormat.SixteenBySixteenThousandMono;
            }
            protected set
            {
            }
        }

        public override byte[] DecodeToBytes(RTPPacket packet)
        {
            short[] sOutput = new short[packet.PayloadData.Length * 2];
            Codec.Decode(DecodeState, sOutput, packet.PayloadData, packet.PayloadData.Length);

            return Utils.ConvertShortArrayToByteArray(sOutput);
        }

        public override short[] DecodeToShorts(RTPPacket packet)
        {
            short[] sOutput = new short[packet.PayloadData.Length*2];
            Codec.Decode(DecodeState, sOutput, packet.PayloadData, packet.PayloadData.Length);
            return sOutput;
        }
        
        public override RTPPacket[] Encode(short[] sData)
        {
            byte [] bCompressed = new byte[this.ReceivePTime*8];
            Codec.Encode(EncodeState, bCompressed, sData, sData.Length);
            RTPPacket packet = new RTPPacket();
            packet.PayloadData = bCompressed;
            return new RTPPacket[] { packet };
        }
    }
}
