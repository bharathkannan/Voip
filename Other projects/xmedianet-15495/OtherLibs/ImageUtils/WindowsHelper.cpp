/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.


#include "StdAfx.h"
#include "WindowsHelper.h"


void ImageUtils::WindowsHelper::MouseEvent(int x, int y, unsigned int MouseEventType)
{
	SetCursorPos(x, y);

	INPUT input;
	input.mi.dx = x;
	input.mi.dy = y;
	input.mi.dwFlags = MouseEventType;
	input.type = INPUT_MOUSE;

	::SendInput(1, &input, sizeof(INPUT));
}

void ImageUtils::WindowsHelper::KeyboardEvent(unsigned int KeyboardEventType, unsigned short virtualkey, unsigned short scan)
{
	INPUT input;     
	input.type = INPUT_KEYBOARD;
	input.ki.wVk = virtualkey;
	input.ki.wScan = scan; //L'c';     
	input.ki.dwFlags = KeyboardEventType;     
	input.ki.time = 0;     
	input.ki.dwExtraInfo = 0; 

	int retval = SendInput(1, &input, sizeof(INPUT)); 
}
