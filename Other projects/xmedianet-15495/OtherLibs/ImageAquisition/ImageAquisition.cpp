/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.


#include "stdafx.h"

#include <windows.h>
#include <Wia.h>
#include <stdio.h>

#include <Mfidl.h>
#include <Mfapi.h >
#include <Mfreadwrite.h>
#include <Mferror.h>
#include <strmif.h>


#include "ImageAquisition.h"
#include <exception>


using namespace ImageAquisition;
using namespace AudioClasses;


// The following code enables you to view the contents of a media type while 
// debugging.

#include <strsafe.h>

LPCWSTR GetGUIDNameConst(const GUID& guid);
HRESULT GetGUIDName(const GUID& guid, WCHAR **ppwsz);

HRESULT LogAttributeValueByIndex(IMFAttributes *pAttr, DWORD index);
System::String ^ LogAttributeValueByIndexString(IMFAttributes *pAttr, DWORD index);
HRESULT SpecialCaseAttributeValue(GUID guid, const PROPVARIANT& var);

void DBGMSG(PCWSTR format, ...);


System::String ^LogMediaTypeString(IMFMediaType *pType)
{
	System::String ^strRet = nullptr;
    UINT32 count = 0;

    HRESULT hr = pType->GetCount(&count);
    if (FAILED(hr))
    {
        return strRet;
    }

    if (count == 0)
    {
        DBGMSG(L"Empty media type.\n");
    }

    for (UINT32 i = 0; i < count; i++)
    {
        strRet = LogAttributeValueByIndexString(pType, i);
        if (FAILED(hr))
        {
            break;
        }
    }
    return strRet;
}

System::String ^ LogAttributeValueByIndexString(IMFAttributes *pAttr, DWORD index)
{
	System::String ^strRet = nullptr;

    WCHAR *pGuidName = NULL;
    WCHAR *pGuidValName = NULL;

    GUID guid = { 0 };

    PROPVARIANT var;
    PropVariantInit(&var);

    HRESULT hr = pAttr->GetItemByIndex(index, &guid, &var);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = GetGUIDName(guid, &pGuidName);
    if (FAILED(hr))
    {
        goto done;
    }

	strRet = gcnew System::String(pGuidName);
    DBGMSG(L"\t%s\t", pGuidName);

    hr = SpecialCaseAttributeValue(guid, var);
    if (FAILED(hr))
    {
        goto done;
    }
    if (hr == S_FALSE)
    {
        switch (var.vt)
        {
        case VT_UI4:
            DBGMSG(L"%d", var.ulVal);
            break;

        case VT_UI8:
            DBGMSG(L"%I64d", var.uhVal);
            break;

        case VT_R8:
            DBGMSG(L"%f", var.dblVal);
            break;

        case VT_CLSID:
            hr = GetGUIDName(*var.puuid, &pGuidValName);
            if (SUCCEEDED(hr))
            {
                DBGMSG(pGuidValName);
            }
            break;

        case VT_LPWSTR:
            DBGMSG(var.pwszVal);
            break;

        case VT_VECTOR | VT_UI1:
            DBGMSG(L"<<byte array>>");
            break;

        case VT_UNKNOWN:
            DBGMSG(L"IUnknown");
            break;

        default:
            DBGMSG(L"Unexpected attribute type (vt = %d)", var.vt);
            break;
        }
    }

done:
    DBGMSG(L"\n");
    CoTaskMemFree(pGuidName);
    CoTaskMemFree(pGuidValName);
    PropVariantClear(&var);
    return strRet;
}



HRESULT LogMediaType(IMFMediaType *pType)
{
    UINT32 count = 0;

    HRESULT hr = pType->GetCount(&count);
    if (FAILED(hr))
    {
        return hr;
    }

    if (count == 0)
    {
        DBGMSG(L"Empty media type.\n");
    }

    for (UINT32 i = 0; i < count; i++)
    {
        hr = LogAttributeValueByIndex(pType, i);
        if (FAILED(hr))
        {
            break;
        }
    }
    return hr;
}

HRESULT LogAttributeValueByIndex(IMFAttributes *pAttr, DWORD index)
{
    WCHAR *pGuidName = NULL;
    WCHAR *pGuidValName = NULL;

    GUID guid = { 0 };

    PROPVARIANT var;
    PropVariantInit(&var);

    HRESULT hr = pAttr->GetItemByIndex(index, &guid, &var);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = GetGUIDName(guid, &pGuidName);
    if (FAILED(hr))
    {
        goto done;
    }

    DBGMSG(L"\t%s\t", pGuidName);

    hr = SpecialCaseAttributeValue(guid, var);
    if (FAILED(hr))
    {
        goto done;
    }
    if (hr == S_FALSE)
    {
        switch (var.vt)
        {
        case VT_UI4:
            DBGMSG(L"%d", var.ulVal);
            break;

        case VT_UI8:
            DBGMSG(L"%I64d", var.uhVal);
            break;

        case VT_R8:
            DBGMSG(L"%f", var.dblVal);
            break;

        case VT_CLSID:
            hr = GetGUIDName(*var.puuid, &pGuidValName);
            if (SUCCEEDED(hr))
            {
                DBGMSG(pGuidValName);
            }
            break;

        case VT_LPWSTR:
            DBGMSG(var.pwszVal);
            break;

        case VT_VECTOR | VT_UI1:
            DBGMSG(L"<<byte array>>");
            break;

        case VT_UNKNOWN:
            DBGMSG(L"IUnknown");
            break;

        default:
            DBGMSG(L"Unexpected attribute type (vt = %d)", var.vt);
            break;
        }
    }

done:
    DBGMSG(L"\n");
    CoTaskMemFree(pGuidName);
    CoTaskMemFree(pGuidValName);
    PropVariantClear(&var);
    return hr;
}

HRESULT GetGUIDName(const GUID& guid, WCHAR **ppwsz)
{
    HRESULT hr = S_OK;
    WCHAR *pName = NULL;

    LPCWSTR pcwsz = GetGUIDNameConst(guid);
    if (pcwsz)
    {
        size_t cchLength = 0;
    
        hr = StringCchLength(pcwsz, STRSAFE_MAX_CCH, &cchLength);
        if (FAILED(hr))
        {
            goto done;
        }
        
        pName = (WCHAR*)CoTaskMemAlloc((cchLength + 1) * sizeof(WCHAR));

        if (pName == NULL)
        {
            hr = E_OUTOFMEMORY;
            goto done;
        }

        hr = StringCchCopy(pName, cchLength + 1, pcwsz);
        if (FAILED(hr))
        {
            goto done;
        }
    }
    else
    {
        hr = StringFromCLSID(guid, &pName);
    }

done:
    if (FAILED(hr))
    {
        *ppwsz = NULL;
        CoTaskMemFree(pName);
    }
    else
    {
        *ppwsz = pName;
    }
    return hr;
}

void LogUINT32AsUINT64(const PROPVARIANT& var)
{
    UINT32 uHigh = 0, uLow = 0;
    Unpack2UINT32AsUINT64(var.uhVal.QuadPart, &uHigh, &uLow);
    DBGMSG(L"%d x %d", uHigh, uLow);
}

float OffsetToFloat(const MFOffset& offset)
{
    return offset.value + (static_cast<float>(offset.fract) / 65536.0f);
}

HRESULT LogVideoArea(const PROPVARIANT& var)
{
    if (var.caub.cElems < sizeof(MFVideoArea))
    {
        return MF_E_BUFFERTOOSMALL;
    }

    MFVideoArea *pArea = (MFVideoArea*)var.caub.pElems;

    DBGMSG(L"(%f,%f) (%d,%d)", OffsetToFloat(pArea->OffsetX), OffsetToFloat(pArea->OffsetY), 
        pArea->Area.cx, pArea->Area.cy);
    return S_OK;
}

// Handle certain known special cases.
HRESULT SpecialCaseAttributeValue(GUID guid, const PROPVARIANT& var)
{
    if ((guid == MF_MT_FRAME_RATE) || (guid == MF_MT_FRAME_RATE_RANGE_MAX) ||
        (guid == MF_MT_FRAME_RATE_RANGE_MIN) || (guid == MF_MT_FRAME_SIZE) ||
        (guid == MF_MT_PIXEL_ASPECT_RATIO))
    {
        // Attributes that contain two packed 32-bit values.
        LogUINT32AsUINT64(var);
    }
    else if ((guid == MF_MT_GEOMETRIC_APERTURE) || 
             (guid == MF_MT_MINIMUM_DISPLAY_APERTURE) || 
             (guid == MF_MT_PAN_SCAN_APERTURE))
    {
        // Attributes that an MFVideoArea structure.
        return LogVideoArea(var);
    }
    else
    {
        return S_FALSE;
    }
    return S_OK;
}

void DBGMSG(PCWSTR format, ...)
{
    va_list args;
    va_start(args, format);

    WCHAR msg[MAX_PATH];

    if (SUCCEEDED(StringCbVPrintf(msg, sizeof(msg), format, args)))
    {
        OutputDebugString(msg);
    }
}

#ifndef IF_EQUAL_RETURN
#define IF_EQUAL_RETURN(param, val) if(val == param) return L#val
#endif

