/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.Net.Sockets;
using AudioClasses;
using SocketServer;

namespace RTP
{
    public class RTPAudioStream : RTPStream, IAudioSink, IAudioSource
    {
        public RTPAudioStream(byte nPayload, Codec codec)
            : base(nPayload)
        {
            AudioCodec = codec;
        }

        Codec m_objAudioCodec = null;

        public Codec AudioCodec
        {
            get { return m_objAudioCodec; }
            set 
            { 
                m_objAudioCodec = value;
                CalculateSampleSizes();
            }
        }

        void CalculateSampleSizes()
        {
            if (AudioCodec != null)
            {
                m_nPacketSamples = AudioCodec.AudioFormat.CalculateNumberOfSamplesForDuration(TimeSpan.FromMilliseconds(PTimeReceive));
                m_nPacketBytes = m_nPacketSamples * m_objAudioCodec.AudioFormat.BytesPerSample;

                m_nMaxSendBufferSize = AudioCodec.AudioFormat.CalculateNumberOfSamplesForDuration(TimeSpan.FromMilliseconds(100));
                m_nMaxRecvBufferSize = AudioCodec.AudioFormat.CalculateNumberOfSamplesForDuration(TimeSpan.FromMilliseconds(100));
            }
        }

        public override int PTimeReceive
        {
            get { return m_nPTimeReceive; }
            set
            {
                m_nPTimeReceive = value;
                CalculateSampleSizes();
            }
        }

        public override int PTimeTransmit
        {
            get { return m_nPTimeTransmit; }
            set 
            { 
                m_nPTimeTransmit = value;
                CalculateSampleSizes();
            }
        }

        protected int m_nPacketSamples = 320;
        /// <summary>
        /// The number of samples in each packet
        /// </summary>
        public int PacketSamples
        {
            get { return m_nPacketSamples; }
            set { m_nPacketSamples = value; }
        }

        protected int m_nPacketBytes = 640;
        /// <summary>
        /// Number of uncompressed bytes in each packet
        /// </summary>
        public int PacketBytes
        {
            get { return m_nPacketBytes; }
            set { m_nPacketBytes = value; }
        }

        public override bool Start(IPEndPoint destinationEp, int nPacketTimeTx, int nPacketTimeRx)
        {
            if (this.AudioCodec == null)
                return false;
            SendAudioQueue.Clear();
            this.AudioCodec.ReceivePTime = nPacketTimeRx;
            this.AudioCodec.TransmitPTime = nPacketTimeTx;
            return base.Start(destinationEp, nPacketTimeTx, nPacketTimeRx);
        }

        object ReceiveLock = new object();
		public static int MaxAudioPacketsQueue = 10;
        protected override void PushNextPacket()
        {
            if (AudioCodec == null)
                return;

            RTPPacket packet = IncomingRTPPacketBuffer.GetPacket();
            if (packet == null)
                return; 
                
            byte[] bNewAudioData = AudioCodec.DecodeToBytes(packet);

            if (bNewAudioData != null)
            {
                ReceiveAudioQueue.AppendData(bNewAudioData);
				if (ReceiveAudioQueue.Size > m_nPacketBytes*MaxAudioPacketsQueue)  // someone isn't taking our packets (either directly our through IAudioSource), so let's not get too big
				{
					ReceiveAudioQueue.GetNSamples(ReceiveAudioQueue.Size-m_nPacketBytes*MaxAudioPacketsQueue);
				}

                if (RenderSink != null)
                {
                    MediaSample samp = new MediaSample(bNewAudioData, AudioCodec.AudioFormat);
                    RenderSink.PushSample(samp, this);
                }
            }
        }

        public override byte[] GetNextPacketSample(bool bReturnArrayOnNA)
        {
            if (AudioCodec == null)
                return null;

            int nSamples = AudioCodec.AudioFormat.CalculateNumberOfSamplesForDuration(TimeSpan.FromMilliseconds(AudioCodec.ReceivePTime));
            int nSizeBytes = nSamples * AudioCodec.AudioFormat.BytesPerSample;

            RTPPacket packet = IncomingRTPPacketBuffer.GetPacket();
            if ( (packet == null) && (bReturnArrayOnNA == true) )
                return new byte[nSizeBytes];

            return AudioCodec.DecodeToBytes(packet);
        }

