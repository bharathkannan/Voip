
#pragma once
using namespace System;

#ifdef USE_IPP

namespace ImageAquisition 
{
	public ref class Utils
	{
    public:
        /// Scales a short array so that the provided max value is of amplitude short::MaxValue
        static array<short>^ ScaleToMaxValue(array<short>^ SourcePCM, short nMaxValue);

        /// Eliminates DC components of a signal
        static array<short>^ EliminateDCOffsets(array<short>^ SourcePCM);

        static array<short>^ ReverseEndianInPlace(array<short>^ SourcePCM);

        /// Scales a short array so that the provided max value is of amplitude short::MaxValue
        static array<short>^ GenerateRandomUniformDistribution(int nLength, short nMinValue, short nMaxValue);

        /// Returns the absolute value of the max sample amplitude in the array
        static short MaxValue(array<short>^ SourcePCM);

        static array<short>^ RemoveAllSilence(array<short>^ SourcePCM);
        static array<short>^ RemoveAllSilence(array<short>^ SourcePCM, short sThresholdSample, int nConsecutiveForSilence);

		  /// Normalizes 8 or 16 bit data to floating point -1.0 to 1.0f
		  static array<float> ^ NormalizeData(array<unsigned char>^ SourcePCM, int nBitsPerSample);
        
        static array<short>^ ShortSample16000To8000(array<short>^ SourcePCM);
        static array<short>^ ShortSample8000To16000(array<short>^ SourcePCM);

		  static array<unsigned char>^ DownSample16To8Dither(array<short>^ SourcePCM);
		  static array<short>^ UpSample8BitTo16bit(array<unsigned char>^ SourcePCM);

		  static array<short>^ GenerateJaehne(double fMag, int nLength);

        static array<unsigned char>^ ConvertShortArrayToByteArray(array<short>^ SourcePCM);
        static array<short>^ ConvertByteArrayToShortArrayLittleEndian(array<unsigned char>^ SourcePCM);


		  // Convert our 16 bit samples to 24 bit samples, each stored as an integer
        static array<int>^ SixteenBitTo24Bit(array<short>^ SourcePCM);

  		  // Convert our 24 bit samples stored as integers to 16 bit samples stored as shorts
		  static array<short>^ TwentyFourBitTo16Bit(array<int>^ SourcePCM);


		  /// Upsample 16 bit data from 16 KHz to 48 KHz
        static array<short>^ Resample(array<short>^ SourcePCM, int nSourceSampleRate, int nDestSampleRate);


	};

	// Resamples blocks of a fixed sample size - introduces a delay 1/4 of the sample size
	public ref class SampleConvertor
	{
    public:
		 /// example... Downsampling from 48000 to 8000, a source factor of 48 and a dest factor of 8 would work for small sample sizes (sizes over 48)
		 /// for a larger sample size, 480 and 80 would work
		 SampleConvertor(int nSourceFactor, int nDestFactor, int nSampleSize);
		 SampleConvertor(int nSourceFactor, int nDestFactor, int nSampleSize, float fPreLowPassFilterFrequencyZeroToPointFive, float fPostLowPassFilterFrequency);
		 SampleConvertor(int nSourceFactor, int nDestFactor, int nSampleSize, int nStartEndBufferSize);
		 virtual ~SampleConvertor() ;

		 array<short>^ Convert(array<short>^ SourcePCM);
		 array<short>^ ConvertAnySize(array<short>^ SourcePCM);

	protected:

		array<short>^ DoubleSource;
		array<short>^ DoubleSourcePreFiltered;
		array<short>^ LastSource;
		array<short>^ Resample;					/// Resampled data on the DoubleSource between 0 and .5 
		array<short>^ ResampleAndFiltered;  /// Resampled data on the DoubleSource between 0 and .5 

		int StartEndBufferSize;
		int SampleSize;
		int ResampleSize;
		int ResampleSizeBuffer;
		int ResampleSizeWithBuffer;
		IntPtr Taps;
		IntPtr DelayLine;
		int filterLen;
		short delayLen;
		int UpSample;
		int DownSample;


		bool bDoPostLowPassFilter;
		IntPtr TapsPostLP;
		int PostLPFilterLen;
		IntPtr DelayLinePost;
		short delayLenPost;

		bool bDoPreLowPassFilter;
		IntPtr TapsPreLP;
		int PreLPFilterLen;
		IntPtr DelayLinePre;
		short delayLenPre;

	};


}

#endif