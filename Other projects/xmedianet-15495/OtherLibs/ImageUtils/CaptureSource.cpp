
#include "StdAfx.h"
#include "CaptureSource.h"

/// See
/// http://msdn.microsoft.com/en-us/library/dd407288(v=VS.85).aspx

HRESULT GrabVideoBitmap(PCWSTR pszVideoFile, PCWSTR pszBitmapFile)
{
    IGraphBuilder *pGraph = NULL;
    IMediaControl *pControl = NULL;
    IMediaEventEx *pEvent = NULL;
    IBaseFilter *pGrabberF = NULL;
    ISampleGrabber *pGrabber = NULL;
    IBaseFilter *pSourceF = NULL;
    IEnumPins *pEnum = NULL;
    IPin *pPin = NULL;
    IBaseFilter *pNullF = NULL;

    BYTE *pBuffer = NULL;

    HRESULT hr = CoCreateInstance(CLSID_FilterGraph, NULL, 
        CLSCTX_INPROC_SERVER,IID_PPV_ARGS(&pGraph));
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pGraph->QueryInterface(IID_PPV_ARGS(&pControl));
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pGraph->QueryInterface(IID_PPV_ARGS(&pEvent));
    if (FAILED(hr))
    {
        goto done;
    }

    // Create the Sample Grabber filter.
    hr = CoCreateInstance(CLSID_SampleGrabber, NULL, CLSCTX_INPROC_SERVER,
        IID_PPV_ARGS(&pGrabberF));
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pGraph->AddFilter(pGrabberF, L"Sample Grabber");
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pGrabberF->QueryInterface(IID_PPV_ARGS(&pGrabber));
    if (FAILED(hr))
    {
        goto done;
    }

    AM_MEDIA_TYPE mt;
    ZeroMemory(&mt, sizeof(mt));
    mt.majortype = MEDIATYPE_Video;
    mt.subtype = MEDIASUBTYPE_RGB24;

    hr = pGrabber->SetMediaType(&mt);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pGraph->AddSourceFilter(pszVideoFile, L"Source", &pSourceF);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pSourceF->EnumPins(&pEnum);
    if (FAILED(hr))
    {
        goto done;
    }

    while (S_OK == pEnum->Next(1, &pPin, NULL))
    {
        hr = ConnectFilters(pGraph, pPin, pGrabberF);
        SafeRelease(&pPin);
        if (SUCCEEDED(hr))
        {
            break;
        }
    }

    if (FAILED(hr))
    {
        goto done;
    }

    hr = CoCreateInstance(CLSID_NullRenderer, NULL, CLSCTX_INPROC_SERVER, 
        IID_PPV_ARGS(&pNullF));
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pGraph->AddFilter(pNullF, L"Null Filter");
    if (FAILED(hr))
    {
        goto done;
    }

    hr = ConnectFilters(pGraph, pGrabberF, pNullF);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pGrabber->SetOneShot(TRUE);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pGrabber->SetBufferSamples(TRUE);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pControl->Run();
    if (FAILED(hr))
    {
        goto done;
    }

    long evCode;
    hr = pEvent->WaitForCompletion(INFINITE, &evCode);

    // Find the required buffer size.
    long cbBuffer;
    hr = pGrabber->GetCurrentBuffer(&cbBuffer, NULL);
    if (FAILED(hr))
    {
        goto done;
    }

    pBuffer = (BYTE*)CoTaskMemAlloc(cbBuffer);
    if (!pBuffer) 
    {
        hr = E_OUTOFMEMORY;
        goto done;
    }

    hr = pGrabber->GetCurrentBuffer(&cbBuffer, (long*)pBuffer);
    if (FAILED(hr))
    {
        goto done;
    }

    hr = pGrabber->GetConnectedMediaType(&mt);
    if (FAILED(hr))
    {
        goto done;
    }

    // Examine the format block.
    if ((mt.formattype == FORMAT_VideoInfo) && 
        (mt.cbFormat >= sizeof(VIDEOINFOHEADER)) &&
        (mt.pbFormat != NULL)) 
    {
        VIDEOINFOHEADER *pVih = (VIDEOINFOHEADER*)mt.pbFormat;

//        hr = WriteBitmap(pszBitmapFile, &pVih->bmiHeader, 
  //          mt.cbFormat - SIZE_PREHEADER, pBuffer, cbBuffer);
    }
    else 
    {
        // Invalid format.
        hr = VFW_E_INVALIDMEDIATYPE; 
    }

    FreeMediaType(mt);

done:
    CoTaskMemFree(pBuffer);
    SafeRelease(&pPin);
    SafeRelease(&pEnum);
    SafeRelease(&pNullF);
    SafeRelease(&pSourceF);
    SafeRelease(&pGrabber);
    SafeRelease(&pGrabberF);
    SafeRelease(&pControl);
    SafeRelease(&pEvent);
    SafeRelease(&pGraph);
    return hr;
};

