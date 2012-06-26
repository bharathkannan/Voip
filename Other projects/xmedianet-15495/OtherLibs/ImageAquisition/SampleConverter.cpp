



#include "stdafx.h"

#include "SampleConverter.h"

#ifdef USE_IPP
#ifdef SIXTYFOURBIT
	#define IPPAPI(type,name,arg) extern type __STDCALL y8_##name arg;
	#define IPPCALL(name) y8_##name
#else
	#define IPPAPI(type,name,arg) extern type __STDCALL w7_##name arg;
	#define IPPCALL(name) w7_##name
#endif


#include <ipps.h>
#include <ippi.h>
#include <ippj.h>
#include <ippcv.h>
#include <ippcc.h>
#endif

#include <float.h>
#include <memory.h>

#ifdef USE_IPP


array<float> ^ ImageAquisition::Utils::NormalizeData(array<unsigned char>^ SourcePCM, int nBitsPerSample)
{
	if (!( (nBitsPerSample == 8) || (nBitsPerSample == 16) ))
		throw gcnew Exception("Can only normalize 8 or 16 bit data");

    /// convert our source data to normalized floating point between -1.0f and 1.0f
  	 pin_ptr<unsigned char> pinnedSource = &SourcePCM[0];
    unsigned char*pSource = (unsigned char *) pinnedSource;

	 int nRetLength = SourcePCM->Length/(nBitsPerSample/8);
	 array<float> ^Return = gcnew array<float>(nRetLength);
  	 pin_ptr<float> pinnedDest = &Return[0];
    float* pDest = (float *) pinnedDest;

	 if (nBitsPerSample == 16)
       IPPCALL(ippsConvert_16s32f((Ipp16s *)(pSource), pDest, nRetLength)); 
	 else  if (nBitsPerSample == 8)
       IPPCALL(ippsConvert_8u32f((Ipp8u *)(pSource), pDest, nRetLength)); 

      /// Scale from -1 to 1
    float fMax = 1.0f/((float)(1<<(nBitsPerSample-1)));
    IPPCALL(ippsMulC_32f_I(fMax, pDest, nRetLength));

	 return Return;
}


array<short>^ ImageAquisition::Utils::GenerateRandomUniformDistribution(int nLength, short nMinValue, short nMaxValue)
{
   array<short>^saRet = gcnew array<short>(nLength);
   pin_ptr<short> pinnedSource = &saRet[0];
   short *pSource = (short *) pinnedSource;

   IppsRandUniState_16s *pRandState = 0;
   IPPCALL(ippsRandUniformInitAlloc_16s(&pRandState, nMinValue, nMaxValue, 23));

   IPPCALL(ippsRandUniform_16s(pSource, nLength, pRandState));

   IPPCALL(ippsRandUniformFree_16s(pRandState));

   return saRet;
}

array<short>^ ImageAquisition::Utils::EliminateDCOffsets(array<short>^ SourcePCM)
{
   int nLength = SourcePCM->Length;
   array<short>^saRet = gcnew array<short>(nLength);

   for (int i=0; i<nLength; i++)
   {
       if (i == 0)
       {
           saRet[i] = SourcePCM[i];
       }
       else
       {
          saRet[i] = SourcePCM[i] - SourcePCM[i-1] + 0.9*saRet[i-1];
       }
   }

   //   y(n) = x(n) - x(n-1) + R * y(n-1) 
// "R" between 0.9 .. 1
// n=current (n-1)=previous in/out value

   return saRet;
}


array<short>^ ImageAquisition::Utils::ReverseEndianInPlace(array<short>^ SourcePCM)
{
   int nLength = SourcePCM->Length;
   pin_ptr<short> pinnedRet = &SourcePCM[0];
   short *pRet = (short *) pinnedRet;

	IPPCALL(ippsSwapBytes_16u_I((Ipp16u*)pRet, nLength>>1));
	return SourcePCM;
}