LPCWSTR GetGUIDNameConst(const GUID& guid)
{
    IF_EQUAL_RETURN(guid, MF_MT_MAJOR_TYPE);
    IF_EQUAL_RETURN(guid, MF_MT_MAJOR_TYPE);
    IF_EQUAL_RETURN(guid, MF_MT_SUBTYPE);
    IF_EQUAL_RETURN(guid, MF_MT_ALL_SAMPLES_INDEPENDENT);
    IF_EQUAL_RETURN(guid, MF_MT_FIXED_SIZE_SAMPLES);
    IF_EQUAL_RETURN(guid, MF_MT_COMPRESSED);
    IF_EQUAL_RETURN(guid, MF_MT_SAMPLE_SIZE);
    IF_EQUAL_RETURN(guid, MF_MT_WRAPPED_TYPE);
    IF_EQUAL_RETURN(guid, MF_MT_AUDIO_NUM_CHANNELS);
    IF_EQUAL_RETURN(guid, MF_MT_AUDIO_SAMPLES_PER_SECOND);
    IF_EQUAL_RETURN(guid, MF_MT_AUDIO_FLOAT_SAMPLES_PER_SECOND);
    IF_EQUAL_RETURN(guid, MF_MT_AUDIO_AVG_BYTES_PER_SECOND);
    IF_EQUAL_RETURN(guid, MF_MT_AUDIO_BLOCK_ALIGNMENT);
    IF_EQUAL_RETURN(guid, MF_MT_AUDIO_BITS_PER_SAMPLE);
    IF_EQUAL_RETURN(guid, MF_MT_AUDIO_VALID_BITS_PER_SAMPLE);
    IF_EQUAL_RETURN(guid, MF_MT_AUDIO_SAMPLES_PER_BLOCK);
    IF_EQUAL_RETURN(guid, MF_MT_AUDIO_CHANNEL_MASK);
    IF_EQUAL_RETURN(guid, MF_MT_AUDIO_FOLDDOWN_MATRIX);
    IF_EQUAL_RETURN(guid, MF_MT_AUDIO_WMADRC_PEAKREF);
    IF_EQUAL_RETURN(guid, MF_MT_AUDIO_WMADRC_PEAKTARGET);
    IF_EQUAL_RETURN(guid, MF_MT_AUDIO_WMADRC_AVGREF);
    IF_EQUAL_RETURN(guid, MF_MT_AUDIO_WMADRC_AVGTARGET);
    IF_EQUAL_RETURN(guid, MF_MT_AUDIO_PREFER_WAVEFORMATEX);
    IF_EQUAL_RETURN(guid, MF_MT_AAC_PAYLOAD_TYPE);
    IF_EQUAL_RETURN(guid, MF_MT_AAC_AUDIO_PROFILE_LEVEL_INDICATION);
    IF_EQUAL_RETURN(guid, MF_MT_FRAME_SIZE);
    IF_EQUAL_RETURN(guid, MF_MT_FRAME_RATE);
    IF_EQUAL_RETURN(guid, MF_MT_FRAME_RATE_RANGE_MAX);
    IF_EQUAL_RETURN(guid, MF_MT_FRAME_RATE_RANGE_MIN);
    IF_EQUAL_RETURN(guid, MF_MT_PIXEL_ASPECT_RATIO);
    IF_EQUAL_RETURN(guid, MF_MT_DRM_FLAGS);
    IF_EQUAL_RETURN(guid, MF_MT_PAD_CONTROL_FLAGS);
    IF_EQUAL_RETURN(guid, MF_MT_SOURCE_CONTENT_HINT);
    IF_EQUAL_RETURN(guid, MF_MT_VIDEO_CHROMA_SITING);
    IF_EQUAL_RETURN(guid, MF_MT_INTERLACE_MODE);
    IF_EQUAL_RETURN(guid, MF_MT_TRANSFER_FUNCTION);
    IF_EQUAL_RETURN(guid, MF_MT_VIDEO_PRIMARIES);
    IF_EQUAL_RETURN(guid, MF_MT_CUSTOM_VIDEO_PRIMARIES);
    IF_EQUAL_RETURN(guid, MF_MT_YUV_MATRIX);
    IF_EQUAL_RETURN(guid, MF_MT_VIDEO_LIGHTING);
    IF_EQUAL_RETURN(guid, MF_MT_VIDEO_NOMINAL_RANGE);
    IF_EQUAL_RETURN(guid, MF_MT_GEOMETRIC_APERTURE);
    IF_EQUAL_RETURN(guid, MF_MT_MINIMUM_DISPLAY_APERTURE);
    IF_EQUAL_RETURN(guid, MF_MT_PAN_SCAN_APERTURE);
    IF_EQUAL_RETURN(guid, MF_MT_PAN_SCAN_ENABLED);
    IF_EQUAL_RETURN(guid, MF_MT_AVG_BITRATE);
    IF_EQUAL_RETURN(guid, MF_MT_AVG_BIT_ERROR_RATE);
    IF_EQUAL_RETURN(guid, MF_MT_MAX_KEYFRAME_SPACING);
    IF_EQUAL_RETURN(guid, MF_MT_DEFAULT_STRIDE);
    IF_EQUAL_RETURN(guid, MF_MT_PALETTE);
    IF_EQUAL_RETURN(guid, MF_MT_USER_DATA);
    IF_EQUAL_RETURN(guid, MF_MT_AM_FORMAT_TYPE);
    IF_EQUAL_RETURN(guid, MF_MT_MPEG_START_TIME_CODE);
    IF_EQUAL_RETURN(guid, MF_MT_MPEG2_PROFILE);
    IF_EQUAL_RETURN(guid, MF_MT_MPEG2_LEVEL);
    IF_EQUAL_RETURN(guid, MF_MT_MPEG2_FLAGS);
    IF_EQUAL_RETURN(guid, MF_MT_MPEG_SEQUENCE_HEADER);
    IF_EQUAL_RETURN(guid, MF_MT_DV_AAUX_SRC_PACK_0);
    IF_EQUAL_RETURN(guid, MF_MT_DV_AAUX_CTRL_PACK_0);
    IF_EQUAL_RETURN(guid, MF_MT_DV_AAUX_SRC_PACK_1);
    IF_EQUAL_RETURN(guid, MF_MT_DV_AAUX_CTRL_PACK_1);
    IF_EQUAL_RETURN(guid, MF_MT_DV_VAUX_SRC_PACK);
    IF_EQUAL_RETURN(guid, MF_MT_DV_VAUX_CTRL_PACK);
    IF_EQUAL_RETURN(guid, MF_MT_ARBITRARY_HEADER);
    IF_EQUAL_RETURN(guid, MF_MT_ARBITRARY_FORMAT);
    IF_EQUAL_RETURN(guid, MF_MT_IMAGE_LOSS_TOLERANT); 
    IF_EQUAL_RETURN(guid, MF_MT_MPEG4_SAMPLE_DESCRIPTION);
    IF_EQUAL_RETURN(guid, MF_MT_MPEG4_CURRENT_SAMPLE_ENTRY);
    IF_EQUAL_RETURN(guid, MF_MT_ORIGINAL_4CC); 
    IF_EQUAL_RETURN(guid, MF_MT_ORIGINAL_WAVE_FORMAT_TAG);
    
    // Media types

    IF_EQUAL_RETURN(guid, MFMediaType_Audio);
    IF_EQUAL_RETURN(guid, MFMediaType_Video);
    IF_EQUAL_RETURN(guid, MFMediaType_Protected);
    IF_EQUAL_RETURN(guid, MFMediaType_SAMI);
    IF_EQUAL_RETURN(guid, MFMediaType_Script);
    IF_EQUAL_RETURN(guid, MFMediaType_Image);
    IF_EQUAL_RETURN(guid, MFMediaType_HTML);
    IF_EQUAL_RETURN(guid, MFMediaType_Binary);
    IF_EQUAL_RETURN(guid, MFMediaType_FileTransfer);

    IF_EQUAL_RETURN(guid, MFVideoFormat_AI44); //     FCC('AI44')
    IF_EQUAL_RETURN(guid, MFVideoFormat_ARGB32); //   D3DFMT_A8R8G8B8 
    IF_EQUAL_RETURN(guid, MFVideoFormat_AYUV); //     FCC('AYUV')
    IF_EQUAL_RETURN(guid, MFVideoFormat_DV25); //     FCC('dv25')
    IF_EQUAL_RETURN(guid, MFVideoFormat_DV50); //     FCC('dv50')
    IF_EQUAL_RETURN(guid, MFVideoFormat_DVH1); //     FCC('dvh1')
    IF_EQUAL_RETURN(guid, MFVideoFormat_DVSD); //     FCC('dvsd')
    IF_EQUAL_RETURN(guid, MFVideoFormat_DVSL); //     FCC('dvsl')
    IF_EQUAL_RETURN(guid, MFVideoFormat_H264); //     FCC('H264')
    IF_EQUAL_RETURN(guid, MFVideoFormat_I420); //     FCC('I420')
    IF_EQUAL_RETURN(guid, MFVideoFormat_IYUV); //     FCC('IYUV')
    IF_EQUAL_RETURN(guid, MFVideoFormat_M4S2); //     FCC('M4S2')
    IF_EQUAL_RETURN(guid, MFVideoFormat_MJPG);
    IF_EQUAL_RETURN(guid, MFVideoFormat_MP43); //     FCC('MP43')
    IF_EQUAL_RETURN(guid, MFVideoFormat_MP4S); //     FCC('MP4S')
    IF_EQUAL_RETURN(guid, MFVideoFormat_MP4V); //     FCC('MP4V')
    IF_EQUAL_RETURN(guid, MFVideoFormat_MPG1); //     FCC('MPG1')
    IF_EQUAL_RETURN(guid, MFVideoFormat_MSS1); //     FCC('MSS1')
    IF_EQUAL_RETURN(guid, MFVideoFormat_MSS2); //     FCC('MSS2')
    IF_EQUAL_RETURN(guid, MFVideoFormat_NV11); //     FCC('NV11')
    IF_EQUAL_RETURN(guid, MFVideoFormat_NV12); //     FCC('NV12')
    IF_EQUAL_RETURN(guid, MFVideoFormat_P010); //     FCC('P010')
    IF_EQUAL_RETURN(guid, MFVideoFormat_P016); //     FCC('P016')
    IF_EQUAL_RETURN(guid, MFVideoFormat_P210); //     FCC('P210')
    IF_EQUAL_RETURN(guid, MFVideoFormat_P216); //     FCC('P216')
    IF_EQUAL_RETURN(guid, MFVideoFormat_RGB24); //    D3DFMT_R8G8B8 
    IF_EQUAL_RETURN(guid, MFVideoFormat_RGB32); //    D3DFMT_X8R8G8B8 
    IF_EQUAL_RETURN(guid, MFVideoFormat_RGB555); //   D3DFMT_X1R5G5B5 
    IF_EQUAL_RETURN(guid, MFVideoFormat_RGB565); //   D3DFMT_R5G6B5 
    IF_EQUAL_RETURN(guid, MFVideoFormat_RGB8);
    IF_EQUAL_RETURN(guid, MFVideoFormat_UYVY); //     FCC('UYVY')
    IF_EQUAL_RETURN(guid, MFVideoFormat_v210); //     FCC('v210')
    IF_EQUAL_RETURN(guid, MFVideoFormat_v410); //     FCC('v410')
    IF_EQUAL_RETURN(guid, MFVideoFormat_WMV1); //     FCC('WMV1')
    IF_EQUAL_RETURN(guid, MFVideoFormat_WMV2); //     FCC('WMV2')
    IF_EQUAL_RETURN(guid, MFVideoFormat_WMV3); //     FCC('WMV3')
    IF_EQUAL_RETURN(guid, MFVideoFormat_WVC1); //     FCC('WVC1')
    IF_EQUAL_RETURN(guid, MFVideoFormat_Y210); //     FCC('Y210')
    IF_EQUAL_RETURN(guid, MFVideoFormat_Y216); //     FCC('Y216')
    IF_EQUAL_RETURN(guid, MFVideoFormat_Y410); //     FCC('Y410')
    IF_EQUAL_RETURN(guid, MFVideoFormat_Y416); //     FCC('Y416')
    IF_EQUAL_RETURN(guid, MFVideoFormat_Y41P);
    IF_EQUAL_RETURN(guid, MFVideoFormat_Y41T);
    IF_EQUAL_RETURN(guid, MFVideoFormat_YUY2); //     FCC('YUY2')
    IF_EQUAL_RETURN(guid, MFVideoFormat_YV12); //     FCC('YV12')
    IF_EQUAL_RETURN(guid, MFVideoFormat_YVYU);

    IF_EQUAL_RETURN(guid, MFAudioFormat_PCM); //              WAVE_FORMAT_PCM 
    IF_EQUAL_RETURN(guid, MFAudioFormat_Float); //            WAVE_FORMAT_IEEE_FLOAT 
    IF_EQUAL_RETURN(guid, MFAudioFormat_DTS); //              WAVE_FORMAT_DTS 
    IF_EQUAL_RETURN(guid, MFAudioFormat_Dolby_AC3_SPDIF); //  WAVE_FORMAT_DOLBY_AC3_SPDIF 
    IF_EQUAL_RETURN(guid, MFAudioFormat_DRM); //              WAVE_FORMAT_DRM 
    IF_EQUAL_RETURN(guid, MFAudioFormat_WMAudioV8); //        WAVE_FORMAT_WMAUDIO2 
    IF_EQUAL_RETURN(guid, MFAudioFormat_WMAudioV9); //        WAVE_FORMAT_WMAUDIO3 
    IF_EQUAL_RETURN(guid, MFAudioFormat_WMAudio_Lossless); // WAVE_FORMAT_WMAUDIO_LOSSLESS 
    IF_EQUAL_RETURN(guid, MFAudioFormat_WMASPDIF); //         WAVE_FORMAT_WMASPDIF 
    IF_EQUAL_RETURN(guid, MFAudioFormat_MSP1); //             WAVE_FORMAT_WMAVOICE9 
    IF_EQUAL_RETURN(guid, MFAudioFormat_MP3); //              WAVE_FORMAT_MPEGLAYER3 
    IF_EQUAL_RETURN(guid, MFAudioFormat_MPEG); //             WAVE_FORMAT_MPEG 
    IF_EQUAL_RETURN(guid, MFAudioFormat_AAC); //              WAVE_FORMAT_MPEG_HEAAC 
    IF_EQUAL_RETURN(guid, MFAudioFormat_ADTS); //             WAVE_FORMAT_MPEG_ADTS_AAC 

    return NULL;
}


