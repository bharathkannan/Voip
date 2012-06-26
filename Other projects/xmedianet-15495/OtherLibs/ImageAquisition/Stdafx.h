
// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently,
// but are changed infrequently

#pragma once


#include <windows.h>

// For configuring DMO properties
#include <wmcodecdsp.h>

// For discovering microphone array device
#include <devicetopology.h>

// For functions and definitions used to create output file
#include <dmo.h> // Mo*MediaType
#include <uuids.h> // FORMAT_WaveFormatEx and such
#include <mfapi.h> // FCC
#include <Audioclient.h>

// For string input,output and manipulation
#include <tchar.h>
#include <strsafe.h>
#include <conio.h>
#include <KsProxy.h>
#include <dsound.h>

#include <endpointvolume.h>



#define SAFE_ARRAYDELETE(p) {if (p) delete[] (p); (p) = NULL;}
#define SAFE_RELEASE(p) {if (NULL != p) {(p)->Release(); (p) = NULL;}}

#define CHECK_RET(hr, message) if (FAILED(hr)) { printf("%s: %08X\n", message, hr); goto exit;}
#define CHECKHR(x) hr = x; if (FAILED(hr)) {printf("%d: %08X\n", __LINE__, hr); goto exit;}
#define CHECK_ALLOC(pb, message) if (NULL == pb) { puts(message); goto exit;}
#define CHECK_BOOL(b, message) if (!b) { hr = E_FAIL; puts(message); goto exit;}

// For CLSID_CMSRKinectAudio GUID
#include "MSRKinectAudio.h"

///Can't uncomment this, we keep getting linker errors saying redefinition, and extern doesn't work, 
/// so we hardcoded this to PKEY_AudioEndpoint_GUID2 in KinectAudio.cpp 
//#define INITGUID  // For PKEY_AudioEndpoint_GUID... 
#include <MMDeviceApi.h>
