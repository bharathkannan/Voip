/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.


#include "stdafx.h"

#include "DirectShowFilters.h"


using namespace System;
using namespace AudioClasses;
using namespace DirectShowFilters;
using namespace System::Collections::Generic;
using namespace System::Runtime::InteropServices;

BOOL CALLBACK DSEnumProc(LPGUID lpGUID, LPCTSTR lpszDesc, LPCTSTR lpszDrvName, LPVOID lpContext)
{
   IntPtr ptr = IntPtr(lpContext);
   GCHandle handle = GCHandle::FromIntPtr(ptr);
   List<AudioDevice ^> ^DeviceList = (List<AudioDevice ^> ^) handle.Target;

   LPGUID lpTemp = NULL;
   System::Guid guid = System::Guid::Empty;

	if (lpGUID != NULL)  //  NULL only for "Primary Sound Driver".
	{
	  array<unsigned char> ^bBytes = gcnew array<unsigned char>(sizeof(GUID));
	  pin_ptr<unsigned char> ppbytes = &bBytes[0];
	  unsigned char *pbytes = (unsigned char *) ppbytes;
	  CopyMemory(pbytes, (void *)lpGUID, sizeof(GUID));

	  guid = Guid(bBytes);
	}
	AudioDevice ^device = gcnew AudioDevice(guid, gcnew String(lpszDesc));
	DeviceList->Add(device);

	return(TRUE);
}

array<AudioDevice ^> ^SpeakerFilter::GetSpeakerDevices()
{
	List<AudioDevice ^> ^DeviceList = gcnew List<AudioDevice ^>();
	GCHandle gchandle = GCHandle::Alloc(DeviceList);
    IntPtr ptr = GCHandle::ToIntPtr(gchandle);

	HRESULT hr = DirectSoundEnumerate((LPDSENUMCALLBACK)DSEnumProc, (VOID*)ptr.ToPointer());

	return DeviceList->ToArray();
}


bool SpeakerFilter::PushSample(MediaSample ^sample, System::Object ^objSource)
{
	if (sample == nullptr)
		return false;

	/// Make sure conversion worked
	if (sample->AudioFormat != this->Format)
		return false;

	/// zero out data
	if (m_bMute == true)
		System::Array::Clear(sample->Data, 0, sample->Data->Length);


	int nNewSize = ByteQueue->AppendData(sample->Data);

	return false;
}


bool SpeakerFilter::Stop()
{
	EventThreadExit->Set();
	if (PlayThread != nullptr)
	   this->PlayThread->Join();
	
	return false;
}

void SpeakerFilter::ClearBuffer()
{
   this->ByteQueue->Clear();
}


