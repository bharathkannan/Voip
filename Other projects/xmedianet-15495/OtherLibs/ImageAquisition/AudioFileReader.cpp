/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.


#include "stdafx.h"
#include "AudioFileReader.h"
#include "SampleConverter.h"
#include <windows.h>
#include <Wia.h>
#include <stdio.h>

#include <Mfidl.h>
#include <Mfapi.h >
#include <Mfreadwrite.h>
#include <Mferror.h>
#include <strmif.h>


#include <exception>


using namespace System;
using namespace ImageAquisition;

AudioFileReader::AudioFileReader(AudioClasses::AudioFormat ^audioformat)
{
	m_fAmplitude = 0.5f;
	m_bActive = true;
	m_bIsPlaying = false;
	m_bFileProcessing = false;
	m_bAbortRead = false;
	m_bLoopQueue = false;
	m_strCurrentSong = "";
	m_nBytesInCurrentSong = 0;
	ThreadFinishedEvent = gcnew System::Threading::ManualResetEvent(true);

	FileQueue = gcnew System::Collections::Generic::Queue<String ^>();
	OutputAudioFormat = audioformat;
	EnqueuedAudioData = gcnew AudioClasses::ByteBuffer();
	HRESULT hr = CoInitializeEx(0, COINIT_MULTITHREADED);
	hr = MFStartup(MF_VERSION);
}

AudioFileReader::~AudioFileReader()
{
	EnqueuedAudioData->GetAllSamples();
	ThreadFinishedEvent->Close();
}


void AudioFileReader::EnqueueFile(String ^strSourceFile)
{
	FileQueue->Enqueue(strSourceFile);
	if (FileQueue->Count == 1)
		FirePropertyChanged("NextTrack");
	StartNextFileQueue();
}


void AudioFileReader::AbortCurrentSong()
{
	m_bAbortRead = true;
	EnqueuedAudioData->GetAllSamples();

	ThreadFinishedEvent->WaitOne(4000);
	FinishCurrentSong();
}

void AudioFileReader::ClearPlayQueue()
{
	FileQueue->Clear();
	m_bAbortRead = true;
}

