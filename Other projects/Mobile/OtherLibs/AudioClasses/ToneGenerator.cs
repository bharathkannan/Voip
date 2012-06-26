/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioClasses
{
    public class ToneGenerator : IAudioSource, IAudioSink
    {
        public ToneGenerator(int nF1, int nF2)
        {
            w1 = nF1 * 2.0 * Math.PI;
            w2 = nF2 * 2.0 * Math.PI;
        }

        double m_fDecibels = -10.0f;

        public double Decibels
        {
            get { return m_fDecibels; }
            set { m_fDecibels = value; }
        }


        #region IAudioSource Members

        double w1 = 350 * 2.0 * Math.PI;
        double w2 = 440 * 2.0 * Math.PI;
        int nSequence = 0;

        short[] BuildTonePayload(AudioFormat format, TimeSpan tsDuration)
        {
            int nSamples = format.CalculateNumberOfSamplesForDuration(tsDuration);

            short[] sPayload = new short[nSamples]; /// 320 samples for 20 ms packets

            if (w1 == w2)
            {
                double fAmplitude = Utils.SampleFromDb(short.MaxValue, m_fDecibels);
                for (int i = 0; i < nSamples; i++)
                {
                    double t = (i + nSequence) / 16000.0f;

                    sPayload[i] = (short)(fAmplitude * (Math.Sin(w1 * t)));
                }
            }
            else
            {
                double fAmplitude = Utils.SampleFromDbDualFrequency(short.MaxValue, m_fDecibels);

                for (int i = 0; i < nSamples; i++)
                {
                    double t = (i + nSequence) / 16000.0f;

                    sPayload[i] = (short)(fAmplitude * (Math.Sin(w1 * t) + Math.Sin(w2 * t)));
                }
            }

            nSequence += nSamples;
            return sPayload;
        }

        public MediaSample PullSample(AudioFormat format, TimeSpan tsDuration)
        {
            short[] sPayload = BuildTonePayload(format, tsDuration);
            return new MediaSample(sPayload, AudioFormat.SixteenBySixteenThousandMono);
        }

        bool m_bIsSourceActive = true;
        public bool IsSourceActive
        {
            get
            {
                return m_bIsSourceActive;
            }
            set
            {
                m_bIsSourceActive = value;
            }
        }

        #endregion

        #region IAudioSink Members

        /// <summary>
        ///  Push to our no where
        /// </summary>
        /// <param name="sample"></param>
        public void PushSample(MediaSample sample, object objSource)
        {
        }

        bool m_bIsSinkActive = false;
        public bool IsSinkActive
        {
            get
            {
                return m_bIsSinkActive;
            }
            set
            {
                m_bIsSinkActive = value;
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
