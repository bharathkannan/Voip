/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

// This is the main DLL file.

#include "stdafx.h"
#include <Windows.h>

#include "ImageUtils.h"


#define IPPAPI(type,name,arg) extern type __STDCALL w7_##name arg;
#define IPPCALL(name) w7_##name

/// TODO... remove Intel Performance Primitive dependencies
#include <ippcore.h>
#include <ipps.h>
#include <ippi.h>
#include <ippj.h>
#include <ippcv.h>
#include <ippcc.h>

struct RGB
{
    unsigned char Red;
    unsigned char Green;
    unsigned char Blue;
};

ImageUtils::ImageWithPosition ^ImageUtils::Utils::GetChangedArea(ImageWithPosition ^RGB24Original, ImageWithPosition ^RGB24New)
{


 //  pin_ptr<unsigned char> ppOrig = &RGB24Original[0];
 //  Ipp8u *pOrig = (Ipp8u *) ppOrig;
	//struct RGB *pOrigRGB =  (struct RGB *)pOrig;

 //  pin_ptr<unsigned char> ppNew = &RGB24New[0];
 //  Ipp8u *pNew = (Ipp8u *) ppNew;
	//struct RGB *pNewRGB =  (struct RGB *)pNew;



	//// Find first
	//for (int y=0; y<RGB24Original->Height; y++)
	//{
	//   for (int x=0; x<RGB24Original->Width; x++)
	//	{


	//		pOrigRGB++;
	//		pNewRGB++;
	//	}
	//}


	pin_ptr<unsigned char> ppOrig = &RGB24Original->ImageBytes[0];
   Ipp8u *pOrig = (Ipp8u *) ppOrig;

   pin_ptr<unsigned char> ppNew = &RGB24New->ImageBytes[0];
   Ipp8u *pNew = (Ipp8u *) ppNew;


   struct RGB *pOrigRGB = (struct RGB *) pOrig;
   struct RGB *pNewRGB = (struct RGB *) pNew;
   bool bSame = true;

	/// find the min x, min y, max x, and max y for our difference
	int nMinX = RGB24Original->Width;
	int nMinY = RGB24Original->Height;
	int nMaxX = 0;
	int nMaxY = 0;

	for (int y=0; y<RGB24Original->Height; y++)
	{
		pOrigRGB = (struct RGB *) (pOrig + y*RGB24Original->RowLengthBytes); /// Move to next row - don't just increment incase their are dword align buffer bytes
		pNewRGB = (struct RGB *) (pNew + y*RGB24New->RowLengthBytes); /// Move to next row - don't just increment incase their are dword align buffer bytes

	   for (int x=0; x<RGB24Original->Width; x++)
		{
			if ( (pOrigRGB->Red != pNewRGB->Red) || (pOrigRGB->Green != pNewRGB->Green) || (pOrigRGB->Blue != pNewRGB->Blue) )
			{
				bSame = false;
				if (x < nMinX)
					nMinX = x;
				else if (x >  nMaxX)
					nMaxX = x;

				if (y < nMinY)
					nMinY = y;
				else if (y >  nMaxY)
					nMaxY = y;

			}
			pOrigRGB++; /// Move to next RGB value
			pNewRGB++; /// Move to next RGB value
		}

		
	}


	/*array<unsigned char> ^bSubtracted = gcnew array<unsigned char>(RGB24Original->ImageBytes->Length);
   pin_ptr<unsigned char> ppSubtracted = &bSubtracted [0];
   Ipp8u *pSubtracted = (Ipp8u *) ppSubtracted;

	IppiSize size;
	size.width = RGB24Original->Width;
	size.height = RGB24Original->Height;

	IPPCALL(ippiSub_8u_C3RSfs(pOrig, RGB24Original->RowLengthBytes, 
							pNew, RGB24New->RowLengthBytes, 
							pSubtracted,  RGB24Original->RowLengthBytes,
							size, 1));

	

	struct RGB *pIndex = (struct RGB *) pSubtracted;
	bool bSame = true;

	for (int y=0; y<RGB24Original->Height; y++)
	{
	   for (int x=0; x<RGB24Original->Width; x++)
		{
			if ( (pIndex->Red != 0) || (pIndex->Green != 0) || (pIndex->Blue != 0) )
			{
				bSame = false;
				if (x < nMinX)
					nMinX = x;
				else if (x >  nMaxX)
					nMaxX = x;

				if (y < nMinY)
					nMinY = y;
				else if (y >  nMaxY)
					nMaxY = y;

			}
			pIndex++; /// Move to next RGB value
		}

		pIndex = (struct RGB *) (pSubtracted + y*RGB24Original->RowLengthBytes); /// Move to next row - don't just increment incase their are dword align buffer bytes
		
	}*/

	if (bSame == true)
		return RGB24New;

	/// Copy the section of the destinatino image that has changed

	ImageWithPosition ^objRet = gcnew ImageWithPosition();
	objRet->X = nMinX;
	objRet->Y = nMinY;
	objRet->Width = nMaxX-nMinX+1; if (objRet->Width == 0) objRet->Width =1;
	objRet->Height = nMaxY-nMinY+1; if (objRet->Height == 0) objRet->Height =1;
	objRet->ImageBytes = gcnew array<unsigned char>(objRet->Width*3*objRet->Height);
	objRet->RowLengthBytes = objRet->Width*3;

   pin_ptr<unsigned char> ppRet = &objRet->ImageBytes[0];
   Ipp8u *pRet = (Ipp8u *) ppRet;

   IppiSize size;
	size.width = objRet->Width;
	size.height = objRet->Height;

	Ipp8u *pNewCopyFrom = pNew + objRet->Y*RGB24New->RowLengthBytes +  3*objRet->X;

	IPPCALL(ippiCopy_8u_C3R(pNewCopyFrom, RGB24New->RowLengthBytes, 
							pRet,  objRet->RowLengthBytes,
							size));

	

	return objRet;
}


