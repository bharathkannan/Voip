/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.Net.XMPP;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Net.XMPP.Jingle;
using RTP;
using System.ComponentModel;

namespace RTP
{
    public enum SessionState
    {
        NotEstablished,
        Connecting,
        Incoming,
        Established,
        TearingDown,
    }

    [Flags]
    public enum KnownAudioPayload
    {
        G711 = 1,
        Speex16000 = 2,
        Speex8000 = 4,
        G722 = 8,
        G722_40 = 16,
    }

    

    /// <summary>
    ///  Responsible for the RTP stream to an from a remote endpoint, as well as session management
    /// </summary>
    /// 

    public class MediaSession :  INotifyPropertyChanged, IComparer<Candidate>
    {
        #region MYEDit

        public MediaSession(IPEndPoint local)
        {
            UserName = GenerateRandomString(8);
            Password = GenerateRandomString(16);

            m_objAudioRTPStream.OnUnhandleSTUNMessage += new DelegateSTUNMessage(m_objAudioRTPStream_OnUnhandleSTUNMessage);
            AudioRTCPStream.OnUnhandleSTUNMessage += new DelegateSTUNMessage(AudioRTCPStream_OnUnhandleSTUNMessage);
            AddKnownAudioPayload(KnownAudioPayload.Speex16000 | KnownAudioPayload.Speex8000 | KnownAudioPayload.G711 | KnownAudioPayload.G722);
            Initiator = true;
            LocalEndpoint = local;
            Session = "";
            RemoteJID = "";
            XMPPClient = null;
            SessionState = SessionState.Connecting;
        }

        #endregion

        public MediaSession(string strSession, string strRemoteJID, IPEndPoint LocalEp, XMPPClient client)
        {
            UserName = GenerateRandomString(8);
            Password = GenerateRandomString(16);

            m_objAudioRTPStream.OnUnhandleSTUNMessage += new DelegateSTUNMessage(m_objAudioRTPStream_OnUnhandleSTUNMessage);
            AudioRTCPStream.OnUnhandleSTUNMessage += new DelegateSTUNMessage(AudioRTCPStream_OnUnhandleSTUNMessage);
            AddKnownAudioPayload(KnownAudioPayload.Speex16000 | KnownAudioPayload.Speex8000 | KnownAudioPayload.G711 | KnownAudioPayload.G722);
            Initiator = true;
            LocalEndpoint = LocalEp;
            Session = strSession;
            RemoteJID = strRemoteJID;
            XMPPClient = client;
            SessionState = SessionState.Connecting;
//Uncomment 2 lines dude

            CheckGoogleTalk();
            Bind();
        }

        private RosterItem m_objRosterItem = null;

        public RosterItem RosterItem
        {
            get { return m_objRosterItem; }
            set { m_objRosterItem = value; }
        }

        /// <summary>
        /// See if this specific instance is running a client we know uses google talk protocl
        /// TODO... see if this a instance running standard jingle as well
        /// </summary>
        void CheckGoogleTalk()
        {
             RosterItem = XMPPClient.FindRosterItem(RemoteJID);
            if (RosterItem != null)
            {
                RosterItemPresenceInstance specificinstance = RosterItem.FindInstance(RemoteJID);
                if (specificinstance != null)
                {
                    UseGoogleTalkProtocol = specificinstance.IsKnownGoogleClient;
                }
            }
            else if (RemoteJID.IndexOf("gmail.com") > 0)
                UseGoogleTalkProtocol = true;
        }

        public MediaSession(string strRemoteJID, IPEndPoint LocalEp, XMPPClient client)
        {
            UserName = GenerateRandomString(8);
            Password = GenerateRandomString(16);

            m_objAudioRTPStream.OnUnhandleSTUNMessage += new DelegateSTUNMessage(m_objAudioRTPStream_OnUnhandleSTUNMessage);
            AudioRTCPStream.OnUnhandleSTUNMessage += new DelegateSTUNMessage(AudioRTCPStream_OnUnhandleSTUNMessage);
            AddKnownAudioPayload(KnownAudioPayload.Speex16000 | KnownAudioPayload.Speex8000 | KnownAudioPayload.G711 | KnownAudioPayload.G722);
            Initiator = true;
            LocalEndpoint = LocalEp;
            RemoteJID = strRemoteJID;
            XMPPClient = client;
            SessionState = SessionState.Connecting;
            //uncomment 2 lines dude
            CheckGoogleTalk();
            Bind();
        }

        public MediaSession(string strSession, IQ intialJingle, KnownAudioPayload LocalPayloads, IPEndPoint LocalEp, XMPPClient client)
        {
            UserName = GenerateRandomString(8);
            Password = GenerateRandomString(16);

            m_objAudioRTPStream.OnUnhandleSTUNMessage += new DelegateSTUNMessage(m_objAudioRTPStream_OnUnhandleSTUNMessage);
            AudioRTCPStream.OnUnhandleSTUNMessage += new DelegateSTUNMessage(AudioRTCPStream_OnUnhandleSTUNMessage);
            AddKnownAudioPayload(LocalPayloads);

            Initiator = false;
            LocalEndpoint = LocalEp;
            Session = strSession;
            XMPPClient = client;
            RemoteJID = intialJingle.From;
            RosterItem = XMPPClient.FindRosterItem(RemoteJID);
            SessionState = SessionState.Incoming;
        }


        public IQ InitialJingle = null;
        public void StartIncoming(IQ initialJingle)
        {

            InitialJingle = initialJingle;

            Bind();

            ParsePayloads(initialJingle);
            /// If we don't have any candidates in our sesion-initate message, we must wait for a transport info to get the candidates
            ParseCandidates(initialJingle);
            //if (RemoteCandidates.Count > 0)
            //    SendAcceptSession();
            if (RemoteCandidates.Count > 0)
                SendTransportInfo();
        }

        public readonly bool Initiator = true;




        protected string UserName = "";
        protected string Password = "";

        protected string RemoteUserName = "";
        protected string RemotePassword = "";

//Read
        private RTPAudioStream m_objAudioRTPStream = new RTPAudioStream(0, null);

