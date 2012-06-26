/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioClasses
{
    public enum MediaType
    {
        Audio,  //audio
        Video,  //video
        RTP,   // not used in SDP, used for our filters though
        Text,  //text
        Application, //application
        Data,
        Control,
        Other
    }

    /// <summary>
    /// An object that audio can be pulled from
    /// </summary>
    public interface IAudioSource
    {
        MediaSample PullSample(AudioFormat format, TimeSpan tsDuration);
        bool IsSourceActive { get; set; }

        double SourceAmplitudeMultiplier { get; set; }

    }

    /// <summary>
    /// A device audio can be pushed to
    /// </summary>
    public interface IAudioSink
    {
        void PushSample(MediaSample sample, object objSource);
        bool IsSinkActive { get; set; }

        double SinkAmplitudeMultiplier { get; set; }
    }


    public class MediaSample
    {
        public MediaSample(MediaType type)
        {
            MediaType = type;
        }



        public MediaSample(MediaSample refsam, bool bShareData)
        {
            MediaType = refsam.MediaType;
            m_objAudioFormt = new AudioFormat(refsam.AudioFormat.AudioSamplingRate, refsam.AudioFormat.AudioBitsPerSample, refsam.AudioFormat.AudioChannels);

            if (bShareData == true)
                m_bData = refsam.Data;
            else
            {
                if (refsam.Data != null)
                {
                    m_bData = new byte[refsam.Data.Length];
                    Array.Copy(refsam.Data, m_bData, m_bData.Length);

                }
            }
        }

        public MediaSample Clone()
        {
            return new MediaSample(this, false);
        }

        public MediaSample(byte[] bData, AudioFormat format)
        {
            MediaType = MediaType.Audio;
            m_objAudioFormt = format;
            m_bData = bData;
        }

        public MediaSample(byte[] bData, VideoCaptureRate format)
        {
            MediaType = MediaType.Video;
            this.m_objVideoFormat = format;
            m_bData = bData;
        }

        public MediaSample(short[] sData, AudioFormat format)
        {
            MediaType = MediaType.Audio;
            m_objAudioFormt = format;
            m_bData = Utils.ConvertShortArrayToByteArray(sData);
        }

        public MediaSample(TimeSpan tsduration, AudioFormat format)
        {
            MediaType = MediaType.Audio;
            m_objAudioFormt = format;
            m_bData = new byte[m_objAudioFormt.CalculateNumberOfSamplesForDuration(tsduration) * BytesPerSample];
        }

        public MediaSample(int nNumberSamples, AudioFormat format)
        {
            MediaType = MediaType.Audio;
            m_objAudioFormt = format;
            Data = new byte[nNumberSamples * BytesPerSample];
        }

        public DateTime SampleStartTime = DateTime.MinValue;

        public readonly MediaType MediaType = MediaType.Other;
        AudioFormat m_objAudioFormt = AudioFormat.EightByEightThousandMono;
        public AudioFormat AudioFormat
        {
            get
            {
                return m_objAudioFormt;
            }
        }

        private VideoCaptureRate m_objVideoFormat;

        public VideoCaptureRate VideoFormat
        {
            get { return m_objVideoFormat; }
        }

        public TimeSpan Duration
        {
            get
            {
                if (MediaType == MediaType.Audio)
                    return AudioFormat.CalculateDurationForNumberOfSamples(NumberSamples);
                else
                    return VideoFormat.FrameDuration;
            }
        }



        public int BytesPerSample
        {
            get
            {
                return m_objAudioFormt.BytesPerSample;
            }
        }

        public int ByteLength
        {
            get
            {
                if (m_bData == null)
                    return 0;
                return m_bData.Length;
            }
        }

        public int ShortLength
        {
            get
            {
                if (m_bData == null)
                    return 0;
                return m_bData.Length / 2;
            }
        }

        public short[] GetShortData()
        {
            return Utils.ConvertByteArrayToShortArrayLittleEndian(m_bData);
        }

        public void SetDataShort(short[] sData)
        {
            this.Data = Utils.ConvertShortArrayToByteArray(sData);
        }

        
      
        public int NumberSamples
        {
            get
            {
                if (m_bData == null)
                    return 0;
                return m_bData.Length / AudioFormat.BytesPerSample;
            }
        }


        protected byte[] m_bData = null;
        public byte[] Data
        {
            get
            {
                return m_bData;
            }
            set
            {
                m_bData = value;
            }
        }


        public bool LastSample = false;

    }

}