bool SpeakerFilter::Start()
{
    Stop();

	LPDIRECTSOUND8 pDs = NULL;

	/// Get our guid
	GUID guiddevice;
	array<unsigned char> ^bBytes = this->SpeakerGuid.ToByteArray();
	pin_ptr<unsigned char> ppbytes = &bBytes[0];
	unsigned char *pbytes = (unsigned char *) ppbytes;
	CopyMemory(&guiddevice, (void *)pbytes, sizeof(GUID));

   HRESULT hRes = DirectSoundCreate8( &guiddevice, &pDs, NULL );
	if (FAILED(hRes))
	{
		return false;
	}
	m_hDS = IntPtr(pDs);
	
	hRes = pDs->SetCooperativeLevel((HWND)Window.ToPointer(), DSSCL_PRIORITY);

   DSCAPS caps; 
   ZeroMemory(&caps, sizeof(DSCAPS));
   caps.dwSize = sizeof(DSCAPS);
	hRes = pDs->GetCaps(&caps);
	

    DSBUFFERDESC dsbd;
    dsbd.dwSize        = sizeof(DSBUFFERDESC);
    dsbd.dwFlags       = DSBCAPS_PRIMARYBUFFER;
    dsbd.dwBufferBytes = 0;
    dsbd.lpwfxFormat   = NULL;
	dsbd.dwReserved = 0;

	LPDIRECTSOUNDBUFFER pBufferPrimary;

	hRes = pDs->CreateSoundBuffer(&dsbd, &pBufferPrimary, NULL);
	if (FAILED(hRes))
	{
		Close();
		return false;
	}

    //    Set primary buffer format
    WAVEFORMATEX wfx;
	ZeroMemory(&wfx, sizeof(WAVEFORMATEX));
    wfx.wFormatTag      = WAVE_FORMAT_PCM;
    wfx.nChannels       = (int) this->Format->AudioChannels;
    wfx.nSamplesPerSec  = (int) this->Format->AudioSamplingRate;
    wfx.wBitsPerSample  = (int) this->Format->BytesPerSample*8;
    wfx.nBlockAlign     = (int) (wfx.wBitsPerSample / 8 * wfx.nChannels);
    wfx.nAvgBytesPerSec = wfx.nSamplesPerSec * wfx.nBlockAlign;
	wfx.cbSize = 0;

    hRes = pBufferPrimary->SetFormat(&wfx);
	if (FAILED(hRes))
	{
		Close();
		return false;
	}

	m_hBufferPrimary = IntPtr(pBufferPrimary);

	LPDIRECTSOUNDBUFFER pBufferSecondary;

    //    Create Secondary buffer
    dsbd.dwSize        = sizeof(DSBUFFERDESC);
    dsbd.dwFlags       = DSBCAPS_GLOBALFOCUS|DSBCAPS_CTRLVOLUME|
                         DSBCAPS_GETCURRENTPOSITION2|DSBCAPS_CTRLPOSITIONNOTIFY  |  DSBCAPS_LOCDEFER;
    dsbd.dwBufferBytes = m_nDSBufferSize;
    dsbd.lpwfxFormat   = &wfx;

	hRes = pDs->CreateSoundBuffer( &dsbd, &pBufferSecondary, NULL);
    if ( FAILED(hRes))
    {
		Close();
		return false;
    }

	/// Lock and zero out the secondary buffer
    void *lock1_ptr, *lock2_ptr;
    int lock1_bytes, lock2_bytes;
	hRes = pBufferSecondary->Lock(0, m_nDSBufferSize,
                                     &lock1_ptr, (LPDWORD)&lock1_bytes,
                                     &lock2_ptr, (LPDWORD)&lock2_bytes,
                                     DSBLOCK_ENTIREBUFFER);
    if (FAILED(hRes))
    {
		Close();
		return false;
    }
	m_hBufferSecondary = IntPtr(pBufferSecondary);

    ZeroMemory((void*)lock1_ptr, lock1_bytes);
    pBufferSecondary->Unlock(lock1_ptr,lock1_bytes,lock2_ptr,0);



    DSCAPS   dscaps = {0};
    ZeroMemory((void *)&dscaps, sizeof(DSCAPS));
    dscaps.dwSize = sizeof(DSCAPS);
	hRes = pDs->GetCaps(&dscaps);
    if (FAILED(hRes))
    {    
		Close();
		return false;
	}


   LPDIRECTSOUNDNOTIFY8 lpDsNotify; 
   DSBPOSITIONNOTIFY PositionNotify[3];
   HRESULT hr = pBufferSecondary->QueryInterface(IID_IDirectSoundNotify8, (LPVOID*)&lpDsNotify);
   if (SUCCEEDED(hr)) 
   { 
     PositionNotify[0].dwOffset = m_nDSBufferSize/3 - 1;
     PositionNotify[0].hEventNotify = (HANDLE)EventBuffer13Way; //SafeWaitHandle->DangerousGetHandle();
     PositionNotify[1].dwOffset = 2*m_nDSBufferSize/3 - 1;  
     PositionNotify[1].hEventNotify = (HANDLE)EventBuffer23Way; //SafeWaitHandle->DangerousGetHandle();
     PositionNotify[2].dwOffset = m_nDSBufferSize - 1;  
     PositionNotify[2].hEventNotify = (HANDLE)EventBuffer33Way; //SafeWaitHandle->DangerousGetHandle();
     hr = lpDsNotify->SetNotificationPositions(3, (DSBPOSITIONNOTIFY *) PositionNotify);
     lpDsNotify->Release();
   }


	/// Start our play thread
	PlayThread = gcnew Thread(gcnew ThreadStart(this, &SpeakerFilter::PlayThreadFunction));
	PlayThread->IsBackground = true;
	PlayThread->Name = String::Format("Audio play thread on device {0}", SpeakerGuid);
	PlayThread->Priority = System::Threading::ThreadPriority::Highest;
	EventThreadExit->Reset();
	PlayThread->Start();

	return true;
}