/// Scales the short array so that the provide max value in the source array is scaled to short.maxvalue.  Everything
/// else is scaled linearly by the nMaxValue/short.MaxValue ration
array<short>^ ImageAquisition::Utils::ScaleToMaxValue(array<short>^ SourcePCM, short nMaxValue)
{
    int nLength = SourcePCM->Length;
    array<short>^saRet = gcnew array<short>(nLength);

    double shortmax = ((double)short::MaxValue);
    double shortmin = ((double)short::MinValue);
    for (int i=0; i<nLength; i++)
    {
        short sCurrentValue = SourcePCM[i];
        double fScale = ((double)sCurrentValue)/((double)nMaxValue);
        double fNewValue = fScale*shortmax;
        if (fNewValue > shortmax)
           saRet[i] = short::MaxValue;
        else if (fNewValue < shortmin)
           saRet[i] = short::MinValue;
        else
           saRet[i] = (short) Math::Floor(fNewValue);

    }

    return saRet;
}

short ImageAquisition::Utils::MaxValue(array<short>^ SourcePCM)
{
   short sMaxValue = 0;
   int nLength = SourcePCM->Length;
   for (int i=0; i<nLength; i++)
   {
       short suCurrentValue = Math::Abs(SourcePCM[i]);
       if (suCurrentValue > sMaxValue)
           sMaxValue = suCurrentValue;
   }

    return sMaxValue;
}

array<short>^ ImageAquisition::Utils::RemoveAllSilence(array<short>^ SourcePCM)
{
    return RemoveAllSilence(SourcePCM, 300, 5);
}

/// Attempts to remove all regions of silence from the wave
array<short>^ ImageAquisition::Utils::RemoveAllSilence(array<short>^ SourcePCM, short sThresholdSample, int nConsecutiveForSilence)
{
    int nLength = SourcePCM->Length;
    array<short>^saRet = gcnew array<short>(nLength);

    double shortmax = ((double)short::MaxValue);
    double shortmin = ((double)short::MinValue);

    int nSilenceCount = 0;
    bool bRemoving = false;
    int nAtInRetArray = 0;
    for (int i=0; i<nLength; i++)
    {
        short sCurrentValue = Math::Abs(SourcePCM[i]);
        if (sCurrentValue < sThresholdSample)
        {
            nSilenceCount++;
            if (nSilenceCount >= nConsecutiveForSilence)
                bRemoving = true;
        }
        else
        {
            nSilenceCount = 0;
            bRemoving = false;
        }

        if (bRemoving == false)
        {
            saRet[nAtInRetArray] = SourcePCM[i];
            nAtInRetArray++;
        }
    }

    array<short>^saRetForReal = gcnew array<short>(nAtInRetArray);
    Array::Copy(saRet, saRetForReal, nAtInRetArray);

    return saRetForReal;
}


array<short>^ ImageAquisition::Utils::ShortSample16000To8000(array<short>^ SourcePCM)
{
	array<short>^saRet = gcnew array<short>(SourcePCM->Length/2);
   pin_ptr<short> pinnedRet = &saRet[0];
   short *pRet = (short *) pinnedRet;

  	pin_ptr<short> pinnedSource = &SourcePCM[0];
   short *pSource = (short *) pinnedSource;

	int nPhase = 0;
	int nDestLen = saRet->Length;
	IPPCALL(ippsSampleDown_16s(pSource, SourcePCM->Length, pRet, &nDestLen, 2,&nPhase));

	return saRet;
}

#define NineBit 0x1FF
array<unsigned char>^ ImageAquisition::Utils::DownSample16To8Dither(array<short>^ SourcePCM)
{
	int nLength = SourcePCM->Length;
	array<unsigned char>^saRet = gcnew array<unsigned char>(nLength/2);
   pin_ptr<unsigned char> pinnedRet = &saRet[0];
   unsigned char *pRet = (unsigned char *) pinnedRet;

  	pin_ptr<short> pinnedSource = &SourcePCM[0];
   short *pSource = (short *) pinnedSource;
 

	Random ^Rand = gcnew Random();
	for (int i=0; i<nLength; i++)
	{
		int randomNumber = Rand->Next(NineBit);
	   if ((pSource[i] & 0x01FF) > randomNumber)
		   pRet[i] = (unsigned char) (((pSource[i] ^ 0x8000) | 0x0100) >> 8);
		else
			pRet[i] = (unsigned char) (((pSource[i] ^ 0x8000) & 0xFE00) >> 8);
	}

	return saRet;
}