        public RTPAudioStream AudioRTPStream
        {
            get { return m_objAudioRTPStream; }
            set { m_objAudioRTPStream = value; }
        }

        protected RTCPSession AudioRTCPStream = new RTCPSession();

      
   //Till this     
        protected List<Candidate> LocalCandidates = new List<Candidate>();
        protected List<Candidate> RemoteCandidates = new List<Candidate>();

        private string m_strSession = null;
        public string Session
        {
            get { return m_strSession; }
            set
            {
                if (m_strSession != value)
                {
                    m_strSession = value;
                    FirePropertyChanged("Session");
                }

            }

        }

        private string m_strRemoteJID = "";
        public string RemoteJID
        {
            get { return m_strRemoteJID; }
            set
            {
                if (m_strRemoteJID != value)
                {
                    m_strRemoteJID = value;
                    FirePropertyChanged("RemoteJID");
                }

            }
        }


        private SessionState m_eSessionState = SessionState.NotEstablished;
        public SessionState SessionState
        {
            get { return m_eSessionState; }
            set
            {
                if (m_eSessionState != value)
                {
                    m_eSessionState = value;
                    FirePropertyChanged("SessionState");
                }

            }
        }

        public XMPPClient XMPPClient = null;

        private IPEndPoint m_objLocalEndpoint = null;
        public IPEndPoint LocalEndpoint
        {
            get { return m_objLocalEndpoint; }
            set { m_objLocalEndpoint = value; }
        }

        IPEndPoint m_objRemoteEndpoint = null;
        public IPEndPoint RemoteEndpoint
        {
            get { return m_objRemoteEndpoint; }
            set
            {
                if (m_objRemoteEndpoint != value)
                {
                    m_objRemoteEndpoint = value;
                    FirePropertyChanged("RemoteEndpoint");
                }
            }
        }

        public bool UseStun = true;

        public DateTime m_dtStartTime = DateTime.Now;
        public TimeSpan CallDuration
        {
            get
            {
                return DateTime.Now - m_dtStartTime;
            }
            set
            {
                FirePropertyChanged("CallDuration");
            }
        }

        public string Statistics
        {
            get
            {
                return AudioRTPStream.IncomingRTPPacketBuffer.Statistics;
            }
            set
            {
                FirePropertyChanged("Statistics");
            }
        }

        public virtual string SendInitiateSession()
        {
            Jingle jingleinfo = BuildOutgoingAudioRequest(true, !this.UseGoogleTalkProtocol);

            string strSession = XMPPClient.JingleSessionManager.InitiateNewSession(RemoteJID, jingleinfo);
            Session = strSession;
            return Session;
        }

        public void GotNewSessionAck()
        {
            if (UseGoogleTalkProtocol == true)
            {
                /// Send our transport info  (google talk sends them right after sending the session initiate
                /// 
                Jingle jingleinfo = this.BuildOutgoingTransportInfo();
                XMPPClient.JingleSessionManager.SendTransportInfo(this.Session, jingleinfo);
            }
        }
        /// <summary>
        /// Accept the session as soon as we agree on media.  Candidates are then tried
        /// </summary>
        bool m_bHasAccepted = false;

        /// <summary>
        ///  The user must accept this call
        /// </summary>
        bool m_bHasUserAccepted = false;
        /// <summary>
        /// ICE negotiation must complete successfully before we can send an accept
        /// </summary>
        bool m_bHasICEAccepted = false;
        protected void ICEDoneStartRTP()
        {
            if (Initiator == false)
            {
                if (RemoteCandidates.Count < 0)
                    return;

                m_bHasICEAccepted = true;
                DoSendAcceptSession();
            }

            if (SelectedPair != null)
                SelectedPair.StartIndicationThread(this.AudioRTPStream, UseGoogleTalkProtocol);
        }

        public void UserAcceptSession()
        {
            m_bHasUserAccepted = true;
            DoSendAcceptSession();
        }

        void DoSendAcceptSession()
        {
            if (m_bHasAccepted == true)
                return;
            if (m_bHasUserAccepted == false)
                return;
            if (m_bHasICEAccepted == false)
                return;

            m_bHasAccepted = true;
            Jingle jingleinfo = BuildOutgoingAudioRequest(false, false);
            XMPPClient.JingleSessionManager.SendAcceptSession(this.Session, jingleinfo);
        }

     
        public virtual void GotTransportInfo(IQ iq)
        {
            /// Ignore additional transport candidates.  If they don't send it to us on the first one, we don't want them (probably TURN)
            /// 
            if (RemoteCandidates.Count > 0)
                return;
            ParseCandidates(iq);

            if (Initiator == true) // we send our transport info in our session initiate, so we should now have both
            {
                BuildCandidatePairs();
                PerformICEStunProcedures(); /// Should have all our transport info  now
            }
            else
            {
                BuildLocalCandidates(); /// Rebuild our local candidates, since this may be google talk and it doesn't follow spec
                SendTransportInfo();
            }
        }

        void SendTransportInfo()
        {
             /// If we not the initiator, it's now time to send our candidates
             /// 
            if (Initiator == true)
            {
            }
            else
            {
                BuildCandidatePairs();
                Jingle jingleinfo = this.BuildOutgoingTransportInfo();
                XMPPClient.JingleSessionManager.SendTransportInfo(this.Session, jingleinfo);
            }

        }

        public void GotSendTransportInfoAck()
        {
            if (Initiator == true)
            {
                /// The terminator got our transport info, now lets wait for theirs
                ///                 

            }
            else
            {
                /// originator got our transport info, they should be starting ICE procedures now
                /// 
                /// Not sure how stun is supposed to work, but unless we send out packets to, the firewall is never opened on the receiver side
                /// 

                Jingle jingleinfo = new Jingle();
                jingleinfo.Action = Jingle.SessionInfo;
                jingleinfo.JingleStateChange = JingleStateChange.Ringing;
                jingleinfo.Content = null;
                XMPPClient.JingleSessionManager.SendJingle(this.Session, jingleinfo);

                PerformICEStunProcedures(); /// Should have all our transport info  now
            }

        }

      
        AudioConferenceMixer AudioMixer = null;
        public void SessionAccepted(IQ iq, AudioConferenceMixer objAudioMixer)
        {
            AudioMixer = objAudioMixer;
            ParsePayloads(iq);
            StartAudio();
        }

       