/// Play data in our queue
/// Wait until the queue is full before starting (at least m_nDSBufferSize in length),
/// then add half the data to the queue everytime our events are signaled
void SpeakerFilter::PlayThreadFunction()
{
	/// Wait until we get at least a buffer full of data before starting.   

	bool bStarted = false;
	 
	int nThirdSize = m_nDSBufferSize/3;
	int nTwoThirdSize = 2*m_nDSBufferSize/3;

	//bool bGotBytes = ByteQueue->WaitForSize(m_nDSBufferSize, Timeout::Infinite, EventThreadExit);
	bool bGotBytes = ByteQueue->WaitForSize(nThirdSize, Timeout::Infinite, EventThreadExit);
	if (bGotBytes == false) // since we waited forever, must have stopped playing
	{
		//System::Diagnostics::Debug::Assert(false);
		return;
	}

	array<unsigned char> ^StartData =ByteQueue->GetNSamples(m_nDSBufferSize);
	int nCopied = CopyDataToBuffer(StartData, 0);

	bool bSuccess;
	EventThreadExit->SafeWaitHandle->DangerousAddRef(bSuccess);

	HANDLE handles[4];
	handles[0] = (HANDLE) EventThreadExit->SafeWaitHandle->DangerousGetHandle();
	handles[1] = (HANDLE) EventBuffer13Way;
	handles[2] = (HANDLE) EventBuffer23Way;
	handles[3] = (HANDLE) EventBuffer33Way;

	array<unsigned char> ^NextData = gcnew array<unsigned char>(nThirdSize);
	Pause(false); /// start playing;
	int nWriteCursor = 0;
	do
	{
		NextData->Initialize();
		DWORD nHandle = ::WaitForMultipleObjects(4, handles, FALSE, INFINITE);

		if (nHandle == 0)
			break;
		else if (nHandle == 1)  // just finished playing to the 1/3 point
		{
			nWriteCursor = 0;
		}
		else if (nHandle == 2)  // just finished playing to the 1/3 point
		{
			nWriteCursor = nThirdSize;
		}
		else if (nHandle == 3)  // just finished playing to the 1/3 point
		{
			nWriteCursor = nTwoThirdSize;
		}


		/// See if our buffer has grown too big, if so, trim it down - trim it down enough (MaxQueueSamplesTrimSize) so it doesn't get big again
		int nNewLength = ByteQueue->Size;
		int nSamples = nNewLength/this->Format->BytesPerSample;

		if (m_bTrim == true)
		{
			int nLengthToTrim = (nSamples - (m_nMaxQueueSamples+MaxQueueSamplesTrimSize))*this->Format->BytesPerSample;
			if (nLengthToTrim > ByteQueue->Size)
				nLengthToTrim = ByteQueue->Size;
			if (nLengthToTrim > 0)
			{
#ifdef DEBUG
				System::Diagnostics::Debug::WriteLine("Removing {0} bytes from our speaker queue of size {1}", nLengthToTrim, ByteQueue->Size);
#endif
				ByteQueue->GetNSamples(nLengthToTrim);  // Flush this many samples
			}
		}



		int nSamplesGot = ByteQueue->GetNSamplesIntoBufferOrNone(NextData, nThirdSize);
		if (nSamplesGot < nThirdSize)  /// Didn't get enought data, clear noise from the buffer
			System::Array::Clear(NextData, nSamplesGot, nThirdSize-nSamplesGot);


		int nCopied = CopyDataToBuffer(NextData, nWriteCursor);

		if (m_bTrim == true)
		{
      	/// Copy what we just played to our output data, so if an echo canceller needs it they have it
			int nBytesInQueue = OutputQueue->AppendData(NextData);
 			nSamples = nBytesInQueue/this->Format->BytesPerSample;
			int nBytesToTrim = (nSamples - m_nMaxOutputSamples)*this->Format->BytesPerSample;
			if (nBytesToTrim > nBytesInQueue)
				nBytesToTrim = nBytesInQueue;
			if (nBytesToTrim > 0)
				OutputQueue->GetNSamples(nBytesToTrim);  // Flush this many samples
		}

	} while (1);

	EventThreadExit->SafeWaitHandle->DangerousRelease();
}