array<short>^ ImageAquisition::Utils::UpSample8BitTo16bit(array<unsigned char>^ SourcePCM)
{
	int nLength = SourcePCM->Length;
	array<short>^saRet = gcnew array<short>(nLength);
   pin_ptr<short> pinnedRet = &saRet[0];
   short *pRet = (short*) pinnedRet;

  	pin_ptr<unsigned char> pinnedSource = &SourcePCM[0];
   unsigned char *pSource = (unsigned char *) pinnedSource;
 

	for (int i=0; i<nLength; i++)
	{
		
      pRet[i] = (((int)(pSource[i]-128))*256);
	}

	return saRet;
}

array<short>^ ImageAquisition::Utils::GenerateJaehne(double fMag, int nLength)
{
	array<short>^saRet = gcnew array<short>(nLength);
   pin_ptr<short> pinnedRet = &saRet[0];
   short *pRet = (short *) pinnedRet;

	/// Computer max sample value from db level
	double fRatio = fMag/20.0f;
   fRatio = Math::Pow(10.0f, fRatio);
   short nMag = ( System::Int16::MaxValue*fRatio);

	IPPCALL(ippsVectorJaehne_16s(pRet, nLength, nMag));

	return saRet;
}

array<short>^ ImageAquisition::Utils::ShortSample8000To16000(array<short>^ SourcePCM)
{
	int nSourceLen = SourcePCM->Length;
	array<short>^saRet = gcnew array<short>(SourcePCM->Length*2);
   pin_ptr<short> pinnedRet = &saRet[0];
   short *pRet = (short *) pinnedRet;

  	pin_ptr<short> pinnedSource = &SourcePCM[0];
   short *pSource = (short *) pinnedSource;

	int nPhase = 0;
	int nDestLen = saRet->Length;

	for (int i=0; i<nDestLen-1; i+=2)
	{
		pRet[i] = pSource[i/2];
		pRet[i+1] = (pSource[(i/2)+1] + pRet[i])/2;
	}



	//IPPCALL(ippsSampleUp_16s(pSource, SourcePCM->Length, pRet, &nDestLen, 2, &nPhase));

	///// Can't use... his puts in pops between packets... works for a continuous source though
	////	// Apply a simple low-pass filter to remove the high frequencies inserted
	//Ipp32f taps[] = {0.25f, 0.5f, 0.25f};
	//Ipp16s delayLine[] = {1, 0, 1 };
	//IppsFIRState32f_16s *pFIRState;
	//IPPCALL(ippsFIRInitAlloc32f_16s(&pFIRState, taps, 3, delayLine));
	//IPPCALL(ippsFIR32f_16s_ISfs(pRet, nDestLen-2, pFIRState, .7));
	//IPPCALL(ippsFIRFree32f_16s(pFIRState));

	pRet[0] = pSource[0];
	pRet[nDestLen-1] = pSource[nSourceLen-1];

	
	return saRet;
}




array<unsigned char>^ ImageAquisition::Utils::ConvertShortArrayToByteArray(array<short>^ SourcePCM)
{
	array<unsigned char>^saRet = gcnew array<unsigned char>(SourcePCM->Length*2);
   pin_ptr<unsigned char> pinnedRet = &saRet[0];
   unsigned char *pRet = (unsigned char *) pinnedRet;

  	pin_ptr<short> pinnedSource = &SourcePCM[0];
   unsigned char *pSource = (unsigned char *) pinnedSource;

	IPPCALL(ippsCopy_8u(pSource, pRet, saRet->Length));

	return saRet;
}

array<short>^ ImageAquisition::Utils::ConvertByteArrayToShortArrayLittleEndian(array<unsigned char>^ SourcePCM)
{
	array<short>^saRet = gcnew array<short>(SourcePCM->Length/2);
   pin_ptr<short> pinnedRet = &saRet[0];
   unsigned char *pRet = (unsigned char *) pinnedRet;

  	pin_ptr<unsigned char> pinnedSource = &SourcePCM[0];
   unsigned char *pSource = (unsigned char *) pinnedSource;

	IPPCALL(ippsCopy_8u(pSource, pRet, SourcePCM->Length));

	return saRet;
}



