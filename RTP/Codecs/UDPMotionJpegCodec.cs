using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RTP.Codecs
{
    public class UDPMotionJpegCodec : Codec
    {

        public UDPMotionJpegCodec() : base("UDP Motion JPEG")
        {
        }

        protected override AudioClasses.VideoCaptureRate VideoFormat
        {
            get
            {
                return base.VideoFormat;
            }
            set
            {
                base.VideoFormat = value;
            }
        }

        /// <summary>
        /// Decode the packets.  Don't return bytes until we have all the fragments
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public override byte[] DecodeToBytes(RTPPacket packet)
        {
            return base.DecodeToBytes(packet);
        }

        public override RTPPacket[] Encode(short[] sData)
        {
            return base.Encode(sData);
        }
    }

}