/// Copys the 24 bit bmp specified in RGB24SourceWithLocation to RGB24Destination at the x, y location specified in RGB24SourceWithLocation
void ImageUtils::Utils::BitBlt(ImageWithPosition ^RGB24SourceWithLocation, ImageWithPosition ^RGB24Destination)
{
	
	pin_ptr<unsigned char> ppSource = &RGB24SourceWithLocation->ImageBytes[0];
   Ipp8u *pSource = (Ipp8u *) ppSource;

   pin_ptr<unsigned char> ppDest = &RGB24Destination->ImageBytes[0];
   Ipp8u *pDest = (Ipp8u *) ppDest;

	IppiSize size;
	size.width = RGB24SourceWithLocation->Width;
	size.height = RGB24SourceWithLocation->Height;

	Ipp8u *pDestLocation = pDest + RGB24SourceWithLocation->Y*RGB24Destination->RowLengthBytes +  3*RGB24SourceWithLocation->X;

	IPPCALL(ippiCopy_8u_C3R(pSource, RGB24SourceWithLocation->RowLengthBytes, 
							pDestLocation,  RGB24Destination->RowLengthBytes,
							size));
	
}


array<unsigned char> ^ImageUtils::Utils::CopyImageBits(System::Drawing::Bitmap ^image, int nWidth, int nHeight, int ImageSizeBytes)
{
	array<unsigned char> ^bData = gcnew array<unsigned char>(ImageSizeBytes);
	System::Drawing::Imaging::BitmapData ^data = image->LockBits(System::Drawing::Rectangle(0, 0, nWidth, nHeight),
		System::Drawing::Imaging::ImageLockMode::ReadOnly, System::Drawing::Imaging::PixelFormat::Format24bppRgb);
       
	pin_ptr<unsigned char> ppDest = &bData[0];
	unsigned char *pDest = (unsigned char *)ppDest;

	unsigned char *pSource = (unsigned char *) data->Scan0.ToPointer();

	IPPCALL(ippsCopy_8u(pSource, pDest, ImageSizeBytes));

    image->UnlockBits(data);

   return bData;
}


array<unsigned char> ^ImageUtils::Utils::Convert24BitImageTo32BitImage(array<unsigned char> ^bSourceImage, int nWidth, int nHeight)
{
	array<unsigned char> ^bRetData = gcnew array<unsigned char>(nWidth*4*nHeight);

	pin_ptr<unsigned char> ppDest = &bRetData[0];
	unsigned char *pDest = (unsigned char *)ppDest;

	pin_ptr<unsigned char> ppSource = &bSourceImage[0];
	unsigned char *pSource = (unsigned char *)ppSource;
	

	//IPPCALL(ippsCopy_8u(pSource, pDest, ImageSizeBytes));

	int dstOrder[] = {0,1,2, 0}; /// copy r, then g, then b, then r (last r is ignored and replaced with 255) to the destination image;
	IppiSize roiSize = { nWidth, nHeight };

    IPPCALL(ippiSwapChannels_8u_C3C4R(pSource, nWidth*3, pDest, nWidth*4, roiSize, dstOrder, 255));

   return bRetData;
}

