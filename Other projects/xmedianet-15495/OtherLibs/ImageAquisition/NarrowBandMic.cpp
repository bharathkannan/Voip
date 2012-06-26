/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.


#include "StdAfx.h"
#include "NarrowBandMic.h"
#include "SampleConverter.h"



using namespace System;
using namespace AudioClasses;
using namespace ImageAquisition;
using namespace System::Collections::Generic;
using namespace System::Runtime::InteropServices;


//#include "MSRKinectAudio.h"

class CStaticMediaBuffer : public IMediaBuffer {
public:
   CStaticMediaBuffer() {}
   CStaticMediaBuffer(BYTE *pData, ULONG ulSize, ULONG ulData) :
      m_pData(pData), m_ulSize(ulSize), m_ulData(ulData), m_cRef(1) {}
   STDMETHODIMP_(ULONG) AddRef() { return 2; }
   STDMETHODIMP_(ULONG) Release() { return 1; }
   STDMETHODIMP QueryInterface(REFIID riid, void **ppv) {
      if (riid == IID_IUnknown) {
         AddRef();
         *ppv = (IUnknown*)this;
         return NOERROR;
      }
      else if (riid == IID_IMediaBuffer) {
         AddRef();
         *ppv = (IMediaBuffer*)this;
         return NOERROR;
      }
      else
         return E_NOINTERFACE;
   }
   STDMETHODIMP SetLength(DWORD ulLength) {m_ulData = ulLength; return NOERROR;}
   STDMETHODIMP GetMaxLength(DWORD *pcbMaxLength) {*pcbMaxLength = m_ulSize; return NOERROR;}
   STDMETHODIMP GetBufferAndLength(BYTE **ppBuffer, DWORD *pcbLength) {
      if (ppBuffer) *ppBuffer = m_pData;
      if (pcbLength) *pcbLength = m_ulData;
      return NOERROR;
   }
   void Init(BYTE *pData, ULONG ulSize, ULONG ulData) {
        m_pData = pData;
        m_ulSize = ulSize;
        m_ulData = ulData;
    }
protected:
   BYTE *m_pData;
   ULONG m_ulSize;
   ULONG m_ulData;
   ULONG m_cRef;
};

// Helper functions used to discover microphone array device
HRESULT GetMicArrayDeviceIndex(int *piDeviceIndex);
HRESULT GetJackSubtypeForEndpoint(IMMDevice* pEndpoint, GUID* pgSubtype);


// Helper functions used to generate output file
HRESULT DShowRecord(IMediaObject* pDMO, IPropertyStore* pPS, const TCHAR* outFile, int  iDuration);
HRESULT WriteToFile(HANDLE hFile, void* p, DWORD cb);
HRESULT WriteWaveHeader(HANDLE hFile, WAVEFORMATEX *pWav, DWORD *pcbWritten);
HRESULT FixUpChunkSizes(HANDLE hFile, DWORD cbHeader, DWORD cbAudioData);


HRESULT GetMicArrayDeviceIndex(int *piDevice)
{
    HRESULT hr = S_OK;
    UINT index, dwCount;
    IMMDeviceEnumerator* spEnumerator;
    IMMDeviceCollection* spEndpoints;

    *piDevice = -1;

    CHECKHR(CoCreateInstance(__uuidof(MMDeviceEnumerator),  NULL, CLSCTX_ALL, __uuidof(IMMDeviceEnumerator), (void**)&spEnumerator));

    CHECKHR(spEnumerator->EnumAudioEndpoints(eCapture, DEVICE_STATE_ACTIVE, &spEndpoints));

    CHECKHR(spEndpoints->GetCount(&dwCount));

    // Iterate over all capture devices until finding one that is a microphone array
    for (index = 0; index < dwCount; index++)
    {
        IMMDevice* spDevice;
		IPropertyStore *pProps = NULL;

        CHECKHR(spEndpoints->Item(index, &spDevice));
        
		
		   //CHECKHR(spDevice->OpenPropertyStore(
        //                  STGM_READ, &pProps));

        //PROPVARIANT varName;
        //// Initialize container for property value.
        //PropVariantInit(&varName);

        //// Get the endpoint's friendly-name property.
        //CHECKHR(pProps->GetValue(
        //               PKEY_Device_FriendlyName, &varName));


		//SAFE_RELEASE(pProps);
        //// Print endpoint friendly name and endpoint ID.
        //printf("Endpoint %d: \"%S\" (%S)\n",
        //       index, varName.pwszVal, pwszID);


        GUID subType = {0};
        CHECKHR(GetJackSubtypeForEndpoint(spDevice, &subType));
        if (subType == KSNODETYPE_MICROPHONE_ARRAY)
        {
            *piDevice = index;
            break;
        }


    }

    hr = (*piDevice >=0) ? S_OK : E_FAIL;

exit:
	SAFE_RELEASE(spEnumerator);
    SAFE_RELEASE(spEndpoints);    
    return hr;
}

#include "Functiondiscoverykeys_devpkey.h"

int GetSpeakerIndex(LPWSTR pGuid)
{
    HRESULT hr = S_OK;
    UINT index, dwCount;
    IMMDeviceEnumerator* spEnumerator;
    IMMDeviceCollection* spEndpoints;

    int nDevice = -1;

    CHECKHR(CoCreateInstance(__uuidof(MMDeviceEnumerator),  NULL, CLSCTX_ALL, __uuidof(IMMDeviceEnumerator), (void**)&spEnumerator));

    CHECKHR(spEnumerator->EnumAudioEndpoints(eRender, DEVICE_STATE_ACTIVE, &spEndpoints));

    CHECKHR(spEndpoints->GetCount(&dwCount));

    // Iterate over all capture devices until finding one that is a microphone array
    for (index = 0; index < dwCount; index++)
    {
        IMMDevice* spDevice;
		IPropertyStore *pProps = NULL;

        CHECKHR(spEndpoints->Item(index, &spDevice));

		LPWSTR pwszID = NULL;

		// Get the endpoint ID string.
        CHECKHR(spDevice->GetId(&pwszID));

		if (wcsstr (pwszID, pGuid) > 0)
			nDevice = index;
		//if (wcscmp (pwszID, pGuid) == 0)
//			nDevice = index;


		CoTaskMemFree(pwszID);
		SAFE_RELEASE(spDevice);

		if (nDevice != -1)
			break;
        //CHECKHR(spDevice->OpenPropertyStore(
        //                  STGM_READ, &pProps));

        //PROPVARIANT varName;
        //// Initialize container for property value.
        //PropVariantInit(&varName);

        //// Get the endpoint's friendly-name property.
        //CHECKHR(pProps->GetValue(
        //               PKEY_Device_FriendlyName, &varName));


		//SAFE_RELEASE(pProps);
        //// Print endpoint friendly name and endpoint ID.
        //printf("Endpoint %d: \"%S\" (%S)\n",
        //       index, varName.pwszVal, pwszID);


      
    }

exit:
	SAFE_RELEASE(spEnumerator);
    SAFE_RELEASE(spEndpoints);    
    
	return nDevice;
}