MFVideoCaptureDevice::MFVideoCaptureDevice(IntPtr nMFActivate)
{
	MFActivate = nMFActivate;
	quit = true;
	VideoFormats = gcnew System::Collections::Generic::List<VideoCaptureRate ^>();
	SourceDevice = IntPtr::Zero;
	SourceReader = IntPtr::Zero;
	HRESULT hr = CoInitializeEx(0, COINIT_MULTITHREADED);
	hr = MFStartup(MF_VERSION);


	IMFActivate *pActivateDevice = (IMFActivate *)MFActivate.ToPointer();
    WCHAR *szFriendlyName = NULL;
    
    UINT32 cchName;
    hr = pActivateDevice->GetAllocatedString(MF_DEVSOURCE_ATTRIBUTE_FRIENDLY_NAME, &szFriendlyName, &cchName);
	DisplayName = gcnew System::String(szFriendlyName);
	CoTaskMemFree(szFriendlyName);

	UINT32 linkLength;
	WCHAR szSymbolicLink[256];
    hr = pActivateDevice->GetString(MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK, szSymbolicLink, 256, &linkLength);
	UniqueName = gcnew System::String(szSymbolicLink);
}

MFVideoCaptureDevice::~MFVideoCaptureDevice()
{
	if (MFActivate != IntPtr::Zero)
	{
		IMFActivate *pActivateDevice = (IMFActivate *)MFActivate.ToPointer();
		pActivateDevice->Release();
		pActivateDevice = NULL;
		MFActivate = IntPtr::Zero;
	}

	if (SourceReader != IntPtr::Zero)
	{
		IMFSourceReader *pReader  = (IMFSourceReader *) SourceReader.ToPointer();
		pReader->Release();
		pReader = NULL;
		SourceReader = IntPtr::Zero;
	}

	if (SourceDevice != IntPtr::Zero)
	{
		IMFMediaSource *pMediaSource = (IMFMediaSource *) SourceDevice.ToPointer();
		pMediaSource->Release();
		pMediaSource = NULL;
		SourceDevice = IntPtr::Zero;
	}

}

array<MFVideoCaptureDevice ^> ^MFVideoCaptureDevice::GetCaptureDevices()
{
	System::Collections::Generic::List<MFVideoCaptureDevice ^> ^Devices = gcnew System::Collections::Generic::List<MFVideoCaptureDevice ^>();

	//// Enumerate all capture devices, then enumerate the formats each one supports
    IMFAttributes *pAttributes = NULL;
    IMFActivate **ppDevices = NULL;

    // Create an attribute store to specify the enumeration parameters.
    HRESULT hr = MFCreateAttributes(&pAttributes, 1);

    if (FAILED(hr))
		return Devices->ToArray();

    // Source type: video capture devices
    hr = pAttributes->SetGUID(MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE, MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID);
    if (FAILED(hr))
    {
		pAttributes->Release();
		pAttributes = NULL;
        return Devices->ToArray();
    }

    // Enumerate devices.
    UINT32 count;
    hr = MFEnumDeviceSources(pAttributes, &ppDevices, &count);
    if (FAILED(hr))
    {
		pAttributes->Release();
		pAttributes = NULL;
        return Devices->ToArray();
	}

    if (count == 0) /// No devices
    {
		pAttributes->Release();
		pAttributes = NULL;
	    CoTaskMemFree(ppDevices);
        return Devices->ToArray();
    }


	for (int i=0; i<count; i++)
	{
	   IntPtr NextMFActivate = IntPtr(ppDevices[i]);
	   MFVideoCaptureDevice ^NextDevice = gcnew MFVideoCaptureDevice(NextMFActivate);
	   NextDevice->Load();
	   Devices->Add(NextDevice);
	}

    pAttributes->Release();
	pAttributes = NULL;
	CoTaskMemFree(ppDevices);
	return Devices->ToArray();
}