void AudioFileReader::QueueFileThread(Object ^objAudioFileReader)
{
	AudioFileReader ^This = (AudioFileReader ^)objAudioFileReader;
	This->ThreadFinishedEvent->Reset();

	IMFSourceReader *pReader = NULL;
    IMFMediaType *pUncompressedAudioType = NULL;
    IMFMediaType *pPartialType = NULL;

	This->CurrentTrack = This->FileQueue->Dequeue();

	LPCWSTR pSourceFile = (LPCWSTR)System::Runtime::InteropServices::Marshal::StringToCoTaskMemUni(This->CurrentTrack).ToPointer(); 
	HRESULT hr = MFCreateSourceReaderFromURL(pSourceFile, NULL, &pReader);
    System::Runtime::InteropServices::Marshal::FreeCoTaskMem(System::IntPtr((void *)pSourceFile));

    if (FAILED(hr))
    {
		//throw new Exception("Error opening {0}, {1}", strSourceFile, hr);
		This->ThreadFinishedEvent->Set();
		return;
    }

	This->m_bAbortRead = false;

    // Select the first audio stream, and deselect all other streams.
    hr = pReader->SetStreamSelection((DWORD)MF_SOURCE_READER_ALL_STREAMS, FALSE);

    if (SUCCEEDED(hr))
        hr = pReader->SetStreamSelection((DWORD)MF_SOURCE_READER_FIRST_AUDIO_STREAM, TRUE);


    // Create a partial media type that specifies uncompressed PCM audio.
    hr = MFCreateMediaType(&pPartialType);

    if (SUCCEEDED(hr))
        hr = pPartialType->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Audio);

    if (SUCCEEDED(hr))
        hr = pPartialType->SetGUID(MF_MT_SUBTYPE, MFAudioFormat_PCM);

    // Set this type on the source reader. The source reader will
    // load the necessary decoder.
    if (SUCCEEDED(hr))
        hr = pReader->SetCurrentMediaType((DWORD)MF_SOURCE_READER_FIRST_AUDIO_STREAM, NULL, pPartialType);

    // Get the complete uncompressed format.
    if (SUCCEEDED(hr))
        hr = pReader->GetCurrentMediaType((DWORD)MF_SOURCE_READER_FIRST_AUDIO_STREAM, &pUncompressedAudioType);

    // Ensure the stream is selected.
    if (SUCCEEDED(hr))
        hr = pReader->SetStreamSelection((DWORD)MF_SOURCE_READER_FIRST_AUDIO_STREAM, TRUE);


	/// Figure out the sampling rate and bit size so we can do our conversion

	UINT32 cbBlockSize = MFGetAttributeUINT32(pUncompressedAudioType, MF_MT_AUDIO_BLOCK_ALIGNMENT, 0);
    UINT32 cbBytesPerSecond = MFGetAttributeUINT32(pUncompressedAudioType, MF_MT_AUDIO_AVG_BYTES_PER_SECOND, 0);
	UINT32 cbSamplesPerSecond = MFGetAttributeUINT32(pUncompressedAudioType, MF_MT_AUDIO_SAMPLES_PER_SECOND, 0);
	UINT32 cbBitsPerSample = MFGetAttributeUINT32(pUncompressedAudioType, MF_MT_AUDIO_BITS_PER_SAMPLE, 0);
	UINT32 cbChannels = MFGetAttributeUINT32(pUncompressedAudioType, MF_MT_AUDIO_NUM_CHANNELS, 0);



	if (pUncompressedAudioType != NULL)
	{
		pUncompressedAudioType->Release();
		pUncompressedAudioType = NULL;
	}
	if (pPartialType != NULL)
	{
		pPartialType->Release();
		pPartialType = NULL;
	}


	hr = S_OK;
    DWORD cbAudioData = 0;
    DWORD cbBuffer = 0;
    BYTE *pAudioData = NULL;

    IMFSample *pSample = NULL;
    IMFMediaBuffer *pBuffer = NULL;
	SampleConvertor ^Converter = nullptr;


	This->m_nBytesInCurrentSong = 0;
	This->IsPlaying = true;

	int nConvertSampleSize = 32000;
	Converter = gcnew SampleConvertor(cbSamplesPerSecond/100, ((int)This->OutputAudioFormat->AudioSamplingRate)/100, nConvertSampleSize);
	AudioClasses::ShortBuffer ^AudioDataWaitingToBeResampled = gcnew AudioClasses::ShortBuffer();

	int nMinSizeBuffer = This->OutputAudioFormat->CalculateNumberOfSamplesForDuration(TimeSpan::FromSeconds(10))*This->OutputAudioFormat->BytesPerSample;

    // Get audio samples from the source reader.
    while (This->m_bAbortRead == false)
    {
		/// Don't want to read in more than 10s of data at a time, so wait for our buffer to go below that size before continuing
		bool bBelowMinSize = This->EnqueuedAudioData->WaitForShrinkingSize(nMinSizeBuffer, 500, nullptr);
		if (bBelowMinSize == false)
			continue;

        DWORD dwFlags = 0;

        // Read the next sample.
        hr = pReader->ReadSample((DWORD)MF_SOURCE_READER_FIRST_AUDIO_STREAM, 0, NULL, &dwFlags, NULL, &pSample );

        if (FAILED(hr)) { break; }

        if (dwFlags & MF_SOURCE_READERF_CURRENTMEDIATYPECHANGED)
        {
            printf("Type change - not supported by WAVE file format.\n");
            break;
        }
        if (dwFlags & MF_SOURCE_READERF_ENDOFSTREAM)
        {
            printf("End of input file.\n");
            break;
        }

        if (pSample == NULL)
        {
            printf("No sample\n");
            continue;
        }

        // Get a pointer to the audio data in the sample.

        hr = pSample->ConvertToContiguousBuffer(&pBuffer);

        if (FAILED(hr)) { break; }


        hr = pBuffer->Lock(&pAudioData, NULL, &cbBuffer);

        if (FAILED(hr)) { break; }



		/// Convert from stereo to mono
		short *pShortAudioData = (short *)pAudioData;

		array<short> ^PCMShorts = nullptr;
		if (cbChannels == 2)
		{
			int nShortLength = cbBuffer/2; /// convert from byte length to short length

			PCMShorts = gcnew array<short>(nShortLength/2); /// half the size for stereo to mon

			int i=0;
			for (i=0; i<PCMShorts->Length; i++)  /// Average left and right
			{
				int nValue = ((int)pShortAudioData[i*2] + (int)pShortAudioData[i*2+1])/2;
				PCMShorts[i] = (short)nValue;
			}
		}
		else if (cbChannels == 1)
		{
			int nShortLength = cbBuffer/2; /// convert from byte length to short length

			PCMShorts = gcnew array<short>(nShortLength); /// 1 channel, same length
			pin_ptr<short> ppPCM = &PCMShorts[0];
			void *pPCM = (void *) ppPCM;
			CopyMemory(pPCM, pAudioData, cbBuffer);
		}
		else
		{
			break;
		}
		/// Resample if needed, then add to our outgoing array
		if (cbSamplesPerSecond != (int)This->OutputAudioFormat->AudioSamplingRate)
		{	
			AudioDataWaitingToBeResampled->AppendData(PCMShorts);

			while (true)
			{
				if (AudioDataWaitingToBeResampled->Size < nConvertSampleSize)
					break;
				array<short> ^bNextBlock =  AudioDataWaitingToBeResampled->GetNSamples(nConvertSampleSize);
				array<short> ^bResampledShorts = Converter->Convert(bNextBlock);

				array<unsigned char> ^bConverted = AudioClasses::Utils::ConvertShortArrayToByteArray(bResampledShorts);

				This->m_nBytesInCurrentSong += bConverted->Length;
				if (This->m_bAbortRead == false)
					This->EnqueuedAudioData->AppendData(bConverted);
			}
		}
		else
		{
			array<unsigned char> ^bConverted = AudioClasses::Utils::ConvertShortArrayToByteArray(PCMShorts);
			This->m_nBytesInCurrentSong += bConverted->Length;
			if (This->m_bAbortRead == false)
				This->EnqueuedAudioData->AppendData(bConverted);
		}


        // Unlock the buffer.
        hr = pBuffer->Unlock();
        pAudioData = NULL;

        if (FAILED(hr)) { break; }

        // Update running total of audio data.
        cbAudioData += cbBuffer;

		if (pSample != NULL)
		{
			pSample->Release();
			pSample = NULL;
		}

		if (pBuffer != NULL)
		{
			pBuffer->Release();
			pBuffer = NULL;
		}
    }

    if (pAudioData)
    {
        pBuffer->Unlock();
    }

	if (pSample != NULL)
	{
		pSample->Release();
		pSample = NULL;
	}
	if (pBuffer != NULL)
	{
		pBuffer->Release();
		pBuffer = NULL;
	}


	if ( (AudioDataWaitingToBeResampled->Size > 0) && (This->m_bAbortRead == false) )
	{
		array<short> ^bNextBlock =  AudioDataWaitingToBeResampled->GetNSamples(nConvertSampleSize);
		array<short> ^bResampledShorts = Converter->Convert(bNextBlock);

		array<unsigned char> ^bConverted = AudioClasses::Utils::ConvertShortArrayToByteArray(bResampledShorts);

		This->m_nBytesInCurrentSong += bConverted->Length;
		This->EnqueuedAudioData->AppendData(bConverted);
	}

	This->m_bAbortRead = false;
	This->m_bFileProcessing = false;
	This->ThreadFinishedEvent->Set();
}