//
//System::Drawing::Bitmap ^RawImageSearch::BitmapSearcher::GetDesktopWindow()
//{
//    System::Drawing::Graphics ^g = System::Drawing::Graphics::FromHwnd(objForm->Handle);
//    
//    System::Drawing::Bitmap ^SourceImage = gcnew System::Drawing::Bitmap(objForm->Bounds.Width, objForm->Bounds.Height, System::Drawing::Imaging::PixelFormat::Format24bppRgb);
//    System::Drawing::Graphics ^gimage = System::Drawing::Graphics::FromImage(SourceImage);
//
//    IntPtr hdcImage = gimage->GetHdc();
//    IntPtr hdcWindow = gimage->GetHdc();
//
//    ::BitBlt((HDC) hdcImage.ToPointer(), 0, 0, SourceImage->Width, SourceImage->Height, (HDC)hdcWindow.ToPointer(), 0, 0, SRCCOPY);
//    
//    gimage->ReleaseHdc(hdcImage);
//    g->ReleaseHdc(hdcWindow);
//
//
//    return SourceImage;
//}
//
//
//ImageUtils::ImageWithPosition ^ImageUtils::Utils::CaptureCursor(int &x, int &y)
//{  
//	ImageUtils::ImageWithPosition ^RetObj = nullptr;
//	
//	BITMAP bmpIcon;
//
//	CURSORINFO cursorInfo = new CURSORINFO();  
//    cursorInfo.cbSize = sizeof(CURSORINFO);
//    if (!GetCursorInfo(&cursorInfo))    
//        return nullptr;  
//          
//    if (cursorInfo.flags != CURSOR_SHOWING)    
//        goto done:
//    HICON hicon = CopyIcon(cursorInfo.hCursor);  
//    if (hicon == NULL)    
//        goto done;
//          
//    ICONINFO iconInfo;
//
//    if (!GetIconInfo(hicon, &iconInfo))
//		goto done;       
//          
//    x = cursorInfo.ptScreenPos.x - ((int)iconInfo.xHotspot);  
//    y = cursorInfo.ptScreenPos.y - ((int)iconInfo.yHotspot);
//
//    HWND hDesktopWindow = User32.GetDesktopWindow();
//    if (hDesktopWindow == NULL)
//		goto done:
//
//
//    hdcScreen = GetDC(hwndDesktop);
//	HDC hdcMemDC = NULL;
//    hdcMemDC = CreateCompatibleDC(hdcScreen); 
//	if(hdcMemDC == NULL)
//    {
//        goto done;
//    }
//
//	///Get the bitmap info about our icon
//    GetObject(iconInfo.hbmMask, sizeof(BITMAP), &bmpIcon);
//
//    SelectObject(hdcMemDC, iconInfo.hbmMask);
//	if (bmpIcon.bmHeight == bmpIcon.bmWidth * 2)    
//	{
//		BitBlt(resultHdc, 0, 0, 32, 32, maskHdc, 0, 32, SRCCOPY);
//		BitBlt(resultHdc, 0, 0, 32, 32, maskHdc, 0, 0, SRCINVERT);        
//	}
//	else
//	{
//	}
//
//          
//    using (maskBitmap)
//    {    
//        // Is this a monochrome cursor?    
//        if (maskBitmap.Height == maskBitmap.Width * 2)    
//        {      
//        Bitmap resultBitmap = new Bitmap(maskBitmap.Width, maskBitmap.Width);
//
//        Graphics desktopGraphics = Graphics.FromHwnd(hDesktopWindow);      
//        IntPtr desktopHdc = desktopGraphics.GetHdc();
//        IntPtr maskHdc = GDI32.CreateCompatibleDC(desktopHdc);
//        IntPtr oldPtr = GDI32.SelectObject(maskHdc, maskBitmap.GetHbitmap());      
//        using (Graphics resultGraphics = Graphics.FromImage(resultBitmap))      
//        {        
//            IntPtr resultHdc = resultGraphics.GetHdc();        
//            // These two operation will result in a black cursor over a white background.        
//            // Later in the code, a call to MakeTransparent() will get rid of the white background.        
//            GDI32.BitBlt(resultHdc, 0, 0, 32, 32, maskHdc, 0, 32, SRCCOPY);
//            GDI32.BitBlt(resultHdc, 0, 0, 32, 32, maskHdc, 0, 0, SRCINVERT);        
//            resultGraphics.ReleaseHdc(resultHdc);      
//        }
//
//        IntPtr newPtr = GDI32.SelectObject(maskHdc, oldPtr);
//        GDI32.DeleteDC(newPtr);
//        GDI32.DeleteDC(maskHdc);      
//        desktopGraphics.ReleaseHdc(desktopHdc);      
//                
//        // Remove the white background from the BitBlt calls,      
//        // resulting in a black cursor over a transparent background.      
//                
//        resultBitmap.MakeTransparent(Color.White);
//        DestroyIcon(hicon);
//        return resultBitmap;    
//        }  
//    }  
//          
//    Icon icon = Icon.FromHandle(hicon);
//    Bitmap bmp = icon.ToBitmap();
//    DestroyIcon(hicon);
//    icon.Dispose();
//    return bmp;
//
//done:
//	DestroyIcon(hicon);
//          
//    DeleteObject(hdcMemDC);
//    ReleaseDC(hwndDesktop, hdcScreen);          
//	return RetObj;
//}