///////////////////////////////////////////////////////////////////////////////
// GetJackSubtypeForEndpoint
//
// Gets the subtype of the jack that the specified endpoint device is plugged
// into.  E.g. if the endpoint is for an array mic, then we would expect the
// subtype of the jack to be KSNODETYPE_MICROPHONE_ARRAY
//
///////////////////////////////////////////////////////////////////////////////
HRESULT GetJackSubtypeForEndpoint(IMMDevice* pEndpoint, GUID* pgSubtype)
{
    HRESULT hr = S_OK;
    IDeviceTopology*    spEndpointTopology = NULL;
    IConnector*         spPlug = NULL;
    IConnector*         spJack = NULL;
    IPart*            spJackAsPart = NULL;
    
    if (pEndpoint == NULL)
        return E_POINTER;
   
    // Get the Device Topology interface
    CHECKHR(pEndpoint->Activate(__uuidof(IDeviceTopology), CLSCTX_INPROC_SERVER, 
                            NULL, (void**)&spEndpointTopology));

    CHECKHR(spEndpointTopology->GetConnector(0, &spPlug));

    CHECKHR(spPlug->GetConnectedTo(&spJack));

	CHECKHR(spJack->QueryInterface(__uuidof(IPart), (void**)&spJackAsPart));

    hr = spJackAsPart->GetSubType(pgSubtype);

exit:
   SAFE_RELEASE(spEndpointTopology);
   SAFE_RELEASE(spPlug);    
   SAFE_RELEASE(spJack);    
   SAFE_RELEASE(spJackAsPart);
   return hr;
}//GetJackSubtypeForEndpoint()



#include <vcclr.h>

int NarrowBandMic::GetSpeakerDeviceIndex(String ^strDevice)
{
	int nId;
	pin_ptr<const wchar_t> wch = PtrToStringChars(strDevice); 
	return GetSpeakerIndex((LPWSTR)wch);
}

/// See notes in stdafx.h for why we have to redefine this
//const PROPERTYKEY PKEY_AudioEndpoint_GUID2 = {0x1da5d803, 0xd492, 0x4edd, 0x8c, 0x23, 0xe0, 0xc0, 0xff, 0xee, 0x7f, 0x0e};
//#define DEFINE_PROPERTYKEY(name, l, w1, w2, b1, b2, b3, b4, b5, b6, b7, b8, pid) EXTERN_C const PROPERTYKEY name = { { l, w1, w2, { b1, b2,  b3,  b4,  b5,  b6,  b7,  b8 } }, 4}
const PROPERTYKEY PKEY_AudioEndpoint_GUID2 = { { 0x1da5d803, 0xd492, 0x4edd, { 0x8c, 0x23,  0xe0,  0xc0,  0xff,  0xee,  0x7f,  0x0e } }, 4 };

array<AudioDevice ^> ^NarrowBandMic::GetMicrophoneDevices()
{
	System::Collections::Generic::List<AudioDevice ^> ^Devicelist = gcnew System::Collections::Generic::List<AudioDevice ^>();

    HRESULT hr = S_OK;
    UINT index, dwCount;
    IMMDeviceEnumerator* spEnumerator;
    IMMDeviceCollection* spEndpoints;

    CHECKHR(CoCreateInstance(__uuidof(MMDeviceEnumerator),  NULL, CLSCTX_ALL, __uuidof(IMMDeviceEnumerator), (void**)&spEnumerator));
    CHECKHR(spEnumerator->EnumAudioEndpoints(eCapture, DEVICE_STATE_ACTIVE, &spEndpoints));
    CHECKHR(spEndpoints->GetCount(&dwCount));

    // Iterate over all capture devices until finding one that is a microphone array
    for (index = 0; index < dwCount; index++)
    {
        IMMDevice* spDevice;
		IPropertyStore *pProps = NULL;

        hr = spEndpoints->Item(index, &spDevice);
		if (FAILED(hr))
			continue;
        
		
		//LPWSTR pwszID = NULL;
  //      hr = spDevice->GetId(&pwszID);
		//System::Guid guid = System::Guid::Parse(gcnew System::String(pwszID));
		//if (SUCCEEDED(hr))
		//   CoTaskMemFree(pwszID);


		hr = spDevice->OpenPropertyStore(STGM_READ, &pProps);
		if (FAILED(hr))
		{
			SAFE_RELEASE(spDevice);
			continue;
		}

        PROPVARIANT varName;
        PropVariantInit(&varName);
        hr = pProps->GetValue(PKEY_Device_FriendlyName, &varName);

        PROPVARIANT varGuid;
        PropVariantInit(&varGuid);
		hr = pProps->GetValue(PKEY_AudioEndpoint_GUID2, &varGuid);
		System::String ^strGuid = gcnew System::String(varGuid.pwszVal);
		System::Guid guid = System::Guid::Parse(strGuid);

	    SAFE_RELEASE(pProps);


		AudioDevice ^NextMicDevice = gcnew AudioDevice(guid, gcnew System::String(varName.pwszVal));
		NextMicDevice->DeviceType = DeviceType::Input;
		NextMicDevice->DeviceId = index;
		Devicelist->Add(NextMicDevice);

        PropVariantClear(&varName);
		PropVariantClear(&varGuid);

		SAFE_RELEASE(spDevice);

    }


exit:
	SAFE_RELEASE(spEnumerator);
    SAFE_RELEASE(spEndpoints);    
    return Devicelist->ToArray();
}


array<AudioDevice ^> ^NarrowBandMic::GetSpeakerDevices()
{
	System::Collections::Generic::List<AudioDevice ^> ^Devicelist = gcnew System::Collections::Generic::List<AudioDevice ^>();

    HRESULT hr = S_OK;
    UINT index, dwCount;
    IMMDeviceEnumerator* spEnumerator;
    IMMDeviceCollection* spEndpoints;

    CHECKHR(CoCreateInstance(__uuidof(MMDeviceEnumerator),  NULL, CLSCTX_ALL, __uuidof(IMMDeviceEnumerator), (void**)&spEnumerator));
    CHECKHR(spEnumerator->EnumAudioEndpoints(eRender, DEVICE_STATE_ACTIVE, &spEndpoints));
    CHECKHR(spEndpoints->GetCount(&dwCount));

    // Iterate over all capture devices until finding one that is a microphone array
    for (index = 0; index < dwCount; index++)
    {
        IMMDevice* spDevice;
		IPropertyStore *pProps = NULL;

        hr = spEndpoints->Item(index, &spDevice);
		if (FAILED(hr))
			continue;
        
		
		//LPWSTR pwszID = NULL;
  //      hr = spDevice->GetId(&pwszID);
		//System::Guid guid = System::Guid::Parse(gcnew System::String(pwszID));
		//if (SUCCEEDED(hr))
		//   CoTaskMemFree(pwszID);


		hr = spDevice->OpenPropertyStore(STGM_READ, &pProps);
		if (FAILED(hr))
		{
			SAFE_RELEASE(spDevice);
			continue;
		}

        PROPVARIANT varName;
        PropVariantInit(&varName);
        hr = pProps->GetValue(PKEY_Device_FriendlyName, &varName);

        PROPVARIANT varGuid;
        PropVariantInit(&varGuid);
		hr = pProps->GetValue(PKEY_AudioEndpoint_GUID2, &varGuid);
		System::String ^strGuid = gcnew System::String(varGuid.pwszVal);
		System::Guid guid = System::Guid::Parse(strGuid);

	    SAFE_RELEASE(pProps);


		AudioDevice ^NextMicDevice = gcnew AudioDevice(guid, gcnew System::String(varName.pwszVal));
		NextMicDevice->DeviceType = DeviceType::Output;
		NextMicDevice->DeviceId = index;
		Devicelist->Add(NextMicDevice);

        PropVariantClear(&varName);
		PropVariantClear(&varGuid);

		SAFE_RELEASE(spDevice);

    }


exit:
	SAFE_RELEASE(spEnumerator);
    SAFE_RELEASE(spEndpoints);    
    return Devicelist->ToArray();
}




array<unsigned char> ^NarrowBandMic::GetData()
{
	return ByteQueue->GetAllSamples();
}


MediaSample ^NarrowBandMic::PullSample(AudioFormat ^format, TimeSpan tsDuration)
{
	// See if we have enough samples in byte queue for the specified duration
	int nSamples = format->CalculateNumberOfSamplesForDuration(tsDuration);
	int nBytes = nSamples*format->BytesPerSample;
	if (ByteQueue->Size >= nBytes)
		return gcnew MediaSample(ByteQueue->GetNSamples(nBytes), format);
	return nullptr;
}