int SpeakerFilter::GetPlayPos()
{
	LPDIRECTSOUNDBUFFER pBufferSecondary = (LPDIRECTSOUNDBUFFER) m_hBufferSecondary.ToPointer();
    if (pBufferSecondary == NULL)
    {
        return -1;
    }
    
   System::Threading::Monitor::Enter(objLock);
	DWORD dwPos = 0;
	HRESULT hr = pBufferSecondary->GetCurrentPosition((LPDWORD)&dwPos, NULL);
    if (FAILED(hr))
    {
        dwPos = 0xFFFFFFFF;
    }
    System::Threading::Monitor::Exit(objLock);
    return (int) dwPos;
}
//
int SpeakerFilter::GetWritePos()
{
	LPDIRECTSOUNDBUFFER pBufferSecondary = (LPDIRECTSOUNDBUFFER) m_hBufferSecondary.ToPointer();
    if (pBufferSecondary == NULL)
    {
        return -1;
    }

	System::Threading::Monitor::Enter(objLock);

	DWORD dwPos = 0;
	HRESULT hr = pBufferSecondary->GetCurrentPosition(NULL, (LPDWORD)&dwPos);
    if (FAILED(hr))
    {
        dwPos = 0xFFFFFFFF;
    }
	System::Threading::Monitor::Exit(objLock);

    return (int) dwPos;
}


void SpeakerFilter::AddBuffer(array<unsigned char> ^bData)
{
	ByteQueue->AppendData(bData);
}


int SpeakerFilter::CopyDataToBuffer(array<unsigned char> ^bData, int nLocation)
{
	pin_ptr<unsigned char> ppData = &bData[0];
   unsigned char *pData = (unsigned char *) ppData;
	int nLength = bData->Length;
   
	int nBytesCopied = 0;
   LPDIRECTSOUNDBUFFER pBufferSecondary = (LPDIRECTSOUNDBUFFER) this->m_hBufferSecondary.ToPointer();

   void *lock1_ptr = NULL;
   void *lock2_ptr = NULL;
   int lock1_bytes = 0;
   int lock2_bytes = 0;

   HRESULT hRes = S_OK;

   if (!pBufferSecondary)
      return 0;

   if (nLength == 0)
      return 0;

   System::Threading::Monitor::Enter(objLock);

   if (pBufferSecondary == NULL)
   {
	   System::Threading::Monitor::Exit(objLock);
	   return 0;
   }

   hRes = pBufferSecondary->Lock(nLocation,
                                     nLength,
                                     &lock1_ptr, (LPDWORD)&lock1_bytes,
                                     &lock2_ptr, (LPDWORD)&lock2_bytes, 0);
   if (hRes == DSERR_BUFFERLOST) 
   {
      pBufferSecondary->Restore();
      hRes = pBufferSecondary->Lock(nLocation,
                                         nLength,
                                         &lock1_ptr, (LPDWORD)&lock1_bytes,
                                         &lock2_ptr, (LPDWORD)&lock2_bytes, 0);
      
	   if (hRes != S_OK)
	   {
		   System::Threading::Monitor::Exit(objLock);
		   return 0;
	   }
    }

//	System::Diagnostics::Debug::WriteLine(String::Format("Writing {0} bytes to lock1_ptr at {1}", lock1_bytes, nLocation)); 
	CopyMemory(lock1_ptr, pData, lock1_bytes);
	 nBytesCopied = lock1_bytes;
	 if  ( (lock1_bytes < nLength) && (lock2_ptr != NULL) )
	 {
//   	System::Diagnostics::Debug::WriteLine(String::Format("Writing {0} bytes to lock2_ptr at {1}", lock2_bytes, nLocation)); 
		 CopyMemory(lock2_ptr, pData+lock1_bytes, lock2_bytes);
		 nBytesCopied += lock2_bytes;
	 }

    hRes = pBufferSecondary->Unlock(lock1_ptr, lock1_bytes, lock2_ptr,0);
    if (hRes != S_OK)
    {
	  
		 System::Threading::Monitor::Exit(objLock);
	    return nBytesCopied;
    }

    System::Threading::Monitor::Exit(objLock);

    return nBytesCopied;
}




void SpeakerFilter::CopyZerosToBuffer(int nLength, int nLocation)
{
	LPDIRECTSOUNDBUFFER pBufferSecondary = (LPDIRECTSOUNDBUFFER) m_hBufferSecondary.ToPointer();
    if (pBufferSecondary == NULL)
        return;

    void *lock1_ptr = NULL;
    void *lock2_ptr = NULL;
    int lock1_bytes = 0;
    int lock2_bytes = 0;

    HRESULT hRes = S_OK;

    if (nLength == 0)
      nLength = m_nDSBufferSize;

    System::Threading::Monitor::Enter(objLock);

    hRes = pBufferSecondary->Lock(nLocation,
                                 nLength,
                                 &lock1_ptr, (LPDWORD)&lock1_bytes,
                                 &lock2_ptr, (LPDWORD)&lock2_bytes, 0);
    if (hRes = DSERR_BUFFERLOST) 
	{
        pBufferSecondary->Restore();
        hRes = pBufferSecondary->Lock(nLocation,
                                     nLength,
                                     &lock1_ptr, (LPDWORD)&lock1_bytes,
                                     &lock2_ptr, (LPDWORD)&lock2_bytes, 0);
    }
    if (FAILED(hRes)) 
	{
		System::Threading::Monitor::Exit(objLock);
		return;
	}

	 ZeroMemory(lock1_ptr, lock1_bytes);
	 if  ( (lock1_bytes < nLength) && (lock2_ptr != NULL) )
	 {
		 ZeroMemory(lock2_ptr, lock2_bytes);
	 }

    hRes = pBufferSecondary->Unlock(lock1_ptr, lock1_bytes, lock2_ptr,0);
    if (FAILED(hRes)) 
	{
		System::Threading::Monitor::Exit(objLock);
		return;
	}

    System::Threading::Monitor::Exit(objLock);
}