// Convert our 16 bit samples to 24 bit samples, each stored as an integer
array<int>^ ImageAquisition::Utils::SixteenBitTo24Bit(array<short>^ SourcePCM)
{
	array<int>^saRet = gcnew array<int>(SourcePCM->Length);
   pin_ptr<int> pinnedRet = &saRet[0];
   Ipp32s *pDst = (Ipp32s *) pinnedRet;

  	pin_ptr<short> pinnedSource = &SourcePCM[0];
   Ipp16s *pSrc = (Ipp16s *) pinnedSource;

	/// Convert our 16 bit soruce to a 32 bit dest
	IPPCALL(ippsConvert_16s32s(pSrc, pDst, SourcePCM->Length));

	/// mulitply by 256, so we are effectively on a 24 bit scale
	IPPCALL(ippsMulC_32s_ISfs(256, pDst, SourcePCM->Length, 1));

	
	return saRet;
}

// Convert our 24 bit samples stored as integers to 16 bit samples stored as shorts
array<short>^ ImageAquisition::Utils::TwentyFourBitTo16Bit(array<int>^ SourcePCM)
{
	array<short>^saRet = gcnew array<short>(SourcePCM->Length);
   pin_ptr<short> pinnedRet = &saRet[0];
   Ipp16s *pDst = (Ipp16s *) pinnedRet;

  	pin_ptr<int> pinnedSource = &SourcePCM[0];
   Ipp32s *pSrc = (Ipp32s *) pinnedSource;

	/// divide by 256, so we are on a 16 bit scale (from 24 bit)
	for (int i=0; i<SourcePCM->Length; i++)
		pSrc[i] /=256;
	//	IPPCALL(ippsMulC_32s_ISfs(1/256, pSrc, SourcePCM->Length, 1));

	/// Convert our 32 bit source to a 16 bit dest
	IPPCALL(ippsConvert_32s16s(pSrc, pDst, SourcePCM->Length));

	
	return saRet;
}



/// Upsample 16 bit data from 16 KHz to 48 KHz
array<short>^ ImageAquisition::Utils::Resample(array<short>^ SourcePCM, int nSourceSampleRate, int nDestSampleRate)
{
	int UpSample = nDestSampleRate/100;
	int DownSample = nSourceSampleRate/100;
	if (DownSample > SourcePCM->Length)  /// If we are doing small sample sizes, are upsample and downsample sizes must be smaller
	{
		UpSample = nDestSampleRate/1000;
		DownSample = nSourceSampleRate/1000;
	}

   int nSourceLen = SourcePCM->Length;
   pin_ptr<short> pinnedSource = &SourcePCM[0];
   short *pSource = (short *) pinnedSource;
	
   double fRatio = (double)UpSample/(double)DownSample;
   int nRetLen = Math::Ceiling(nSourceLen*fRatio);
	array<short>^saRet = gcnew array<short>(nRetLen);
   pin_ptr<short> pinnedRet = &saRet[0];
   short *pDest = (short *) pinnedRet;

	int nPhase = 0;
	int nDestLen = saRet->Length;

	int filterLen = UpSample*2-1;
	//Ipp32f *taps = IPPCALL(ippsMalloc_32f(filterLen));
	Ipp32f *taps = new Ipp32f[filterLen];

	IPPCALL(ippsVectorRamp_32f(taps, UpSample, 1.0/UpSample, 1.0/UpSample));
	IPPCALL(ippsVectorRamp_32f(taps+UpSample-1, UpSample, 1.0, -1.0/UpSample));
	Ipp16s delayLen = (filterLen+UpSample-1)/UpSample;
	//Ipp16s *pDelayLine = IPPCALL(ippsMalloc_16s(delayLen));
	Ipp16s *pDelayLine = new Ipp16s[delayLen];
	IPPCALL(ippsZero_16s(pDelayLine, delayLen));

	IppStatus status = IPPCALL(ippsFIRMR32f_Direct_16s_Sfs(pSource, pDest, SourcePCM->Length/DownSample, taps, filterLen, UpSample, 1, DownSample, 0, pDelayLine, 0));


	delete [] taps;
	delete [] pDelayLine;
	//IPPCALL(ippsFree(taps));
	//IPPCALL(ippsFree(pDelayLine));
	
	return saRet;
}