        public virtual void GotAcceptSessionAck(AudioConferenceMixer objAudioMixer)
        {
            AudioMixer = objAudioMixer;
            StartAudio();
        }

        // Start Audio once ICE is done



        /// <summary>
        //Needed for audio send
        /// </summary>
        void StartAudio()
        {
            SessionState = SessionState.Established;
            if (AudioMixer != null)
                AudioMixer.AddInputOutputSource(AudioRTPStream, AudioRTPStream);
            AudioRTPStream.Start(RemoteEndpoint, AudioRTPStream.PTimeTransmit, AudioRTPStream.PTimeReceive);
        }

        public void StopMedia(AudioConferenceMixer AudioMixer)
        {
            SessionState = SessionState.TearingDown;
            if (AudioMixer != null)
                AudioMixer.RemoveInputOutputSource(AudioRTPStream, AudioRTPStream);
            AudioRTPStream.StopSending();
            AudioRTCPStream.Stop();

            if (SelectedPair != null)
            {
                SelectedPair.StopIndicationThread();
                SelectedPair = null;
            }
        }


        void Bind()
        {
            AudioRTPStream.Bind(LocalEndpoint);  // For ICE we should have one rtp stream for each candidate, and they should receive stun packets as well as rtp
            AudioRTCPStream.Bind(new IPEndPoint(LocalEndpoint.Address, LocalEndpoint.Port + 1));
            BuildLocalCandidates();
        }

        private bool m_bUseGoogleTalkProtocol = false;
        public bool UseGoogleTalkProtocol
        {
            get { return m_bUseGoogleTalkProtocol; }
            set { m_bUseGoogleTalkProtocol = value; }
        }


        protected virtual void ParsePayloads(IQ iq)
        {
        }

        protected virtual void ParseCandidates(IQ iq)
        {

        }

        /// <summary>
        /// some protocols want to send candidate separately for some strange reason
        /// </summary>
        /// <returns></returns>
        protected virtual Jingle BuildOutgoingTransportInfo()
        {
            Jingle jingleinfo = new Jingle();
            jingleinfo.Content = new Content();
            jingleinfo.Action = Jingle.TransportInfo;
            jingleinfo.Content.Name = "audio";
            jingleinfo.Content.Description = null;
            //if (Initiator == true)
                jingleinfo.Content.Creator = "initiator";
            //else
              //  jingleinfo.Content.Creator = "responder";

            if (UseGoogleTalkProtocol == true)
            {
                jingleinfo.Content.ICETransport = null;
                jingleinfo.Content.GoogleTransport = new Transport();
                jingleinfo.Content.GoogleTransport.Candidates.AddRange(LocalCandidates);
            }
            else
            {
                jingleinfo.Content.ICETransport = new Transport();
                jingleinfo.Content.GoogleTransport = null;
                jingleinfo.Content.ICETransport.ufrag = UserName;
                jingleinfo.Content.ICETransport.pwd = Password;

                jingleinfo.Content.ICETransport.Candidates.AddRange(LocalCandidates);
            }

            return jingleinfo;
        }

        protected Jingle BuildOutgoingAudioRequest(bool IsInitiation, bool bAddCandidates)
        {
            Jingle jingleinfo = JingleSessionManager.CreateBasicOutgoingAudioRequest(this.AudioRTPStream.LocalEndpoint.Address.ToString(), this.AudioRTPStream.LocalEndpoint.Port);
            jingleinfo.Content.Description.Payloads.Clear();
            jingleinfo.Content.Name = "audio";

            if (IsInitiation == true)
            {
                jingleinfo.Content.Description.Payloads.AddRange(LocalPayloads);
                jingleinfo.Content.Creator = "initiator";
            }

            else
            {
                jingleinfo.Content.Creator = "initiator";
                //jingleinfo.Content.Creator = "responder";
                SetCodecsFromPayloads();
                jingleinfo.Content.Description.Payloads.Add(AgreedPayload);
            }

            if (UseGoogleTalkProtocol == true)
            {
                jingleinfo.Content.ICETransport = null;
                jingleinfo.Content.GoogleTransport = new Transport();
                if (bAddCandidates == true)
                    jingleinfo.Content.GoogleTransport.Candidates.AddRange(LocalCandidates);
            }
            else
            {
                jingleinfo.Content.ICETransport = new Transport();
                jingleinfo.Content.GoogleTransport = null;
                if (bAddCandidates == true)
                {
                    jingleinfo.Content.ICETransport.ufrag = UserName;
                    jingleinfo.Content.ICETransport.pwd = Password;

                    jingleinfo.Content.ICETransport.Candidates.AddRange(LocalCandidates);
                }
            }

            return jingleinfo;
        }


