/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioClasses
{
    public class Utils
    {
     
        public static byte [] ConvertShortArrayToByteArray(short []sSource)
        {
	        byte [] bRet = new byte[sSource.Length*2];
            for (int i=0; i<sSource.Length; i++)
            {
                bRet[i*2] = (byte) (sSource[i]&0xFF);
                bRet[i*2 + 1] = (byte) ((sSource[i]&0xFF00)>>8);
            }
	        return bRet;
        }

        public static short [] ConvertByteArrayToShortArrayLittleEndian(byte []  bSource)
        {
	        short []sRet = new short[bSource.Length/2];
            for (int i = 0; i < sRet.Length; i++)
            {
                sRet[i] = (short)  ( (bSource[i*2+1]<<8) |  (bSource[i*2]));
            }
	        return sRet;
        }

        public static int[] MakeIntArrayFromShortArray(short[] sData)
        {
            int[] sIntData = new int[sData.Length];
            for (int i = 0; i < sData.Length; i++)
            {
                sIntData[i] = sData[i];
            }

            return sIntData;
        }

        public static short[] MakeShortArrayFromIntArray(int[] nData)
        {
            short[] sShortData = new short[nData.Length];
            for (int i = 0; i < nData.Length; i++)
            {
                sShortData[i] = (short)nData[i];
            }

            return sShortData;
        }

        /// <summary>
        /// COnvert the int array to a short array.  If any values in the array are greater than nMaxSampleValue (our clipping value), scale down the values.
        /// (Note, TODO, make these normalized doubles instead of integeters)
        /// </summary>
        /// <param name="nData"></param>
        /// <param name="nMaxSampleValue"></param>
        /// <returns></returns>
        public static short[] AGCAndShortArray(int[] nData, int nMaxSampleValue)
        {
            short[] sShortData = new short[nData.Length];
            int nMaxValueFound = 0;
            for (int i = 0; i < nData.Length; i++)
            {
                if (nData[i] > nMaxValueFound)
                    nMaxValueFound = nData[i];
            }

            if (nMaxValueFound > nMaxSampleValue)
            {
                double fScale = (double)nMaxSampleValue / (double)nMaxValueFound;
                for (int i = 0; i < nData.Length; i++)
                {
                    sShortData[i] = (short)(fScale*nData[i]);
                }
            }
            else
            {
                for (int i = 0; i < nData.Length; i++)
                {
                    sShortData[i] = (short)nData[i];
                }
            }

            return sShortData;
        }

        public static void SumArrays(int[] sCombined, short[] sData)
        {
            for (int i = 0; i < sCombined.Length; i++)
            {
                sCombined[i] += sData[i];
            }
        }


        public static void SubtractArray(int[] sCombined, short[] sData)
        {
            for (int i = 0; i < sCombined.Length; i++)
            {
                sCombined[i] -= sData[i];
            }
        }

        /// <returns></returns>
        public static double DBFromSample(int nSampleValue, int nSampleMax)
        {
            /// Ldb = 10 Log(A1*A1/A2*A2) = 20 Log(A1/A2), since power is proportionaly to the square of the amplitude
            double fRatio = (double)((double)nSampleValue / (double)nSampleMax);
            return Math.Log10(fRatio) * 20;
        }

        /// <summary>
        /// Figures out what a sample value would be at the given decibel level if the maximum sample value is nSampleMax
        /// </summary>
        /// <param name="nSampleMax"></param>
        /// <param name="fDb"></param>
        /// <returns></returns>
        public static int SampleFromDb(int nSampleMax, double fDb)
        {
            double fRatio = fDb / 20.0f;
            fRatio = Math.Pow(10.0f, fRatio);
            return (int)(nSampleMax * fRatio);
        }


        public static int SampleFromDbRMS(int nSampleMax, double fDb)
        {
            double fRatio = fDb / 20.0f;
            fRatio = Math.Pow(10.0f, fRatio);
            return (int)(nSampleMax * fRatio * Math.Sqrt(2.0f));
        }


        public static double CombinedDbForTwoFrequencies(double fDb1, double fDb2)
        {
            /// decibel levels don't add together linearly...
            ///  see for explanation: http://www.epd.gov.hk/epd/noise_education/web/ENG_EPD_HTML/m1/intro_5.html
            return 10 * Math.Log10(Math.Pow(10, fDb1 / 10.0f) + Math.Pow(10, fDb2 / 10.0f));
        }

        /// <summary>
        ///  Returns the db level of each individual frequency if together they should sum up to fDbDesired (opposite of above function CombinedDbForTwoFrequencies)
        /// </summary>
        /// <param name="fDb"></param>
        /// <returns></returns>
        public static double GetDbForTwoFrequenciesToMakeCombinedDbLevel(double fDbDesired)
        {
            return 10.0f * Math.Log10(Math.Pow(10.0f, fDbDesired / 10) / 2.0f);
        }

        public static int SampleFromDbDualFrequency(int nSampleMax, double fDb)
        {

            double fDbFreqs = GetDbForTwoFrequenciesToMakeCombinedDbLevel(fDb);


            double fRatio = fDbFreqs / 20.0f;
            fRatio = Math.Pow(10.0f, fRatio);
            return (int)(nSampleMax * fRatio);
        }

        public static int SampleFromDbDualFrequencyRMS(int nSampleMax, double fDb)
        {

            double fDbFreqs = GetDbForTwoFrequenciesToMakeCombinedDbLevel(fDb);


            double fRatio = fDbFreqs / 20.0f;
            fRatio = Math.Pow(10.0f, fRatio);
            return (int)(nSampleMax * fRatio * Math.Sqrt(2.0));
        }

        /// <summary>
        ///  Crude resampling functions.  Should apply a filter like we do in C++ with IPP, but use this for now
        /// </summary>
        /// <param name="SourcePCM"></param>
        /// <returns></returns>
        public static short[] Resample8000To16000(short[] SourcePCM)
        {
            int nSourceLen = SourcePCM.Length;
            short[] saRet = new short[SourcePCM.Length * 2];

            int nDestLen = saRet.Length;

            for (int i = 0; i < nDestLen - 2; i += 2)
            {
                saRet[i] = SourcePCM[i / 2];
                saRet[i + 1] = (short)((SourcePCM[(i / 2) + 1] + SourcePCM[i / 2]) / 2);
            }


            saRet[0] = SourcePCM[0];
            saRet[nDestLen - 1] = SourcePCM[nSourceLen - 1];

            return saRet;
        }
     
        
        public static short [] Resample16000To8000(short [] SourcePCM)
        {
	        short [] saRet = new short[SourcePCM.Length/2];

            int nDestLen = saRet.Length;
            for (int i = 0; i < nDestLen; i++)
            {
                saRet[i] = (short)((SourcePCM[(i*2) + 1] + SourcePCM[i*2]) / 2);
            }
	        return saRet;
        }

    }
}
