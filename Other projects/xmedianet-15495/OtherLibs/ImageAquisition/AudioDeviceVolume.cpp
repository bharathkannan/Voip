/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

#include "stdafx.h"

#include "AudioDeviceVolume.h"


using namespace System;
using namespace AudioClasses;
using namespace ImageAquisition;
using namespace System::Collections::Generic;
using namespace System::Runtime::InteropServices;


#include <vcclr.h>


//-----------------------------------------------------------
// Client implementation of IAudioEndpointVolumeCallback
// interface. When a method in the IAudioEndpointVolume
// interface changes the volume level or muting state of the
// endpoint device, the change initiates a call to the
// client's IAudioEndpointVolumeCallback::OnNotify method.
//-----------------------------------------------------------
class CAudioEndpointVolumeCallback : public IAudioEndpointVolumeCallback
{
    LONG _cRef;

public:
    CAudioEndpointVolumeCallback(GUID guidMyContext) :
        _cRef(1)
    {
		g_guidMyContext = guidMyContext;
		AudioDeviceVolume = nullptr;
    }

	/// Pointer to our managed object for callbacks
	gcroot<AudioDeviceVolume ^> AudioDeviceVolume;

    ~CAudioEndpointVolumeCallback()
    {
		AudioDeviceVolume = nullptr;
    }

	GUID g_guidMyContext;
    // IUnknown methods -- AddRef, Release, and QueryInterface

    ULONG STDMETHODCALLTYPE AddRef()
    {
        return InterlockedIncrement(&_cRef);
    }

    ULONG STDMETHODCALLTYPE Release()
    {
        ULONG ulRef = InterlockedDecrement(&_cRef);
        if (0 == ulRef)
        {
            delete this;
        }
        return ulRef;

    }

    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, VOID **ppvInterface)
    {
        if (IID_IUnknown == riid)
        {
            AddRef();
            *ppvInterface = (IUnknown*)this;
        }
        else if (__uuidof(IAudioEndpointVolumeCallback) == riid)
        {
            AddRef();
            *ppvInterface = (IAudioEndpointVolumeCallback*)this;
        }
        else
        {
            *ppvInterface = NULL;
            return E_NOINTERFACE;
        }
        return S_OK;
    }

    // Callback method for endpoint-volume-change notifications.

    HRESULT STDMETHODCALLTYPE OnNotify(PAUDIO_VOLUME_NOTIFICATION_DATA pNotify)
    {
        if (pNotify == NULL)
        {
            return E_INVALIDARG;
        }
		

		int nVolume = (int) (100*pNotify->fMasterVolume + 0.5);

		/// Always notify our parent when this changed, even if we are the one changing it
		AudioDeviceVolume->NotifyMuteStatus((pNotify->bMuted==TRUE)?true:false);
		AudioDeviceVolume->NotifyVolumeLevel(nVolume);
		
	  //  if ((AudioDeviceVolume != nullptr) && (pNotify->guidEventContext != g_guidMyContext))
   //     {

			///// Notify our managed object that our volume or mute status has changed

   //    /*     PostMessage(GetDlgItem(g_hDlg, IDC_CHECK_MUTE), BM_SETCHECK,
   //                     (pNotify->bMuted) ? BST_CHECKED : BST_UNCHECKED, 0);

   //         PostMessage(GetDlgItem(g_hDlg, IDC_SLIDER_VOLUME),
   //                     TBM_SETPOS, TRUE,
   //                     );*/
   //     }
        return S_OK;
    }
};




/// Get the microphone device with the specified index
IMMDevice *GetCaptureDevice(UINT index)
{
	IMMDevice *pAudioDevRet = NULL;
    HRESULT hr = S_OK;
    UINT dwCount;
    IMMDeviceEnumerator* spEnumerator;
    IMMDeviceCollection* spEndpoints;

    CHECKHR(CoCreateInstance(__uuidof(MMDeviceEnumerator),  NULL, CLSCTX_ALL, __uuidof(IMMDeviceEnumerator), (void**)&spEnumerator));
    CHECKHR(spEnumerator->EnumAudioEndpoints(eCapture, DEVICE_STATE_ACTIVE, &spEndpoints));
    CHECKHR(spEndpoints->GetCount(&dwCount));

 
    hr = spEndpoints->Item(index, &pAudioDevRet);

exit:
	SAFE_RELEASE(spEnumerator);
    SAFE_RELEASE(spEndpoints);    

	return pAudioDevRet;
}

/// Get the speaker device with the specified index
IMMDevice *GetRenderDevice(UINT index)
{
	IMMDevice *pAudioDevRet = NULL;
    HRESULT hr = S_OK;
    UINT dwCount;
    IMMDeviceEnumerator* spEnumerator;
    IMMDeviceCollection* spEndpoints;

    CHECKHR(CoCreateInstance(__uuidof(MMDeviceEnumerator),  NULL, CLSCTX_ALL, __uuidof(IMMDeviceEnumerator), (void**)&spEnumerator));
    CHECKHR(spEnumerator->EnumAudioEndpoints(eRender, DEVICE_STATE_ACTIVE, &spEndpoints));
    CHECKHR(spEndpoints->GetCount(&dwCount));

 
    hr = spEndpoints->Item(index, &pAudioDevRet);

exit:
	SAFE_RELEASE(spEnumerator);
    SAFE_RELEASE(spEndpoints);    

	return pAudioDevRet;
}