void MFVideoCaptureDevice::Load()
{
	if (SourceDevice != IntPtr::Zero)
		return;

    IMFActivate *pActivateDevice = (IMFActivate *)MFActivate.ToPointer();

	HRESULT hr;

	IMFMediaSource *pMediaSource = NULL;

    // Create the media source object.
    hr = pActivateDevice->ActivateObject(IID_PPV_ARGS(&pMediaSource));
    if (FAILED(hr))
		return;

    pMediaSource->AddRef();
	SourceDevice = IntPtr(pMediaSource);


	IMFSourceReader *pReader = NULL;
	
	IMFAttributes *pAttributes = NULL;
    hr = MFCreateAttributes(&pAttributes, 1);
	hr = pAttributes->SetUINT32(MF_SOURCE_READER_ENABLE_VIDEO_PROCESSING, 1);
	hr = MFCreateSourceReaderFromMediaSource(pMediaSource, pAttributes, &pReader);
    if (FAILED(hr))
		return;
	SourceReader = IntPtr(pReader);

	pAttributes->Release();
	pAttributes = NULL;


	 GUID loopmajortype;

	 VideoFormats->Clear();
	 /// Get the media types supported by this webcam/etc
	 for (int i=0; i<3; i++)
	 {
		DWORD dwMediaTypeIndex = 0;
		 DWORD dwStreamIndex = i; /// stream index... seems to correspond to the direct
		 hr = S_OK;
		 while (SUCCEEDED(hr))
		{
			IMFMediaType *pNativeType = NULL;
		
			hr = pReader->GetNativeMediaType(dwStreamIndex, dwMediaTypeIndex, &pNativeType);
			if (hr == MF_E_NO_MORE_TYPES)
			{
				hr = S_OK;
				break;
			}
			else if (SUCCEEDED(hr))
			{
				// Examine the media type. (Not shown.)
				pNativeType->GetMajorType(&loopmajortype);

				System::String ^strType = gcnew System::String(GetGUIDNameConst(loopmajortype));

				if (loopmajortype == MFMediaType_Audio)
				{
				}
				else if (loopmajortype == MFMediaType_Video)
				{
					MFVIDEOFORMAT *vf = NULL;
					AM_MEDIA_TYPE *type = NULL;
					pNativeType->GetRepresentation(FORMAT_MFVideoFormat, (void **)&type);
					vf = (MFVIDEOFORMAT *)type->pbFormat;

				
					//LogMediaType(pNativeType);

					VideoCaptureRate ^format = gcnew VideoCaptureRate(vf->videoInfo.dwWidth, vf->videoInfo.dwHeight, vf->videoInfo.FramesPerSecond.Numerator, 2000000);
					format->StreamIndex = i;
					format->VideoFormatString = gcnew System::String(GetGUIDNameConst(type->subtype));
					if (type->subtype == MFVideoFormat_MJPG)
					{
						format->VideoDataFormat = VideoDataFormat::MJPEG;
						bool bHas = false;
						for (int i=0; i<VideoFormats->Count; i++)
						{
							VideoCaptureRate ^nextformat = VideoFormats[i];
							if( (nextformat->Width == format->Width) && (nextformat->Height == format->Height) && (nextformat->FrameRate == format->FrameRate) )
							{
								bHas = true;
								break;
							}
						}
						if (bHas == false)
						   VideoFormats->Add(format);
					}
					else if ( (type->subtype == MFVideoFormat_YUY2) || (type->subtype == MFVideoFormat_RGB24) || (type->subtype == MFVideoFormat_RGB32))
					{
						format->VideoDataFormat = VideoDataFormat::RGB32;
						bool bHas = false;
						for (int i=0; i<VideoFormats->Count; i++)
						{
							VideoCaptureRate ^nextformat = VideoFormats[i];
							if( (nextformat->Width == format->Width) && (nextformat->Height == format->Height) && (nextformat->FrameRate == format->FrameRate) )
							{
								bHas = true;
								break;
							}
						}
						if (bHas == false)
						   VideoFormats->Add(format);
					}
					else if (type->subtype == MFVideoFormat_H264)
					{
						format->VideoDataFormat = VideoDataFormat::H264;
	 				   VideoFormats->Add(format);
					}
					else
					{
						format->VideoDataFormat = VideoDataFormat::Unknown;
						VideoFormats->Add(format);
					}

					pNativeType->FreeRepresentation(FORMAT_MFVideoFormat, type);

				

				}
				else if (loopmajortype == MFMediaType_Binary)
				{
					Sleep(0);
				}
				else
				{
					Sleep(0);
				}

				pNativeType->Release();
			}
			++dwMediaTypeIndex;
		}
	}

	 if (SourceReader != IntPtr::Zero)
	{
		IMFSourceReader *pReader  = (IMFSourceReader *) SourceReader.ToPointer();
		pReader->Release();
		pReader = NULL;
		SourceReader = IntPtr::Zero;
	}

	if (SourceDevice != IntPtr::Zero)
	{
		IMFMediaSource *pMediaSource = (IMFMediaSource *) SourceDevice.ToPointer();
		pMediaSource->Release();
		pMediaSource = NULL;
		SourceDevice = IntPtr::Zero;
	}
}

bool MFVideoCaptureDevice::Start(VideoCaptureRate ^videoformat)
{
	if (quit == false)
		return false;

	ActiveVideoFormat = videoformat;
   quit = false;
   CaptureThread = gcnew System::Threading::Thread(gcnew System::Threading::ThreadStart(this, &MFVideoCaptureDevice::OurCaptureThread));
   CaptureThread->Name = "Video Capture Thread";
   CaptureThread->IsBackground = true;
   CaptureThread->Start();

   return true;
}

void MFVideoCaptureDevice::Stop()
{
	quit = true;
}

