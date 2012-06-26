/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioClasses
{
    /// <summary>
    ///  A buffer filter.  Data that is pushed to this filter is not sent down the pipeline, but is put in a buffer where it collects until it is 
    ///  pulled from the other end.  The data may also be amplified, and aa minimum and maximum size for the buffer can be set
    /// </summary>
    public class BufferFilter : IAudioSource, IAudioSink
    {
        public BufferFilter(AudioFormat format)
        {
            AudioFormat = format;
        }

        public AudioFormat AudioFormat = null;

        private double m_fMutliplier = 1.0f;

        /// <summary>
        /// A multiplier applied to the samples to increase or decrease their amplitude
        /// </summary>
        public double AmplitudeMutliplier
        {
            get { return m_fMutliplier; }
            set { m_fMutliplier = value; }
        }

        private int m_nMaxSamples = -1;

        /// <summary>
        /// The maximum number of samples to buffer before discarding older samples
        /// </summary>
        public int MaxSamples
        {
            get { return m_nMaxSamples; }
            set { m_nMaxSamples = value; }
        }

        private int m_nMinSamples = -1;
        public int MinimumSamples
        {
            get { return m_nMinSamples; }
            set { m_nMinSamples = value; }
        }



        ByteBuffer QueueBuffer = new ByteBuffer();


        #region IAudioSource Members

        public MediaSample PullSample(AudioFormat format, TimeSpan tsDuration)
        {
            int nSamples = this.AudioFormat.CalculateNumberOfSamplesForDuration(tsDuration);
            MediaSample RetSample = new MediaSample(nSamples, this.AudioFormat);

            if (MinimumSamples != -1)  // See if we have enough min samples to send
            {
                if ((QueueBuffer.Size / AudioFormat.BytesPerSample) < MinimumSamples)
                    return RetSample;
            }

            byte[] bData = QueueBuffer.GetNSamples(RetSample.ByteLength);
            RetSample.Data = bData;
            return RetSample;
        }

        bool m_bSourceActive = true;
        public bool IsSourceActive
        {
            get
            {
                return m_bSourceActive;
            }
            set
            {
                m_bSourceActive = false;
            }
        }

        #endregion

        #region IAudioSink Members

        public void PushSample(MediaSample sample, object objSource)
        {
            // no conversion here, sample must be in 16x16 form

            if (AmplitudeMutliplier != 1.0f)
            {
                if (AudioFormat.AudioBitsPerSample == AudioBitsPerSample.Sixteen)
                {
                    short[] sSamples = sample.GetShortData();
                    for (int i = 0; i < sSamples.Length; i++)
                    {
                        sSamples[i] = (short)(AmplitudeMutliplier * sSamples[i]);
                    }
                    QueueBuffer.AppendData(Utils.ConvertShortArrayToByteArray(sSamples));
                }
                if (AudioFormat.AudioBitsPerSample == AudioBitsPerSample.Eight)
                {
                    byte[] bSamples = sample.Data;
                    for (int i = 0; i < bSamples.Length; i++)
                    {
                        bSamples[i] = (byte)(AmplitudeMutliplier * bSamples[i]);
                    }
                    QueueBuffer.AppendData(bSamples);
                }
            }
            else
            {
                QueueBuffer.AppendData(sample.Data);
            }

            int nSamplesInQueueBuffer = QueueBuffer.Size / AudioFormat.BytesPerSample;
            if ((MaxSamples > 0) && (nSamplesInQueueBuffer > MaxSamples))
            {
                int nBytesRemove = (nSamplesInQueueBuffer - MaxSamples) * AudioFormat.BytesPerSample;
                QueueBuffer.GetNSamples(nBytesRemove);
            }
        }

        bool m_bSinkActive = true;
        public bool IsSinkActive
        {
            get
            {
                return m_bSinkActive;
            }
            set
            {
                m_bSinkActive = false;
            }
        }

        #endregion

        #region IAudioSource Members

        double m_fSourceAmplitudeMultiplier = 1.0f;
        public double SourceAmplitudeMultiplier
        {
            get
            {
                return m_fSourceAmplitudeMultiplier;
            }
            set
            {
                m_fSourceAmplitudeMultiplier = value;
            }
        }

        #endregion

        #region IAudioSink Members

        double m_fSinkAmplitudeMultiplier = 1.0f;
        public double SinkAmplitudeMultiplier
        {
            get
            {
                return m_fSinkAmplitudeMultiplier;
            }
            set
            {
                m_fSinkAmplitudeMultiplier = value;
            }
        }

        #endregion
    }
}
