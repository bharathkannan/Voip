/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.


// ImageUtils.h

#pragma once

using namespace System;

namespace ImageUtils 
{
	public ref class ImageWithPosition
	{
	public: 
		ImageWithPosition() 
		{
			m_nX = 0;
			m_nY = 0;
			Width= 0;
			Height = 0;
			m_nRowLengthBytes = 0;
			ImageBytes = nullptr;
			PixelFormat = System::Windows::Media::PixelFormats::Rgb24;
		}

		ImageWithPosition(int nW, int nH, array<unsigned char> ^bytes) 
		{
			m_nX = 0;
			m_nY = 0;
			Width = nW;
			Height = nH;
			m_nRowLengthBytes = bytes->Length/Height;
			//m_nRowLengthBytes = ((((Width * 8) + 31) & ~31) / 8) * 3;

			ImageBytes = bytes;
			PixelFormat = System::Windows::Media::PixelFormats::Rgb24;
		}

		property int X
		{
			int get() { return m_nX; }
			void set(int value) { m_nX = value; }
		}

		property int Y
		{
			int get() { return m_nY; }
			void set(int value) { m_nY = value; }
		}

		
		property int Width
		{
			int get() { return m_nW; }
			void set(int value) { m_nW = value; }
		}

		property int Height
		{
			int get() { return m_nH; }
			void set(int value) { m_nH = value; }
		}

		property int RowLengthBytes
		{
			int get() { return m_nRowLengthBytes; }
			void set(int value) { m_nRowLengthBytes = value; }
		}

		property array<unsigned char> ^ ImageBytes
		{
			array<unsigned char> ^get() { return m_aImageBytes; }
			void set(array<unsigned char> ^value) { m_aImageBytes = value; }
		}

		property System::Windows::Media::PixelFormat PixelFormat
		{
			System::Windows::Media::PixelFormat get() { return m_format; }
			void set(System::Windows::Media::PixelFormat value) { m_format = value; }
		}


	protected:
		int m_nX;
		int m_nY;
		int m_nW;
		int m_nH;
		int m_nRowLengthBytes;
		array<unsigned char> ^m_aImageBytes;
		System::Windows::Media::PixelFormat m_format;
	};

	public ref class Utils
	{
	public:
		static ImageWithPosition ^GetChangedArea(ImageWithPosition ^RGB24Original, ImageWithPosition ^RGB24New);

		static void BitBlt(ImageWithPosition ^RGB24SourceWithLocation, ImageWithPosition ^RGB24Destination);

		static array<unsigned char> ^CopyImageBits(System::Drawing::Bitmap ^image, int nWidth, int nHeight, int ImageSizeBytes);

		static array<unsigned char> ^Convert24BitImageTo32BitImage(array<unsigned char> ^bData, int nWidth, int nHeight);

		static ImageWithPosition ^GetWindowBytes(System::IntPtr ptrWnd);
		static ImageWithPosition^ GetDesktopWindowBytes(int nX, int nY, int nW, int nH);
	};
}
