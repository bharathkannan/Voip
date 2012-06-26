/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

#pragma once

using namespace System;
using namespace AudioClasses;
using namespace System::Threading;

namespace ImageAquisition 
{

	public enum class MicrophoneMode
	{
       SINGLE_CHANNEL_AEC = 0, /// AEC processing only
		 //ADAPTIVE_ARRAY_ONLY = 1, /// reserved
	    OPTIBEAM_ARRAY_ONLY = 2, /// Microphone array processing only.
		 //ADAPTIVE_ARRAY_AND_AEC = 3, /// Reserved.
       OPTIBEAM_ARRAY_AND_AEC = 4, /// Microphone array processing and AEC processing.
       SINGLE_CHANNEL_NSAGC = 5, /// No microphone array processing and no AEC processing.
		 //MODE_NOT_SET = 6, /// do not use
	};

	public ref class NarrowBandMic
	{
	public:
		NarrowBandMic(AudioDevice ^device, Guid speakerguid, IntPtr hWindow) 
		{
			SpeakerGuid = speakerguid;

			m_objAudioDevice = device;
			
			m_bMute = false;
			m_eMicrophoneMode = ImageAquisition::MicrophoneMode::SINGLE_CHANNEL_AEC;
			m_dPosition = 0.0f;
			m_dBeamAngle = 0.0f;
			m_dConfidence = 0.0f;
			m_bUseKinectArray = true;
			m_bAGC = false;
			m_bNoiseSupression = true;

			Window = hWindow;

			m_bExit = false;

			ByteQueue = gcnew ByteBuffer();

			objLock = gcnew Object();
		}

		~NarrowBandMic()
		{
			Stop();
		}

		//LogMessaging::LogClient ^Logger;

		Guid SpeakerGuid;

		array<unsigned char> ^GetData();
		MediaSample ^PullSample(AudioFormat ^format, TimeSpan tsDuration);

		static int GetSpeakerDeviceIndex(String ^strDevice);
		static array<AudioDevice ^> ^GetMicrophoneDevices();
		static array<AudioDevice ^> ^GetSpeakerDevices();

		bool Start();

#ifdef USE_IPP		
		bool StartNoEchoCancellation();
		bool StartRawMicMode();
		bool StartSpeakerDeviceLoopBackMode();
#endif
		bool Stop();
		
		property String ^Name
		{
			String ^get()
			{
				return m_objAudioDevice->Name;
			}
			void set(String ^ strValue)
			{
				m_objAudioDevice->Name = strValue;
			}
		};

		property AudioDevice ^OurAudioDevice
		{
			AudioDevice ^get()
			{
				return m_objAudioDevice;
			}
			void set(AudioDevice ^objValue)
			{
				m_objAudioDevice = objValue;
			}
		};

		property bool Mute
		{
			bool get()
			{
				return m_bMute;
			}
			void set(bool bValue)
			{
				m_bMute = bValue;
			}
		};
			
		property bool AGC
		{
			bool get()
			{
				return m_bAGC;
			}
			void set(bool bValue)
			{
				m_bAGC = bValue;
			}
		};

		property bool NoiseSupression
		{
			bool get()
			{
				return m_bNoiseSupression;
			}
			void set(bool bValue)
			{
				m_bNoiseSupression = bValue;
			}
		};

		

		property ImageAquisition::MicrophoneMode MicrophoneMode
		{
			ImageAquisition::MicrophoneMode get()
			{
				return m_eMicrophoneMode;
			}
			void set(ImageAquisition::MicrophoneMode eValue)
			{
				m_eMicrophoneMode = eValue;
			}
		};

		
		
		void ClearBuffer();

		property int MaxQueueSamples
		{
			int get()
			{
				return m_nMaxQueueSamples;
			}
			void set(int nValue)
			{
				m_nMaxQueueSamples = nValue;
			}
		};

		property int MaxQueueSamplesTrimSize
		{
			int get()
			{
				return m_nMaxQueueSamplesTrimSize;
			}
			void set(int nValue)
			{
				m_nMaxQueueSamplesTrimSize = nValue;
			}
		};

		property bool UseKinectArray
		{
			bool get()
			{
				return m_bUseKinectArray;
			}
			void set(bool nValue)
			{
				m_bUseKinectArray = nValue;
			}
		};

		property double BeamPosition
		{
			double get()
			{
				return m_dPosition;
			}
		};
		property double BeamAngle
		{
			double get()
			{
				return m_dBeamAngle;
			}
		};
		property double Confidence
		{
			double get()
			{
				return m_dConfidence;
			}
		};

	protected:

		ImageAquisition::MicrophoneMode m_eMicrophoneMode;
		bool m_bUseKinectArray;
		bool m_bMute;
		bool m_bAGC;
		bool m_bNoiseSupression;
	
		AudioDevice ^m_objAudioDevice;
		

		int m_nMaxQueueSamples;
		int m_nMaxQueueSamplesTrimSize;

		double m_dBeamAngle;
		double m_dConfidence;	
		double m_dPosition;	


		void ReadMicEchoCancellationFunction();
#ifdef USE_IPP		
		void ReadMicNoEchoFunction();
		void ReadMicRawMode();
		void ReadSpeakerAsMicInLoopBackModeFunction();
#endif

		Thread ^RecordThread;
		bool m_bExit;

		ByteBuffer ^ByteQueue;

		IntPtr Window;
		int m_nDSBufferSize;

		System::Object ^objLock;
        int m_nInitedBufferSize;	
	};


}

