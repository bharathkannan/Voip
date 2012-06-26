using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

using SocketServer;
using RTP;

namespace XMPPClient
{
    public class WaveFormat
    {
        public WaveFormat()
        {
        }

        public byte[] Bytes
        {
            get
            {
                // Little endian now... see http://msdn.microsoft.com/en-us/library/hh180779(v=VS.95).aspx
                byte[] bBytes = new byte[18];
                bBytes[0] = (byte)(FormatTag & 0xFF);
                bBytes[1] = (byte)((FormatTag & 0xFF00) >> 8); /// big endian, right?

                bBytes[2] = (byte)(Channels & 0xFF);
                bBytes[3] = (byte)((Channels & 0xFF00) >> 8);
                
                bBytes[7] = (byte) ((SamplesPerSec&0xFF000000)>>24);
                bBytes[6] = (byte) ((SamplesPerSec&0x00FF0000)>>16);
                bBytes[5] = (byte) ((SamplesPerSec&0x0000FF00)>>8);
                bBytes[4] = (byte) ((SamplesPerSec&0x000000FF)>>0);
                
                bBytes[11] = (byte) ((AvgBytesPerSec&0xFF000000)>>24);
                bBytes[10] = (byte) ((AvgBytesPerSec&0x00FF0000)>>16);
                bBytes[9] = (byte)((AvgBytesPerSec&0x0000FF00)>>8);
                bBytes[8] = (byte)((AvgBytesPerSec&0x000000FF)>>0);
                
                bBytes[13] = (byte)((BlockAlign&0xFF00) >> 8);
                bBytes[12] = (byte)(BlockAlign&0xFF);
                
                bBytes[15] = (byte)((BitsPerSample & 0xFF00) >> 8);
                bBytes[14] = (byte)(BitsPerSample & 0xFF);
                
                bBytes[17] = (byte)((Size & 0xFF00) >> 8);
                bBytes[16] = (byte)(Size & 0xFF);

                return bBytes;

            }
        }

        public string HexString
        {
            get
            {
                return HexStringFromByte(Bytes, false);
            }
        }

        public static string HexStringFromByte(byte[] aBytes, bool Spaces)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder(aBytes.Length * 3 + 10);
            foreach (byte b in aBytes)
            {
                string strHex = Convert.ToString(b, 16);
                if (strHex.Length == 1)
                    strHex = "0" + strHex;
                if (Spaces)
                    strHex += " ";
                builder.Append(strHex);
                //builder.AppendFormat("{X}", b);
            }

            string strRet = builder.ToString();
            strRet = strRet.TrimEnd();
            strRet = strRet.ToUpper();
            return strRet;
        }