ImageUtils::ImageWithPosition ^ImageUtils::Utils::GetWindowBytes(IntPtr ptrWnd)
{
	ImageUtils::ImageWithPosition ^RetObj = nullptr;
	HDC hdcScreen;
    HDC hdcMemDC = NULL;
    HBITMAP hbmScreen = NULL;
    BITMAP bmpScreen;
	HICON hicon = NULL;

	HWND hwndOurWnd = (HWND) ptrWnd.ToInt32();
	HWND hwndDesktop = GetDesktopWindow(); 
    // Retrieve the handle to a display device context for the client 
    // area of the window. 
    hdcScreen = GetDC(hwndDesktop);

    // Create a compatible DC which is used in a BitBlt from the window DC
    hdcMemDC = CreateCompatibleDC(hdcScreen); 

    if(hdcMemDC == NULL)
    {
        //MessageBox(hWnd, L"CreateCompatibleDC has failed",L"Failed", MB_OK);
        goto done;
    }


	CURSORINFO cursorInfo;  
    ICONINFO iconInfo;
    cursorInfo.cbSize = sizeof(CURSORINFO);
    if (!GetCursorInfo(&cursorInfo))    
        return nullptr;  
          
    if (cursorInfo.flags != CURSOR_SHOWING)    
        goto done;
    hicon = CopyIcon(cursorInfo.hCursor);  
    if (hicon == NULL)    
	{
		goto done;
		DWORD dwError = GetLastError();
	}
        
	if (hicon != NULL)
	{
		if (!GetIconInfo(hicon, &iconInfo))
			goto done;       
	}
    int xicon = cursorInfo.ptScreenPos.x - ((int)iconInfo.xHotspot);  
    int yicon = cursorInfo.ptScreenPos.y - ((int)iconInfo.yHotspot);

    // Get the size of our desktop
    RECT rcClient;
	GetWindowRect(hwndOurWnd, &rcClient);
	int width = rcClient.right - rcClient.left;
    int height = rcClient.bottom - rcClient.top;

	xicon -= rcClient.left;
    yicon -= rcClient.top;

    //This is the best stretch mode
    //SetStretchBltMode(hdcWindow,HALFTONE);

    
    // Create a compatible bitmap from the Window DC
    hbmScreen = CreateCompatibleBitmap(hdcScreen, width, height);
    
    if(hbmScreen == NULL)
    {
        goto done;
    }

    // Select the compatible bitmap into the compatible memory DC.
    SelectObject(hdcMemDC, hbmScreen);
    
    // Bit block transfer into our compatible memory DC.
    if(!::BitBlt(hdcMemDC, 0,0, width, height, 
               hdcScreen, rcClient.left, rcClient.top,
               SRCCOPY))
    {
        goto done;
    }
	if (hicon != NULL)
	   DrawIcon(hdcMemDC, xicon, yicon, hicon);

  /*
    if(!::PlgBlt(hdcMemDC, 0,0, POITN(rcClient.right-rcClient.left, rcClient.bottom-rcClient.top), 
               hdcScreen, 0,0,
               SRCCOPY))
    {
        goto done;
    }
	*/
	
    // Get the BITMAP from the HBITMAP
    GetObject(hbmScreen, sizeof(BITMAP), &bmpScreen);
     
    BITMAPINFOHEADER   bi;
     
    bi.biSize = sizeof(BITMAPINFOHEADER);    
    bi.biWidth = bmpScreen.bmWidth;    
    bi.biHeight = bmpScreen.bmHeight;  
    bi.biPlanes = 1;    
    bi.biBitCount = 24;//32;    
    bi.biCompression = BI_RGB;    
    bi.biSizeImage = 0;  
    bi.biXPelsPerMeter = 0;    
    bi.biYPelsPerMeter = 0;    
    bi.biClrUsed = 0;    
    bi.biClrImportant = 0;

    DWORD dwBmpSize = ((bmpScreen.bmWidth * bi.biBitCount + 31) / 32) * 4 * bmpScreen.bmHeight;

    // Starting with 32-bit Windows, GlobalAlloc and LocalAlloc are implemented as wrapper functions that 
    // call HeapAlloc using a handle to the process's default heap. Therefore, GlobalAlloc and LocalAlloc 
    // have greater overhead than HeapAlloc.
    HANDLE hDIB = GlobalAlloc(GHND,dwBmpSize); 
    char *lpbitmap = (char *)GlobalLock(hDIB);    

    // Gets the "bits" from the bitmap and copies them into a buffer 
    // which is pointed to by lpbitmap.
    GetDIBits(hdcMemDC, hbmScreen, 0,
        (UINT)bmpScreen.bmHeight,
        lpbitmap,
        (BITMAPINFO *)&bi, DIB_RGB_COLORS);

	
	array<unsigned char> ^RetArray = gcnew array<unsigned char>(dwBmpSize);
	pin_ptr<unsigned char> ppArray = &RetArray[0];
	unsigned char *pArray = (unsigned char *) ppArray;

	int nScanLineBytes = dwBmpSize/bmpScreen.bmHeight;
	for (int y=0; y<bmpScreen.bmHeight; y++)
	{
		memcpy(pArray+y*nScanLineBytes, lpbitmap+(bmpScreen.bmHeight-1-y)*nScanLineBytes, nScanLineBytes);
	}
	
	//memcpy(pArray, lpbitmap, dwBmpSize);

	RetObj = gcnew ImageUtils::ImageWithPosition(bmpScreen.bmWidth, bmpScreen.bmHeight, RetArray);

    //Unlock and Free the DIB from the heap
    GlobalUnlock(hDIB);    
    GlobalFree(hDIB);

       
    //Clean up
done:
    DeleteObject(hbmScreen);
    DeleteObject(hdcMemDC);
    ReleaseDC(hwndDesktop, hdcScreen);

	DeleteObject(iconInfo.hbmMask);
	DeleteObject(iconInfo.hbmColor);
	DestroyIcon(hicon);

    return RetObj;
}


