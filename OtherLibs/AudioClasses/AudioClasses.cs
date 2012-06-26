/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.Serialization;
using System.ServiceModel;
using System.ComponentModel;


namespace AudioClasses
{
    public enum AudioSamplingRate : uint
    {
        sr8000 = 8000,
        sr11025 = 11025,
        sr16000 = 16000,
        sr22050 = 22050,
        sr48000 = 48000,
    }

    public enum AudioBitsPerSample : byte
    {
        Eight = 8,
        Sixteen = 16,
        TwentyFour = 24,
    }
    public enum AudioChannels : byte
    {
        Mono = 1,
        Stereo = 2,
    }

    public class AudioFormat
    {
        public AudioFormat(AudioSamplingRate rate, AudioBitsPerSample bits)
        {
            AudioSamplingRate = rate;
            AudioBitsPerSample = bits;
        }

        public AudioFormat(AudioSamplingRate rate, AudioBitsPerSample bits, AudioChannels ch)
        {
            AudioSamplingRate = rate;
            AudioBitsPerSample = bits;
            AudioChannels = ch;
        }

        public readonly AudioSamplingRate AudioSamplingRate = AudioSamplingRate.sr8000;
        public readonly AudioBitsPerSample AudioBitsPerSample = AudioBitsPerSample.Eight;
        public readonly AudioChannels AudioChannels = AudioChannels.Mono;

        public static AudioFormat EightByEightThousandMono = new AudioFormat(AudioSamplingRate.sr8000, AudioBitsPerSample.Eight);
        public static AudioFormat EightBySixteenThousandMono = new AudioFormat(AudioSamplingRate.sr16000, AudioBitsPerSample.Eight);
        public static AudioFormat SixteenByEightThousandMono = new AudioFormat(AudioSamplingRate.sr8000, AudioBitsPerSample.Sixteen);
        public static AudioFormat SixteenBySixteenThousandMono = new AudioFormat(AudioSamplingRate.sr16000, AudioBitsPerSample.Sixteen);

        public int BytesPerSample
        {
            get
            {
                if ((this.AudioBitsPerSample == AudioBitsPerSample.Sixteen) && (AudioChannels == AudioChannels.Mono))
                    return 2;
                else if ((this.AudioBitsPerSample == AudioBitsPerSample.Sixteen) && (AudioChannels == AudioChannels.Stereo))
                    return 4;
                else if ((this.AudioBitsPerSample == AudioBitsPerSample.Eight) && (AudioChannels == AudioChannels.Mono))
                    return 1;
                else if ((this.AudioBitsPerSample == AudioBitsPerSample.Eight) && (AudioChannels == AudioChannels.Stereo))
                    return 2;
                return 1;
            }
        }

        // Only mono for now
        public int CalculateNumberOfSamplesForDuration(TimeSpan tsDuration)
        {
            if (this.AudioSamplingRate == AudioSamplingRate.sr16000)
            {
                //                return (tsDuration.TotalMilliseconds * 16000 /*samples per s*/) / 1000 /* ms per s */;
                return (int)(tsDuration.TotalMilliseconds * 16);
            }
            else if (this.AudioSamplingRate == AudioSamplingRate.sr8000)
            {
                //return (tsDuration.TotalMilliseconds * 8000 /*samples per s*/) / 1000 /* ms per s */;
                return (int)(tsDuration.TotalMilliseconds * 8);
            }
            return 0;
        }

        public TimeSpan CalculateDurationForNumberOfSamples(int nSamples)
        {
            int nMs = 0;
            if (this.AudioSamplingRate == AudioSamplingRate.sr8000)
            {
                //nMs = (int)(nSamples * 1000 / 8000);
                nMs = (int)(nSamples / 8);
            }
            else if (this.AudioSamplingRate == AudioSamplingRate.sr16000)
            {
                //nMs = (int)(nSamples * 1000 / 16000);
                nMs = (int)(nSamples / 16);
            }
            return new TimeSpan(0, 0, 0, 0, nMs);
        }

        public AudioFormat Clone()
        {
            return new AudioFormat(AudioSamplingRate, AudioBitsPerSample, AudioChannels);
        }


        public static bool operator ==(AudioFormat a, AudioFormat b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(AudioFormat a, AudioFormat b)
        {
            return !(a == b);
        }


        public override bool Equals(object obj)
        {
            if (obj is AudioFormat)
            {
                AudioFormat audiof = obj as AudioFormat;
                if ((audiof.AudioBitsPerSample == this.AudioBitsPerSample) && (audiof.AudioChannels == this.AudioChannels)
                    && (audiof.AudioSamplingRate == this.AudioSamplingRate))
                    return true;
            }

            return false;
        }
    }

    public enum DeviceType
    {
        Input,
        Output
    }

    
	/// A speaker or microphone device
    [DataContractFormat]
    [DataContract]
    public class AudioDevice : INotifyPropertyChanged
	{
	
        public AudioDevice(Guid objGuid, string strName)
		{
			Guid = objGuid;
			Name = strName;
		}

        Guid m_objGuid;
        [DataMember]
        public Guid Guid
        {
            get { return m_objGuid; }
            set 
            {
                if (m_objGuid != value)
                {
                    m_objGuid = value;
                    FirePropertyChanged("Guid");
                }
            }
        }

        private string m_strName;
        [DataMember]
        public string Name
        {
            get { return m_strName; }
            set
            {
                if (m_strName != value)
                {
                    m_strName = value;
                    FirePropertyChanged("Name");
                }
            }
        }

        public override string ToString()
        {
            return Name;
        }

        private int m_nDeviceId = 0;
        [DataMember]
        public int DeviceId
        {
            get { return m_nDeviceId; }
            set
            {
                if (m_nDeviceId != value)
                {
                    m_nDeviceId = value;
                    FirePropertyChanged("DeviceId");
                }
            }
        }

        private DeviceType m_eDeviceType = DeviceType.Input;
        [DataMember]
        public DeviceType DeviceType
        {
            get { return m_eDeviceType; }
            set
            {
                if (DeviceType != value)
                {
                    m_eDeviceType = value;
                    FirePropertyChanged("DeviceType");
                }
            }
        }

        #region INotifyPropertyChanged Members

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        void FirePropertyChanged(string strProp)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(strProp));
        }

        #endregion
	}
}
