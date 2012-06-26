using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioClasses
{
    public class SimpleEchoCanceller
    {
        public SimpleEchoCanceller(AudioFormat format, TimeSpan tsDurationSave)
        {
            AudioFormat = format;

            IncomingSamples = format.CalculateNumberOfSamplesForDuration(tsDurationSave);
            IncomingData = new short[IncomingSamples];
            IncomingDataTemp = new short[IncomingSamples];
        }

        readonly int IncomingSamples = 0;
        int IncomingSamplesStored = 0;
        short[] IncomingData = null;
        short[] IncomingDataTemp = null;
        object DataLock = new object();

        public AudioFormat AudioFormat = AudioFormat.SixteenBySixteenThousandMono;

        int nConvergedSampleAt = 0;

        public void NewRTPInput(short[] sSamples)
        {
            lock (DataLock)
            {
                // append this data to our IncomingData buffer, discard oldest data by shifting array first if necessary
                if (sSamples.Length > (IncomingSamples - IncomingSamplesStored))
                {
                    /// shift data in the array to make room for new data
                    /// 
                    int nMove = sSamples.Length - (IncomingSamples - IncomingSamplesStored);
                    int nNewLength = IncomingSamples - sSamples.Length;
                    BlockMove(nMove, nNewLength);
                    Array.Copy(sSamples, 0, IncomingData, nNewLength, sSamples.Length);
                    IncomingSamplesStored = IncomingSamples;
                }
                else
                {
                    Array.Copy(sSamples, 0, IncomingData, IncomingSamplesStored, sSamples.Length);
                    IncomingSamplesStored += sSamples.Length;
                }
            }
        }

        void BlockMove(int nMove, int nNewLength)
        {
            Array.Copy(IncomingData, nMove, IncomingDataTemp, 0, nNewLength);
            Array.Copy(IncomingDataTemp, 0, IncomingData, 0, nNewLength);
        }

        public short[] EchoCancelSamples(short[] sSamples)
        {
            /// sSamples was just received from the mic, see if we see any of this in our incoming rtp stream, if so, try to remove it
            /// 
            if (IncomingSamplesStored <= sSamples.Length)
                return sSamples;

            int nSourceMaxIndex = 0;
            lock (DataLock)
            {
                float[] fXCorr = CrossCorrelation(IncomingData, IncomingSamplesStored, sSamples, out nSourceMaxIndex);
                float fAutoCorrValue = GetSelfCorrelationValue(IncomingData, nSourceMaxIndex, sSamples.Length);
				if (fAutoCorrValue <= 0)
					return sSamples;
				
                float fScale = fXCorr[nSourceMaxIndex] / fAutoCorrValue;
                if (fScale > .05) // ignore if we're not 5% of the value
                {
                    nConvergedSampleAt = nSourceMaxIndex;

                    /// Now subtract the scaled value from the input
                    for (int i = 0; i < sSamples.Length; i++)
                    {
                        sSamples[i] -= (short)((float)IncomingData[i + nSourceMaxIndex] * fScale);
                    }

                    // Now remove this samples
                    int nNewLength = IncomingSamplesStored - (sSamples.Length+nSourceMaxIndex);
                    BlockMove(sSamples.Length+nSourceMaxIndex, nNewLength);
                    IncomingSamplesStored = nNewLength;

                }

            }

            return sSamples;
        }


        public static float GetSelfCorrelationValue(short[] sInput)
        {
            return GetSelfCorrelationValue(sInput, 0, sInput.Length);
        }

        public static float GetSelfCorrelationValue(short []sInput, int nIndex, int nLen)
        {
            float fAutoCorrelationValue = 0.0f;
            for (int j = nIndex; j < nLen; j++)
            {
                fAutoCorrelationValue += sInput[j] * sInput[j];
            }
            float fShortMaxSquare = short.MaxValue * short.MaxValue;
            fAutoCorrelationValue = Math.Abs(fAutoCorrelationValue / fShortMaxSquare);
            return fAutoCorrelationValue;
        }

        public static void ScaleArray(float fFactor, short[] sInput)
        {
            for (int j = 0; j < sInput.Length; j++)
            {
                sInput[j] = (short) ((float)sInput[j] * fFactor);
            }
        }

        public static float[] CrossCorrelation(short[] sSource, int nSourceLength, short[] sSearch, out int nSourceMaxIndex)
        {
            if (sSearch.Length >= nSourceLength)
                throw new Exception("Search pattern length must be less than source length");
            nSourceMaxIndex = 0;
            float fMaxValue = 0.0f;

            //float[] fRet = new float[sSource.Length + sSearch.Length + 1];
            int nRetLen = nSourceLength - sSearch.Length;
            float[] fRet = new float[nRetLen];

            float fShortMaxSquare = short.MaxValue * short.MaxValue;

            for (int i = 0; i < nRetLen; i++)
            {
                fRet[i] = 0;
                for (int j = 0; j < sSearch.Length; j++)
                {
                    fRet[i] += (sSource[i+j] * sSearch[j]);
                }

                fRet[i] = Math.Abs(fRet[i] / fShortMaxSquare); 
                if (fRet[i] > fMaxValue)
                {
                    fMaxValue = fRet[i];
                    nSourceMaxIndex = i;
                }

            }

            return fRet;
        }

    }
}