        public byte[] WaitNextPacketSample(bool bReturnArrayOnNA, int nTimeOut, out int nMsTook)
        {
            nMsTook = 0;
            if (AudioCodec == null)
                return null;

            int nSamples = AudioCodec.AudioFormat.CalculateNumberOfSamplesForDuration(TimeSpan.FromMilliseconds(AudioCodec.ReceivePTime));
            int nSizeBytes = nSamples * AudioCodec.AudioFormat.BytesPerSample;

            RTPPacket packet = IncomingRTPPacketBuffer.WaitPacket(nTimeOut, out nMsTook);
            if ( (packet == null) && (bReturnArrayOnNA == true))
                return new byte[nSizeBytes];

            return AudioCodec.DecodeToBytes(packet);
        }



        public IAudioSink RenderSink = null;


        public AudioClasses.ByteBuffer SendAudioQueue = new AudioClasses.ByteBuffer();
        public AudioClasses.ByteBuffer ReceiveAudioQueue = new AudioClasses.ByteBuffer();

        /// <summary>
        /// Sends the next sample directly.  A client can either do the default, with UseInternalTimersForPacketPushPull set to true, which uses timers and reads audio from the
        /// SendAudioQueue, or it can push them directly by turning off UseInternalTimersForPacketPushPull and calling SendNextSample at the appropriate ptime
        /// </summary>
        /// <param name="bData"></param>
        public override void SendNextSample(byte[] bUncompressedAudio)
        {
            if (IsActive == false)
                return;

            if (AudioCodec == null)
                return;

            if (DestinationEndpoint == null)
                return;

            SendAudio(bUncompressedAudio);
        }

        object SendLock = new object();
        protected override void SendNextPacket()
        {
            if (IsActive == false)
                return;

            if (AudioCodec == null)
                return;


            if (SendAudioQueue.Size < m_nPacketBytes)
                return;


            byte[] bUncompressedAudio = SendAudioQueue.GetNSamples(m_nPacketBytes);

            if (DestinationEndpoint == null)
                return;

            SendAudio(bUncompressedAudio);
        }

        protected void SendAudio(byte[] bUncompressedAudio)
        {
            short[] sUncompressedAudio = AudioClasses.Utils.ConvertByteArrayToShortArrayLittleEndian(bUncompressedAudio);


            RTPPacket[] packets = new RTPPacket[] { };
            lock (SendLock)
            {
                packets = AudioCodec.Encode(sUncompressedAudio);
            }

            lock (SocketLock)
            {
                foreach (RTPPacket packet in packets)
                {
                    FormatNextPacket(packet);
                    byte[] bDataPacket = packet.GetBytes();
                    if (RTPUDPClient != null)
                        RTPUDPClient.SendUDP(bDataPacket, bDataPacket.Length, DestinationEndpoint);
                }
            }
        }
        private int m_nMaxSendBufferSize = AudioClasses.AudioFormat.SixteenBySixteenThousandMono.CalculateNumberOfSamplesForDuration(TimeSpan.FromMilliseconds(100));
        public int MaxSendBufferSize
        {
            get { return m_nMaxSendBufferSize; }
            set { m_nMaxSendBufferSize = value; }
        }

        private int m_nMaxRecvBufferSize = AudioClasses.AudioFormat.SixteenBySixteenThousandMono.CalculateNumberOfSamplesForDuration(TimeSpan.FromMilliseconds(100));
        public int MaxRecvBufferSize
        {
            get { return m_nMaxRecvBufferSize; }
            set { m_nMaxRecvBufferSize = value; }
        }

        #region IAudioSink Members

        public AudioResampler SendResampler = new AudioResampler();
        public AudioResampler RecvResampler = new AudioResampler();
        /// <summary>
        /// Push a sample to this filter's outgoing queue.
        /// </summary>
        /// <param name="sample"></param>
        public void PushSample(MediaSample sample, object objSource)
        {
            if (AudioCodec == null)
                return;

            MediaSample newsample = SendResampler.Resample(sample, AudioCodec.AudioFormat);
            SendAudioQueue.AppendData(newsample.Data);

            if (SendAudioQueue.Size > MaxSendBufferSize)
            {
                SendAudioQueue.GetNSamples(SendAudioQueue.Size - MaxSendBufferSize);
            }
        }

        bool m_bIsDeafened = false;
        public bool IsSinkActive
        {
            get
            {
                return !m_bIsDeafened;
            }
            set
            {
                m_bIsDeafened = value;
            }
        }

        public bool Deafened
        {
            get
            {
                return m_bIsDeafened;
            }
            set
            {
                if (m_bIsDeafened != value)
                {
                    m_bIsDeafened = value;
                }
            }
        }