ImageAquisition::AudioDeviceVolume::AudioDeviceVolume(AudioDevice ^dev)
{
	IntPtrComAudioDevice = IntPtr::Zero;
	IntPtrAudioEndpointVolume = IntPtr::Zero;
	IntPtrAudioCallBackObject = IntPtr::Zero;

	AudioDev = dev;
	IMMDevice *pComPtrAudioDev = NULL; 
	IAudioEndpointVolume *pComPtrEndptVol = NULL;

	if (dev->DeviceType == DeviceType::Input) /// microphone device
		pComPtrAudioDev = GetCaptureDevice(AudioDev->DeviceId);
	else
		pComPtrAudioDev = GetRenderDevice(AudioDev->DeviceId);

	IntPtrComAudioDevice = IntPtr(pComPtrAudioDev);

	if (pComPtrAudioDev == NULL)
		throw gcnew Exception(String::Format("Cannot create Audio device for {0}", dev));

	/// Hook into our COM events for volume changes, etc
    //CoInitialize(NULL);

	HRESULT hr;
	GUID g_guidMyContext = GUID_NULL;
    hr = CoCreateGuid(&g_guidMyContext);
	CAudioEndpointVolumeCallback *pCallbackObject = new CAudioEndpointVolumeCallback(g_guidMyContext);
	pCallbackObject->AudioDeviceVolume = this;

	IntPtrAudioCallBackObject = IntPtr(pCallbackObject);

    hr = pComPtrAudioDev->Activate(__uuidof(IAudioEndpointVolume), CLSCTX_ALL, NULL, (void**)&pComPtrEndptVol);
    if (FAILED(hr))
	{
   	   Cleanup();
	   throw gcnew Exception(String::Format("Unable to active IAudioEndpointVolume for {0}", dev));
	}
	IntPtrAudioEndpointVolume = IntPtr(pComPtrEndptVol);


    hr = pComPtrEndptVol->RegisterControlChangeNotify((IAudioEndpointVolumeCallback*)pCallbackObject);
    if (FAILED(hr))
	{
	   Cleanup();
	   throw gcnew Exception(String::Format("Unable to register with volume change events for {0}", dev));
	}

    m_nVolume = GetCurrentVolume();

	//CoUninitialize();
}

void ImageAquisition::AudioDeviceVolume::Cleanup()
{
    IMMDevice *pComPtrAudioDev = (IMMDevice *) IntPtrComAudioDevice.ToPointer();
    IAudioEndpointVolume *pComPtrEndptVol = (IAudioEndpointVolume *) IntPtrAudioEndpointVolume.ToPointer();
    CAudioEndpointVolumeCallback *pCallbackObject = (CAudioEndpointVolumeCallback *) IntPtrComAudioDevice.ToPointer();

	if ( (pComPtrEndptVol != NULL) && (pCallbackObject != NULL) )
	{
		pComPtrEndptVol->UnregisterControlChangeNotify((IAudioEndpointVolumeCallback*)pCallbackObject);
	}

	if (IntPtrComAudioDevice != IntPtr::Zero)
	{
	   SAFE_RELEASE(pComPtrAudioDev);
	   IntPtrComAudioDevice = IntPtr::Zero;
	}
	if (IntPtrAudioEndpointVolume != IntPtr::Zero)
	{
	   SAFE_RELEASE(pComPtrEndptVol);
	   IntPtrAudioEndpointVolume = IntPtr::Zero;
	}

	if (IntPtrAudioCallBackObject != IntPtr::Zero)
	{
	   delete pComPtrAudioDev;
	   IntPtrAudioCallBackObject = IntPtr::Zero;
	}

}


bool ImageAquisition::AudioDeviceVolume::Mute::get()
{
	return m_bMuted;
}

void ImageAquisition::AudioDeviceVolume::Mute::set(bool value)
{
	/// Set mute... don't set our variable, that will get set through the callback
    IAudioEndpointVolume *pComPtrEndptVol = (IAudioEndpointVolume *) IntPtrAudioEndpointVolume.ToPointer();
	CAudioEndpointVolumeCallback *pCallbackObject = (CAudioEndpointVolumeCallback *) IntPtrComAudioDevice.ToPointer();

	HRESULT hr = pComPtrEndptVol->SetMute((value==true)?TRUE:FALSE, &pCallbackObject->g_guidMyContext);
}

void ImageAquisition::AudioDeviceVolume::NotifyMuteStatus(bool value)
{
	if (m_bMuted != value)
	{
		m_bMuted = value;
		FirePropertyChanged("Mute");
	}
}

int ImageAquisition::AudioDeviceVolume::Volume::get()
{

	return m_nVolume;
}

int ImageAquisition::AudioDeviceVolume::GetCurrentVolume()
{
    IAudioEndpointVolume *pComPtrEndptVol = (IAudioEndpointVolume *) IntPtrAudioEndpointVolume.ToPointer();
	float fLevel = 0.0f;
	HRESULT hr = pComPtrEndptVol->GetMasterVolumeLevelScalar(&fLevel);
	if (SUCCEEDED(hr))
	{
		return (int) (100*fLevel + 0.5);
	}
	return 0;
}


void ImageAquisition::AudioDeviceVolume::Volume::set(int value)
{
	/// Set mute... don't set our variable, that will get set through the callback
    IAudioEndpointVolume *pComPtrEndptVol = (IAudioEndpointVolume *) IntPtrAudioEndpointVolume.ToPointer();
	CAudioEndpointVolumeCallback *pCallbackObject = (CAudioEndpointVolumeCallback *) IntPtrComAudioDevice.ToPointer();

	double fValue = value/100.0f;
	HRESULT hr = pComPtrEndptVol->SetMasterVolumeLevelScalar(fValue, &pCallbackObject->g_guidMyContext);
}

void ImageAquisition::AudioDeviceVolume::NotifyVolumeLevel(int value)
{
	if (m_nVolume != value)
	{
		m_nVolume = value;
		FirePropertyChanged("Volume");
	}
}