void MFVideoCaptureDevice::OurCaptureThread()
{
	IMFMediaSource *pMediaSource = NULL;
	IMFSourceReader *pReader = NULL;
    IMFActivate *pActivateDevice = NULL;

	//// Enumerate all capture devices, then enumerate the formats each one supports
    IMFAttributes *pAttributes = NULL;
    IMFActivate **ppDevices = NULL;

    // Create an attribute store to specify the enumeration parameters.
    HRESULT hr = MFCreateAttributes(&pAttributes, 1);

    if (FAILED(hr))
		return;

    // Source type: video capture devices
    hr = pAttributes->SetGUID(MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE, MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID);
    if (FAILED(hr))
    {
		pAttributes->Release();
		pAttributes = NULL;
        return;
    }

    // Enumerate devices.
    UINT32 count;
    hr = MFEnumDeviceSources(pAttributes, &ppDevices, &count);
    if (FAILED(hr))
    {
		pAttributes->Release();
		pAttributes = NULL;
        return;
	}

    if (count == 0) /// No devices
    {
		pAttributes->Release();
		pAttributes = NULL;
	    CoTaskMemFree(ppDevices);
        return;
    }


	for (int i=0; i<count; i++)
	{
		IMFActivate *nextact = ppDevices[i];
		/// Find us

		UINT32 linkLength;
		WCHAR szSymbolicLink[256];
		hr = nextact->GetString(MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK, szSymbolicLink, 256, &linkLength);
		System::String ^NextUniqueName = gcnew System::String(szSymbolicLink);

		if (this->UniqueName == NextUniqueName)
		{
			pActivateDevice = nextact;
		   break;
		}
	}

    pAttributes->Release();
	pAttributes = NULL;
	CoTaskMemFree(ppDevices);
	
	if (pActivateDevice == NULL)
		return;



    // Create the media source object.
    hr = pActivateDevice->ActivateObject(IID_PPV_ARGS(&pMediaSource));
    if (FAILED(hr))
		return;

    pMediaSource->AddRef();
	SourceDevice = IntPtr(pMediaSource);

	IMFAttributes *pAttributes2 = NULL;
    hr = MFCreateAttributes(&pAttributes2, 1);
	hr = pAttributes2->SetUINT32(MF_SOURCE_READER_ENABLE_VIDEO_PROCESSING, 1);
	hr = MFCreateSourceReaderFromMediaSource(pMediaSource, pAttributes2, &pReader);
    if (FAILED(hr))
		return;
	SourceReader = IntPtr(pReader);

	pAttributes2->Release();
	pAttributes2 = NULL;






	 DWORD dwMediaTypeIndex = 0;
	 

	 GUID loopmajortype;

	 /// Get the media types supported by this webcam/etc
	 while (SUCCEEDED(hr))
    {
        IMFMediaType *pNativeType = NULL;
		
		DWORD dwStreamIndex = ActiveVideoFormat->StreamIndex;
        hr = pReader->GetNativeMediaType(dwStreamIndex, dwMediaTypeIndex, &pNativeType);
        if (hr == MF_E_NO_MORE_TYPES)
        {
            hr = S_OK;
            break;
        }
        else if (SUCCEEDED(hr))
        {
            // Examine the media type. (Not shown.)
			pNativeType->GetMajorType(&loopmajortype);
			if (loopmajortype == MFMediaType_Audio)
			{
			}
			else if (loopmajortype == MFMediaType_Video)
			{
				MFVIDEOFORMAT *vf = NULL;
				AM_MEDIA_TYPE *type = NULL;
				pNativeType->GetRepresentation(FORMAT_MFVideoFormat, (void **)&type);
				vf = (MFVIDEOFORMAT *)type->pbFormat;
				//LogMediaType(pNativeType);

					/// See if this one matches the video format we have choosen
				if ( (vf->videoInfo.dwWidth == ActiveVideoFormat->Width) &&
					 (vf->videoInfo.dwHeight == ActiveVideoFormat->Height) &&
					 (vf->videoInfo.FramesPerSecond.Numerator == ActiveVideoFormat->FrameRate))

				{
					if ((type->subtype == MFVideoFormat_MJPG) && (ActiveVideoFormat->VideoDataFormat == VideoDataFormat::MJPEG) )
					{
						LogMediaType(pNativeType);
					
						hr = pReader->SetCurrentMediaType(dwStreamIndex, NULL, pNativeType);

						GUID majorType, subtype;
						IMFMediaType *pType = NULL;
						hr = pNativeType->GetGUID(MF_MT_MAJOR_TYPE, &majorType);
						hr = MFCreateMediaType(&pType);
						hr = pType->SetGUID(MF_MT_MAJOR_TYPE, majorType);
						subtype= MFVideoFormat_RGB32;
						hr = pType->SetGUID(MF_MT_SUBTYPE, subtype);

						UINT32 pWidth;
						UINT32 pHeight;
						hr = MFGetAttributeSize(pNativeType, MF_MT_FRAME_SIZE, &pWidth, &pHeight);

						LogMediaType(pType);
						hr = pReader->SetCurrentMediaType(dwStreamIndex, NULL, pType);
						pType->Release();
						hr = pReader->GetCurrentMediaType(dwStreamIndex, &pType);
						LogMediaType(pType);

						break;
					}
					else if (  ((type->subtype == MFVideoFormat_YUY2) || (type->subtype == MFVideoFormat_RGB24) || (type->subtype == MFVideoFormat_RGB32))  && (ActiveVideoFormat->VideoDataFormat == VideoDataFormat::RGB32) )
					{
						GUID majorType, subtype;
						LogMediaType(pNativeType);
						IMFMediaType *pType = NULL;
						//hr = MFCreateMediaType(&pType);
						//((IMFAttributes *)pNativeType)->CopyAllItems((IMFAttributes *)pType);

						hr = pReader->SetCurrentMediaType(dwStreamIndex, NULL, pNativeType);

						hr = pNativeType->GetGUID(MF_MT_MAJOR_TYPE, &majorType);
						hr = MFCreateMediaType(&pType);
						hr = pType->SetGUID(MF_MT_MAJOR_TYPE, majorType);
						subtype= MFVideoFormat_RGB32;
						hr = pType->SetGUID(MF_MT_SUBTYPE, subtype);

						UINT32 pWidth;
						UINT32 pHeight;
						hr = MFGetAttributeSize(pNativeType, MF_MT_FRAME_SIZE, &pWidth, &pHeight);

						LogMediaType(pType);
						hr = pReader->SetCurrentMediaType(dwStreamIndex, NULL, pType);
						pType->Release();
						hr = pReader->GetCurrentMediaType(dwStreamIndex, &pType);
						LogMediaType(pType);

						break;
					}
					else if (type->subtype == MFVideoFormat_H264)
					{
						LogMediaType(pNativeType);	

						hr = pReader->SetCurrentMediaType(dwStreamIndex, NULL, pNativeType);

						GUID majorType, subtype;
						IMFMediaType *pType = NULL;
						hr = pNativeType->GetGUID(MF_MT_MAJOR_TYPE, &majorType);
						hr = MFCreateMediaType(&pType);
						hr = pType->SetGUID(MF_MT_MAJOR_TYPE, majorType);
						subtype= MFVideoFormat_YUY2;
						hr = pType->SetGUID(MF_MT_SUBTYPE, subtype);

						UINT32 pWidth;
						UINT32 pHeight;
						hr = MFGetAttributeSize(pNativeType, MF_MT_FRAME_SIZE, &pWidth, &pHeight);

						LogMediaType(pType);
						hr = pReader->SetCurrentMediaType(dwStreamIndex, NULL, pType);
						pType->Release();

						break;					
					}
					else
					{
					}
				}

				pNativeType->FreeRepresentation(FORMAT_MFVideoFormat, type);

			}
			else if (loopmajortype == MFMediaType_Binary)
			{
			}
			else
			{
			}

            pNativeType->Release();
        }
        ++dwMediaTypeIndex;
    }

	 IMFSample *pSample = NULL;
	 IMFMediaBuffer *pBuffer = NULL;

	 array<unsigned char> ^ arrayBytesRet = nullptr;

	 bool bGotFormat = false;

	 hr = pReader->SetStreamSelection(ActiveVideoFormat->StreamIndex, TRUE);
	  
    while (!quit)
    {
	  DWORD streamIndex=0;
	  DWORD flags= 0;
      LONGLONG llTimeStamp = 0;

	 
        hr = pReader->ReadSample(
            MF_SOURCE_READER_ANY_STREAM, //MF_SOURCE_READER_FIRST_VIDEO_STREAM, //MF_SOURCE_READER_ANY_STREAM,    // Stream index.
            0,                              // Flags.
            &streamIndex,                   // Receives the actual stream index. 
            &flags,                         // Receives status flags.
            &llTimeStamp,                   // Receives the time stamp.
            &pSample                        // Receives the sample or NULL.
            );

		/*}
		catch (std::bad_alloc& b)
		{
			Sleep(0); /// This exception was called by the Axis mjpeg decoder filter.  I un-registered it and the bug went away
		}
*/
		if ((hr == 0) && (bGotFormat == false) )
		{
			IMFMediaType *pType = NULL;
			hr = pReader->GetCurrentMediaType(streamIndex, &pType);
			LogMediaType(pType);
			pType->Release();
			bGotFormat = true;
		}


		if (hr == MF_E_HW_MFT_FAILED_START_STREAMING)
		{
			OnFailStartCapture("Capture failed: Failed to start streaming");
			break;
		}

		if (hr == MF_E_INVALIDREQUEST)
        {
			OnFailStartCapture(String::Format("Capture failed, Invalid Request: {0}", hr));
            break;
        }
		if (FAILED(hr))
        {
			OnFailStartCapture(String::Format("Capture failed: {0}", hr));
            break;
        }

        if (flags & MF_SOURCE_READERF_ENDOFSTREAM)
        {
            //wprintf(L"\tEnd of stream\n");
            quit = true;
        }
        if (flags & MF_SOURCE_READERF_NEWSTREAM)
        {
            //wprintf(L"\tNew stream\n");
        }
        if (flags & MF_SOURCE_READERF_NATIVEMEDIATYPECHANGED)
        {
            //wprintf(L"\tNative type changed\n");
        }
        if (flags & MF_SOURCE_READERF_CURRENTMEDIATYPECHANGED)
        {
            //wprintf(L"\tCurrent type changed\n");
        }
        if (flags & MF_SOURCE_READERF_STREAMTICK)
        {
            //wprintf(L"\tStream tick\n");
        }

        if (flags & MF_SOURCE_READERF_NATIVEMEDIATYPECHANGED)
        {
            //// The format changed. Reconfigure the decoder.
            //hr = ConfigureDecoder(pReader, streamIndex);
            //if (FAILED(hr))
            //{
            //    break;
            //}
        }

        if (pSample != NULL)
        {
			//DWORD dwBufferCount = 0;
			//pSample->GetBufferCount(&dwBufferCount);

		
			DWORD dwLength = 0;
			pSample->GetTotalLength(&dwLength);
			
			if (pBuffer == NULL)
			{
			   MFCreateMemoryBuffer(dwLength, &pBuffer);
			   arrayBytesRet = gcnew array<unsigned char>(dwLength);
			}
			else
			{
				DWORD dwCurrentLen = 0;
				pBuffer->GetCurrentLength(&dwCurrentLen);
				if (dwCurrentLen != dwLength)
				{
					pBuffer->SetCurrentLength(dwLength);
					arrayBytesRet = gcnew array<unsigned char>(dwLength);
				}
			}

			pSample->CopyToBuffer(pBuffer);

			/// Lock our buffer so we can copy our data
			BYTE *pBytes = NULL;
			pBuffer->Lock(&pBytes, NULL, NULL);

			pin_ptr<unsigned char> ppBytesRet = &arrayBytesRet[0];
			unsigned char *pBytesRet = (unsigned char *)ppBytesRet;

			memcpy(pBytesRet, pBytes, dwLength);
			pBuffer->Unlock();


			OnNewFrame(arrayBytesRet, ActiveVideoFormat);


        
			pSample->Release();
			pSample = NULL;
        }
    }


	if (pBuffer != NULL)
	{
		pBuffer->Release();
		pBuffer = NULL;
	}
	hr = pReader->Flush(MF_SOURCE_READER_FIRST_VIDEO_STREAM);

	 if (SourceReader != IntPtr::Zero)
	{
		IMFSourceReader *pReader  = (IMFSourceReader *) SourceReader.ToPointer();
		pReader->Release();
		pReader = NULL;
		SourceReader = IntPtr::Zero;
	}

	if (SourceDevice != IntPtr::Zero)
	{
		IMFMediaSource *pMediaSource = (IMFMediaSource *) SourceDevice.ToPointer();
		pMediaSource->Release();
		pMediaSource = NULL;
		SourceDevice = IntPtr::Zero;
	}

}



MFVideoEncoder::MFVideoEncoder()
{
	HRESULT hr = CoInitializeEx(0, COINIT_MULTITHREADED);
	hr = MFStartup(MF_VERSION);
}

MFVideoEncoder::~MFVideoEncoder()
{
	if (SinkWriter != IntPtr::Zero)
	{
		IMFSinkWriter   *pSinkWriter = (IMFSinkWriter   *) SinkWriter.ToPointer();
		pSinkWriter->Release();
		pSinkWriter = NULL;
		SinkWriter = IntPtr::Zero;
	}

}



bool MFVideoEncoder::Start(String ^strFileName, VideoCaptureRate ^videoformat, DateTime dtStart, bool Supply48by16Audio)
{
	return Start(strFileName, videoformat, dtStart, true, Supply48by16Audio);
}