ImageAquisition::SampleConvertor::SampleConvertor(int nSourceFactor, int nDestFactor, int nSampleSize)
{
	SampleSize = nSampleSize;
	StartEndBufferSize = nSampleSize/2;
	if (StartEndBufferSize >  (SampleSize/2))
		StartEndBufferSize = SampleSize/2;

	DoubleSource = gcnew array<short>(SampleSize + 2*StartEndBufferSize);
	DoubleSourcePreFiltered = gcnew array<short>(SampleSize + 2*StartEndBufferSize);

	LastSource = gcnew array<short>(SampleSize);
	UpSample = nDestFactor;
	DownSample = nSourceFactor;
	
	ResampleSize = SampleSize*UpSample/DownSample;
	ResampleSizeBuffer = StartEndBufferSize*UpSample/DownSample;
	ResampleSizeWithBuffer = (SampleSize+2*StartEndBufferSize)*UpSample/DownSample;

	Resample = gcnew array<short>(ResampleSizeWithBuffer);
	ResampleAndFiltered = gcnew array<short>(ResampleSizeWithBuffer);

	filterLen = UpSample*2-1;
	//Ipp32f *taps = IPPCALL(ippsMalloc_32f(filterLen));
	Ipp32f *taps = new Ipp32f[filterLen];
	Taps = IntPtr(taps);


	IPPCALL(ippsVectorRamp_32f(taps, UpSample, 1.0/UpSample, 1.0/UpSample));
	IPPCALL(ippsVectorRamp_32f(taps+UpSample-1, UpSample, 1.0, -1.0/UpSample));
	delayLen = (filterLen+UpSample-1)/UpSample;
	//Ipp16s *pDelayLine = IPPCALL(ippsMalloc_16s(delayLen));
	Ipp16s *pDelayLine = new Ipp16s[delayLen];
	IPPCALL(ippsZero_16s(pDelayLine, delayLen));

	DelayLine = IntPtr(pDelayLine);
	TapsPostLP = System::IntPtr::Zero;
	TapsPreLP = System::IntPtr::Zero;
	bDoPreLowPassFilter = false;
	bDoPostLowPassFilter = false;
	DelayLinePre = System::IntPtr::Zero;
	DelayLinePost = System::IntPtr::Zero;
}