bool NarrowBandMic::Stop()
{
	m_bExit = true;
	if (RecordThread != nullptr)
	   this->RecordThread->Join();
	
	return false;
}

void NarrowBandMic::ClearBuffer()
{
	ByteQueue->GetAllSamples();
}


bool NarrowBandMic::Start()
{
    Stop();

	/// Initialize Kinect

	m_bExit = false;
	/// Start our play thread
	RecordThread = gcnew Thread(gcnew ThreadStart(this, &NarrowBandMic::ReadMicEchoCancellationFunction));
	RecordThread->IsBackground = true;
	RecordThread->Name = String::Format("MIC record thread on device {0}", SpeakerGuid);
	RecordThread->Priority = System::Threading::ThreadPriority::AboveNormal;
	RecordThread->Start();

	return true;
}

#ifdef USE_IPP		

bool NarrowBandMic::StartNoEchoCancellation()
{
    Stop();

	/// Initialize Kinect

	m_bExit = false;
	/// Start our play thread
	RecordThread = gcnew Thread(gcnew ThreadStart(this, &NarrowBandMic::ReadMicNoEchoFunction));
	RecordThread->IsBackground = true;
	RecordThread->Name = String::Format("MIC record thread on device {0}", SpeakerGuid);
	RecordThread->Priority = System::Threading::ThreadPriority::AboveNormal;
	RecordThread->Start();

	return true;
}


	
bool NarrowBandMic::StartRawMicMode()
{
    Stop();

	/// Initialize Kinect

	m_bExit = false;
	/// Start our play thread
	RecordThread = gcnew Thread(gcnew ThreadStart(this, &NarrowBandMic::ReadMicRawMode));
	RecordThread->IsBackground = true;
	RecordThread->Name = String::Format("Raw MIC Record thread on device {0}", SpeakerGuid);
	RecordThread->Priority = System::Threading::ThreadPriority::AboveNormal;
	RecordThread->Start();

	return true;
}
bool NarrowBandMic::StartSpeakerDeviceLoopBackMode()
{
    Stop();

	/// Initialize Kinect

	m_bExit = false;
	/// Start our play thread
	RecordThread = gcnew Thread(gcnew ThreadStart(this, &NarrowBandMic::ReadSpeakerAsMicInLoopBackModeFunction));
	RecordThread->IsBackground = true;
	RecordThread->Name = String::Format("Kinect record thread on device {0}", SpeakerGuid);
	RecordThread->Priority = System::Threading::ThreadPriority::AboveNormal;
	RecordThread->Start();

	return true;
}
#endif