        double m_fSinkAmplitudeMultiplier = 1.0f;
        public double SinkAmplitudeMultiplier
        {
            get { return m_fSinkAmplitudeMultiplier; }
            set { m_fSinkAmplitudeMultiplier = value; }
        }


        #endregion

        #region IAudioSource Members

        public MediaSample PullSample(AudioFormat format, TimeSpan tsDuration)
        {

            if (AudioCodec == null)
                return null;

            int nSamples = AudioCodec.AudioFormat.CalculateNumberOfSamplesForDuration(tsDuration);
            int nBytesNeeded = nSamples * AudioCodec.AudioFormat.BytesPerSample;

            /// Greater than 4 samples in our buffer, remove some
            if (ReceiveAudioQueue.Size > nBytesNeeded * AudioCodec.AudioFormat.BytesPerSample)
                ReceiveAudioQueue.GetNSamples(ReceiveAudioQueue.Size - nBytesNeeded*2);

            if (ReceiveAudioQueue.Size >= nBytesNeeded)
            {
                byte [] bAudioData = ReceiveAudioQueue.GetNSamples(nBytesNeeded);

                /// Incoming RTP packets' audio data is in the codecs native format, we may need to resample for our host (Our windows muxer always expects 16x16, so ulaw must be resampled)
                MediaSample currentsample = new MediaSample(bAudioData, AudioCodec.AudioFormat);

                MediaSample newsample = RecvResampler.Resample(currentsample, format);

                return newsample;
            }

            return null;
        }

        public bool m_bMuted = false;
        public bool IsSourceActive
        {
            get
            {
                return !m_bMuted;
            }
            set
            {
                if (m_bMuted != value)
                {
                    m_bMuted = !value;
                }
            }
        }

        public bool Muted
        {
            get
            {
                return m_bMuted;
            }
            set
            {
                if (m_bMuted != value)
                {
                    m_bMuted = value;
                }
            }
        }

        double m_fSourceAmplitudeMultiplier = 1.0f;
        public double SourceAmplitudeMultiplier
        {
            get { return m_fSourceAmplitudeMultiplier; }
            set { m_fSourceAmplitudeMultiplier = value; }
        }
        

        #endregion



        #region Call Setup


        public IPEndPoint Server = new IPEndPoint(IPAddress.Parse("172.16.41.174"), 4505);


        public string Testsend(IPEndPoint remote, String data)
        {
            return RTPUDPClient.MySend(remote.Address.ToString(), remote.Port, data);
        }

        public string TestRecv()
        {
            return RTPUDPClient.MyReceive1(4507);
        }

        public string CreateAndSendSTUN(DnsEndPoint serverEp)
        {
            STUN2Message msgRequest = new STUN2Message();
            msgRequest.Method = StunMethod.Binding;
            msgRequest.Class = StunClass.Request;


            MappedAddressAttribute mattr = new MappedAddressAttribute();
            mattr.IPAddress = LocalEndpoint.Address;
            mattr.Port = (ushort)LocalEndpoint.Port;

            msgRequest.AddAttribute(mattr);

            byte[] tosend = msgRequest.Bytes;

            return RTPUDPClient.SendByteArray(tosend, serverEp);
        }



        public IPEndPoint RecvSTUN(DnsEndPoint serverEp,int timeout)
        {
            byte[] recv = RTPUDPClient.ReceiveByteArray(serverEp.Port,timeout);
            STUNMessage ResponseMessage = new STUNMessage();
            ResponseMessage.Bytes = recv;
            IPEndPoint retep = LocalEndpoint;
            if (ResponseMessage != null)
            {
                foreach (STUNAttributeContainer cont in ResponseMessage.Attributes)
                {
                    if (cont.ParsedAttribute.Type == StunAttributeType.MappedAddress)
                    {

                        MappedAddressAttribute attrib = cont.ParsedAttribute as MappedAddressAttribute;
                        retep = new IPEndPoint(attrib.IPAddress, attrib.Port);
                    }
                }
            }
            return retep;
        }


        public  IPEndPoint GetSTUNAddress(DnsEndPoint server,int timeout)
        {
            CreateAndSendSTUN(server);
            return RecvSTUN(server,timeout);
        }




       

        public bool SignIn(string username)
        {
            RTPUDPClient.MySend(Server.Address.ToString(),Server.Port,"set "+username);
            string result = RTPUDPClient.MyReceive(4505);
            if(result.Equals("Changed"))
                    return true;
            return false;
	    }