        protected virtual void BuildLocalCandidates()
        {
            //#if WINDOWS_PHONE
            //if (this.AudioRTPStream.LocalEndpoint.Address.Address != 0)
            //{
            //    this.PerformSTUNRequest(new IPEndPoint(IPAddress.Parse("0.0.0.0"), this.AudioRTPStream.LocalEndpoint.Port), 100);
            //}
            //#endif
            IPEndPoint PublicIPEndpoint = null;
            LocalCandidates.Clear();
            if (UseStun == true)
            {
                try
                {
                    PublicIPEndpoint = this.PerformSTUNRequest(STUNServer, 4000);
                    if (PublicIPEndpoint != null)
                    {

                        //Candidate stuncand = new Candidate() { ipaddress = PublicIPEndpoint.Address.ToString(), port = PublicIPEndpoint.Port, type = "stun", component = 1 };
                        Candidate stuncand = new Candidate() { ipaddress = PublicIPEndpoint.Address.ToString(), port = PublicIPEndpoint.Port, type = "srflx", component = 1, relport = this.AudioRTPStream.LocalEndpoint.Port.ToString(), reladdr = this.AudioRTPStream.LocalEndpoint.Address.ToString() };
                        stuncand.IPEndPoint = PublicIPEndpoint;
                        if (UseGoogleTalkProtocol == true)
                        {
                            stuncand.username = UserName;
                            stuncand.password = Password;
                            stuncand.foundation = null;
                            stuncand.preference = "0.9";
                        }
                        else
                        {
                            stuncand.foundation = "2";
                            stuncand.id = "2";
                            stuncand.preference = null;
                            stuncand.name = null;
                        }

                        CalculatePriority(100, 10, stuncand);
                        LocalCandidates.Add(stuncand);

                        if (UseGoogleTalkProtocol == false)
                        {
                            /// RTCP candidate
                            Candidate stunrtcpcand = new Candidate() { ipaddress = PublicIPEndpoint.Address.ToString(), port = PublicIPEndpoint.Port + 1, type = "srflx", component = 2, relport = (this.AudioRTPStream.LocalEndpoint.Port + 1).ToString(), reladdr = this.AudioRTPStream.LocalEndpoint.Address.ToString() };
                            stunrtcpcand.IPEndPoint = new IPEndPoint(PublicIPEndpoint.Address, PublicIPEndpoint.Port + 1);
                            stunrtcpcand.foundation = "2";
                            stunrtcpcand.id = "4";
                            stunrtcpcand.preference = null;
                            stunrtcpcand.name = null;
                            CalculatePriority(100, 10, stunrtcpcand);
                            LocalCandidates.Add(stunrtcpcand);
                        }

                    }
                }
                catch (Exception ex)
                {
                    /// STUN server DNS was invalid, guess we get not public ip
                }
            }

            /// Windows phone can't always give us an address, so we have to rely on the stun address alone
            if (this.AudioRTPStream.LocalEndpoint.Address.Address != 0)
            {
#if WINDOWS_PHONE
//                if (PublicIPEndpoint != null)
//                    this.AudioRTPStream.LocalEndpoint.Port = PublicIPEndpoint.Port; /// Windows phone won't let us bind to a port, so we have to figure out what it is here if we can
#endif
                Candidate cand = new Candidate() { ipaddress = this.AudioRTPStream.LocalEndpoint.Address.ToString(), port = this.AudioRTPStream.LocalEndpoint.Port, type = "host", component = 1 };
                cand.IPEndPoint = new IPEndPoint(this.AudioRTPStream.LocalEndpoint.Address, this.AudioRTPStream.LocalEndpoint.Port);
                //Candidate cand = new Candidate() { ipaddress = this.AudioRTPStream.LocalEndpoint.Address.ToString(), port = this.AudioRTPStream.LocalEndpoint.Port, type = "local", component = 1 };
                if (UseGoogleTalkProtocol == true)
                {
                    cand.username = UserName;
                    cand.password = Password;
                    cand.foundation = null;
                    cand.preference = "1.0";
                }
                else
                {
                    cand.foundation = "1";
                    cand.id = "1";
                    cand.preference = null;
                    cand.name = null;

                }
                CalculatePriority(126, 40, cand);
                LocalCandidates.Add(cand);
            }

            if (UseGoogleTalkProtocol == false)
            {
                /// RTCP candidate
                Candidate rtcpcand = new Candidate() { ipaddress = this.AudioRTPStream.LocalEndpoint.Address.ToString(), port = this.AudioRTPStream.LocalEndpoint.Port+1, type = "host", component=2 };
                rtcpcand.IPEndPoint = new IPEndPoint(this.AudioRTPStream.LocalEndpoint.Address, this.AudioRTPStream.LocalEndpoint.Port + 1);

                rtcpcand.foundation = "1";
                rtcpcand.id = "3";
                rtcpcand.preference = null;
                rtcpcand.name = null;


                CalculatePriority(126, 10, rtcpcand);
                LocalCandidates.Add(rtcpcand);
            }
        }

        protected void SetCodecsFromPayloads()
        {
            if ((LocalPayloads.Count <= 0) || (RemotePayloads.Count <= 0))
                return;

            bool bFoundAgreeableCodec = false;
            /// Send back the first codec we agree upon
            /// 
            foreach (Payload remotepayload in RemotePayloads)
            {
                foreach (Payload localpayload in LocalPayloads)
                {
#if !WINDOWS_PHONE
                    if ( (string.Compare(remotepayload.Name, localpayload.Name, true) == 0) && (remotepayload.ClockRate == localpayload.ClockRate))
#else
                    if ((string.Compare(remotepayload.Name, localpayload.Name, StringComparison.CurrentCultureIgnoreCase) == 0) && (remotepayload.ClockRate == localpayload.ClockRate))
#endif
                    {
                        bFoundAgreeableCodec = true;
                        AgreedPayload = remotepayload;
                        if ((AgreedPayload.Ptime != null) && (AgreedPayload.Ptime.Length > 0))
                        {
                            this.AudioRTPStream.PTimeReceive = Convert.ToInt32(AgreedPayload.Ptime);
                            this.AudioRTPStream.PTimeTransmit = Convert.ToInt32(AgreedPayload.Ptime);
                        }

                        break;
                    }
                }
                if (bFoundAgreeableCodec == true)
                    break;
            }

            if (bFoundAgreeableCodec == false)
            {
                /// Call must die, no agreeable codecs
                /// 
                throw new Exception("No agreeable codecs found");
            }
        }

      

        public List<Payload> RemotePayloads = new List<Payload>();

        public List<Payload> LocalPayloads = new List<Payload>();
        public void ClearAllPayloads()
        {
            LocalPayloads.Clear();
        }

        private Payload m_objAgreedPayload = null;