void SpeakerFilter::Pause(bool bPause)
{
	LPDIRECTSOUNDBUFFER pBufferSecondary = (LPDIRECTSOUNDBUFFER) m_hBufferSecondary.ToPointer();
    if (pBufferSecondary == NULL)
        return;

    System::Threading::Monitor::Enter(objLock);

    if (bPause)
        pBufferSecondary->Stop();
    else
        pBufferSecondary->Play( 0, 0xFFFFFFFF, DSBPLAY_LOOPING);

    System::Threading::Monitor::Exit(objLock);
}

void SpeakerFilter::SetVolume(int nVolume)
{
	LPDIRECTSOUNDBUFFER pBufferSecondary = (LPDIRECTSOUNDBUFFER) m_hBufferSecondary.ToPointer();
    if (pBufferSecondary == NULL)
        return;

	System::Threading::Monitor::Enter(objLock);
    //LONG previous_volume;
    //pBufferSecondary->GetVolume(&previous_volume);
    //nVolume = min(1,max(0,nVolume));
    //pBufferSecondary->SetVolume((LONG)(nVolume*(DSBVOLUME_MAX-DSBVOLUME_MIN)+DSBVOLUME_MIN));
	 pBufferSecondary->SetVolume((LONG)nVolume);
    System::Threading::Monitor::Exit(objLock);
}


int SpeakerFilter::GetVolume()
{
	LPDIRECTSOUNDBUFFER pBufferSecondary = (LPDIRECTSOUNDBUFFER) m_hBufferSecondary.ToPointer();
    if (pBufferSecondary == NULL)
        return -1;

    LONG previous_volume;
    System::Threading::Monitor::Enter(objLock);
    pBufferSecondary->GetVolume(&previous_volume);
    System::Threading::Monitor::Exit(objLock);
    return (int)(previous_volume-DSBVOLUME_MIN)/(DSBVOLUME_MAX-DSBVOLUME_MIN);
}

void SpeakerFilter::Reset()
{
   	LPDIRECTSOUNDBUFFER pBufferSecondary = (LPDIRECTSOUNDBUFFER) m_hBufferSecondary.ToPointer();
    if (pBufferSecondary == NULL)
        return;

    System::Threading::Monitor::Enter(objLock);

    void *lock1_ptr = NULL;
    void *lock2_ptr = NULL;
    int lock1_bytes = 0;
    int lock2_bytes = 0;

	if (FAILED(pBufferSecondary->Lock(0, m_nDSBufferSize,
                                         &lock1_ptr, (LPDWORD)&lock1_bytes,
                                         &lock2_ptr, (LPDWORD)&lock2_bytes,
                                         DSBLOCK_ENTIREBUFFER)))
    {
		System::Threading::Monitor::Exit(objLock);
		return;
    }

    ZeroMemory(lock1_ptr, lock1_bytes);
    pBufferSecondary->Unlock(lock1_ptr, lock1_bytes, lock2_ptr, 0);
    pBufferSecondary->SetCurrentPosition(0);

	System::Threading::Monitor::Exit(objLock);
}