/// Play data in our queue
/// Wait until the queue is full before starting (at least m_nDSBufferSize in length),
/// then add half the data to the queue everytime our events are signaled
void NarrowBandMic::ReadMicEchoCancellationFunction()
{
	HRESULT hr = S_OK;
    CoInitialize(NULL);
    int  iMicDevIdx = -1; 
	int  iSpkDevIdx = GetSpeakerDeviceIndex(SpeakerGuid.ToString());
	if (iSpkDevIdx == -1)
		iSpkDevIdx = 0;

    IMediaObject* pDMO = NULL;  
    IPropertyStore* pPS = NULL;
	ISoundSourceLocalizer* pSC = NULL;
    HANDLE mmHandle = NULL;
    DWORD mmTaskIndex = 0;


    // Set high priority to avoid getting preempted while capturing sound
    mmHandle = AvSetMmThreadCharacteristics(L"Audio", &mmTaskIndex);
    CHECK_BOOL(mmHandle != NULL, "failed to set thread priority\n");

    // DMO initialization
	 /// Don't have Kinect, use CLSID CLSID_CWMAudioAEC
	/// See http://msdn.microsoft.com/en-us/library/dd443455(v=VS.85).aspx

	if (UseKinectArray == true)
	{
		hr = CoCreateInstance(CLSID_CMSRKinectAudio, NULL, CLSCTX_INPROC_SERVER, IID_IMediaObject, (void**)&pDMO);
		if (FAILED(hr))
		{
			UseKinectArray = false;
			CHECKHR(CoCreateInstance(CLSID_CWMAudioAEC, NULL, CLSCTX_INPROC_SERVER, IID_IMediaObject, (void**)&pDMO));
		}
	}
	else
	{
      CHECKHR(CoCreateInstance(CLSID_CWMAudioAEC, NULL, CLSCTX_INPROC_SERVER, IID_IMediaObject, (void**)&pDMO));
	}
    CHECKHR(pDMO->QueryInterface(IID_IPropertyStore, (void**)&pPS));

    // Set AEC-MicArray DMO system mode.
    // This must be set for the DMO to work properly
    PROPVARIANT pvSysMode;
    PropVariantInit(&pvSysMode);
    pvSysMode.vt = VT_I4;
    //   SINGLE_CHANNEL_AEC = 0
    //   OPTIBEAM_ARRAY_ONLY = 2
    //   OPTIBEAM_ARRAY_AND_AEC = 4
    //   SINGLE_CHANNEL_NSAGC = 5
	pvSysMode.lVal = (LONG)(m_eMicrophoneMode);
//	if (UseKinectArray == false)
	//	pvSysMode.lVal = (LONG)(0);

    CHECKHR(pPS->SetValue(MFPKEY_WMAAECMA_SYSTEM_MODE, pvSysMode));
    PropVariantClear(&pvSysMode);

	iMicDevIdx = m_objAudioDevice->DeviceId;
 //   // Tell DMO which capture device to use (we're using whichever device is a microphone array).
 //   // Default rendering device (speaker) will be used.
 //   hr = GetMicArrayDeviceIndex(&iMicDevIdx);
	//if ((UseKinectArray == true) && (FAILED(hr)) )
 //      goto exit;
	//else if (FAILED(hr))
	//	iMicDevIdx = 0;
    
    PROPVARIANT pvDeviceId;
    PropVariantInit(&pvDeviceId);
    pvDeviceId.vt = VT_I4;
	//Speaker index is the two high order bytes and the mic index the two low order ones
    pvDeviceId.lVal = (unsigned long)(iSpkDevIdx<<16) | (unsigned long)(0x0000ffff & iMicDevIdx);
    CHECKHR(pPS->SetValue(MFPKEY_WMAAECMA_DEVICE_INDEXES, pvDeviceId));
    PropVariantClear(&pvDeviceId);


	// Added... Set the feature mode so we can tweak gain settings
   PROPVARIANT pvFeatureMode;
   PropVariantInit(&pvFeatureMode);
   pvFeatureMode.vt = VT_BOOL;
   pvFeatureMode.boolVal = VARIANT_TRUE;
   CHECKHR(pPS->SetValue(MFPKEY_WMAAECMA_FEATURE_MODE, pvFeatureMode));
   PropVariantClear(&pvFeatureMode);
	

	/// Added, set AES
 	PROPVARIANT pvAES;
   PropVariantInit(&pvAES);
   pvAES.vt = VT_I4;
   pvAES.lVal = 0; /// 0, 1, or 2... default is 0
   CHECKHR(pPS->SetValue(MFPKEY_WMAAECMA_FEATR_AES, pvAES));
   PropVariantClear(&pvAES);

	/// Added... AGC - default is off
	PROPVARIANT pvAGC;
	PropVariantInit(&pvAGC);
	pvAGC.vt = VT_BOOL;
	pvAGC.boolVal = m_bAGC?VARIANT_TRUE:VARIANT_FALSE;
	CHECKHR(pPS->SetValue(MFPKEY_WMAAECMA_FEATR_AGC, pvAGC));
	PropVariantClear(&pvAGC);

	///// Added... Noise supression, default is true
	//PROPVARIANT pvNoise;
	//PropVariantInit(&pvNoise);
	//pvNoise.vt = VT_I4;
	//pvNoise.lVal = m_bNoiseSupression?1:0;
	//CHECKHR(pPS->SetValue(MFPKEY_WMAAECMA_FEATR_NS, pvNoise));
	//PropVariantClear(&pvNoise);

	PROPVARIANT pvEchoLength;
	PropVariantInit(&pvEchoLength);
    pvEchoLength.vt = VT_I4;
    pvEchoLength.lVal = 512;  //128, 256, 512, 1024
    CHECKHR(pPS->SetValue(MFPKEY_WMAAECMA_FEATR_ECHO_LENGTH, pvEchoLength));
    PropVariantClear(&pvEchoLength);

	 
    DWORD cOutputBufLen = 0;
    BYTE *pbOutputBuffer = NULL;

    WAVEFORMATEX wfxOut = {WAVE_FORMAT_PCM, 1, 16000, 32000, 2, 16, 0};
	 CStaticMediaBuffer outputBuffer;
    DMO_OUTPUT_DATA_BUFFER OutputBufferStruct = {0};
    OutputBufferStruct.pBuffer = &outputBuffer;
    DMO_MEDIA_TYPE mt = {0};

    ULONG cbProduced = 0;
    DWORD dwStatus;

    // Set DMO output format
    hr = MoInitMediaType(&mt, sizeof(WAVEFORMATEX));
    CHECK_RET(hr, "MoInitMediaType failed");
    
    mt.majortype = MEDIATYPE_Audio;
    mt.subtype = MEDIASUBTYPE_PCM;
    mt.lSampleSize = 0;
    mt.bFixedSizeSamples = TRUE;
    mt.bTemporalCompression = FALSE;
    mt.formattype = FORMAT_WaveFormatEx;	
    memcpy(mt.pbFormat, &wfxOut, sizeof(WAVEFORMATEX));
    
    hr = pDMO->SetOutputType(0, &mt, 0); 
    CHECK_RET(hr, "SetOutputType failed");
    MoFreeMediaType(&mt);

    // Allocate streaming resources. This step is optional. If it is not called here, it
    // will be called when first time ProcessInput() is called. However, if you want to 
    // get the actual frame size being used, it should be called explicitly here.
    hr = pDMO->AllocateStreamingResources();
    CHECK_RET(hr, "AllocateStreamingResources failed");
    
    // Get actually frame size being used in the DMO. (optional, do as you need)
    int iFrameSize;
    PROPVARIANT pvFrameSize;
    PropVariantInit(&pvFrameSize);
    CHECKHR(pPS->GetValue(MFPKEY_WMAAECMA_FEATR_FRAME_SIZE, &pvFrameSize));
    iFrameSize = pvFrameSize.lVal;
    PropVariantClear(&pvFrameSize);

    // allocate output buffer
    cOutputBufLen = 2*iFrameSize; //wfxOut.nSamplesPerSec * wfxOut.nBlockAlign;
    pbOutputBuffer = new BYTE[cOutputBufLen];
    CHECK_ALLOC (pbOutputBuffer, "out of memory.\n");


	int totalBytes = 0;
	
	hr = pDMO->QueryInterface(IID_ISoundSourceLocalizer, (void**)&pSC);
	if (FAILED(hr))
		pSC = NULL;
	//CHECK_RET (hr, "QueryInterface for IID_ISoundSourceLocalizer failed");


    // main loop to get mic output from the DMO
    while (m_bExit == false)
    {
        Sleep(10); //sleep 10ms
		if (m_bExit == true)
			break;

        do
		{
            outputBuffer.Init((byte*)pbOutputBuffer, cOutputBufLen, 0);
            OutputBufferStruct.dwStatus = 0;
            hr = pDMO->ProcessOutput(0, 1, &OutputBufferStruct, &dwStatus);
            CHECK_RET (hr, "ProcessOutput failed. You must be rendering sound through the speakers before you start recording in order to perform echo cancellation.");

            if (hr == S_FALSE) 
			{
                cbProduced = 0;
            } 
			else 
			{
                hr = outputBuffer.GetBufferAndLength(NULL, &cbProduced);
                CHECK_RET (hr, "GetBufferAndLength failed");
            }
			
			totalBytes += cbProduced;

			if (cbProduced > 0)
			{
				array<unsigned char> ^bNextData = gcnew array<unsigned char>(cbProduced);
				pin_ptr<unsigned char> ppbNextData = &bNextData[0];
				unsigned char *pbNextData = (unsigned char *)ppbNextData;

				memcpy(pbNextData, pbOutputBuffer, cbProduced);
				ByteQueue->AppendData(bNextData);

				if (ByteQueue->Size >= 640*4)
					ByteQueue->GetNSamples(ByteQueue->Size-640*4);

			}


			if (pSC != NULL) /// May not have a sound localizer if we're not using a mic array
			{
				double dBeamAngle;
				double dConfidence;	
				double dPosition;	
				// Obtain beam angle from ISoundSourceLocalizer afforded by microphone array
				hr = pSC->GetBeam(&dBeamAngle);
				hr = pSC->GetPosition(&dPosition, &dConfidence);

				m_dBeamAngle = dBeamAngle;
				m_dConfidence = dConfidence;	
				m_dPosition = dPosition;	

				if(SUCCEEDED(hr))
				{								
				
					//Use a moving average to smooth this out
					if(m_dConfidence>0.9)
					{					
						//_tprintf(_T("Position: %f\t\tConfidence: %f\t\tBeam Angle = %f\r"), m_dPosition, m_dConfidence, m_dBeamAngle);					
					}
				}
			}

        } 
		while (OutputBufferStruct.dwStatus & DMO_OUTPUT_DATA_BUFFERF_INCOMPLETE);

    }

exit:
    SAFE_ARRAYDELETE(pbOutputBuffer);    
	SAFE_RELEASE(pSC);
	SAFE_RELEASE(pDMO);
    SAFE_RELEASE(pPS);

    AvRevertMmThreadCharacteristics(mmHandle);
    CoUninitialize();
}


/// Need Intel Performance Primitives to compile this since my audio resampler uses it
#ifdef USE_IPP