ImageAquisition::SampleConvertor::SampleConvertor(int nSourceFactor, int nDestFactor, int nSampleSize, float fPreLowPassFilterFrequencyZeroToPointFive, float fPostLowPassFilterFrequency)
{
	SampleSize = nSampleSize;
	StartEndBufferSize = nSampleSize/2;
	if (StartEndBufferSize >  (SampleSize/2))
		StartEndBufferSize = SampleSize/2;

	DoubleSource = gcnew array<short>(SampleSize + 2*StartEndBufferSize);
	DoubleSourcePreFiltered = gcnew array<short>(SampleSize + 2*StartEndBufferSize);
	LastSource = gcnew array<short>(SampleSize);
	UpSample = nDestFactor;
	DownSample = nSourceFactor;
	
	ResampleSize = SampleSize*UpSample/DownSample;
	ResampleSizeBuffer = StartEndBufferSize*UpSample/DownSample;
	ResampleSizeWithBuffer = (SampleSize+2*StartEndBufferSize)*UpSample/DownSample;

	Resample = gcnew array<short>(ResampleSizeWithBuffer);
	ResampleAndFiltered = gcnew array<short>(ResampleSizeWithBuffer);

	// isolated so we don't re-use variables
	{
		filterLen = UpSample*2-1;

	   //Ipp32f *taps = IPPCALL(ippsMalloc_32f(filterLen));
		Ipp32f *taps = new Ipp32f[filterLen];
	   Taps = IntPtr(taps);

		// for upsample 2 - filterlen = 3, taps[0] = .5, taps[1] = 1.0, taps[1] = 1.0, taps[2] = 1-.5 = 0.5
		// for upsample 4 - filterlen = 7, taps[0] = 1/4, taps[1] = 2/4, taps[2] = 3/4, taps[3] - 4/4 => 4/4 3/4, 2/4, 1/4
  	   IPPCALL(ippsVectorRamp_32f(taps, UpSample, 1.0/UpSample, 1.0/UpSample));
	   IPPCALL(ippsVectorRamp_32f(taps+UpSample-1, UpSample, 1.0, -1.0/UpSample));

		delayLen = (filterLen+UpSample-1)/UpSample;
		//Ipp16s *pDelayLine = IPPCALL(ippsMalloc_16s(delayLen));
		Ipp16s *pDelayLine = new Ipp16s[delayLen];
		IPPCALL(ippsZero_16s(pDelayLine, delayLen));

		DelayLine = IntPtr(pDelayLine);

   }


	TapsPostLP = System::IntPtr::Zero;
	TapsPreLP = System::IntPtr::Zero;
	DelayLinePre = System::IntPtr::Zero;
	DelayLinePost = System::IntPtr::Zero;

	// Pre-Low pass filter for down sampling, to remove frequencies that won't exist once the signal is down sampled
	bDoPreLowPassFilter = false;
	if (fPreLowPassFilterFrequencyZeroToPointFive < 0.5f)
	{
		bDoPreLowPassFilter = true;

		PreLPFilterLen = 2*(9+1);
		//Ipp32f *tapsfilteredPre = IPPCALL(ippsMalloc_32f(PreLPFilterLen));
		Ipp32f *tapsfilteredPre = new Ipp32f[PreLPFilterLen];
		TapsPreLP = IntPtr(tapsfilteredPre);

		Ipp64f pTapsPre[20];
		IPPCALL(ippsZero_64f(pTapsPre, PreLPFilterLen));

		delayLenPre = 2*PreLPFilterLen;
		//Ipp16s *pDelayLinePre = IPPCALL(ippsMalloc_16s(delayLenPre));
		Ipp16s *pDelayLinePre = new Ipp16s[delayLenPre];
		IPPCALL(ippsZero_16s(pDelayLinePre, delayLenPre));

		DelayLinePre = IntPtr(pDelayLinePre);

		IPPCALL(ippsFIRGenLowpass_64f(fPreLowPassFilterFrequencyZeroToPointFive, pTapsPre, PreLPFilterLen, ippWinHamming, ippTrue));
		for (int i=0; i<20; i++)
			tapsfilteredPre[i] = (Ipp32f) pTapsPre[i];
	}
	
	/// Low pass filter stuff for upsampling filters  (removes the newly inserted frequencies)
	bDoPostLowPassFilter = false;
	if (fPostLowPassFilterFrequency < 0.5f)
	{
		bDoPostLowPassFilter = true;

		PostLPFilterLen = 2*(9+1);
		//Ipp32f *tapsfilteredPost = IPPCALL(ippsMalloc_32f(PostLPFilterLen));
		Ipp32f *tapsfilteredPost = new Ipp32f[PostLPFilterLen];
		TapsPostLP = IntPtr(tapsfilteredPost);

		Ipp64f pTapsPost[20];
		IPPCALL(ippsZero_64f(pTapsPost, PostLPFilterLen));

		delayLenPost = 2*PostLPFilterLen;
		//Ipp16s *pDelayLinePost = IPPCALL(ippsMalloc_16s(delayLenPost));
		Ipp16s *pDelayLinePost = new Ipp16s[delayLenPost];
		IPPCALL(ippsZero_16s(pDelayLinePost, delayLenPost));
		
		DelayLinePost = IntPtr(pDelayLinePost);

		IPPCALL(ippsFIRGenLowpass_64f(fPostLowPassFilterFrequency, pTapsPost, PostLPFilterLen, ippWinHamming, ippTrue));
		for (int i=0; i<20; i++)
			tapsfilteredPost[i] = (Ipp32f) pTapsPost[i];
	}

}