        public Payload AgreedPayload
        {
            get { return m_objAgreedPayload; }
            set 
            { 
                m_objAgreedPayload = value;
                AudioRTPStream.Payload = (byte) m_objAgreedPayload.PayloadId;
                if (m_objAgreedPayload.Name == "G722")
                    AudioRTPStream.AudioCodec = new G722CodecWrapper();
                else if (m_objAgreedPayload.Name == "G722_40")
                    AudioRTPStream.AudioCodec = new G722CodecWrapper();
#if !WINDOWS_PHONE
                else if ((m_objAgreedPayload.Name == "speex") && (m_objAgreedPayload.ClockRate == "16000"))
                   AudioRTPStream.AudioCodec = new SpeexCodec(NSpeex.BandMode.Wide);
                else if ((m_objAgreedPayload.Name == "speex") && (m_objAgreedPayload.ClockRate == "8000") )
                   AudioRTPStream.AudioCodec = new SpeexCodec(NSpeex.BandMode.Narrow);
#endif
                else if ((m_objAgreedPayload.Name == "PCMU") && (m_objAgreedPayload.ClockRate == "8000"))
                   AudioRTPStream.AudioCodec = new G711Codec();
            }
        }

        public void AddKnownAudioPayload(KnownAudioPayload payload)
        {
            if ((payload & KnownAudioPayload.G722) == KnownAudioPayload.G722)
                LocalPayloads.Add(new Payload() { PayloadId = 9, Channels = "1", ClockRate = "8000", Name = "G722", Ptime="20" });
            if ((payload & KnownAudioPayload.G722_40) == KnownAudioPayload.G722_40)
                LocalPayloads.Add(new Payload() { PayloadId = 9, Channels = "1", ClockRate = "8000", Name = "G722", Ptime = "40" });
            if ((payload & KnownAudioPayload.Speex16000) == KnownAudioPayload.Speex16000)
               LocalPayloads.Add(new Payload() { PayloadId = 96, Channels = "1", ClockRate = "16000", Name = "speex" });
            if ((payload & KnownAudioPayload.Speex8000) == KnownAudioPayload.Speex8000)
                LocalPayloads.Add(new Payload() { PayloadId = 97, Channels = "1", ClockRate = "8000", Name = "speex" });
            if ((payload & KnownAudioPayload.G711) == KnownAudioPayload.G711)
                LocalPayloads.Add(new Payload() { PayloadId = 0, Channels = "1", ClockRate = "8000", Name = "PCMU" });
        }


        public static string STUNServer = "stun.ekiga.net";


        static Random rand = new Random();
        public static string GenerateRandomString(int nLength)
        {
            /// 48-57, 65-90, 97-122
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < nLength; i++)
            {
                int c = rand.Next(62);
                if (c < 10)
                    c += 48;
                else if ((c >= 10) && (c < 36))
                    c += (65 - 10);
                else if (c >= 36)
                    c += (97 - 36);

                sb.Append((char)c);
            }

            return sb.ToString();
        }