/// Play data in our queue
/// Wait until the queue is full before starting (at least m_nDSBufferSize in length),
/// then add half the data to the queue everytime our events are signaled
void NarrowBandMic::ReadMicNoEchoFunction()
{
	HRESULT hr = S_OK;
    CoInitialize(NULL);
    int  iMicDevIdx = -1; 

    IMediaObject* pDMO = NULL;  
    IPropertyStore* pPS = NULL;
    HANDLE mmHandle = NULL;
    DWORD mmTaskIndex = 0;


    // Set high priority to avoid getting preempted while capturing sound
    mmHandle = AvSetMmThreadCharacteristics(L"Audio", &mmTaskIndex);
    CHECK_BOOL(mmHandle != NULL, "failed to set thread priority\n");

    // DMO initialization
	 /// Don't have Kinect, use CLSID CLSID_CWMAudioAEC
	/// See http://msdn.microsoft.com/en-us/library/dd443455(v=VS.85).aspx


	CHECKHR(CoCreateInstance(CLSID_CWMAudioAEC, NULL, CLSCTX_INPROC_SERVER, IID_IMediaObject, (void**)&pDMO));
    CHECKHR(pDMO->QueryInterface(IID_IPropertyStore, (void**)&pPS));


	iMicDevIdx = m_objAudioDevice->DeviceId;
    

    PROPVARIANT pvSysMode;
    PropVariantInit(&pvSysMode);
    pvSysMode.vt = VT_I4;
	pvSysMode.lVal = 5;
    CHECKHR(pPS->SetValue(MFPKEY_WMAAECMA_SYSTEM_MODE, pvSysMode));
    PropVariantClear(&pvSysMode);


    PROPVARIANT pvDeviceId;
    PropVariantInit(&pvDeviceId);
    pvDeviceId.vt = VT_I4;
    pvDeviceId.lVal = (unsigned long)(0<<16) | (unsigned long)(0x0000ffff & iMicDevIdx);
    CHECKHR(pPS->SetValue(MFPKEY_WMAAECMA_DEVICE_INDEXES, pvDeviceId));
    PropVariantClear(&pvDeviceId);


	/// Added... AGC - default is off
	PROPVARIANT pvAGC;
	PropVariantInit(&pvAGC);
	pvAGC.vt = VT_BOOL;
	pvAGC.boolVal = m_bAGC?VARIANT_TRUE:VARIANT_FALSE;
	CHECKHR(pPS->SetValue(MFPKEY_WMAAECMA_FEATR_AGC, pvAGC));
	PropVariantClear(&pvAGC);

    DWORD cOutputBufLen = 0;
    BYTE *pbOutputBuffer = NULL;

    WAVEFORMATEX wfxOut = {WAVE_FORMAT_PCM, 1, 16000, 32000, 2, 16, 0};
	//WAVEFORMATEX wfxOut = {WAVE_FORMAT_PCM, 1, 48000, 96000, 2, 16, 0};
    CStaticMediaBuffer outputBuffer;
    DMO_OUTPUT_DATA_BUFFER OutputBufferStruct = {0};
    OutputBufferStruct.pBuffer = &outputBuffer;
    DMO_MEDIA_TYPE mt = {0};

    ULONG cbProduced = 0;
    DWORD dwStatus;

    // Set DMO output format
    hr = MoInitMediaType(&mt, sizeof(WAVEFORMATEX));
    CHECK_RET(hr, "MoInitMediaType failed");
    
    mt.majortype = MEDIATYPE_Audio;
    mt.subtype = MEDIASUBTYPE_PCM;
    mt.lSampleSize = 0;
    mt.bFixedSizeSamples = TRUE;
    mt.bTemporalCompression = FALSE;
    mt.formattype = FORMAT_WaveFormatEx;	
    memcpy(mt.pbFormat, &wfxOut, sizeof(WAVEFORMATEX));
    
    hr = pDMO->SetOutputType(0, &mt, 0); 
    CHECK_RET(hr, "SetOutputType failed");
    MoFreeMediaType(&mt);

    // Allocate streaming resources. This step is optional. If it is not called here, it
    // will be called when first time ProcessInput() is called. However, if you want to 
    // get the actual frame size being used, it should be called explicitly here.
    hr = pDMO->AllocateStreamingResources();
    CHECK_RET(hr, "AllocateStreamingResources failed");
    
    // Get actually frame size being used in the DMO. (optional, do as you need)
    int iFrameSize;
    PROPVARIANT pvFrameSize;
    PropVariantInit(&pvFrameSize);
    CHECKHR(pPS->GetValue(MFPKEY_WMAAECMA_FEATR_FRAME_SIZE, &pvFrameSize));
    iFrameSize = pvFrameSize.lVal;
    PropVariantClear(&pvFrameSize);

    // allocate output buffer
    cOutputBufLen = 2*iFrameSize; //wfxOut.nSamplesPerSec * wfxOut.nBlockAlign;
    pbOutputBuffer = new BYTE[cOutputBufLen];
    CHECK_ALLOC (pbOutputBuffer, "out of memory.\n");


	/// Use this is we need to resample our data (in this case from 16000 to 48000)
	//ImageAquisition::SampleConvertor ^Converter = gcnew ImageAquisition::SampleConvertor(16, 48, iFrameSize);

	int totalBytes = 0;
	

    // main loop to get mic output from the DMO
    while (m_bExit == false)
    {
        //Sleep(10); //sleep 10ms
		if (m_bExit == true)
			break;

        do
		{
            outputBuffer.Init((byte*)pbOutputBuffer, cOutputBufLen, 0);
            OutputBufferStruct.dwStatus = 0;
            hr = pDMO->ProcessOutput(0, 1, &OutputBufferStruct, &dwStatus);
            CHECK_RET (hr, "ProcessOutput failed. You must be rendering sound through the speakers before you start recording in order to perform echo cancellation.");

            if (hr == S_FALSE) 
			{
                cbProduced = 0;
            } 
			else 
			{
                hr = outputBuffer.GetBufferAndLength(NULL, &cbProduced);
                CHECK_RET (hr, "GetBufferAndLength failed");
            }
			
			totalBytes += cbProduced;

			if (cbProduced > 0)
			{
				array<unsigned char> ^bNextData = gcnew array<unsigned char>(cbProduced);
				pin_ptr<unsigned char> ppbNextData = &bNextData[0];
				unsigned char *pbNextData = (unsigned char *)ppbNextData;

				memcpy(pbNextData, pbOutputBuffer, cbProduced);
				ByteQueue->AppendData(bNextData);
				if (ByteQueue->Size >= 640*4)
					ByteQueue->GetNSamples(ByteQueue->Size-640*4);

				//// Upsample our data because we need 48KHz audio for aac.
				//// can't record at this rate because some of these devices don't support it
			 //   array<short> ^sData = ImageAquisition::Utils::ConvertByteArrayToShortArrayLittleEndian(bNextData);
	   //         array<short> ^sUpSample = Converter->Convert(sData);
				//array<unsigned char> ^bConverted = ImageAquisition::Utils::ConvertShortArrayToByteArray(sUpSample);
				//ByteQueue->AppendData(bConverted);

	/*			if (ByteQueue->Size > 96000*10) /// If we have more than 10 s data queued, somethings wrong, remove the first 9 seconds
				{
					ByteQueue->GetNSamples(96000*9);
				}*/
			}

        } 
		while (OutputBufferStruct.dwStatus & DMO_OUTPUT_DATA_BUFFERF_INCOMPLETE);

    }

exit:
    SAFE_ARRAYDELETE(pbOutputBuffer);    
	SAFE_RELEASE(pDMO);
    SAFE_RELEASE(pPS);

    AvRevertMmThreadCharacteristics(mmHandle);
    CoUninitialize();
}

#define REFTIMES_PER_SEC  10000000
#define REFTIMES_PER_MILLISEC  10000

const IID IID_IAudioClient = __uuidof(IAudioClient);
const IID IID_IAudioCaptureClient = __uuidof(IAudioCaptureClient);


