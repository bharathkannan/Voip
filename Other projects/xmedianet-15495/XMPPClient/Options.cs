/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using System.Runtime.Serialization;

namespace XMPPClient
{
    [DataContract]
    public class Options
    {
        public Options()
        {}

        private bool m_bRunWithScreenLocked = true;
        [DataMember]
        public bool RunWithScreenLocked
        {
            get { return m_bRunWithScreenLocked; }
            set { m_bRunWithScreenLocked = value; }
        }

        private bool m_bLogXML = false;
        [DataMember]
        public bool LogXML
        {
            get { return m_bLogXML; }
            set { m_bLogXML = value; }
        }

        private bool m_bLogDebug = true;
        [DataMember]
        public bool LogDebug
        {
            get { return m_bLogDebug; }
            set { m_bLogDebug = value; }
        }

        private bool m_bLogTLS = false;
        [DataMember]
        public bool LogTLS
        {
            get { return m_bLogTLS; }
            set { m_bLogTLS = value; }
        }

        private bool m_bSendGeoCoordinates = false;
        [DataMember]
        public bool SendGeoCoordinates
        {
            get { return m_bSendGeoCoordinates; }
            set { m_bSendGeoCoordinates = value; }
        }

        private int m_nGeoTimeFrequency = 60;
        [DataMember]
        public int GeoTimeFrequency
        {
            get { return m_nGeoTimeFrequency; }
            set { m_nGeoTimeFrequency = value; }
        }

        private bool m_bSavePasswords = true;
        [DataMember]
        public bool SavePasswords
        {
            get { return m_bSavePasswords; }
            set { m_bSavePasswords = value; }
        }

        private bool m_bUseOnlyIBBFileTransfer = false;
        [DataMember]
        public bool UseOnlyIBBFileTransfer
        {
            get { return m_bUseOnlyIBBFileTransfer; }
            set { m_bUseOnlyIBBFileTransfer = value; }
        }

        private string m_strSOCKS5ByteStreamProxy = null;
        [DataMember]
        public string SOCKS5ByteStreamProxy
        {
            get { return m_strSOCKS5ByteStreamProxy; }
            set { m_strSOCKS5ByteStreamProxy = value; }
        }

        private bool m_bPlaySoundOnNewMessage = true;
        [DataMember]
        public bool PlaySoundOnNewMessage
        {
            get { return m_bPlaySoundOnNewMessage; }
            set { m_bPlaySoundOnNewMessage = value; }
        }

        private bool m_BVibrateOnNewMessage = true;
        [DataMember]
        public bool VibrateOnNewMessage
        {
            get { return m_BVibrateOnNewMessage; }
            set { m_BVibrateOnNewMessage = value; }
        }
    }
}