        List<CandidatePair> CandidatePairs = new List<CandidatePair>();
        object CandidatePairsLock = new object();
        CandidatePair SelectedPair = null;
        protected bool IceDone = false;
        System.Threading.ManualResetEvent EventWaitForInitiatedToRespond = new System.Threading.ManualResetEvent(false);
        protected virtual void PerformICEStunProcedures()
        {
          
            System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(DoICEStunProcedures), this);
        }

        void BuildCandidatePairs()
        {
            lock (CandidatePairsLock)
            {
                // Now perform ICE tests to each of our candidates, see which one we get a response from.
                foreach (Candidate nextcand in this.RemoteCandidates)
                {
                    AddLocalPairsForRemoteCandidate(nextcand);
                }
            }
        }

        // must be locked before calling
        void AddLocalPairsForRemoteCandidate(Candidate nextcand)
        {
            if (nextcand.name == "rtcp")
                return;

            if (nextcand.component != 1)
                return;

            if (nextcand.IPEndPoint.Address.AddressFamily != AddressFamily.InterNetwork)
                return;

            foreach (Candidate nextlocalcand in this.LocalCandidates)
            {
                if (nextlocalcand.component == 1)  // May need stun checks on rtcp
                    CandidatePairs.Add(new CandidatePair(nextlocalcand, nextcand, this.Initiator));
            }
        }

        void DoICEStunProcedures(object obj)
        {
            IceDone = false;
            epDefault = null;

            string strUserName = string.Format("{0}:{1}", this.RemoteUserName, this.UserName);
            string strPassword = this.RemotePassword;


            while (true)
            {
                CandidatePair pairchecked = CheckNextPair();
                if (pairchecked != null)
                {

                    /// See if we have any successful candidates yet if we are the controlling agent
                    if (Initiator == true)
                    {
                        CandidatePair[] pairs = new CandidatePair[] { };
                        lock (CandidatePairsLock) 
                            pairs = CandidatePairs.ToArray();
                        foreach (CandidatePair nextpair2 in pairs)
                        {
                            if ((nextpair2.HasReceivedSuccessfulIncomingSTUNCheck == true) && (nextpair2.CandidatePairState == CandidatePairState.Succeeded))
                            {
                                /// Tell the other end we will be using this candidate pair
                                /// 
                                nextpair2.TellRemoteEndToUseThisPair(this.AudioRTPStream, strUserName, strPassword);

                                /// Finished
                                this.RemoteEndpoint = nextpair2.RemoteCandidate.IPEndPoint;
                                SelectedPair = nextpair2;
                                ICEDoneStartRTP();
                                return;
                            }
                        }
                    }
                    else
                    {
                        /// See if the the other end (the initiator) has told us to use candidates
                        if (IceDone == true)
                            return;
                    }

                    if (pairchecked.CandidatePairState == CandidatePairState.Succeeded)
                    {
                        this.RemoteEndpoint = pairchecked.RemoteCandidate.IPEndPoint;
                        /// Succeeded here, may not succeed the other way though.  Still, store this as a last resort
                    }
                }
                else
                    break;
            }
           
              
        
            if (this.RemoteEndpoint == null)
                this.RemoteEndpoint = epDefault;

            ICEDoneStartRTP();
        }


        IPEndPoint epDefault = null;


        CandidatePair CheckNextPair()
        {

            string strUserName = string.Format("{0}:{1}", this.RemoteUserName, this.UserName);
            string strPassword = this.RemotePassword;

            CandidatePair[] pairs = new CandidatePair[] { };
            lock (CandidatePairsLock)
                pairs = CandidatePairs.ToArray();

            foreach (CandidatePair nextpair in pairs)
            {
                if (UseGoogleTalkProtocol == true)
                {
                    strUserName = string.Format("{0}{1}", nextpair.RemoteCandidate.username, this.UserName);
                    strPassword = nextpair.RemoteCandidate.password;
                }
               
                if (epDefault == null)
                    epDefault = nextpair.RemoteCandidate.IPEndPoint;

                if (!((nextpair.CandidatePairState == CandidatePairState.Succeeded) || (nextpair.CandidatePairState == CandidatePairState.Failed)))
                {
                    if (UseGoogleTalkProtocol == false)
                        nextpair.PerformOutgoingSTUNCheck(this.AudioRTPStream, strUserName, strPassword);
                    else
                        nextpair.PerformOutgoingSTUNCheckGoogle(this.AudioRTPStream, strUserName, strPassword);

                    return nextpair;
                }
            }

            return null;
        }

        
        void m_objAudioRTPStream_OnUnhandleSTUNMessage(STUNMessage smsg, IPEndPoint epfrom)
        {
                
            /// Our RTPStream received a STUN message.
            if (smsg.Class == StunClass.Request)
            {
                if (smsg.Method == StunMethod.Binding)
                {
                    UserNameAttribute IncomingUserNameAttribute = smsg.FindAttribute(StunAttributeType.UserName) as UserNameAttribute;
                    UseCandidateAttribute IncomingUseCandidateAttribute = smsg.FindAttribute(StunAttributeType.UseCandidate) as UseCandidateAttribute;

                    CandidatePair PairReferenced = null;

                    lock (CandidatePairsLock)
                    {
                        if (UseGoogleTalkProtocol == false)
                        {
                            foreach (CandidatePair nextpair in this.CandidatePairs)
                            {
                                if ((epfrom.Address.Equals(nextpair.RemoteCandidate.IPEndPoint.Address) == true) && (epfrom.Port == nextpair.RemoteCandidate.IPEndPoint.Port))
                                {
                                    PairReferenced = nextpair;
                                    nextpair.HasReceivedSuccessfulIncomingSTUNCheck = true;
                                    break;
                                }
                            }

                            if (PairReferenced == null)
                            {
                                /// We're receiving a binding request from an unknown candidate.  If the credentials match we should add it to our stun check list
                                /// 

                                Candidate newcand = new Candidate();
                                newcand.IPEndPoint = epfrom;
                                newcand.priority = (int)CalculatePriority(110, 10, 1);
                                AddLocalPairsForRemoteCandidate(newcand);
                            }
                        }
                        else if (IncomingUserNameAttribute != null)
                        {
                            foreach (CandidatePair nextpair in this.CandidatePairs)
                            {
                                if (IncomingUserNameAttribute.UserName.IndexOf(nextpair.RemoteCandidate.username) >= 0)
                                {
                                    PairReferenced = nextpair;
                                    nextpair.HasReceivedSuccessfulIncomingSTUNCheck = true;
                                    break;
                                }
                            }
                        }
                    }


                    /// Send a response
                    STUN2Message sresp = new STUN2Message();
                    sresp.TransactionId = smsg.TransactionId;
                    sresp.Method = StunMethod.Binding;
                    sresp.Class = StunClass.Success;

                    if (UseGoogleTalkProtocol == false)
                    {
                        XORMappedAddressAttribute attr = new XORMappedAddressAttribute();
                        attr.Port = (ushort)epfrom.Port;
                        attr.IPAddress = epfrom.Address;
                        attr.AddressFamily = StunAddressFamily.IPv4;
                        sresp.AddAttribute(attr);

                        IceControlledAttribute iattr = new IceControlledAttribute();
                        sresp.AddAttribute(iattr);

                        UserNameAttribute unameattr = new UserNameAttribute();
                        unameattr.UserName = string.Format("{0}:{1}", this.UserName, this.RemoteUserName);
                        sresp.AddAttribute(unameattr);

                        /// Add message integrity, computes over all the items currently added
                        /// 
                        int nLengthWithoutMessageIntegrity = sresp.Bytes.Length;
                        MessageIntegrityAttribute mac = new MessageIntegrityAttribute();
                        sresp.AddAttribute(mac);
                        mac.ComputeHMACShortTermCredentials(sresp, nLengthWithoutMessageIntegrity, this.RemotePassword);

                        /// Add fingerprint
                        /// 
                        int nLengthWithoutFingerPrint = sresp.Bytes.Length;
                        FingerPrintAttribute fattr = new FingerPrintAttribute();
                        sresp.AddAttribute(fattr);
                        fattr.ComputeCRC(sresp, nLengthWithoutFingerPrint);
                    }
                    else
                    {
                        MappedAddressAttribute attr = new MappedAddressAttribute();
                        attr.Port = (ushort)epfrom.Port;
                        attr.IPAddress = epfrom.Address;
                        attr.AddressFamily = StunAddressFamily.IPv4;
                        sresp.AddAttribute(attr);

                        UserNameAttribute unameattr = new UserNameAttribute();
                        if (PairReferenced != null)
                        {
                            unameattr.UserName = string.Format("{0}{1}", this.UserName, PairReferenced.RemoteCandidate.username);
                        }
                        sresp.AddAttribute(unameattr);

                    }



                    AudioRTPStream.SendSTUNMessage(sresp, epfrom);

                    string strrfrag = "";
                    string strlfrag = "";
                    bool UseThisCandidate = false;

                    if (IncomingUserNameAttribute != null)
                    {
                        int nColonAt = IncomingUserNameAttribute.UserName.IndexOf(":");
                        if (nColonAt > 0)
                        {
                            /// should be rusername:lusername
                            /// 
                            strrfrag = IncomingUserNameAttribute.UserName.Substring(0, nColonAt);
                            strlfrag = IncomingUserNameAttribute.UserName.Substring(nColonAt + 1);
                        }
                    }
                    if (IncomingUseCandidateAttribute != null)
                    {
                        UseThisCandidate = true;
                    }
                 
                    if ( (UseThisCandidate == true) && (PairReferenced != null) )
                    {
                        SelectedPair = PairReferenced;
                        IceDone = true;
                        this.RemoteEndpoint = PairReferenced.RemoteCandidate.IPEndPoint;
                        ICEDoneStartRTP();
                    }

                    if (UseGoogleTalkProtocol == true)
                    {
                        // Google talk doesn't have a usecandidate option, so I guess we use the first candidate we send and receive successfully on
                        if ((PairReferenced != null) && (PairReferenced.CandidatePairState == CandidatePairState.Succeeded) )
                        {
                            SelectedPair = PairReferenced;
                            IceDone = true;
                            this.RemoteEndpoint = PairReferenced.RemoteCandidate.IPEndPoint;
                            ICEDoneStartRTP();
                        }
                    }

                }
            }
        }

        void AudioRTCPStream_OnUnhandleSTUNMessage(STUNMessage smsg, IPEndPoint epfrom)
        {
                
            /// Our RTPStream received a STUN message.
            if (smsg.Class == StunClass.Request)
            {
                if (smsg.Method == StunMethod.Binding)
                {

                    STUN2Message sresp = new STUN2Message();
                    sresp.TransactionId = smsg.TransactionId;
                    sresp.Method = StunMethod.Binding;
                    sresp.Class = StunClass.Success;

                    XORMappedAddressAttribute attr = new XORMappedAddressAttribute();
                    attr.Port = (ushort)epfrom.Port;
                    attr.IPAddress = epfrom.Address;
                    attr.AddressFamily = StunAddressFamily.IPv4;
                    sresp.AddAttribute(attr);

                    IceControlledAttribute iattr = new IceControlledAttribute();
                    sresp.AddAttribute(iattr);


                    UserNameAttribute unameattr = new UserNameAttribute();
                    unameattr.UserName = string.Format("{0}:{1}", this.UserName, this.RemoteUserName);
                    sresp.AddAttribute(unameattr);

                    /// Add message integrity, computes over all the items currently added
                    /// 
                    int nLengthWithoutMessageIntegrity = sresp.Bytes.Length;
                    MessageIntegrityAttribute mac = new MessageIntegrityAttribute();
                    sresp.AddAttribute(mac);
                    mac.ComputeHMACShortTermCredentials(sresp, nLengthWithoutMessageIntegrity, this.RemotePassword);

                    /// Add fingerprint
                    /// 
                    int nLengthWithoutFingerPrint = sresp.Bytes.Length;
                    FingerPrintAttribute fattr = new FingerPrintAttribute();
                    sresp.AddAttribute(fattr);
                    fattr.ComputeCRC(sresp, nLengthWithoutFingerPrint);


                    AudioRTCPStream.SendSTUNMessage(sresp, epfrom);
                }
            }
        }

        public const ushort StunPort = 3478;
        public IPEndPoint PerformSTUNRequest(string strStunServer, int nTimeout)
        {
            EndPoint epStun = SocketServer.ConnectMgr.GetIPEndpoint(strStunServer, StunPort);
            return PerformSTUNRequest(epStun, nTimeout);
        }

         public IPEndPoint PerformSTUNRequest(EndPoint epStun, int nTimeout)
        {
            return PerformSTUNRequest(epStun, nTimeout, false, false, 0, null, null);
        }


         public IPEndPoint PerformSTUNRequest1(EndPoint epStun, int nTimeout)
         {
             return PerformSTUNRequest1(epStun, nTimeout, false, false, 0, null, null);
         }



        
        /// <summary>
        ///  Send out a stun request to discover our IP address transalation
        /// </summary>
        /// <param name="strStunServer"></param>
        /// <returns></returns>
        public IPEndPoint PerformSTUNRequest(EndPoint epStun, int nTimeout, bool bICE, bool bIsControlling, int nPriority, string strUsername, string strPassword)
        {

            //needed
            STUN2Message msgRequest = new STUN2Message();
            msgRequest.Method = StunMethod.Binding;
            msgRequest.Class = StunClass.Request;


            MappedAddressAttribute mattr = new MappedAddressAttribute();
            mattr.IPAddress = LocalEndpoint.Address;
            mattr.Port = (ushort)LocalEndpoint.Port;

            msgRequest.AddAttribute(mattr);

            ///needed
            if (bICE == true)
            {
                PriorityAttribute pattr = new PriorityAttribute();
                pattr.Priority = nPriority;
                msgRequest.AddAttribute(pattr);

                UseCandidateAttribute uattr = new UseCandidateAttribute();
                msgRequest.AddAttribute(uattr);

                if (strUsername != null)
                {
                    UserNameAttribute unameattr = new UserNameAttribute();
                    unameattr.UserName = strUsername;
                    msgRequest.AddAttribute(unameattr);
                }
                if (strPassword != null)
                {
                    PasswordAttribute passattr = new PasswordAttribute();
                    passattr.Password = strPassword;
                    msgRequest.AddAttribute(passattr);
                }

                if (bIsControlling == true)
                {
                    IceControllingAttribute cattr = new IceControllingAttribute();
                    msgRequest.AddAttribute(cattr);
                }
                else
                {
                    IceControlledAttribute cattr = new IceControlledAttribute();
                    msgRequest.AddAttribute(cattr);
                }
            }

            STUNMessage ResponseMessage = this.AudioRTPStream.SendRecvSTUN(epStun, msgRequest, nTimeout);

            IPEndPoint retep = null;
            if (ResponseMessage != null)
            {
                foreach (STUNAttributeContainer cont in ResponseMessage.Attributes)
                {
                    if (cont.ParsedAttribute.Type == StunAttributeType.MappedAddress)
                    {

                        MappedAddressAttribute attrib = cont.ParsedAttribute as MappedAddressAttribute;
                        retep = new IPEndPoint(attrib.IPAddress, attrib.Port);
                    }
                }
            }
            return retep;

        }


        public IPEndPoint PerformSTUNRequest1(EndPoint epStun, int nTimeout, bool bICE, bool bIsControlling, int nPriority, string strUsername, string strPassword)
        {
            STUN2Message msgRequest = new STUN2Message();
            msgRequest.Method = StunMethod.Binding;
            msgRequest.Class = StunClass.Request;


            MappedAddressAttribute mattr = new MappedAddressAttribute();
            mattr.IPAddress = LocalEndpoint.Address;
            mattr.Port = (ushort)LocalEndpoint.Port;

            msgRequest.AddAttribute(mattr);

            if (bICE == true)
            {
                PriorityAttribute pattr = new PriorityAttribute();
                pattr.Priority = nPriority;
                msgRequest.AddAttribute(pattr);

                UseCandidateAttribute uattr = new UseCandidateAttribute();
                msgRequest.AddAttribute(uattr);

                if (strUsername != null)
                {
                    UserNameAttribute unameattr = new UserNameAttribute();
                    unameattr.UserName = strUsername;
                    msgRequest.AddAttribute(unameattr);
                }
                if (strPassword != null)
                {
                    PasswordAttribute passattr = new PasswordAttribute();
                    passattr.Password = strPassword;
                    msgRequest.AddAttribute(passattr);
                }

                if (bIsControlling == true)
                {
                    IceControllingAttribute cattr = new IceControllingAttribute();
                    msgRequest.AddAttribute(cattr);
                }
                else
                {
                    IceControlledAttribute cattr = new IceControlledAttribute();
                    msgRequest.AddAttribute(cattr);
                }
            }

            STUNMessage ResponseMessage = this.AudioRTPStream.SendRecvSTUN1(epStun, msgRequest, nTimeout);

            IPEndPoint retep = null;
            if (ResponseMessage != null)
            {
                foreach (STUNAttributeContainer cont in ResponseMessage.Attributes)
                {
                    if (cont.ParsedAttribute.Type == StunAttributeType.MappedAddress)
                    {

                        MappedAddressAttribute attrib = cont.ParsedAttribute as MappedAddressAttribute;
                        retep = new IPEndPoint(attrib.IPAddress, attrib.Port);
                    }
                }
            }
            return retep;

        }


        public static uint CalculatePriority(int typepref, int localpref, int nComponentid)
        {
              // priority = (2^24)*(type preference) +
              //(2^8)*(local preference) +
              //(2^0)*(256 - component ID)

            uint nPriority = (uint) (((typepref & 0x7E) << 24) | ((localpref & 0xFFFF) << 8) | (256 - nComponentid & 0xFF));
            return nPriority;
        }

        public static void CalculatePriority(int typepref, int localpref, Candidate cand)
        {
            // priority = (2^24)*(type preference) +
            //(2^8)*(local preference) +
            //(2^0)*(256 - component ID)
            cand.priority = (int)(((typepref & 0x7E) << 24) | ((localpref & 0xFFFF) << 8) | (256 - cand.component & 0xFF));
        }

       

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        void FirePropertyChanged(string strName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(strName));
        }

        #endregion

        #region IComparer<Candidate> Members

        public int Compare(Candidate x, Candidate y)
        {
            return x.priority.CompareTo(y.priority);
        }

        #endregion
    }

    public class JingleMediaSession : MediaSession
    {
        public JingleMediaSession(string strSession, string strRemoteJID, IPEndPoint LocalEp, XMPPClient client) : 
            base(strSession, strRemoteJID, LocalEp, client) 
        {
        }

        public JingleMediaSession(string strRemoteJID, IPEndPoint LocalEp, XMPPClient client) : 
            base(strRemoteJID, LocalEp, client)
        {
        }

        public JingleMediaSession(string strSession, IQ intitialIQ, KnownAudioPayload LocalPayloads, IPEndPoint LocalEp, XMPPClient client) :
            base(strSession, intitialIQ, LocalPayloads, LocalEp, client)
        {
        }

        #region MyEdit
       
        
        public JingleMediaSession(IPEndPoint local) : base(local) 
        { 
        }
        
        
        #endregion

        protected override void ParsePayloads(IQ iq)
        {
            JingleIQ jingleiq = iq as JingleIQ;
            if (jingleiq == null)
                return;
            Jingle jingle = jingleiq.Jingle;
            if (jingle == null)
                return;

            if (this.RemotePayloads.Count > 0)
                return;

            if (jingle != null)
            {
                if ((jingle.Content != null) && (jingle.Content.Description != null) && (jingle.Content.Description.Payloads != null) && (jingle.Content.Description.Payloads.Count > 0))
                {
                    foreach (Payload pay in jingle.Content.Description.Payloads)
                    {
                        this.RemotePayloads.Add(pay);
                    }
                }
            }

            if (this.RemotePayloads.Count >= 0)
            {
                SetCodecsFromPayloads();
            }

        }


        protected override void ParseCandidates(IQ iq)
        {
            JingleIQ jingleiq = iq as JingleIQ;
            if (jingleiq == null)
                return;
            Jingle jingle = jingleiq.Jingle;
            if (jingle == null)
                return;

            if (jingle != null)
            {
                if ((jingle.Content != null) && (jingle.Content.ICETransport != null))
                {
                    this.RemoteUserName = jingle.Content.ICETransport.ufrag;
                    this.RemotePassword = jingle.Content.ICETransport.pwd;
                    if (jingle.Content.ICETransport.Candidates.Count > 0)
                    {
                        UseGoogleTalkProtocol = false;
                        foreach (Candidate nextcand in jingle.Content.ICETransport.Candidates)
                        {
                            nextcand.IPEndPoint = new IPEndPoint(IPAddress.Parse(nextcand.ipaddress), nextcand.port);
                            RemoteCandidates.Add(nextcand);
                        }
                    }

                }
                else if ((jingle.Content != null) && (jingle.Content.GoogleTransport != null))
                {
                    if (jingle.Content.GoogleTransport.Candidates.Count > 0)
                    {
                        UseGoogleTalkProtocol = true;
                        foreach (Candidate nextcand in jingle.Content.GoogleTransport.Candidates)
                        {
                            nextcand.IPEndPoint = new IPEndPoint(IPAddress.Parse(nextcand.ipaddress), nextcand.port);
                            RemoteCandidates.Add(nextcand);
                        }
                    }

                }
            }
        }


    
    }
}