bool MFVideoEncoder::Start(String ^strFileName, VideoCaptureRate ^videoformat, DateTime dtStart, bool bSupplyVideo, bool Supply48by16Audio)
{    

	if ( (bSupplyVideo == false) && (Supply48by16Audio == false) )
		throw gcnew Exception("Must supply video, audio or both");

	StartTime = dtStart;
	VideoFormat = videoformat;
	HRESULT hr;
	
	IMFAttributes *pAttribute;
	hr = MFCreateAttributes(&pAttribute, 1);
	if (videoformat->VideoDataFormat == VideoDataFormat::MP4)
	   hr = pAttribute->SetGUID(MF_TRANSCODE_CONTAINERTYPE, MFTranscodeContainerType_MPEG4);
	else if ((videoformat->VideoDataFormat == VideoDataFormat::WMV9) || (videoformat->VideoDataFormat == VideoDataFormat::WMVSCREEN) || (videoformat->VideoDataFormat == VideoDataFormat::VC1) )
	   hr = pAttribute->SetGUID(MF_TRANSCODE_CONTAINERTYPE, MFTranscodeContainerType_ASF);

	hr = pAttribute->SetUINT32(MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, TRUE);


	IMFSinkWriter   *pSinkWriter = NULL;
    IMFMediaType    *pMediaTypeOut = NULL;   
    IMFMediaType    *pAudioMediaTypeOut = NULL;   
    IMFMediaType    *pMediaTypeIn = NULL;   
	IMFMediaType    *pAudioMediaTypeIn = NULL;   
    DWORD           streamIndexVideo;     
	DWORD			streamIndexAudio;

    IntPtr ptrstr = System::Runtime::InteropServices::Marshal::StringToCoTaskMemAuto(strFileName);

    hr = MFCreateSinkWriterFromURL((const wchar_t *)ptrstr.ToPointer(), NULL, pAttribute, &pSinkWriter);
    System::Runtime::InteropServices::Marshal::FreeCoTaskMem(ptrstr);

	
	

	
    // Set the output media type.
	hr = MFCreateMediaType(&pMediaTypeOut);   

	if (bSupplyVideo == true)
	{
		hr = pMediaTypeOut->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Video);     
		if (videoformat->VideoDataFormat == VideoDataFormat::MP4)
			hr = pMediaTypeOut->SetGUID(MF_MT_SUBTYPE, MFVideoFormat_H264);   
		else if (videoformat->VideoDataFormat == VideoDataFormat::WMV9)
			hr = pMediaTypeOut->SetGUID(MF_MT_SUBTYPE, MFVideoFormat_WMV3);   
		else if (videoformat->VideoDataFormat == VideoDataFormat::WMVSCREEN)
			hr = pMediaTypeOut->SetGUID(MF_MT_SUBTYPE, MFVideoFormat_MSS2);   
		else if (videoformat->VideoDataFormat == VideoDataFormat::VC1)
			hr = pMediaTypeOut->SetGUID(MF_MT_SUBTYPE, MFVideoFormat_WVC1);   
		else
			hr = pMediaTypeOut->SetGUID(MF_MT_SUBTYPE, MFVideoFormat_H264);   

		hr = pMediaTypeOut->SetUINT32(MF_MT_AVG_BITRATE, videoformat->EncodingBitRate);   
		hr = pMediaTypeOut->SetUINT32(MF_MT_INTERLACE_MODE, MFVideoInterlace_Progressive);   
		hr = MFSetAttributeSize(pMediaTypeOut, MF_MT_FRAME_SIZE, videoformat->Width, videoformat->Height);
		hr = MFSetAttributeRatio(pMediaTypeOut, MF_MT_FRAME_RATE, videoformat->FrameRate, 1);   
		hr = MFSetAttributeRatio(pMediaTypeOut, MF_MT_PIXEL_ASPECT_RATIO, 1, 1);   

		if (videoformat->VideoDataFormat == VideoDataFormat::WMVSCREEN)
		{
			//hr = pMediaTypeOut->SetUINT32(MFPKEY_ASFOVERHEADPERFRAME, MFVideoInterlace_Progressive);   
		
		}


		hr = pSinkWriter->AddStream(pMediaTypeOut, &streamIndexVideo);   


		// Set the video input media type.
		hr = MFCreateMediaType(&pMediaTypeIn);   
		hr = pMediaTypeIn->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Video);   
		hr = pMediaTypeIn->SetGUID(MF_MT_SUBTYPE, MFVideoFormat_RGB32);     
		hr = pMediaTypeIn->SetUINT32(MF_MT_INTERLACE_MODE, MFVideoInterlace_Progressive);   
		hr = MFSetAttributeSize(pMediaTypeIn, MF_MT_FRAME_SIZE, videoformat->Width, videoformat->Height);   
		hr = MFSetAttributeRatio(pMediaTypeIn, MF_MT_FRAME_RATE, videoformat->FrameRate, 1);   
		hr = MFSetAttributeRatio(pMediaTypeIn, MF_MT_PIXEL_ASPECT_RATIO, 1, 1);   
		hr = pMediaTypeIn->SetUINT32(MF_MT_DEFAULT_STRIDE, videoformat->Width*4);   
		hr = pSinkWriter->SetInputMediaType(streamIndexVideo, pMediaTypeIn, NULL);   
	}


	if (Supply48by16Audio == true)
	{
			/// Add an audio output stream

		if (videoformat->VideoDataFormat == VideoDataFormat::MP4)
		{
			hr = MFCreateMediaType(&pAudioMediaTypeOut);   
			hr = pAudioMediaTypeOut->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Audio);     
			hr = pAudioMediaTypeOut->SetGUID(MF_MT_SUBTYPE, MFAudioFormat_AAC);   
			hr = pAudioMediaTypeOut->SetUINT32(MF_MT_AUDIO_BITS_PER_SAMPLE, 16);   
			hr = pAudioMediaTypeOut->SetUINT32(MF_MT_AUDIO_SAMPLES_PER_SECOND, 48000);   
			hr = pAudioMediaTypeOut->SetUINT32(MF_MT_AUDIO_NUM_CHANNELS, 1);   
			hr = pAudioMediaTypeOut->SetUINT32(MF_MT_AUDIO_AVG_BYTES_PER_SECOND, 12000); 
		}
		else /// windows media audio
		{
			hr = MFCreateMediaType(&pAudioMediaTypeOut);   
			hr = pAudioMediaTypeOut->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Audio);     
			hr = pAudioMediaTypeOut->SetGUID(MF_MT_SUBTYPE, MFAudioFormat_WMAudioV9);   
			hr = pAudioMediaTypeOut->SetUINT32(MF_MT_AUDIO_BITS_PER_SAMPLE, 16);   
			hr = pAudioMediaTypeOut->SetUINT32(MF_MT_AUDIO_SAMPLES_PER_SECOND, 48000);   
			hr = pAudioMediaTypeOut->SetUINT32(MF_MT_AUDIO_NUM_CHANNELS, 1);   
			hr = pAudioMediaTypeOut->SetUINT32(MF_MT_AUDIO_AVG_BYTES_PER_SECOND, 12000); 
		}
		//hr = pAudioMediaTypeOut->SetUINT32(MF_MT_AUDIO_BLOCK_ALIGNMENT, 1);
		//hr = pAudioMediaTypeOut->SetUINT32(MF_MT_AAC_AUDIO_PROFILE_LEVEL_INDICATION, 0xFE);
		//hr = pAudioMediaTypeOut->SetUINT32(MF_MT_AAC_PAYLOAD_TYPE, 0);


		hr = pSinkWriter->AddStream(pAudioMediaTypeOut, &streamIndexAudio);   



		/// Set the audio input stream
		hr = MFCreateMediaType(&pAudioMediaTypeIn);   
		hr = pAudioMediaTypeIn->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Audio);  
		hr = pAudioMediaTypeIn->SetGUID(MF_MT_SUBTYPE, MFAudioFormat_PCM);
		hr = pAudioMediaTypeIn->SetUINT32(MF_MT_AUDIO_BITS_PER_SAMPLE, 16);
		hr = pAudioMediaTypeIn->SetUINT32(MF_MT_AUDIO_SAMPLES_PER_SECOND, 48000);
		hr = pAudioMediaTypeIn->SetUINT32(MF_MT_AUDIO_NUM_CHANNELS, 1);
//		hr = pAudioMediaTypeIn->SetUINT32(MF_MT_AUDIO_AVG_BYTES_PER_SECOND, 96000);

		hr = pSinkWriter->SetInputMediaType(streamIndexAudio, pAudioMediaTypeIn, NULL);   
		if (hr == MF_E_INVALIDMEDIATYPE)
		{
			System::Diagnostics::Debug::WriteLine("");
		}
		else
		{
			System::Diagnostics::Debug::WriteLine("");
		}
	}



    // Tell the sink writer to start accepting data.
    if (SUCCEEDED(hr))
    {
        hr = pSinkWriter->BeginWriting();
    }

    // Return the pointer to the caller.
    if (SUCCEEDED(hr))
    {
        //pSinkWriter->AddRef();
		SinkWriter = IntPtr(pSinkWriter);
		StreamIndexVideo = streamIndexVideo;
		StreamIndexAudio = streamIndexAudio;
    }

	if (pMediaTypeOut != NULL)
	{
		pMediaTypeOut->Release();
		pMediaTypeOut = NULL;
	}

	if (pAudioMediaTypeOut != NULL)
	{
		pAudioMediaTypeOut->Release();
		pAudioMediaTypeOut = NULL;
	}

	if (pMediaTypeIn != NULL)
	{
		pMediaTypeIn->Release();
		pMediaTypeIn = NULL;
	}

	if (pAudioMediaTypeIn != NULL)
	{
		pAudioMediaTypeIn->Release();
		pAudioMediaTypeIn = NULL;
	}

	pAttribute->Release();
	pAttribute = NULL;


	return true;
}