void AudioFileReader::StartNextFileQueue()
{
	if (IsSourceActive == false)
		return;

	if ( (FileQueue->Count >= 1) && (EnqueuedAudioData->Size <= 0) && (m_bFileProcessing == false) )
	{
		m_bFileProcessing = true;
		System::Threading::ThreadPool::QueueUserWorkItem(gcnew System::Threading::WaitCallback(&AudioFileReader::QueueFileThread), this);
	}

	if ( (FileQueue->Count <= 0) && (EnqueuedAudioData->Size <= 0) && (m_bFileProcessing == false) )
		IsPlaying = false;
}

AudioClasses::MediaSample ^AudioFileReader::PullSample(AudioClasses::AudioFormat ^format, TimeSpan tsDuration)
{
	if (EnqueuedAudioData->Size <= 0)
	{
		return nullptr;
	}

	int nBytesNeeded = format->CalculateNumberOfSamplesForDuration(tsDuration)*format->BytesPerSample;


	if (EnqueuedAudioData->Size <= nBytesNeeded)
	{
		EnqueuedAudioData->GetAllSamples(); /// only a fraction left, ignore
		FinishCurrentSong();
		return nullptr;
	}

	array<unsigned char> ^bSamples = EnqueuedAudioData->GetNSamples(nBytesNeeded);
	
	if (EnqueuedAudioData->Size <= 0) /// did we finish this song?
	{
		FinishCurrentSong();
	}
	
	return gcnew AudioClasses::MediaSample(bSamples, format);
}

void AudioFileReader::FinishCurrentSong()
{
	FirePlayFinished(CurrentTrack);
	if (m_bLoopQueue == true)
		FileQueue->Enqueue(CurrentTrack);
	StartNextFileQueue();
}
