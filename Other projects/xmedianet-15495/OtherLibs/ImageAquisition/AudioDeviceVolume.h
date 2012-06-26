/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

#pragma once

using namespace System;

using namespace AudioClasses;
using namespace System::Collections::Generic;
using namespace System::Threading;


namespace ImageAquisition 
{
	/// Class for controlling the volume of an audio device, and receiving feedback when the volume changes
	public ref class AudioDeviceVolume : System::ComponentModel::INotifyPropertyChanged
	{
	public:
		AudioDeviceVolume(AudioDevice ^dev);
		virtual ~AudioDeviceVolume()
		{
			Cleanup();
		}
	
		property bool Mute
		{
			bool get();
			void set(bool value);
		}
	
		void NotifyMuteStatus(bool value);

		property int Volume
		{
			int get();
			void set(int value);
		}

		int GetCurrentVolume();

		void NotifyVolumeLevel(int value);
	
		virtual event System::ComponentModel::PropertyChangedEventHandler ^PropertyChanged;

	protected:

		void FirePropertyChanged(System::String ^strProp)
		{
  	       PropertyChanged(this, gcnew System::ComponentModel::PropertyChangedEventArgs(strProp));
		}

		bool m_bMuted;
		int m_nVolume;

		AudioDevice ^AudioDev;
	    IntPtr IntPtrComAudioDevice;
		IntPtr IntPtrAudioEndpointVolume;
		IntPtr IntPtrAudioCallBackObject;

		void Cleanup();
	};




}