void SpeakerFilter::Close()
{
 	LPDIRECTSOUNDBUFFER pBufferSecondary = (LPDIRECTSOUNDBUFFER) m_hBufferSecondary.ToPointer();
 	
	LPDIRECTSOUNDBUFFER pBufferPrimary = (LPDIRECTSOUNDBUFFER) m_hBufferPrimary.ToPointer();
	
	LPDIRECTSOUND8 pDs = (LPDIRECTSOUND8) m_hDS.ToPointer();

	System::Threading::Monitor::Enter(objLock);

    if (pBufferPrimary != NULL)
        pBufferPrimary->Play(0,0,DSBPLAY_LOOPING);

    if (pBufferSecondary != NULL) 
	{
        Pause(true);
        pBufferSecondary->Release();
        pBufferSecondary = NULL;
		this->m_hBufferSecondary = IntPtr::Zero;
    }

    if (pBufferPrimary != NULL) 
	{
        pBufferPrimary->Release();
        pBufferPrimary = NULL;
		this->m_hBufferPrimary = IntPtr::Zero;
    }

    if (pDs != NULL) 
	{
        pDs->Release();
        pDs = NULL;
		this->m_hDS = IntPtr::Zero;
    }


    System::Threading::Monitor::Exit(objLock);
}







BOOL CALLBACK DSCaptureEnumProc(LPGUID lpGUID, LPCTSTR lpszDesc, LPCTSTR lpcstrModule, LPVOID lpContext)
{
   IntPtr ptr = IntPtr(lpContext);
   GCHandle handle = GCHandle::FromIntPtr(ptr);
   List<AudioDevice ^> ^DeviceList = (List<AudioDevice ^> ^) handle.Target;

   LPGUID lpTemp = NULL;
   System::Guid guid = System::Guid::Empty;

	if (lpGUID != NULL)  //  NULL only for "Primary Sound Driver".
	{
	  array<unsigned char> ^bBytes = gcnew array<unsigned char>(sizeof(GUID));
	  pin_ptr<unsigned char> ppbytes = &bBytes[0];
	  unsigned char *pbytes = (unsigned char *) ppbytes;
	  CopyMemory(pbytes, (void *)lpGUID, sizeof(GUID));

	  guid = Guid(bBytes);
	}
	AudioDevice ^device = gcnew AudioDevice(guid, gcnew String(lpszDesc));
	DeviceList->Add(device);

	return(TRUE);
}

array<AudioDevice ^> ^MicrophoneFilter::GetMicrophoneDevices()
{
	List<AudioDevice ^> ^DeviceList = gcnew List<AudioDevice ^>();
	GCHandle gchandle = GCHandle::Alloc(DeviceList);
    IntPtr ptr = GCHandle::ToIntPtr(gchandle);

	HRESULT hr = DirectSoundCaptureEnumerate((LPDSENUMCALLBACK )DSCaptureEnumProc, (VOID*)ptr.ToPointer());

	return DeviceList->ToArray();
}


array<unsigned char> ^MicrophoneFilter::PullDataNotConnectedToFilterGraph()
{
		
	return ByteQueue->GetAllSamples();

}

MediaSample ^MicrophoneFilter::PullSample(TimeSpan tsDuration)
{
	int nSamples = Format->CalculateNumberOfSamplesForDuration(tsDuration);
	MediaSample ^sample = gcnew MediaSample(nSamples, Format);

	// Copy data from our queue
	int nBytes = nSamples*Format->BytesPerSample;
	if (ByteQueue->Size >= nBytes) /// make sure we have enough, otherwise return 0's until we're buffered
	   sample->Data = ByteQueue->GetNSamples(nBytes);
	
	/// zero out data if we are muted
	if (m_bMute == true)
		System::Array::Clear(sample->Data, 0, sample->Data->Length);

	return sample;
}



bool MicrophoneFilter::Stop()
{
	EventThreadExit->Set();
	return false;
}