ImageUtils::ImageWithPosition ^ImageUtils::Utils::GetDesktopWindowBytes(int nX, int nY, int nW, int nH)
{
	ImageUtils::ImageWithPosition ^RetObj = nullptr;
	HDC hdcScreen;
    HDC hdcMemDC = NULL;
    HBITMAP hbmScreen = NULL;
    BITMAP bmpScreen;
	HICON hicon = NULL;

	HWND hwndDesktop = GetDesktopWindow();
    // Retrieve the handle to a display device context for the client 
    // area of the window. 
    hdcScreen = GetDC(hwndDesktop);

    // Create a compatible DC which is used in a BitBlt from the window DC
    hdcMemDC = CreateCompatibleDC(hdcScreen); 

    if(hdcMemDC == NULL)
    {
        //MessageBox(hWnd, L"CreateCompatibleDC has failed",L"Failed", MB_OK);
        goto done;
    }

	CURSORINFO cursorInfo;  
    ICONINFO iconInfo;
    cursorInfo.cbSize = sizeof(CURSORINFO);
    if (!GetCursorInfo(&cursorInfo))    
        return nullptr;  
          
    if (cursorInfo.flags != CURSOR_SHOWING)    
        goto done;
    hicon = CopyIcon(cursorInfo.hCursor);  
	if (hicon == NULL)   
	{
		DWORD dwError = GetLastError();
		goto done;
	}
        
	if (hicon != NULL)
	{
		if (!GetIconInfo(hicon, &iconInfo))
			goto done;       
	}
    int xicon = cursorInfo.ptScreenPos.x - ((int)iconInfo.xHotspot);  
    int yicon = cursorInfo.ptScreenPos.y - ((int)iconInfo.yHotspot);



    // Get the size of our desktop
    RECT rcClient;
	GetWindowRect(hwndDesktop, &rcClient);

	rcClient.left = nX;
	rcClient.top = nY;
	rcClient.right = nX+nW;
	rcClient.bottom = nY+nH;


    //This is the best stretch mode
    //SetStretchBltMode(hdcWindow,HALFTONE);

    
    // Create a compatible bitmap from the Window DC
    hbmScreen = CreateCompatibleBitmap(hdcScreen, rcClient.right-rcClient.left, rcClient.bottom-rcClient.top);
    
    if(hbmScreen == NULL)
    {
        goto done;
    }

    // Select the compatible bitmap into the compatible memory DC.
    SelectObject(hdcMemDC, hbmScreen);
    
    // Bit block transfer into our compatible memory DC.
    if(!::BitBlt(hdcMemDC, 0,0, rcClient.right-rcClient.left, rcClient.bottom-rcClient.top, 
               hdcScreen, nX,nY,
               SRCCOPY))
    {
        goto done;
    }

	if (hicon != NULL)
    	DrawIcon(hdcMemDC, xicon-nX, yicon-nY, hicon);
  /*
    if(!::PlgBlt(hdcMemDC, 0,0, POITN(rcClient.right-rcClient.left, rcClient.bottom-rcClient.top), 
               hdcScreen, 0,0,
               SRCCOPY))
    {
        goto done;
    }
	*/
	
    // Get the BITMAP from the HBITMAP
    GetObject(hbmScreen, sizeof(BITMAP), &bmpScreen);
     
    BITMAPINFOHEADER   bi;
     
    bi.biSize = sizeof(BITMAPINFOHEADER);    
    bi.biWidth = bmpScreen.bmWidth;    
    bi.biHeight = bmpScreen.bmHeight;  
    bi.biPlanes = 1;    
    bi.biBitCount = 24;//32;    
    bi.biCompression = BI_RGB;    
    bi.biSizeImage = 0;  
    bi.biXPelsPerMeter = 0;    
    bi.biYPelsPerMeter = 0;    
    bi.biClrUsed = 0;    
    bi.biClrImportant = 0;

    DWORD dwBmpSize = ((bmpScreen.bmWidth * bi.biBitCount + 31) / 32) * 4 * bmpScreen.bmHeight;

    // Starting with 32-bit Windows, GlobalAlloc and LocalAlloc are implemented as wrapper functions that 
    // call HeapAlloc using a handle to the process's default heap. Therefore, GlobalAlloc and LocalAlloc 
    // have greater overhead than HeapAlloc.
    HANDLE hDIB = GlobalAlloc(GHND,dwBmpSize); 
    char *lpbitmap = (char *)GlobalLock(hDIB);    

    // Gets the "bits" from the bitmap and copies them into a buffer 
    // which is pointed to by lpbitmap.
    GetDIBits(hdcMemDC, hbmScreen, 0,
        (UINT)bmpScreen.bmHeight,
        lpbitmap,
        (BITMAPINFO *)&bi, DIB_RGB_COLORS);

	
	array<unsigned char> ^RetArray = gcnew array<unsigned char>(dwBmpSize);
	pin_ptr<unsigned char> ppArray = &RetArray[0];
	unsigned char *pArray = (unsigned char *) ppArray;

	int nScanLineBytes = dwBmpSize/bmpScreen.bmHeight;
	for (int y=0; y<bmpScreen.bmHeight; y++)
	{
		memcpy(pArray+y*nScanLineBytes, lpbitmap+(bmpScreen.bmHeight-1-y)*nScanLineBytes, nScanLineBytes);
	}
	
	//memcpy(pArray, lpbitmap, dwBmpSize);

	RetObj = gcnew ImageUtils::ImageWithPosition(bmpScreen.bmWidth, bmpScreen.bmHeight, RetArray);

    //Unlock and Free the DIB from the heap
    GlobalUnlock(hDIB);    
    GlobalFree(hDIB);

       
    //Clean up
done:
    DeleteObject(hbmScreen);
    DeleteObject(hdcMemDC);
    ReleaseDC(hwndDesktop, hdcScreen);
	DestroyIcon(hicon);
	DeleteObject(iconInfo.hbmMask);
	DeleteObject(iconInfo.hbmColor);

    return RetObj;
}