ImageAquisition::SampleConvertor::SampleConvertor(int nSourceFactor, int nDestFactor, int nSampleSize, int nStartEndBufferSize)
{
	SampleSize = nSampleSize;
	StartEndBufferSize = nStartEndBufferSize;
	if (StartEndBufferSize >  (SampleSize/2))
		StartEndBufferSize = SampleSize/2;

	DoubleSource = gcnew array<short>(SampleSize + 2*StartEndBufferSize);
	DoubleSourcePreFiltered = gcnew array<short>(SampleSize + 2*StartEndBufferSize);

	LastSource = gcnew array<short>(SampleSize);
	UpSample = nDestFactor;
	DownSample = nSourceFactor;
	
	ResampleSize = SampleSize*UpSample/DownSample;
	ResampleSizeBuffer = StartEndBufferSize*UpSample/DownSample;
	ResampleSizeWithBuffer = (SampleSize+2*StartEndBufferSize)*UpSample/DownSample;

	Resample = gcnew array<short>(ResampleSizeWithBuffer);
	ResampleAndFiltered = gcnew array<short>(ResampleSizeWithBuffer);

	filterLen = UpSample*2-1;
	//Ipp32f *taps = IPPCALL(ippsMalloc_32f(filterLen));
	Ipp32f *taps = new Ipp32f[filterLen];
	Taps = IntPtr(taps);


	IPPCALL(ippsVectorRamp_32f(taps, UpSample, 1.0/UpSample, 1.0/UpSample));
	IPPCALL(ippsVectorRamp_32f(taps+UpSample-1, UpSample, 1.0, -1.0/UpSample));
	delayLen = (filterLen+UpSample-1)/UpSample;
	//Ipp16s *pDelayLine = IPPCALL(ippsMalloc_16s(delayLen));
	Ipp16s *pDelayLine = new Ipp16s[delayLen];
	IPPCALL(ippsZero_16s(pDelayLine, delayLen));

	DelayLine = IntPtr(pDelayLine);
	TapsPostLP = System::IntPtr::Zero;
	TapsPreLP = System::IntPtr::Zero;
	bDoPreLowPassFilter = false;
	bDoPostLowPassFilter = false;
	DelayLinePre = System::IntPtr::Zero;
	DelayLinePost = System::IntPtr::Zero;
}

ImageAquisition::SampleConvertor::~SampleConvertor()
{
	Ipp32f *taps = (Ipp32f *) Taps.ToPointer();
	Ipp16s *pDelayLine = (Ipp16s *) DelayLine.ToPointer();

	delete [] taps;
	delete [] pDelayLine;

	Taps = IntPtr::Zero;
	DelayLine = IntPtr::Zero;
	//IPPCALL(ippsFree(taps));
	//IPPCALL(ippsFree(pDelayLine));

	/// Delete pre-low pass filter taps
	if (TapsPreLP != System::IntPtr::Zero)
	{
		Ipp32f *tapslppre = (Ipp32f *) TapsPreLP.ToPointer();
   	//IPPCALL(ippsFree(tapslppre));
		delete [] tapslppre;
		TapsPreLP = System::IntPtr::Zero;
	}

	if (DelayLinePre != System::IntPtr::Zero)
	{
		Ipp16s *pDelayLinePre = (Ipp16s *) DelayLinePre.ToPointer();
   	//IPPCALL(ippsFree(pDelayLinePre));
		delete [] pDelayLinePre;
		DelayLinePre = System::IntPtr::Zero;
	}

	/// Delete the post-low pass filter taps
	if (TapsPostLP != System::IntPtr::Zero)
	{
		Ipp32f *tapslppost = (Ipp32f *) TapsPostLP.ToPointer();
   	//IPPCALL(ippsFree(tapslppost));
		delete [] tapslppost;
		TapsPostLP = System::IntPtr::Zero;
	}

	if (DelayLinePost != System::IntPtr::Zero)
	{
		Ipp16s *pDelayLinePost = (Ipp16s *) DelayLinePost.ToPointer();
   	//IPPCALL(ippsFree(pDelayLinePost));
		delete [] pDelayLinePost;
		DelayLinePost = System::IntPtr::Zero;
	}

}