bool MicrophoneFilter::Start()
{
   Stop();


   LPDIRECTSOUNDCAPTURE8 pDs = NULL;
	/// Get our guid
	GUID guiddevice;
	array<unsigned char> ^bBytes = this->MicrophoneGuid.ToByteArray();
	pin_ptr<unsigned char> ppbytes = &bBytes[0];
	unsigned char *pbytes = (unsigned char *) ppbytes;
	CopyMemory(&guiddevice, (void *)pbytes, sizeof(GUID));

   HRESULT hRes = DirectSoundCaptureCreate8( &guiddevice, &pDs, NULL );
	if (FAILED(hRes))
	{
		return false;
	}
	m_hDS = IntPtr(pDs);
	

   // Set primary buffer format
   WAVEFORMATEX wfx;
	ZeroMemory(&wfx, sizeof(WAVEFORMATEX));
   wfx.wFormatTag      = WAVE_FORMAT_PCM;
   wfx.nChannels       = (int) this->Format->AudioChannels;
   wfx.nSamplesPerSec  = (int) this->Format->AudioSamplingRate;
   wfx.wBitsPerSample  = (int) this->Format->BytesPerSample*8;
   wfx.nBlockAlign     = (int) (wfx.wBitsPerSample / 8 * wfx.nChannels);
   wfx.nAvgBytesPerSec = wfx.nSamplesPerSec * wfx.nBlockAlign;
	wfx.cbSize = 0;


    DSCBUFFERDESC dscbd;
    dscbd.dwSize        = sizeof(DSCBUFFERDESC);
    dscbd.dwFlags       = DSCBCAPS_WAVEMAPPED; 
    dscbd.dwBufferBytes = m_nDSBufferSize;
    dscbd.lpwfxFormat   = &wfx;
	 dscbd.dwFXCount = 0;
	 dscbd.lpDSCFXDesc = NULL;
	 dscbd.dwReserved = 0;

	LPDIRECTSOUNDCAPTUREBUFFER  pCaptureBuffer = NULL;

	hRes = pDs->CreateCaptureBuffer(&dscbd, &pCaptureBuffer, NULL);
	if (FAILED(hRes))
	{
		Close();
		return false;
	}

	LPDIRECTSOUNDCAPTUREBUFFER8 pCaptureBuffer8 = NULL;
	hRes = pCaptureBuffer->QueryInterface(IID_IDirectSoundCaptureBuffer8, (LPVOID *) &pCaptureBuffer8);
	if (FAILED(hRes))
	{
		Close();
	}
	pCaptureBuffer->Release(); /// we have the 8 version

  

	m_hBufferCapture = IntPtr(pCaptureBuffer8);

	
	LPDIRECTSOUNDNOTIFY8 lpDsNotify; 
   DSBPOSITIONNOTIFY PositionNotify[2];
   HRESULT hr = pCaptureBuffer8->QueryInterface(IID_IDirectSoundNotify8, (LPVOID*)&lpDsNotify);
   if (SUCCEEDED(hr)) 
   { 
     PositionNotify[0].dwOffset = (m_nDSBufferSize/2) - 1;
     PositionNotify[0].hEventNotify = (HANDLE)EventBufferHalfWay;
     PositionNotify[1].dwOffset = m_nDSBufferSize - 1;  
     PositionNotify[1].hEventNotify = (HANDLE)EventBufferFullWay;
     hr = lpDsNotify->SetNotificationPositions(2, (DSBPOSITIONNOTIFY *) PositionNotify);
     lpDsNotify->Release();
   }


	/// Start our play thread
	RecordThread = gcnew Thread(gcnew ThreadStart(this, &MicrophoneFilter::ReadMicFunction));
	RecordThread->IsBackground = true;
	RecordThread->Name = String::Format("Audio record thread on device {0}", this->MicrophoneGuid);
	RecordThread->Priority = System::Threading::ThreadPriority::Highest;
	EventThreadExit->Reset();
	RecordThread->Start();

	return true;
}


/// Play data in our queue
/// Wait until the queue is full before starting (at least m_nDSBufferSize in length),
/// then add half the data to the queue everytime our events are signaled
void MicrophoneFilter::ReadMicFunction()
{
	/// Wait until we get at least a buffer full of data before starting.   

	bool bStarted = false;
	 
	int nHalfSize = m_nDSBufferSize/2;

	bool bSuccess;
	EventThreadExit->SafeWaitHandle->DangerousAddRef(bSuccess);
	HANDLE handles[3];
	handles[0] = (HANDLE) EventThreadExit->SafeWaitHandle->DangerousGetHandle();
	handles[1] = (HANDLE) EventBufferHalfWay;
	handles[2] = (HANDLE) EventBufferFullWay;

	ResetEvent((HANDLE) EventBufferHalfWay);
	ResetEvent((HANDLE) EventBufferFullWay);

	Pause(false); /// start recording;

	int nReadLocation = 0;
	do
	{
		DWORD nHandle = ::WaitForMultipleObjects(3, handles, FALSE, -1);

		if (nHandle == 0)
			break;
		else if (nHandle == 1)  // just finished writing to the 1/3 point
		{
			nReadLocation = 0;
		}
		else if (nHandle == 2)  // just finished writing to the 2/3 point
		{
			nReadLocation = nHalfSize;
		}
		

		array<unsigned char> ^bNextData = ReadDataFromBuffer(nReadLocation, nHalfSize);
		if (bNextData != nullptr)
		{
			int nNow = ByteQueue->AppendData(bNextData);

			int nNewLength = ByteQueue->Size;
			int nSamples = nNewLength/this->Format->BytesPerSample;
			if (nSamples > m_nMaxQueueSamples)
			{
				int nLengthToGet = (nSamples - (m_nMaxQueueSamples-MaxQueueSamplesTrimSize))*this->Format->BytesPerSample;
				if (nLengthToGet > ByteQueue->Size)
					nLengthToGet = ByteQueue->Size;
				if (nLengthToGet > 0)
				{
				   ByteQueue->GetNSamples(nLengthToGet);
				}
			}

				
	      
		}
	
	} while (1);

	EventThreadExit->SafeWaitHandle->DangerousRelease();
}