// Writes a bitmap file
//  pszFileName:  Output file name.
//  pBMI:         Bitmap format information (including pallete).
//  cbBMI:        Size of the BITMAPINFOHEADER, including palette, if present.
//  pData:        Pointer to the bitmap bits.
//  cbData        Size of the bitmap, in bytes.

HRESULT WriteBitmap(PCWSTR pszFileName, BITMAPINFOHEADER *pBMI, size_t cbBMI,
    BYTE *pData, size_t cbData)
{
    HANDLE hFile = CreateFile(pszFileName, GENERIC_WRITE, 0, NULL, 
        CREATE_ALWAYS, 0, NULL);
    if (hFile == NULL)
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    BITMAPFILEHEADER bmf = { };

    bmf.bfType = 'MB';
    bmf.bfSize = cbBMI+ cbData + sizeof(bmf); 
    bmf.bfOffBits = sizeof(bmf) + cbBMI; 

    DWORD cbWritten = 0;
    BOOL result = WriteFile(hFile, &bmf, sizeof(bmf), &cbWritten, NULL);
    if (result)
    {
        result = WriteFile(hFile, pBMI, cbBMI, &cbWritten, NULL);
    }
    if (result)
    {
        result = WriteFile(hFile, pData, cbData, &cbWritten, NULL);
    }

    HRESULT hr = result ? S_OK : HRESULT_FROM_WIN32(GetLastError());

    CloseHandle(hFile);

    return hr;
}



ImageUtils::CaptureSource::CaptureSource(System::Guid guid)
{
}


void ImageUtils::CaptureSource::StartCapture()
{


}


void ImageUtils::CaptureSource::StopCapture()
{

}


/// Get all capture devices... TODO.. actually return something
void ImageUtils::CaptureSource::GetCaptureDevices()
{
	ICreateDevEnum *pSysDevEnum = NULL;

    HRESULT hr = CoCreateInstance(CLSID_SystemDeviceEnum, NULL, 
        CLSCTX_INPROC_SERVER, IID_ICreateDevEnum, (void **) &pSysDevEnum);
    if (FAILED(hr))
    {
        return;
    }

	// Obtain a class enumerator for the video compressor category.
	IEnumMoniker *pEnumCat = NULL;
	hr = pSysDevEnum->CreateClassEnumerator(CLSID_VideoInputDeviceCategory, &pEnumCat, 0);

	if (hr == S_OK) 
	{
		// Enumerate the monikers.
		IMoniker *pMoniker = NULL;
		ULONG cFetched;
		while(pEnumCat->Next(1, &pMoniker, &cFetched) == S_OK)
		{
			IPropertyBag *pPropBag;
			hr = pMoniker->BindToStorage(0, 0, IID_IPropertyBag, (void **)&pPropBag);
			if (SUCCEEDED(hr))
			{
				// To retrieve the filter's friendly name, do the following:
				VARIANT varName;
				VariantInit(&varName);
				hr = pPropBag->Read(L"FriendlyName", &varName, 0);
				if (SUCCEEDED(hr))
				{
					// Display the name in your UI somehow.
				}
				VariantClear(&varName);

				// To create an instance of the filter, do the following:
				IBaseFilter *pFilter;
				hr = pMoniker->BindToObject(NULL, NULL, IID_IBaseFilter,
					(void**)&pFilter);
				// Now add the filter to the graph. 
				//Remember to release pFilter later.
				pPropBag->Release();
			}
			pMoniker->Release();
		}
		pEnumCat->Release();
	}
	pSysDevEnum->Release();


}