void MFVideoEncoder::AddVideoFrame(array<unsigned char> ^RGBData, System::DateTime dtFrameAt)
{
	IMFSinkWriter   *pSinkWriter = (IMFSinkWriter   *) SinkWriter.ToPointer();

	IMFSample *pSample = NULL;
    IMFMediaBuffer *pBuffer = NULL;

    const LONG cbWidth = 4 * VideoFormat->Width;
    const DWORD cbBuffer = cbWidth * VideoFormat->Height;
	if (RGBData->Length != cbBuffer)
		throw gcnew Exception(String::Format("Incoming buffer is not the right size, its size is {0}, but should be {1}", RGBData->Length, cbBuffer));

	TimeSpan duration = dtFrameAt-StartTime;
	double fMs = duration.TotalMilliseconds;        
	UINT64 rtStart = fMs*1000*10;
	UINT64 rtDuration; 
	MFFrameRateToAverageTimePerFrame(VideoFormat->FrameRate, 1, &rtDuration);

    BYTE *pData = NULL;

    // Create a new memory buffer.
    HRESULT hr = MFCreateMemoryBuffer(cbBuffer, &pBuffer);
    if (FAILED(hr))
		throw gcnew Exception(String::Format("Could not create memory buffer: {0}", hr));

    // Lock the buffer and copy the video frame to the buffer.
    hr = pBuffer->Lock(&pData, NULL, NULL);

	pin_ptr<unsigned char> ppBytesIn = &RGBData[0];
	unsigned char *pBytesIn = (unsigned char *)ppBytesIn;
	
	///// H.264 is upside down, copy by hand
	//// Fixed... Microsoft didn't set the default stride in their examples, after setting this everything is oriented correctly
	//for (int y=VideoFormat->Height-1; y>=0; y--)
	//{
	//	int nCopyToIdx = ((VideoFormat->Height-1)-y)*cbWidth;
	//	int nCopyFromIdx =y*cbWidth;
	//	memcpy(pData+nCopyToIdx, pBytesIn+nCopyFromIdx, cbWidth);
	//}   


    if (SUCCEEDED(hr))
    {
        hr = MFCopyImage(
            pData,                      // Destination buffer.
            cbWidth,                    // Destination stride.
            (BYTE*)pBytesIn,    // First row in source image.
            cbWidth,                    // Source stride.
            cbWidth,                    // Image width in bytes.
            VideoFormat->Height                // Image height in pixels.
            );
    }
    if (pBuffer)
    {
        pBuffer->Unlock();
    }

    // Set the data length of the buffer.
    hr = pBuffer->SetCurrentLength(cbBuffer);

    // Create a media sample and add the buffer to the sample.
    hr = MFCreateSample(&pSample);
    hr = pSample->AddBuffer(pBuffer);

    // Set the time stamp and the duration.
    hr = pSample->SetSampleTime(rtStart);
    hr = pSample->SetSampleDuration(rtDuration);

    // Send the sample to the Sink Writer.
    hr = pSinkWriter->WriteSample(StreamIndexVideo, pSample);

	System::Diagnostics::Debug::WriteLine("WriteSample returned {0}", hr);

	pSample->Release();
	pSample= NULL;

	pBuffer->Release();
	pBuffer = NULL;
	
}

void MFVideoEncoder::AddAudioFrame(array<unsigned char> ^PCMData48KHz16Bit, DateTime dtStart)
{
	IMFSinkWriter   *pSinkWriter = (IMFSinkWriter   *) SinkWriter.ToPointer();

	IMFSample *pSample = NULL;
    IMFMediaBuffer *pBuffer = NULL;

	TimeSpan duration = dtStart-StartTime;
	double fMs = duration.TotalMilliseconds;        
	UINT64 rtStart = fMs*1000*10;
	UINT64 rtDuration; 
	MFFrameRateToAverageTimePerFrame(VideoFormat->FrameRate, 1, &rtDuration);

    BYTE *pData = NULL;

    // Create a new memory buffer.
    HRESULT hr = MFCreateMemoryBuffer(PCMData48KHz16Bit->Length, &pBuffer);
    if (FAILED(hr))
		throw gcnew Exception(String::Format("Could not create memory buffer: {0}", hr));

    // Lock the buffer and copy the video frame to the buffer.
    hr = pBuffer->Lock(&pData, NULL, NULL);

	pin_ptr<unsigned char> ppBytesIn = &PCMData48KHz16Bit[0];
	unsigned char *pBytesIn = (unsigned char *)ppBytesIn;

	memcpy(pData, pBytesIn, PCMData48KHz16Bit->Length);

    pBuffer->Unlock();

    // Set the data length of the buffer.
    hr = pBuffer->SetCurrentLength(PCMData48KHz16Bit->Length);

    // Create a media sample and add the buffer to the sample.
    hr = MFCreateSample(&pSample);
    hr = pSample->AddBuffer(pBuffer);

    // Set the time stamp and the duration.
    hr = pSample->SetSampleTime(rtStart);
    hr = pSample->SetSampleDuration(rtDuration);

    // Send the sample to the Sink Writer.
    hr = pSinkWriter->WriteSample(StreamIndexAudio, pSample);

	System::Diagnostics::Debug::WriteLine("Audio:WriteSample returned {0}", hr);

	pSample->Release();
	pSample= NULL;

	pBuffer->Release();
	pBuffer = NULL;
}

void MFVideoEncoder::Stop()
{
	IMFSinkWriter   *pSinkWriter = (IMFSinkWriter   *) SinkWriter.ToPointer();
	
	pSinkWriter->Finalize();
	pSinkWriter->Release();
	pSinkWriter = NULL;
	SinkWriter = IntPtr::Zero;
}









MFAudioDevice::MFAudioDevice(IntPtr nMFActivate)
{
	MFActivate = nMFActivate;
	quit = true;
	SourceDevice = IntPtr::Zero;
	SourceReader = IntPtr::Zero;
	HRESULT hr = CoInitializeEx(0, COINIT_MULTITHREADED);
	hr = MFStartup(MF_VERSION);


	IMFActivate *pActivateDevice = (IMFActivate *)MFActivate.ToPointer();
    WCHAR *szFriendlyName = NULL;
    
    UINT32 cchName;
    hr = pActivateDevice->GetAllocatedString(MF_DEVSOURCE_ATTRIBUTE_FRIENDLY_NAME, &szFriendlyName, &cchName);
	Name = gcnew System::String(szFriendlyName);
	CoTaskMemFree(szFriendlyName);
}

MFAudioDevice::~MFAudioDevice()
{
	if (MFActivate != IntPtr::Zero)
	{
		IMFActivate *pActivateDevice = (IMFActivate *)MFActivate.ToPointer();
		pActivateDevice->Release();
		pActivateDevice = NULL;
		MFActivate = IntPtr::Zero;
	}

	if (SourceReader != IntPtr::Zero)
	{
		IMFSourceReader *pReader  = (IMFSourceReader *) SourceReader.ToPointer();
		pReader->Release();
		pReader = NULL;
		SourceReader = IntPtr::Zero;
	}

	if (SourceDevice != IntPtr::Zero)
	{
		IMFMediaSource *pMediaSource = (IMFMediaSource *) SourceDevice.ToPointer();
		pMediaSource->Release();
		pMediaSource = NULL;
		SourceDevice = IntPtr::Zero;
	}

}

array<MFAudioDevice ^> ^MFAudioDevice::GetCaptureDevices()
{
	System::Collections::Generic::List<MFAudioDevice ^> ^Devices = gcnew System::Collections::Generic::List<MFAudioDevice ^>();

	//// Enumerate all capture devices, then enumerate the formats each one supports
    IMFAttributes *pAttributes = NULL;
    IMFActivate **ppDevices = NULL;

    // Create an attribute store to specify the enumeration parameters.
    HRESULT hr = MFCreateAttributes(&pAttributes, 1);

    if (FAILED(hr))
		return Devices->ToArray();

    // Source type: audio capture devices
    hr = pAttributes->SetGUID(MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE, MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_AUDCAP_GUID);
    if (FAILED(hr))
    {
		pAttributes->Release();
		pAttributes = NULL;
        return Devices->ToArray();
    }

    // Enumerate devices.
    UINT32 count;
    hr = MFEnumDeviceSources(pAttributes, &ppDevices, &count);
    if (FAILED(hr))
    {
		pAttributes->Release();
		pAttributes = NULL;
        return Devices->ToArray();
	}

    if (count == 0) /// No devices
    {
		pAttributes->Release();
		pAttributes = NULL;
	    CoTaskMemFree(ppDevices);
        return Devices->ToArray();
    }


	for (int i=0; i<count; i++)
	{
	   IntPtr NextMFActivate = IntPtr(ppDevices[i]);
	   MFAudioDevice ^NextDevice = gcnew MFAudioDevice(NextMFActivate);
	   NextDevice->Load();
	   Devices->Add(NextDevice);
	}

    pAttributes->Release();
	pAttributes = NULL;
	CoTaskMemFree(ppDevices);
	return Devices->ToArray();
}



void MFAudioDevice::Load()
{
	if (SourceDevice != IntPtr::Zero)
		return;

    IMFActivate *pActivateDevice = (IMFActivate *)MFActivate.ToPointer();

	HRESULT hr;

	IMFMediaSource *pMediaSource = NULL;

    // Create the media source object.
    hr = pActivateDevice->ActivateObject(IID_PPV_ARGS(&pMediaSource));
    if (FAILED(hr))
		return;

    pMediaSource->AddRef();
	SourceDevice = IntPtr(pMediaSource);


	IMFSourceReader *pReader = NULL;
	
	IMFAttributes *pAttributes = NULL;
    hr = MFCreateAttributes(&pAttributes, 1);
	hr = pAttributes->SetUINT32(MF_SOURCE_READER_ENABLE_VIDEO_PROCESSING, 1);
	hr = MFCreateSourceReaderFromMediaSource(pMediaSource, pAttributes, &pReader);
    if (FAILED(hr))
		return;
	SourceReader = IntPtr(pReader);

	pAttributes->Release();
	pAttributes = NULL;

	DWORD dwMediaTypeIndex = 0;
	 DWORD dwStreamIndex = 0;
	 hr = S_OK;

	 GUID loopmajortype;

	 /// Get the media types supported by this webcam/etc
	 while (SUCCEEDED(hr))
    {
        IMFMediaType *pNativeType = NULL;
		
        hr = pReader->GetNativeMediaType(dwStreamIndex, dwMediaTypeIndex, &pNativeType);
        if (hr == MF_E_NO_MORE_TYPES)
        {
            hr = S_OK;
            break;
        }
        else if (SUCCEEDED(hr))
        {
            // Examine the media type. (Not shown.)
			pNativeType->GetMajorType(&loopmajortype);
			if (loopmajortype == MFMediaType_Audio)
			{
				AM_MEDIA_TYPE *type = NULL;
				pNativeType->GetRepresentation(AM_MEDIA_TYPE_REPRESENTATION, (void **)&type);
				LogMediaType(pNativeType);
				pNativeType->FreeRepresentation(AM_MEDIA_TYPE_REPRESENTATION, type);
			}
			
			else if (loopmajortype == MFMediaType_Binary)
			{
			}
			else
			{
			}

            pNativeType->Release();
        }
        ++dwMediaTypeIndex;
    }

	 if (SourceReader != IntPtr::Zero)
	{
		IMFSourceReader *pReader  = (IMFSourceReader *) SourceReader.ToPointer();
		pReader->Release();
		pReader = NULL;
		SourceReader = IntPtr::Zero;
	}

	if (SourceDevice != IntPtr::Zero)
	{
		IMFMediaSource *pMediaSource = (IMFMediaSource *) SourceDevice.ToPointer();
		pMediaSource->Release();
		pMediaSource = NULL;
		SourceDevice = IntPtr::Zero;
	}
}