void MicrophoneFilter::Pause(bool bPause)
{
   LPDIRECTSOUNDCAPTUREBUFFER8 pBufferSecondary = (LPDIRECTSOUNDCAPTUREBUFFER8) this->m_hBufferCapture.ToPointer();
    if (pBufferSecondary == NULL)
        return;

    System::Threading::Monitor::Enter(objLock);

    if (bPause)
        pBufferSecondary->Stop();
    else
		 pBufferSecondary->Start(DSCBSTART_LOOPING);

    System::Threading::Monitor::Exit(objLock);
}



array<unsigned char> ^MicrophoneFilter::ReadDataFromBuffer(int nLocation, int nLength)
{
  
	int nBytesCopied = 0;
   LPDIRECTSOUNDCAPTUREBUFFER8 pBufferSecondary = (LPDIRECTSOUNDCAPTUREBUFFER8) this->m_hBufferCapture.ToPointer();

   void *lock1_ptr = NULL;
   void *lock2_ptr = NULL;
   int lock1_bytes = 0;
   int lock2_bytes = 0;

   HRESULT hRes = S_OK;

   if (!pBufferSecondary)
      return nullptr;

   if (nLength == 0)
      return nullptr;

   System::Threading::Monitor::Enter(objLock);

   if (pBufferSecondary == NULL)
   {
	   System::Threading::Monitor::Exit(objLock);
	   return nullptr;
   }

   hRes = pBufferSecondary->Lock(nLocation,
                                     nLength,
                                     &lock1_ptr, (LPDWORD)&lock1_bytes,
                                     &lock2_ptr, (LPDWORD)&lock2_bytes, 0);
 

	int nTotalBytes = lock1_bytes + lock2_bytes;
	array<unsigned char> ^bData = gcnew array<unsigned char>(nTotalBytes);
	pin_ptr<unsigned char> ppData = &bData[0];
   unsigned char *pData = (unsigned char *) ppData;

	::CopyMemory(pData, lock1_ptr, lock1_bytes);
   if  ( (lock1_bytes < nLength) && (lock2_ptr != NULL) )
   {
      CopyMemory(pData+lock1_bytes, lock2_ptr, lock2_bytes);
	}


   hRes = pBufferSecondary->Unlock(lock1_ptr, lock1_bytes, lock2_ptr,0);

	if (nTotalBytes < nLength)
		System::Diagnostics::Debug::WriteLine(String::Format("**************** Could not read as many bytes as request, read {0} of {1}", nTotalBytes, nLength));

   if (hRes != S_OK)
   {
      System::Threading::Monitor::Exit(objLock);
      return nullptr;
   }

   System::Threading::Monitor::Exit(objLock);

    return bData;
}



void MicrophoneFilter::Reset()
{
     LPDIRECTSOUNDCAPTUREBUFFER8 pBufferSecondary = (LPDIRECTSOUNDCAPTUREBUFFER8) m_hBufferCapture.ToPointer();
    if (pBufferSecondary == NULL)
        return;

    System::Threading::Monitor::Enter(objLock);

 
	System::Threading::Monitor::Exit(objLock);
}



void MicrophoneFilter::Close()
{
 	LPDIRECTSOUNDCAPTUREBUFFER8 pBufferSecondary = (LPDIRECTSOUNDCAPTUREBUFFER8) m_hBufferCapture.ToPointer();
 	
	LPDIRECTSOUNDCAPTURE8 pDs = (LPDIRECTSOUNDCAPTURE8) m_hDS.ToPointer();

	System::Threading::Monitor::Enter(objLock);


   if (pBufferSecondary != NULL) 
	{
        Pause(true);
        pBufferSecondary->Release();
        pBufferSecondary = NULL;
		  this->m_hBufferCapture = IntPtr::Zero;
    }

   if (pDs != NULL) 
	{
      pDs->Release();
       pDs = NULL;
		this->m_hDS = IntPtr::Zero;
    }

    System::Threading::Monitor::Exit(objLock);
}
