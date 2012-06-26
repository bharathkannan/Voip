/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

#pragma once

using namespace System;

namespace ImageUtils 
{

	public ref class CaptureSource
	{
	public:
		CaptureSource(System::Guid guid);


		void StartCapture();
	
		void StopCapture();

		static void GetCaptureDevices();
		static void GetCaptureDevicesMF();

		//event OnNewFrame(array<unsigned char> ^RGB24Frame, int nWidth, int nHeight);

	protected:

		IntPtr GraphPointer;
		IntPtr ControlPointer;
		IntPtr EventPointer;

	};


}