bool MFAudioDevice::Start()
{
	if (quit == false)
		return false;

   quit = false;
   CaptureThread = gcnew System::Threading::Thread(gcnew System::Threading::ThreadStart(this, &MFAudioDevice::OurCaptureThread));
   CaptureThread->Name = "Audio Capture Thread";
   CaptureThread->IsBackground = true;
   CaptureThread->Start();

   return true;
}

void MFAudioDevice::Stop()
{
	quit = true;
}

void MFAudioDevice::OurCaptureThread()
{
	IMFMediaSource *pMediaSource = NULL;
	IMFSourceReader *pReader = NULL;
    IMFActivate *pActivateDevice = NULL;

	//// Enumerate all capture devices, then enumerate the formats each one supports
    IMFAttributes *pAttributes = NULL;
    IMFActivate **ppDevices = NULL;

    // Create an attribute store to specify the enumeration parameters.
    HRESULT hr = MFCreateAttributes(&pAttributes, 1);

    if (FAILED(hr))
		return;

    // Source type: audio capture devices
    hr = pAttributes->SetGUID(MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE, MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_AUDCAP_GUID);
    if (FAILED(hr))
    {
		pAttributes->Release();
		pAttributes = NULL;
        return;
    }

    // Enumerate devices.
    UINT32 count;
    hr = MFEnumDeviceSources(pAttributes, &ppDevices, &count);
    if (FAILED(hr))
    {
		pAttributes->Release();
		pAttributes = NULL;
        return;
	}

    if (count == 0) /// No devices
    {
		pAttributes->Release();
		pAttributes = NULL;
	    CoTaskMemFree(ppDevices);
        return;
    }


	for (int i=0; i<count; i++)
	{
		IMFActivate *nextact = ppDevices[i];
		/// Find us

		WCHAR *szFriendlyName = NULL;
    
		UINT32 cchName;
		hr = nextact->GetAllocatedString(MF_DEVSOURCE_ATTRIBUTE_FRIENDLY_NAME, &szFriendlyName, &cchName);
		System::String ^NextName = gcnew System::String(szFriendlyName);
		CoTaskMemFree(szFriendlyName);
		

		if (this->Name == NextName)
		{
			pActivateDevice = nextact;
		   break;
		}
	}

    pAttributes->Release();
	pAttributes = NULL;
	CoTaskMemFree(ppDevices);
	
	if (pActivateDevice == NULL)
		return;



    // Create the media source object.
    hr = pActivateDevice->ActivateObject(IID_PPV_ARGS(&pMediaSource));
    if (FAILED(hr))
		return;

    pMediaSource->AddRef();
	SourceDevice = IntPtr(pMediaSource);

	IMFAttributes *pAttributes2 = NULL;
    hr = MFCreateAttributes(&pAttributes2, 1);
	hr = pAttributes2->SetUINT32(MF_SOURCE_READER_ENABLE_VIDEO_PROCESSING, 1);
	hr = MFCreateSourceReaderFromMediaSource(pMediaSource, pAttributes2, &pReader);
    if (FAILED(hr))
		return;
	SourceReader = IntPtr(pReader);

	pAttributes2->Release();
	pAttributes2 = NULL;






	 DWORD dwMediaTypeIndex = 0;
	 DWORD dwStreamIndex = 0;
	 

	 GUID loopmajortype;

	 /// Get the media types supported by this webcam/etc
	 while (SUCCEEDED(hr))
    {
        IMFMediaType *pNativeType = NULL;
		
        hr = pReader->GetNativeMediaType(dwStreamIndex, dwMediaTypeIndex, &pNativeType);
        if (hr == MF_E_NO_MORE_TYPES)
        {
            hr = S_OK;
            break;
        }
        else if (SUCCEEDED(hr))
        {
            // Examine the media type. (Not shown.)
			pNativeType->GetMajorType(&loopmajortype);
			if (loopmajortype == MFMediaType_Audio)
			{
				hr = pReader->SetCurrentMediaType(dwStreamIndex, NULL, pNativeType);
				LogMediaType(pNativeType);

				//IMFMediaType *pType = NULL;
				//hr = MFCreateMediaType(&pType);   

				//hr = pType->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Audio);  
				//hr = pType->SetGUID(MF_MT_SUBTYPE, MFAudioFormat_PCM);
				//hr = pType->SetUINT32(MF_MT_AUDIO_BITS_PER_SAMPLE, 16);
				//hr = pType->SetUINT32(MF_MT_AUDIO_SAMPLES_PER_SECOND, 48000);
				//hr = pType->SetUINT32(MF_MT_AUDIO_NUM_CHANNELS, 1);
				//hr = pReader->SetCurrentMediaType(dwStreamIndex, NULL, pType);
				//pType->Release();
						
				break;

			}
			else if (loopmajortype == MFMediaType_Video)
			{
		
			}
			else if (loopmajortype == MFMediaType_Binary)
			{
			}
			else
			{
			}

            pNativeType->Release();
        }
        ++dwMediaTypeIndex;
    }

	 IMFSample *pSample = NULL;
	 IMFMediaBuffer *pBuffer = NULL;

	  
    while (!quit)
    {
	  DWORD streamIndex=0;
	  DWORD flags= 0;
      LONGLONG llTimeStamp = 0;

	 
        hr = pReader->ReadSample(
            MF_SOURCE_READER_FIRST_AUDIO_STREAM, //MF_SOURCE_READER_ANY_STREAM,    // Stream index.
            0,                              // Flags.
            &streamIndex,                   // Receives the actual stream index. 
            &flags,                         // Receives status flags.
            &llTimeStamp,                   // Receives the time stamp.
            &pSample                        // Receives the sample or NULL.
            );

		/*}
		catch (std::bad_alloc& b)
		{
			Sleep(0); /// This exception was called by the Axis mjpeg decoder filter.  I un-registered it and the bug went away
		}
*/
		if (FAILED(hr))
        {
            break;
        }

        if (flags & MF_SOURCE_READERF_ENDOFSTREAM)
        {
            //wprintf(L"\tEnd of stream\n");
            quit = true;
        }
        if (flags & MF_SOURCE_READERF_NEWSTREAM)
        {
            //wprintf(L"\tNew stream\n");
        }
        if (flags & MF_SOURCE_READERF_NATIVEMEDIATYPECHANGED)
        {
            //wprintf(L"\tNative type changed\n");
        }
        if (flags & MF_SOURCE_READERF_CURRENTMEDIATYPECHANGED)
        {
            //wprintf(L"\tCurrent type changed\n");
        }
        if (flags & MF_SOURCE_READERF_STREAMTICK)
        {
            //wprintf(L"\tStream tick\n");
        }

        if (flags & MF_SOURCE_READERF_NATIVEMEDIATYPECHANGED)
        {
            //// The format changed. Reconfigure the decoder.
            //hr = ConfigureDecoder(pReader, streamIndex);
            //if (FAILED(hr))
            //{
            //    break;
            //}
        }

        if (pSample != NULL)
        {
			//DWORD dwBufferCount = 0;
			//pSample->GetBufferCount(&dwBufferCount);

			DWORD dwLength = 0;
			pSample->GetTotalLength(&dwLength);
			
			if (pBuffer == NULL)
			   MFCreateMemoryBuffer(dwLength, &pBuffer);
			else
			{
				DWORD dwCurrentLen = 0;
				pBuffer->GetCurrentLength(&dwCurrentLen);
				if (dwCurrentLen < dwLength)
				{
					pBuffer->SetCurrentLength(dwLength);
				}
			}

			pSample->CopyToBuffer(pBuffer);

			/// Lock our buffer so we can copy our data
			BYTE *pBytes = NULL;
			pBuffer->Lock(&pBytes, NULL, NULL);

			array<unsigned char> ^ arrayBytesRet = gcnew array<unsigned char>(dwLength);
			pin_ptr<unsigned char> ppBytesRet = &arrayBytesRet[0];
			unsigned char *pBytesRet = (unsigned char *)ppBytesRet;

			memcpy(pBytesRet, pBytes, dwLength);
			pBuffer->Unlock();


			OnNewPCMFrame(arrayBytesRet);


        
			pSample->Release();
			pSample = NULL;
        }
    }


	if (pBuffer != NULL)
	{
		pBuffer->Release();
		pBuffer = NULL;
	}
	hr = pReader->Flush(MF_SOURCE_READER_FIRST_AUDIO_STREAM);

	 if (SourceReader != IntPtr::Zero)
	{
		IMFSourceReader *pReader  = (IMFSourceReader *) SourceReader.ToPointer();
		pReader->Release();
		pReader = NULL;
		SourceReader = IntPtr::Zero;
	}

	if (SourceDevice != IntPtr::Zero)
	{
		IMFMediaSource *pMediaSource = (IMFMediaSource *) SourceDevice.ToPointer();
		pMediaSource->Release();
		pMediaSource = NULL;
		SourceDevice = IntPtr::Zero;
	}

}