/// Next steps
/// create the IMFMediaSource for the video capture device, then call:

//// HRESULT MFCreateSourceReaderFromMediaSource(
//  __in   IMFMediaSource *pMediaSource,
//  __in   IMFAttributes *pAttributes,
//  __out  IMFSourceReader **ppSourceReader
//);



/// Media foundation method
void ImageUtils::CaptureSource::GetCaptureDevicesMF()
//HRESULT CreateVideoCaptureDevice(IMFMediaSource **ppSource)
{
	//IMFMediaSource *ppSource= NULL;

 //   UINT32 count = 0;

 //   IMFAttributes *pConfig = NULL;
 //   IMFActivate **ppDevices = NULL;

 //   // Create an attribute store to hold the search criteria.
 //   HRESULT hr = MFCreateAttributes(&pConfig, 1);

 //   // Request video capture devices.
 //   if (SUCCEEDED(hr))
 //   {
 //       hr = pConfig->SetGUID(
 //           MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE, 
 //           MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID
 //           );
 //   }

 //   // Enumerate the devices,
 //   if (SUCCEEDED(hr))
 //   {
 //       hr = MFEnumDeviceSources(pConfig, &ppDevices, &count);
 //   }

 //   // Create a media source for the first device in the list.
 //   if (SUCCEEDED(hr))
 //   {
 //       if (count > 0)
 //       {
 //           hr = ppDevices[0]->ActivateObject(IID_PPV_ARGS(&ppSource));
 //       }
 //       else
 //       {
 //           //hr = MF_E_NOT_FOUND;
 //       }
 //   }

 //   for (DWORD i = 0; i < count; i++)
 //   {
 //       ppDevices[i]->Release();
 //   }
 //   CoTaskMemFree(ppDevices);
    //return hr;
}

