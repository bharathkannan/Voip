/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RTP
{
    public class VideoFrameFragmentor
    {
        public VideoFrameFragmentor(IVideoPacketReceiver rec)
        {
            Receiver = rec;
        }

        IVideoPacketReceiver Receiver = null;

        private uint m_nCurrentFrameTimeStamp = 0;
        public uint CurrentFrameTimeStamp
        {
            get { return m_nCurrentFrameTimeStamp; }
            set { m_nCurrentFrameTimeStamp = value; }
        }

        uint m_nSequence = 0;
        
        List<byte[]> LastPacketFrames = new List<byte[]>();

        public void NewFragmentReceived(RTPPacket packet)
        {
            byte[] bPayload = packet.PayloadData;
            if (packet.Marker == true) /// This should be a header fragment, extract the width, height, and total frame size
            {
                AssembleLastPackets();
                LastPacketFrames.Add(bPayload);
                m_nSequence = packet.SequenceNumber;
                CurrentFrameTimeStamp = packet.TimeStamp;
                if (bPayload.Length < 60000)
                    AssembleLastPackets();

            }
            else
            {
                
                if ( (packet.TimeStamp == CurrentFrameTimeStamp) && (packet.SequenceNumber == (m_nSequence+1)) )
                {
                    m_nSequence = packet.SequenceNumber;

                    LastPacketFrames.Add(bPayload);

                    if (bPayload.Length < 60000)
                        AssembleLastPackets();
                    
                }
            }
        }

        byte [] AssembleLastPackets()
        {
            if (LastPacketFrames.Count <= 0)
                return null;

            int nLength = 0;
            foreach (byte[] bNextpacket in LastPacketFrames)
            {
                nLength += bNextpacket.Length;
            }

            byte[] bFrame = new byte[nLength];
            int nAt = 0;
            foreach (byte[] bNextpacket in LastPacketFrames)
            {

                Array.Copy(bNextpacket, 0, bFrame, nAt, bNextpacket.Length);
                nAt += bNextpacket.Length;
            }

            Receiver.OnNewPacket(bFrame);

            LastPacketFrames.Clear();

            return bFrame;
        }
    }
}