void NarrowBandMic::ReadMicRawMode()
{
	///Open this speaker device as an IMMDevice
	HRESULT hr = S_OK;
    HANDLE mmHandle = NULL;
	UINT index, dwCount;
    IMMDeviceEnumerator* spEnumerator;
    IMMDeviceCollection* spEndpoints;
	IMMDevice* pOurMicDevice = NULL;
	UINT32 bufferFrameCount;
	IAudioClient *pAudioClient = NULL;
	REFERENCE_TIME hnsRequestedDuration = REFTIMES_PER_SEC;
    REFERENCE_TIME hnsActualDuration;
	IAudioCaptureClient *pCaptureClient = NULL;
    WAVEFORMATEX *pwfx = NULL;

    CoInitialize(NULL);

    CHECKHR(CoCreateInstance(__uuidof(MMDeviceEnumerator),  NULL, CLSCTX_ALL, __uuidof(IMMDeviceEnumerator), (void**)&spEnumerator));
    CHECKHR(spEnumerator->EnumAudioEndpoints(eCapture, DEVICE_STATE_ACTIVE, &spEndpoints));
    CHECKHR(spEndpoints->GetCount(&dwCount));


    // Iterate over all microphone devices until finding one that matches our guid
    for (index = 0; index < dwCount; index++)
    {
        IMMDevice* spDevice;
		IPropertyStore *pProps = NULL;
        CHECKHR(spEndpoints->Item(index, &spDevice));
		LPWSTR pwszID = NULL;

		hr = spDevice->OpenPropertyStore(STGM_READ, &pProps);
		if (FAILED(hr))
		{
			SAFE_RELEASE(spDevice);
			continue;
		}


		PROPVARIANT varGuid;
        PropVariantInit(&varGuid);
		hr = pProps->GetValue(PKEY_AudioEndpoint_GUID2, &varGuid);
		System::String ^strGuid = gcnew System::String(varGuid.pwszVal);
		System::Guid guid = System::Guid::Parse(strGuid);

	    SAFE_RELEASE(pProps);

		if (guid == m_objAudioDevice->Guid)
		{
			pOurMicDevice = spDevice;
			break;
		}



		SAFE_RELEASE(spDevice);
    }

	SAFE_RELEASE(spEnumerator);
    SAFE_RELEASE(spEndpoints);    
    
	if (pOurMicDevice == NULL)
		throw gcnew Exception("Can't find a speaker device with this GUID");



	hr = pOurMicDevice->Activate(IID_IAudioClient, CLSCTX_ALL, NULL, (void**)&pAudioClient);

	hr = pAudioClient->GetMixFormat(&pwfx);
    CHECK_RET(hr, "Failed to get mix format");


	hr = pAudioClient->Initialize(AUDCLNT_SHAREMODE_SHARED, 0, hnsRequestedDuration, 0, pwfx, NULL);
	if (hr == AUDCLNT_E_UNSUPPORTED_FORMAT)
		throw gcnew Exception("Unsupported format");

    CHECK_RET(hr, "Audio Client Initialize failed");


	  // Get the size of the allocated buffer.
    hr = pAudioClient->GetBufferSize(&bufferFrameCount);
    CHECK_RET(hr, "Failed to GetBufferSize()")


    hr = pAudioClient->GetService(IID_IAudioCaptureClient, (void**)&pCaptureClient);
    CHECK_RET(hr, "Failed to get IAudioCaptureClient service");


	ImageAquisition::SampleConvertor ^Converter = nullptr; //gcnew ImageAquisition::SampleConvertor(16, 48, bufferFrameCount);
   
	/// Have to figure out how to do a conversion between the native sample format and ours (48000 Mono)
	if (pwfx->nSamplesPerSec != 48000)
	{
		Converter = nullptr;
		//Converter = gcnew ImageAquisition::SampleConvertor(pwfx->nSamplesPerSec/100, 480, bufferFrameCount);
	}


	int totalBytes = 0;
	BYTE *pData;
	DWORD flags;
    
	
	hr = pAudioClient->Start();
	unsigned int packetLength = 0;
	unsigned int numFramesAvailable = 0;

	int nOutputByteSize = 48000*2/50; /// Final format is 48000 Hz, 2 byte samples, in 20 ms intervals (50 intervals/s)
	int nOuputShortSize = 48000/50;
	array<unsigned char> ^bNextData = gcnew array<unsigned char>(nOutputByteSize); 

	/// Step 1.  A buffer to store native data in mono form by combining left and right channels
	int nIncomingBytesPerSample = pwfx->wBitsPerSample/8;
	array<unsigned char> ^bMonoIncomingData = gcnew array<unsigned char>(pwfx->nSamplesPerSec*nIncomingBytesPerSample/50); /// Final format is 48000 Hz, 2 byte samples, in 20 ms intervals (50 intervals/s)
	array<unsigned char> ^bSource = nullptr;

	ByteBuffer ^ConvertorQueue = gcnew ByteBuffer();

	while (m_bExit == false)
    {
		// Copy the available capture data to the audio sink.
		pin_ptr<unsigned char> ppbNextData = &bNextData[0];
		unsigned char *pNextData = (unsigned char *)ppbNextData;

		pin_ptr<unsigned char> ppbMonoIncomingData = &bMonoIncomingData[0];
		unsigned char *pMonoIncomingData = (unsigned char *)ppbMonoIncomingData;

        // Sleep for half the buffer duration.
        Sleep(20);

        hr = pCaptureClient->GetNextPacketSize(&packetLength);
        CHECK_RET(hr, "GetNextPacketSize() Failed");

        while (packetLength != 0)
		//while (m_bExit == false)
        {
            // Get the available data in the shared buffer.
            hr = pCaptureClient->GetBuffer(&pData, &numFramesAvailable, &flags, NULL, NULL);
            CHECK_RET(hr, "GetBuffer() failed");

            if (flags & AUDCLNT_BUFFERFLAGS_SILENT)
            {
                pData = NULL;  // Tell CopyData to write silence.
            }

			int nBytesIfMono = numFramesAvailable*nIncomingBytesPerSample;

			int nBytesInOurFormat = numFramesAvailable*2;

			if ( (pData != NULL) && (numFramesAvailable > 0) )
			{

				void *vData = (void *)pData;
				/// See if we have to convert stereo to mono, average the left and right channels
				if (pwfx->nChannels == 2)
				{
					if (pwfx->wBitsPerSample == 32) // Even though msdn says shared mode must be PCM, we are getting floating point from the driver
					{
						float *pDataAsFloat = ((float *)vData);
						int *pOutputAsInt = (int *) pMonoIncomingData;
						for (int i=0; i<numFramesAvailable; i++)
						{
							/// Combine left and right channels
							*pOutputAsInt = (int) ((float)   (((*pDataAsFloat)+(*(pDataAsFloat+1)))/2)   *0x7FFFFFFF); 

							pOutputAsInt++;
							pDataAsFloat += 2;
						}
					}	
					else if (pwfx->wBitsPerSample == 16)
					{
						short *pDataAsShort = (short *)vData;
						short *pOutputAsShort = (short *) pMonoIncomingData;
						for (int i=0; i<numFramesAvailable; i++)
						{
							/// Combine left and right channels
							*pOutputAsShort = ( (*pDataAsShort) + (*(pDataAsShort+1)))/2;

							pOutputAsShort++;
							pDataAsShort += 2;
						}
					}
				}
				else if (pwfx->nChannels == 1)
				{
					if (pwfx->wBitsPerSample == 32) // Even though msdn says shared mode must be PCM, we are getting floating point from the driver
					{
						float *pDataAsFloat = ((float *)vData);
						int *pOutputAsInt = (int *) pMonoIncomingData;
						for (int i=0; i<numFramesAvailable; i++)
						{
							/// mask the channel
							*pOutputAsInt = (int) ((float)   ((*pDataAsFloat)   *0x7FFFFFFF)); 

							pOutputAsInt++;
							pDataAsFloat++;
						}
					}	
					else if (pwfx->wBitsPerSample == 16)
					{
						memcpy(pMonoIncomingData, pData, nBytesIfMono);
					}
				}
				else /// Kinect has 4 channels, who knows what other devices have
				{
					if (pwfx->wBitsPerSample == 32) // Even though msdn says shared mode must be PCM, we are getting floating point from the driver
					{
						float *pDataAsFloat = ((float *)vData);
						int *pOutputAsInt = (int *) pMonoIncomingData;
						for (int i=0; i<numFramesAvailable; i++)
						{
							/// Combine left and right channels
							*pOutputAsInt = (int) ((float)   (((*pDataAsFloat)+(*(pDataAsFloat+1)))/2)   *0x7FFFFFFF); 

							pOutputAsInt++;
							pDataAsFloat += pwfx->nChannels;
						}
					}	
					else if (pwfx->wBitsPerSample == 16)
					{
						short *pDataAsShort = (short *)vData;
						short *pOutputAsShort = (short *) pMonoIncomingData;
						for (int i=0; i<numFramesAvailable; i++)
						{
							/// Combine left and right channels
							*pOutputAsShort = ( (*pDataAsShort) + (*(pDataAsShort+1)))/2;

							pOutputAsShort++;
							pDataAsShort += pwfx->nChannels;
						}
					}
				}


				/// Now Convert our mono data to the right number of bits (16)
				if (pwfx->wBitsPerSample != 16)
				{
					short *pShortOutput = (short *) pNextData;

					if (pwfx->wBitsPerSample == 32)
					{
						int *pInputAsInt = (int *) pMonoIncomingData;
						for (int i=0; i<numFramesAvailable; i++)
						{
							*pShortOutput = (short) ((*pInputAsInt)>>16);  /// we could do this above, but needs more 'ifs'

							pInputAsInt++;
							pShortOutput++;
						}
					}
				}
				else
				{
					memcpy(pNextData, pMonoIncomingData, nOutputByteSize);
				}


				/// Finally, change our sampling rate to 48KHz if it isn't already
				
				if (pwfx->nSamplesPerSec != 48000)
				{

					if (Converter == nullptr)
					{
						bSource = gcnew array<unsigned char>(nBytesInOurFormat);
						Converter = gcnew ImageAquisition::SampleConvertor(pwfx->nSamplesPerSec/100, 480, nBytesInOurFormat/2);
					}

					Array::Copy(bNextData, 0, bSource, 0, nBytesInOurFormat);


					//// Upsample our data because we need 48KHz audio for aac.
					//// can't record at this rate because some of these devices don't support it
					array<short> ^sData = ImageAquisition::Utils::ConvertByteArrayToShortArrayLittleEndian(bSource);
					array<short> ^sUpSample = Converter->Convert(sData);
					array<unsigned char> ^bConverted = ImageAquisition::Utils::ConvertShortArrayToByteArray(sUpSample);
					ByteQueue->AppendData(bConverted);
				}
				else
				{
					ByteQueue->AppendData(bNextData, 0, nBytesInOurFormat);
				}

				if (ByteQueue->Size > 96000*10) /// If we have more than 10 s data queued, somethings wrong, remove the first 9 seconds
				{
					ByteQueue->GetNSamples(96000*9);
				}
			}

            hr = pCaptureClient->ReleaseBuffer(numFramesAvailable);
            CHECK_RET(hr, "Release Buffer failed");

            hr = pCaptureClient->GetNextPacketSize(&packetLength);
            CHECK_RET(hr, "GetNextPacketSize() 2 failed");
        }
    }
	 
    hr = pAudioClient->Stop();  // Stop recording.

exit:
    SAFE_RELEASE(pOurMicDevice);
	SAFE_RELEASE(pCaptureClient);
	SAFE_RELEASE(pAudioClient);

    CoUninitialize();
}