//
//HRESULT EnumerateCaptureFormats(IMFMediaSource *pSource)
//{
//    IMFPresentationDescriptor *pPD = NULL;
//    IMFStreamDescriptor *pSD = NULL;
//    IMFMediaTypeHandler *pHandler = NULL;
//    IMFMediaType *pType = NULL;
//
//    HRESULT hr = pSource->CreatePresentationDescriptor(&pPD);
//    if (FAILED(hr))
//    {
//        goto done;
//    }
//
//    BOOL fSelected;
//    hr = pPD->GetStreamDescriptorByIndex(0, &fSelected, &pSD);
//    if (FAILED(hr))
//    {
//        goto done;
//    }
//
//    hr = pSD->GetMediaTypeHandler(&pHandler);
//    if (FAILED(hr))
//    {
//        goto done;
//    }
//
//    DWORD cTypes = 0;
//    hr = pHandler->GetMediaTypeCount(&cTypes);
//    if (FAILED(hr))
//    {
//        goto done;
//    }
//
//    for (DWORD i = 0; i < cTypes; i++)
//    {
//        hr = pHandler->GetMediaTypeByIndex(i, &pType);
//        if (FAILED(hr))
//        {
//            goto done;
//        }
//
//        LogMediaType(pType);
//        OutputDebugString(L"\n");
//
//        SafeRelease(&pType);
//    }
//
//done:
//    SafeRelease(&pPD);
//    SafeRelease(&pSD);
//    SafeRelease(&pHandler);
//    SafeRelease(&pType);
//    return hr;
//}
//
//
//HRESULT SetDeviceFormat(IMFMediaSource *pSource, DWORD dwFormatIndex)
//{
//    IMFPresentationDescriptor *pPD = NULL;
//    IMFStreamDescriptor *pSD = NULL;
//    IMFMediaTypeHandler *pHandler = NULL;
//    IMFMediaType *pType = NULL;
//
//    HRESULT hr = pSource->CreatePresentationDescriptor(&pPD);
//    if (FAILED(hr))
//    {
//        goto done;
//    }
//
//    BOOL fSelected;
//    hr = pPD->GetStreamDescriptorByIndex(0, &fSelected, &pSD);
//    if (FAILED(hr))
//    {
//        goto done;
//    }
//
//    hr = pSD->GetMediaTypeHandler(&pHandler);
//    if (FAILED(hr))
//    {
//        goto done;
//    }
//
//    hr = pHandler->GetMediaTypeByIndex(dwFormatIndex, &pType);
//    if (FAILED(hr))
//    {
//        goto done;
//    }
//
//    hr = pHandler->SetCurrentMediaType(pType);
//
//done:
//    SafeRelease(&pPD);
//    SafeRelease(&pSD);
//    SafeRelease(&pHandler);
//    SafeRelease(&pType);
//    return hr;
//}
//
//HRESULT SetMaxFrameRate(IMFMediaSource *pSource, DWORD dwTypeIndex)
//{
//    IMFPresentationDescriptor *pPD = NULL;
//    IMFStreamDescriptor *pSD = NULL;
//    IMFMediaTypeHandler *pHandler = NULL;
//    IMFMediaType *pType = NULL;
//
//    HRESULT hr = pSource->CreatePresentationDescriptor(&pPD);
//    if (FAILED(hr))
//    {
//        goto done;
//    }
//
//    BOOL fSelected;
//    hr = pPD->GetStreamDescriptorByIndex(dwTypeIndex, &fSelected, &pSD);
//    if (FAILED(hr))
//    {
//        goto done;
//    }
//
//    hr = pSD->GetMediaTypeHandler(&pHandler);
//    if (FAILED(hr))
//    {
//        goto done;
//    }
//
//    hr = pHandler->GetCurrentMediaType(&pType);
//    if (FAILED(hr))
//    {
//        goto done;
//    }
//
//    // Get the maximum frame rate for the selected capture format.
//
//    // Note: To get the minimum frame rate, use the 
//    // MF_MT_FRAME_RATE_RANGE_MIN attribute instead.
//
//    PROPVARIANT var;
//    if (SUCCEEDED(pType->GetItem(MF_MT_FRAME_RATE_RANGE_MAX, &var)))
//    {
//        hr = pType->SetItem(MF_MT_FRAME_RATE, var);
//
//        PropVariantClear(&var);
//
//        if (FAILED(hr))
//        {
//            goto done;
//        }
//
//        hr = pHandler->SetCurrentMediaType(pType);
//    }
//
//done:
//    SafeRelease(&pPD);
//    SafeRelease(&pSD);
//    SafeRelease(&pHandler);
//    SafeRelease(&pType);
//    return hr;
//}
//


//
///// Display a filters property page
//IBaseFilter *pFilter;
///* Obtain the filter's IBaseFilter interface. (Not shown) */
//ISpecifyPropertyPages *pProp;
//HRESULT hr = pFilter->QueryInterface(IID_ISpecifyPropertyPages, (void **)&pProp);
//if (SUCCEEDED(hr)) 
//{
//    // Get the filter's name and IUnknown pointer.
//    FILTER_INFO FilterInfo;
//    hr = pFilter->QueryFilterInfo(&FilterInfo); 
//    IUnknown *pFilterUnk;
//    pFilter->QueryInterface(IID_IUnknown, (void **)&pFilterUnk);
//
//    // Show the page. 
//    CAUUID caGUID;
//    pProp->GetPages(&caGUID);
//    pProp->Release();
//    OleCreatePropertyFrame(
//        hWnd,                   // Parent window
//        0, 0,                   // Reserved
//        FilterInfo.achName,     // Caption for the dialog box
//        1,                      // Number of objects (just the filter)
//        &pFilterUnk,            // Array of object pointers. 
//        caGUID.cElems,          // Number of property pages
//        caGUID.pElems,          // Array of property page CLSIDs
//        0,                      // Locale identifier
//        0, NULL                 // Reserved
//    );
//
//    // Clean up.
//    pFilterUnk->Release();
//    FilterInfo.pGraph->Release(); 
//    CoTaskMemFree(caGUID.pElems);
//}
//