        public ushort FormatTag;
        public ushort Channels;
        public uint SamplesPerSec;
        public uint AvgBytesPerSec;
        public ushort BlockAlign;
        public ushort BitsPerSample;
        public ushort Size = 0;
    }


    public class MyAudioSink : AudioSink
    {
        public MyAudioSink()
            : base()
        {
        }

        protected override void OnCaptureStarted()
        {
            throw new NotImplementedException();
        }

        protected override void OnCaptureStopped()
        {
            throw new NotImplementedException();
        }

        protected override void OnFormatChange(AudioFormat audioFormat)
        {
            throw new NotImplementedException();
        }

        protected override void OnSamples(long sampleTimeInHundredNanoseconds, long sampleDurationInHundredNanoseconds, byte[] sampleData)
        {
            throw new NotImplementedException();
        }
    }

    public class AudioStreamSource : MediaStreamSource
    {

        public AudioStreamSource()
            : base()
        {
            //AudioCaptureDevice dev = CaptureDeviceConfiguration.GetDefaultAudioCaptureDevice();
            //CaptureSource source = new CaptureSource();
            //source.AudioCaptureDevice = dev;
            //source.VideoCaptureDevice = null;
            
            //MyAudioSink sink = new MyAudioSink();
            //sink.CaptureSource = source;

            //source.Start();

        }

    
        bool HaveReportedSize = false;
        int MinimumQueueSize = 3;
        WaveFormat WaveFormat = new WaveFormat();
        MediaStreamDescription MediaStreamDescription = null;

        Dictionary<MediaSourceAttributesKeys, string> sourceAttributes = null;
        List<MediaStreamDescription> availableStreams = null;
        Dictionary<MediaStreamAttributeKeys, string> streamAttributes = null;

        Queue<byte[]> AudioPackets = new Queue<byte[]>();
        object AudioPacketLock = new object();

        protected override void OpenMediaAsync()
        {
            WaveFormat.BitsPerSample = 16;
            WaveFormat.Channels = 1;
            WaveFormat.BlockAlign = (ushort)(WaveFormat.Channels * (WaveFormat.BitsPerSample / 8));
            WaveFormat.FormatTag = (ushort)WaveFormatType.Pcm;
            WaveFormat.SamplesPerSec = 16000;
            WaveFormat.AvgBytesPerSec = (ushort)(WaveFormat.SamplesPerSec * WaveFormat.Channels * (WaveFormat.BitsPerSample / 8));
            WaveFormat.Size = 0; // must be zero

            AudioBufferLength = 80;
            sourceAttributes = new Dictionary<MediaSourceAttributesKeys, string>();
            availableStreams = new List<MediaStreamDescription>();

            streamAttributes = new Dictionary<MediaStreamAttributeKeys, string>();
            /// Hex string of our wave format
            streamAttributes[MediaStreamAttributeKeys.CodecPrivateData] = WaveFormat.HexString;
           
            MediaStreamDescription = new MediaStreamDescription(MediaStreamType.Audio, streamAttributes);
            availableStreams.Add(MediaStreamDescription);

            // a zero timespan is an infinite video
            sourceAttributes[MediaSourceAttributesKeys.Duration] = TimeSpan.FromSeconds(0).Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture);
            sourceAttributes[MediaSourceAttributesKeys.CanSeek] = false.ToString();

        }

   
        public void Write(byte[] bAudioData)
        {
            int nCurrentQueueSize = 0;
            lock (AudioPacketLock)
            {
                AudioPackets.Enqueue(bAudioData);
                nCurrentQueueSize = AudioPackets.Count;
            }

            if ((HaveReportedSize == false) && (nCurrentQueueSize > MinimumQueueSize))
            {
                HaveReportedSize = true;
                ReportOpenMediaCompleted(sourceAttributes, availableStreams);
            }

            if (HostWaitingCount > 0)
                GetAudioSample(false); /// in case there are any async operations waiting on data
        }

        void client_OnAsyncConnectFinished(SocketClient client, bool bSuccess, string strErrors)
        {
            
        }


        protected override void GetSampleAsync(MediaStreamType mediaStreamType)
        {
            if (mediaStreamType == MediaStreamType.Audio)
            {
                GetAudioSample(true);
            }
            else if (mediaStreamType == MediaStreamType.Video)
            {
                //GetVideoSample();
            }
        }

        long m_nTimeStamp = 0;

        Dictionary<MediaSampleAttributeKeys, string> EmptyDictionary = new Dictionary<MediaSampleAttributeKeys, string>();

        public int PacketSize = 640;
        int HostWaitingCount = 0;
        object GetAudioSampleFunctionLock = new object();
        private void GetAudioSample(bool bDirect)
        {
            lock (GetAudioSampleFunctionLock)
            {
                if ( (bDirect == false) && (HostWaitingCount <= 0))
                    return;

                int nPackets = 1;
                MemoryStream ReceivedAudioStream = new MemoryStream();

                lock (AudioPacketLock)
                {
                    while (AudioPackets.Count > 0)
                    {
                        byte[] bPacket = AudioPackets.Dequeue();
                        ReceivedAudioStream.Write(bPacket, 0, bPacket.Length);
                        nPackets++;
                    }


                }


                if (ReceivedAudioStream.Length <= 0) // nothing available, have to call when the next sample is available
                {
                    if (bDirect == true)
                        HostWaitingCount++;
                    return;
                }

                if (bDirect == false)
                    HostWaitingCount--;

                ReceivedAudioStream.Seek(0, SeekOrigin.Begin);


                // Send out the next sample
                MediaStreamSample msSamp = new MediaStreamSample(
                    MediaStreamDescription,
                    ReceivedAudioStream,
                    0,
                    ReceivedAudioStream.Length,
                    m_nTimeStamp,
                    EmptyDictionary);

                m_nTimeStamp += 400000 * nPackets; // render time in in 100 nanosecond units
                ReportGetSampleCompleted(msSamp);
            }
        }

        protected override void SeekAsync(long seekToTime)
        {
            ReportSeekCompleted(seekToTime);
        }

        protected override void SwitchMediaStreamAsync(MediaStreamDescription mediaStreamDescription)
        {
            throw new NotImplementedException();
        }

        protected override void GetDiagnosticAsync(MediaStreamSourceDiagnosticKind diagnosticKind)
        {
            throw new NotImplementedException();
        }

        protected override void CloseMedia()
        {
            //WebClient.Stop();
            //WebClient.CancelAsync();   
        }


    }
}
