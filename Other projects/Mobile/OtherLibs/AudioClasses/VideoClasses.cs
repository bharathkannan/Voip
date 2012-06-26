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
    public enum VideoDataFormat
	{
		Unknown,
		RGB32, // RGB32 frames
        RGB24,
		MJPEG, /// motion jpeg frames (each frame a jpeg)
        MP4, // H.264 in mp4 container
        WMV9, // windows media 9 in asf container
        WMVSCREEN, /// windows media screen in asf container
        VC1, /// vc1 in asf container
        H264, /// Raw h.264 stream returned from cam
	};



    [DataContractFormat]
    [DataContract]
    public class VideoCaptureRate : INotifyPropertyChanged
    {
        public VideoCaptureRate()
        {
        }

        public VideoCaptureRate(int nWidth, int nHeight, int nFrameRate, int nBitRate)
        {
            m_nWidth = nWidth;
            m_nHeight = nHeight;
            m_nFrameRate = nFrameRate;
            m_nEncodingBitRate = nBitRate;
        }

        #region INotifyPropertyChanged Members

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        void FirePropertyChanged(string strProp)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(strProp));
        }

        #endregion


        public override string ToString()
        {
            return string.Format("{0} x {1}, {2} fps - {3}, Stream: {4}", Width, Height, FrameRate, VideoFormatString, StreamIndex);
        }

        private int m_nWidth = 640;

        [DataMember]
        public int Width
        {
            get { return m_nWidth; }
            set { m_nWidth = value; FirePropertyChanged("Width"); }
        }

        private int m_nHeight = 480;

        [DataMember]
        public int Height
        {
            get { return m_nHeight; }
            set { m_nHeight = value; FirePropertyChanged("Height"); }
        }

        private int m_nFrameRate = 30;

        [DataMember]
        public int FrameRate
        {
            get { return m_nFrameRate; }
            set { m_nFrameRate = value; FirePropertyChanged("FrameRate"); }
        }

        public TimeSpan FrameDuration
        {
            get
            {
                return new TimeSpan(0, 0, 0, 0, 1000 / FrameRate);
            }
        }

        //private int m_nEncodingBitRate = 5000000;
        private int m_nEncodingBitRate = 2000000;
        [DataMember]
        public int EncodingBitRate
        {
            get { return m_nEncodingBitRate; }
            set { m_nEncodingBitRate = value; FirePropertyChanged("EncodingBitRate"); }
        }

        public override bool Equals(object obj)
        {
            if (obj is VideoCaptureRate)
            {
                VideoCaptureRate cr = obj as VideoCaptureRate;
                if ((Width == cr.Width) && (Height == cr.Height) && (FrameRate == cr.FrameRate))
                    return true;
            }
            return false;
        }

        private VideoDataFormat m_eVideoDataFormat = VideoDataFormat.RGB32;
        [DataMember]
        public VideoDataFormat VideoDataFormat
        {
            get { return m_eVideoDataFormat; }
            set { m_eVideoDataFormat = value; }
        }

        private string m_strVideoFormatString = "";

        public string VideoFormatString
        {
            get { return m_strVideoFormatString; }
            set { m_strVideoFormatString = value; }
        }


        private int m_nStreamIndex = 0;

        public int StreamIndex
        {
            get { return m_nStreamIndex; }
            set { m_nStreamIndex = value; }
        }
    }


    /// <summary>
    /// Our main WCF interface for controlling network cameras
    /// </summary>
    public delegate void DelegateRawFrame(byte[] bRawData, VideoDataFormat format, int nWidth, int nHeight);

    public interface ICameraControl
    {
        void PanLeft();
        void PanRight();
        void PanRelative(int Units);

        void TiltUp();
        void TiltDown();
        void TiltRelative(int Units);

        void Zoom(int Factor);

        void TurnOffLED();

    }

    public interface IVideoSource
    {
        VideoCaptureRate[] GetSupportedCaptureRates();

        event DelegateRawFrame NewFrame;

        VideoCaptureRate ActiveVideoCaptureRate { get; set; }

        string Name { get; set; }
    }

   
}