        public IPEndPoint FindIpPort(string username)
        {
            IPEndPoint ret = new IPEndPoint(IPAddress.Parse("255.255.255.255"), 0);
            RTPUDPClient.MySend(Server.Address.ToString(),Server.Port, "get " + username);
            string result = RTPUDPClient.MyReceive(4505);
            if (result.Equals("Failed"))
                return ret;
            else
            {
                int pos = result.IndexOf(":");
                string a = result.Substring(0, pos);
                string b = result.Substring(pos + 1);
                return new IPEndPoint(IPAddress.Parse(a), Convert.ToInt32(b));
            }
        }

        public IPEndPoint CallUser(string username,string myname)
        {
            IPEndPoint remote = FindIpPort(username);
            string result = RTPUDPClient.MySend(remote.Address.ToString(), remote.Port, "CallRequest "+myname);
            return remote;
        }

        public IPEndPoint WaitForCall()
        {
            string result = RTPUDPClient.MyReceive(4505);
            int pos = result.IndexOf(' ');
            string mes = result.Substring(0, pos);
            string uname = result.Substring(pos + 1);
            if (mes.Equals("CallRequest"))
            {
                return FindIpPort(uname);
            }
            else
                return null;
        }




        public IPEndPoint[] WaitForCall(string tosend,string uname)
        {
           

            string result = RTPUDPClient.MyReceive(4505);
            int pos = result.IndexOf(' ');
            string sender = result.Substring(0, pos);
            string remain = result.Substring(pos+1);
            IPEndPoint[] ret = new IPEndPoint[2];
            ret[0] = ConvertTo(remain.Substring(0,remain.IndexOf(';')));
            ret[1] = ConvertTo(remain.Substring(remain.IndexOf(';')+1));
            RTPUDPClient.MySend(Server.Address.ToString(),Server.Port, "snd " + uname + ' ' + sender + ' ' + tosend);
            return ret;

            
        }


        public IPEndPoint StartNeg(bool IsCaller, IPEndPoint[] local, IPEndPoint[] remote)
        {
            if (IsCaller)
            {
                bool done = false;
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        if (!done)
                        {
                            IPEndPoint from = local[i];
                            IPEndPoint to = remote[j];
                            RTPUDPClient.MySend(to.Address.ToString(), to.Port, "tst " + from.ToString());
                            string res = RTPUDPClient.MyReceive1(4505);
                            if (res == "Success")
                            {
                                RTPUDPClient.MySend(to.Address.ToString(), to.Port, "fin " + from.ToString());
                                return to;
                            }
                        }
                    }
                }
                return null;

            }
            else
            {
            top:
                string message = RTPUDPClient.MyReceive(4505);
                string mestype = message.Substring(0, 3);
                string remain = message.Substring(4);
                if (mestype.Equals("fin"))
                {
                    return new IPEndPoint(IPAddress.Parse(remain.Substring(0, remain.IndexOf(':'))), Convert.ToInt32(remain.Substring(remain.IndexOf(':')+1)));
                }
                else if (mestype.Equals("tst"))
                {
                    IPEndPoint to = new IPEndPoint(IPAddress.Parse(remain.Substring(0, remain.IndexOf(':'))), Convert.ToInt32(remain.Substring(remain.IndexOf(':')+1)));
                    RTPUDPClient.MySend(to.Address.ToString(), to.Port, "Success");
                    goto top;
                }
            }
            return null;
        }

        public IPEndPoint[] CallUser(string username, string myname, string localCandidates)
        {
            RTPUDPClient.MySend(Server.Address.ToString(),Server.Port, "snd " + myname + " " + username + " " + localCandidates);
            string result = RTPUDPClient.MyReceive(4505);
            int pos = result.IndexOf(' ');
            string sender = result.Substring(0, pos);
            string remain = result.Substring(pos + 1);
            IPEndPoint[] ret = new IPEndPoint[2];
            ret[0] = ConvertTo(remain.Substring(0, remain.IndexOf(';')));
            ret[1] = ConvertTo(remain.Substring(remain.IndexOf(';') + 1));
            return ret;
        }
                

        public IPEndPoint ConvertTo(string s)
        {
            int pos = s.IndexOf(':');
            IPEndPoint ret = new IPEndPoint(IPAddress.Parse(s.Substring(0,pos)),Convert.ToInt32(s.Substring(pos+1)));
            return ret;
        }
        public bool SignOut(string username)
        {
            RTPUDPClient.MySend(Server.Address.ToString(),Server.Port, "out " + username);
            string result = RTPUDPClient.MyReceive(4505);
            return result.Equals("Removed");
        }


                   
            

                  
            


        #endregion
    }
}
