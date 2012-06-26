/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.


#pragma once

using namespace System;
using namespace AudioClasses;
using namespace System::Collections::Generic;
using namespace System::Threading;

using namespace System::ComponentModel;

namespace DirectShowFilters 
{

	public ref class SpeakerFilter
	{
	public:
		SpeakerFilter(Guid speakerguid, int nMsPacket, AudioFormat ^format, IntPtr hWindow) 
		{
			SpeakerGuid = speakerguid;

			m_bTrim = true; /// trim audio buffer to maximum length
			m_bMute = false;
			Window = hWindow;
			Format = format;
			int nSamplesPerPacket = format->CalculateNumberOfSamplesForDuration(TimeSpan(0, 0, 0, 0, nMsPacket));
			m_nDSBufferSize = nSamplesPerPacket*3*Format->BytesPerSample;
			ByteQueue = gcnew ByteBuffer();
			OutputQueue = gcnew ByteBuffer();

			m_hDS= System::IntPtr::Zero;
			m_hBufferPrimary = System::IntPtr::Zero;
			m_hBufferSecondary = System::IntPtr::Zero;
			objLock = gcnew Object();

			m_nMaxQueueSamples = nSamplesPerPacket*4;
			m_nMaxQueueSamplesTrimSize = nSamplesPerPacket;
			m_nMaxOutputSamples = nSamplesPerPacket*2;


			EventBuffer13Way = IntPtr(::CreateEvent(NULL, FALSE, FALSE, NULL));
			EventBuffer23Way = IntPtr(::CreateEvent(NULL, FALSE, FALSE, NULL));
			EventBuffer33Way = IntPtr(::CreateEvent(NULL, FALSE, FALSE, NULL));
			EventThreadExit = gcnew ManualResetEvent(false);
		}

		~SpeakerFilter()
		{
			Stop();
			Close();

			EventThreadExit->Close();
			::CloseHandle((HANDLE) EventBuffer13Way);
			::CloseHandle((HANDLE) EventBuffer23Way);
			::CloseHandle((HANDLE) EventBuffer33Way);
		}

		Guid SpeakerGuid;
        bool PushSample(MediaSample ^sample, System::Object ^objSource);
		static array<AudioDevice ^> ^GetSpeakerDevices();
		
		bool Start();
		void AddBuffer(array<unsigned char> ^bData);
		bool Stop();
		
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

		property bool TrimAudioBufferToConstraints
		{
			bool get()
			{
				return m_bTrim;
			}
			void set(bool bValue)
			{
				m_bTrim = bValue;
			}
		};

		int GetPlayPos();
		int GetWritePos();

		AudioFormat ^Format;

		void Pause(bool bPause);
		void SetVolume(int nVolume);
		int GetVolume();
		void Reset();
		virtual void Close() override;

		void ClearBuffer();

		property int BufferRemainingLength
		{
			int get()
			{
				return ByteQueue->Size;
			}
		}

		/// The maximum queue size of our buffer, after which we stop adding packets (actually we drop old ones and add new ones)
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

		/// The amount we trim down our buffer when it goes over MaxQueueSamples size.  Needed so we don't keep trimming and dropping packets after we get in an overloaded state
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

			property int MaxOutputSamples
		{
			int get()
			{
				return m_nMaxOutputSamples;
			}
			void set(int nValue)
			{
				m_nMaxOutputSamples = nValue;
			}
		};

	protected:

		bool m_bMute;
		bool m_bTrim;
	
		int m_nMaxQueueSamples;
		int m_nMaxQueueSamplesTrimSize;
		int m_nMaxOutputSamples;
		int CopyDataToBuffer(array<unsigned char> ^bData, int nLocation);
		void CopyZerosToBuffer(int nLength, int nLocation);

		ByteBuffer ^ByteQueue;
		ByteBuffer ^OutputQueue;
		void PlayThreadFunction();
		Thread ^PlayThread;
		IntPtr EventBuffer13Way;
		IntPtr EventBuffer23Way;
		IntPtr EventBuffer33Way;
		ManualResetEvent ^EventThreadExit;

		IntPtr Window;
		/// directsound stuff 
		IntPtr m_hDS; /*LPDIRECTSOUND8*/
		IntPtr m_hBufferPrimary; /*LPDIRECTSOUNDBUFFER*/
		IntPtr m_hBufferSecondary; /*LPDIRECTSOUNDBUFFER*/

		int m_nDSBufferSize;

		System::Object ^objLock;
		int m_nInitedBufferSize;
	};




	public ref class MicrophoneFilter 
	{
	public:
		MicrophoneFilter(Guid micguid, int nMsPacket, AudioFormat ^format, IntPtr hWindow) 
		{
			MicrophoneGuid = micguid;

			m_bMute = false;
			
			Window = hWindow;
			Format = format;
			
			int nSamplesPerPacket = format->CalculateNumberOfSamplesForDuration(TimeSpan(0, 0, 0, 0, nMsPacket));

			m_nMaxQueueSamples = nSamplesPerPacket*4;
			m_nMaxQueueSamplesTrimSize = nSamplesPerPacket;

			m_nDSBufferSize = nSamplesPerPacket*2*Format->BytesPerSample;
			ByteQueue = gcnew ByteBuffer();
			m_hDS= System::IntPtr::Zero;
			m_hBufferCapture = System::IntPtr::Zero;
			objLock = gcnew Object();

			EventBufferHalfWay = IntPtr(::CreateEvent(NULL, FALSE, FALSE, NULL));
			EventBufferFullWay = IntPtr(::CreateEvent(NULL, FALSE, FALSE, NULL));
			EventThreadExit = gcnew ManualResetEvent(false);
		}

		~MicrophoneFilter()
		{
			Stop();
			EventThreadExit->Close();
			::CloseHandle((HANDLE) EventBufferHalfWay);
			::CloseHandle((HANDLE) EventBufferFullWay);
		}

		Guid MicrophoneGuid;

        MediaSample ^PullSample(TimeSpan tsDuration);

	    array<unsigned char> ^PullDataNotConnectedToFilterGraph();

		static array<AudioDevice ^> ^GetMicrophoneDevices();
		
		bool Start();
		bool Stop();

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
		AudioFormat ^Format;

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
		void Pause(bool bPause);
		void Reset();
		virtual void Close() override;

	protected:

		bool m_bMute;
		int m_nMaxQueueSamples;
		int m_nMaxQueueSamplesTrimSize;
		array<unsigned char> ^MicrophoneFilter::ReadDataFromBuffer(int nLocation, int nLength);

		ByteBuffer ^ByteQueue;
		void ReadMicFunction();
		Thread ^RecordThread;
		IntPtr EventBufferHalfWay;
		IntPtr EventBufferFullWay;
		ManualResetEvent ^EventThreadExit;

		IntPtr Window;
		/// directsound stuff 
		IntPtr m_hDS; /*LPDIRECTSOUNDCAPTURE8*/
        IntPtr m_hBufferCapture; /*LPDIRECTSOUNDCAPTUREBUFFER8*/

		int m_nDSBufferSize;
		System::Object ^objLock;
	};
}