/// Resample our block
array<short>^ ImageAquisition::SampleConvertor::Convert(array<short>^ SourcePCM)
{
	int nSourceLen = SourcePCM->Length;
	if (nSourceLen != SampleSize)
		throw gcnew Exception(System::String::Format("Incoming sample array must be of length {0}", SampleSize));

	/// Copy the new data to the end of the buffer
	Array::Copy(SourcePCM, 0,  DoubleSource, StartEndBufferSize*2, SampleSize);

	/// Copy the previous source to the beginning of the buffer
	Array::Copy(LastSource, SampleSize-StartEndBufferSize*2, DoubleSource, 0, StartEndBufferSize*2);

	/// Save the data for next time around
	Array::Copy(SourcePCM, 0,  LastSource, 0, SampleSize);



   pin_ptr<short> pinnedSource = &DoubleSource[0];
   short *pSource = (short *) pinnedSource;
	
	Ipp16s *pDelayLine = (Ipp16s *) DelayLine.ToPointer();


	/// Run our source data through a pre-filter (low-pass) to remove frequencies
	if (bDoPreLowPassFilter == true)
	{
	   pin_ptr<short> pinnedSourcePreFiltered = &DoubleSourcePreFiltered[0];
      short *pSourcePreFiltered = (short *) pinnedSourcePreFiltered;

		Ipp32f *tapsfilterpre = (Ipp32f *) TapsPreLP.ToPointer();

		Ipp16s *pDelayLinePre = (Ipp16s *) DelayLinePre.ToPointer();
		int nDelayLen = 0; /// This is the delay line index, not the length of the delay line//delayLenPre;
  	   IPPCALL(ippsFIR32f_Direct_16s_Sfs(pSource, pSourcePreFiltered, DoubleSource->Length, tapsfilterpre, PreLPFilterLen, pDelayLinePre, &nDelayLen, 0));

		/// Copy the newly filtered data back to pSource so we can resample
		IPPCALL(ippsCopy_16s(pSourcePreFiltered, pSource, DoubleSource->Length));
	}


	pin_ptr<short> pinnedDest = &Resample[0];
	short *pDest = (short *) pinnedDest;


	/// Hanning window inplace on our source
	//IPPCALL(ippsWinHann_16s_I(pSource, DoubleSource->Length));

	Ipp32f *taps = (Ipp32f *) Taps.ToPointer();


	Ipp16s sDelayLen = delayLen;
	IPPCALL(ippsFIRMR32f_Direct_16s_Sfs(pSource, pDest, DoubleSource->Length/DownSample, taps, filterLen, UpSample, 1, DownSample, 0, pDelayLine, 0));

	if (bDoPostLowPassFilter == true)
	{
		pin_ptr<short> pinnedDest2 = &ResampleAndFiltered[0];
		short *pDest2 = (short *) pinnedDest2;

		Ipp32f *tapsfilterpost = (Ipp32f *) TapsPostLP.ToPointer();

		/// Note:
		/// Kept getting a heap exception, activating gflags:
		/// C:\Program Files (x86)\Debugging Tools for Windows (x86)>gflags -p /enable "c:\users\AutoAdmin\Desktop\Documents\ITPMedia\VzPhone\bin\Debug\VzPhone.exe" /full
		/// Allow me to identify that the ippsFIR32f_Direct_16s_Sfs was going out of bounds.   
		/// Turns out I was passing in delayLenPost for nDelayLen instead of 0, causing the exception
		/// De-activate using
		/// gflags.exe -p /disable yourexecutable.exe


		Ipp16s *pDelayLinePost = (Ipp16s *) DelayLinePost.ToPointer();
		int nDelayLen = 0;//delayLenPost;
		IPPCALL(ippsFIR32f_Direct_16s_Sfs(pDest, pDest2, ResampleSizeWithBuffer, tapsfilterpost, PostLPFilterLen, pDelayLinePost, &nDelayLen, 0));

		array<short>^saRet = gcnew array<short>(ResampleSize);
		System::Array::Copy(ResampleAndFiltered, ResampleSizeBuffer, saRet, 0, ResampleSize);

		return saRet;
	}
	else
	{
		/// Copy over our resample data to our return buffer
		array<short>^saRet = gcnew array<short>(ResampleSize);
		System::Array::Copy(Resample, ResampleSizeBuffer, saRet, 0, ResampleSize);

		return saRet;
	}

}

array<short>^ ImageAquisition::SampleConvertor::ConvertAnySize(array<short>^ SourcePCM)
{
	int nSourceLen = SourcePCM->Length;

   pin_ptr<short> pinnedSource = &SourcePCM[0];
   short *pSource = (short *) pinnedSource;
	

	int TempResampleSize = nSourceLen*UpSample/DownSample;
	
	array<short>^saRet = gcnew array<short>(TempResampleSize);
	pin_ptr<short> pinnedDest = &saRet[0];
	short *pDest = (short *) pinnedDest;

	/// Hanning window inplace on our source
	//IPPCALL(ippsWinHann_16s_I(pSource, SourcePCM->Length));

	Ipp32f *taps = (Ipp32f *) Taps.ToPointer();
	Ipp16s *pDelayLine = (Ipp16s *) DelayLine.ToPointer();


	Ipp16s sDelayLen = delayLen;
	IPPCALL(ippsFIRMR32f_Direct_16s_Sfs(pSource, pDest, SourcePCM->Length/DownSample, taps, filterLen, UpSample, 1, DownSample, 0, pDelayLine, 0));
	
	return saRet;
}

#endif