/// Play data in our queue
/// Wait until the queue is full before starting (at least m_nDSBufferSize in length),
/// then add half the data to the queue everytime our events are signaled
void NarrowBandMic::ReadSpeakerAsMicInLoopBackModeFunction()
{
	///Open this speaker device as an IMMDevice
	HRESULT hr = S_OK;
    HANDLE mmHandle = NULL;
	UINT index, dwCount;
    IMMDeviceEnumerator* spEnumerator;
    IMMDeviceCollection* spEndpoints;
	IMMDevice* pOurSpeakerDevice = NULL;
	UINT32 bufferFrameCount;
	IAudioClient *pAudioClient = NULL;
	REFERENCE_TIME hnsRequestedDuration = REFTIMES_PER_SEC;
    REFERENCE_TIME hnsActualDuration;
	IAudioCaptureClient *pCaptureClient = NULL;
    WAVEFORMATEX *pwfx = NULL;

    CoInitialize(NULL);

    CHECKHR(CoCreateInstance(__uuidof(MMDeviceEnumerator),  NULL, CLSCTX_ALL, __uuidof(IMMDeviceEnumerator), (void**)&spEnumerator));
    CHECKHR(spEnumerator->EnumAudioEndpoints(eRender, DEVICE_STATE_ACTIVE, &spEndpoints));
    CHECKHR(spEndpoints->GetCount(&dwCount));


    // Iterate over all speaker devices until finding one that matches our guid
    for (index = 0; index < dwCount; index++)
    {
        IMMDevice* spDevice;
		IPropertyStore *pProps = NULL;
        CHECKHR(spEndpoints->Item(index, &spDevice));
		LPWSTR pwszID = NULL;

		hr = spDevice->OpenPropertyStore(STGM_READ, &pProps);
		if (FAILED(hr))
		{
			SAFE_RELEASE(spDevice);
			continue;
		}


		PROPVARIANT varGuid;
        PropVariantInit(&varGuid);
		hr = pProps->GetValue(PKEY_AudioEndpoint_GUID2, &varGuid);
		System::String ^strGuid = gcnew System::String(varGuid.pwszVal);
		System::Guid guid = System::Guid::Parse(strGuid);

	    SAFE_RELEASE(pProps);

		if (guid == m_objAudioDevice->Guid)
		{
			pOurSpeakerDevice = spDevice;
			break;
		}



		SAFE_RELEASE(spDevice);
    }

	SAFE_RELEASE(spEnumerator);
    SAFE_RELEASE(spEndpoints);    
    
	if (pOurSpeakerDevice == NULL)
		throw gcnew Exception("Can't find a speaker device with this GUID");



	hr = pOurSpeakerDevice->Activate(IID_IAudioClient, CLSCTX_ALL, NULL, (void**)&pAudioClient);

	hr = pAudioClient->GetMixFormat(&pwfx);
    CHECK_RET(hr, "Failed to get mix format");

    //WAVEFORMATEX wfxOut = {WAVE_FORMAT_PCM, 1, 16000, 32000, 2, 16, 0};
	//WAVEFORMATEX wfxOut = {WAVE_FORMAT_PCM, 2, 48000, 192000, 2, 16, 0};

	///// We want samples that are 20 ms in duration, so in 100 nanosecond units that is
	//// 20 000 000 ns = 20 000 0  (100 ns units)
	//hnsRequestedDuration = 200000;
	//	// Calculate the actual duration of the allocated buffer.
 //   //hnsActualDuration = (double)REFTIMES_PER_SEC *bufferFrameCount / pwfx->nSamplesPerSec;

	//WAVEFORMATEX *pClosest = NULL;
	//hr = pAudioClient->IsFormatSupported(AUDCLNT_SHAREMODE_SHARED, &wfxOut, &pClosest);


    //hr = pAudioClient->Initialize(AUDCLNT_SHAREMODE_SHARED, AUDCLNT_STREAMFLAGS_LOOPBACK, hnsRequestedDuration, 0, &wfxOut, NULL);
	hr = pAudioClient->Initialize(AUDCLNT_SHAREMODE_SHARED, AUDCLNT_STREAMFLAGS_LOOPBACK, hnsRequestedDuration, 0, pwfx, NULL);
	if (hr == AUDCLNT_E_UNSUPPORTED_FORMAT)
		throw gcnew Exception("Unsupported format");

    CHECK_RET(hr, "Audio Client Initialize failed");


	  // Get the size of the allocated buffer.
    hr = pAudioClient->GetBufferSize(&bufferFrameCount);
    CHECK_RET(hr, "Failed to GetBufferSize()")


    hr = pAudioClient->GetService(IID_IAudioCaptureClient, (void**)&pCaptureClient);
    CHECK_RET(hr, "Failed to get IAudioCaptureClient service");


	ImageAquisition::SampleConvertor ^Converter = nullptr; //gcnew ImageAquisition::SampleConvertor(16, 48, bufferFrameCount);
   
	/// Have to figure out how to do a conversion between the native sample format and ours (48000 Mono)
	if (pwfx->nSamplesPerSec != 48000)
	{
		Converter = gcnew ImageAquisition::SampleConvertor(pwfx->nSamplesPerSec/100, 480, bufferFrameCount);
	}


	int totalBytes = 0;
	BYTE *pData;
	DWORD flags;
    
	
	hr = pAudioClient->Start();
	unsigned int packetLength = 0;
	unsigned int numFramesAvailable = 0;

	int nOutputByteSize = 48000*2/50; /// Final format is 48000 Hz, 2 byte samples, in 20 ms intervals (50 intervals/s)
	int nOuputShortSize = 48000/50;
	array<unsigned char> ^bNextData = gcnew array<unsigned char>(nOutputByteSize); 

	/// Step 1.  A buffer to store native data in mono form by combining left and right channels
	int nIncomingBytesPerSample = pwfx->wBitsPerSample/8;
	array<unsigned char> ^bMonoIncomingData = gcnew array<unsigned char>(pwfx->nSamplesPerSec*nIncomingBytesPerSample/50); /// Final format is 48000 Hz, 2 byte samples, in 20 ms intervals (50 intervals/s)
	array<unsigned char> ^bSource = nullptr;


	while (m_bExit == false)
    {
		// Copy the available capture data to the audio sink.
		pin_ptr<unsigned char> ppbNextData = &bNextData[0];
		unsigned char *pNextData = (unsigned char *)ppbNextData;

		pin_ptr<unsigned char> ppbMonoIncomingData = &bMonoIncomingData[0];
		unsigned char *pMonoIncomingData = (unsigned char *)ppbMonoIncomingData;

        // Sleep for half the buffer duration.
        Sleep(20);

        hr = pCaptureClient->GetNextPacketSize(&packetLength);
        CHECK_RET(hr, "GetNextPacketSize() Failed");

        while (packetLength != 0)
		//while (m_bExit == false)
        {
            // Get the available data in the shared buffer.
            hr = pCaptureClient->GetBuffer(&pData, &numFramesAvailable, &flags, NULL, NULL);
            CHECK_RET(hr, "GetBuffer() failed");

            if (flags & AUDCLNT_BUFFERFLAGS_SILENT)
            {
                pData = NULL;  // Tell CopyData to write silence.
            }

			int nBytesIfMono = numFramesAvailable*nIncomingBytesPerSample;

			int nBytesInOurFormat = numFramesAvailable*2;

			
			if ( (pData != NULL) && (numFramesAvailable > 0) )
			{

				void *vData = (void *)pData;
				/// See if we have to convert stereo to mono, average the left and right channels
				if (pwfx->nChannels == 2)
				{
					if (pwfx->wBitsPerSample == 32) // Even though msdn says shared mode must be PCM, we are getting floating point from the driver
					{
						float *pDataAsFloat = ((float *)vData);
						int *pOutputAsInt = (int *) pMonoIncomingData;
						for (int i=0; i<numFramesAvailable; i++)
						{
							/// Combine left and right channels
							*pOutputAsInt = (int) ((float)   (((*pDataAsFloat)+(*(pDataAsFloat+1)))/2)   *0x7FFFFFFF); 

							pOutputAsInt++;
							pDataAsFloat += 2;
						}
					}	
					else if (pwfx->wBitsPerSample == 16)
					{
						short *pDataAsShort = (short *)vData;
						short *pOutputAsShort = (short *) pMonoIncomingData;
						for (int i=0; i<numFramesAvailable; i++)
						{
							/// Combine left and right channels
							*pOutputAsShort = ( (*pDataAsShort) + (*(pDataAsShort+1)))/2;

							pOutputAsShort++;
							pDataAsShort += 2;
						}
					}
				}
				else if (pwfx->nChannels == 1)
				{
					if (pwfx->wBitsPerSample == 32) // Even though msdn says shared mode must be PCM, we are getting floating point from the driver
					{
						float *pDataAsFloat = ((float *)vData);
						int *pOutputAsInt = (int *) pMonoIncomingData;
						for (int i=0; i<numFramesAvailable; i++)
						{
							/// mask the channel
							*pOutputAsInt = (int) ((float)   ((*pDataAsFloat)   *0x7FFFFFFF)); 

							pOutputAsInt++;
							pDataAsFloat++;
						}
					}	
					else if (pwfx->wBitsPerSample == 16)
					{
						memcpy(pMonoIncomingData, pData, nBytesIfMono);
					}
				}
				else /// Kinect has 4 channels, who knows what other devices have
				{
					if (pwfx->wBitsPerSample == 32) // Even though msdn says shared mode must be PCM, we are getting floating point from the driver
					{
						float *pDataAsFloat = ((float *)vData);
						int *pOutputAsInt = (int *) pMonoIncomingData;
						for (int i=0; i<numFramesAvailable; i++)
						{
							/// Combine left and right channels
							*pOutputAsInt = (int) ((float)   (((*pDataAsFloat)+(*(pDataAsFloat+1)))/2)   *0x7FFFFFFF); 

							pOutputAsInt++;
							pDataAsFloat += pwfx->nChannels;
						}
					}	
					else if (pwfx->wBitsPerSample == 16)
					{
						short *pDataAsShort = (short *)vData;
						short *pOutputAsShort = (short *) pMonoIncomingData;
						for (int i=0; i<numFramesAvailable; i++)
						{
							/// Combine left and right channels
							*pOutputAsShort = ( (*pDataAsShort) + (*(pDataAsShort+1)))/2;

							pOutputAsShort++;
							pDataAsShort += pwfx->nChannels;
						}
					}
				}


				/// Now Convert our mono data to the right number of bits (16)
				if (pwfx->wBitsPerSample != 16)
				{
					short *pShortOutput = (short *) pNextData;

					if (pwfx->wBitsPerSample == 32)
					{
						int *pInputAsInt = (int *) pMonoIncomingData;
						for (int i=0; i<numFramesAvailable; i++)
						{
							*pShortOutput = (short) ((*pInputAsInt)>>16);  /// we could do this above, but needs more 'ifs'

							pInputAsInt++;
							pShortOutput++;
						}
					}
				}
				else
				{
					memcpy(pNextData, pMonoIncomingData, nOutputByteSize);
				}


				/// Finally, change our sampling rate to 48KHz if it isn't already
				
				if (pwfx->nSamplesPerSec != 48000)
				{

					if (Converter == nullptr)
					{
						bSource = gcnew array<unsigned char>(nBytesInOurFormat);
						Converter = gcnew ImageAquisition::SampleConvertor(pwfx->nSamplesPerSec/100, 480, nBytesInOurFormat/2);
					}

					Array::Copy(bNextData, 0, bSource, 0, nBytesInOurFormat);


					//// Upsample our data because we need 48KHz audio for aac.
					//// can't record at this rate because some of these devices don't support it
					array<short> ^sData = ImageAquisition::Utils::ConvertByteArrayToShortArrayLittleEndian(bSource);
					array<short> ^sUpSample = Converter->Convert(sData);
					array<unsigned char> ^bConverted = ImageAquisition::Utils::ConvertShortArrayToByteArray(sUpSample);
					ByteQueue->AppendData(bConverted);
				}
				else
				{
					ByteQueue->AppendData(bNextData, 0, nBytesInOurFormat);
				}

				if (ByteQueue->Size > 96000*10) /// If we have more than 10 s data queued, somethings wrong, remove the first 9 seconds
				{
					ByteQueue->GetNSamples(96000*9);
				}
			}

            hr = pCaptureClient->ReleaseBuffer(numFramesAvailable);
            CHECK_RET(hr, "Release Buffer failed");

            hr = pCaptureClient->GetNextPacketSize(&packetLength);
            CHECK_RET(hr, "GetNextPacketSize() 2 failed");
        }
    }
	 
    hr = pAudioClient->Stop();  // Stop recording.

exit:
    SAFE_RELEASE(pOurSpeakerDevice);
	SAFE_RELEASE(pCaptureClient);
	SAFE_RELEASE(pAudioClient);

    CoUninitialize();
}

#endif