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

using System.Collections.Generic;
using System.Net.XMPP;
using System.Net.NetworkInformation;
using RTP;
using System.Net.XMPP.Jingle;
using AudioClasses;
using System.Threading;
using Microsoft.Xna.Framework.Audio;


namespace XMPPClient
{
    public class VoiceCall
    {
        public VoiceCall()
        {
        }

         public void RegisterXMPPClient()
        {
            addresses = FindAddresses();


            App.XMPPClient.JingleSessionManager.OnNewSession += new System.Net.XMPP.Jingle.JingleSessionManager.DelegateJingleSessionEventWithInfo(JingleSessionManager_OnNewSession);
            App.XMPPClient.JingleSessionManager.OnNewSessionAckReceived += new System.Net.XMPP.Jingle.JingleSessionManager.DelegateJingleSessionEventBool(JingleSessionManager_OnNewSessionAckReceived);
            App.XMPPClient.JingleSessionManager.OnSessionAcceptedAckReceived += new System.Net.XMPP.Jingle.JingleSessionManager.DelegateJingleSessionEventBool(JingleSessionManager_OnSessionAcceptedAckReceived);
            App.XMPPClient.JingleSessionManager.OnSessionAcceptedReceived += new System.Net.XMPP.Jingle.JingleSessionManager.DelegateJingleSessionEventWithInfo(JingleSessionManager_OnSessionAcceptedReceived);
            App.XMPPClient.JingleSessionManager.OnSessionTerminated += new System.Net.XMPP.Jingle.JingleSessionManager.DelegateJingleSessionEvent(JingleSessionManager_OnSessionTerminated);
            App.XMPPClient.JingleSessionManager.OnSessionTransportInfoReceived += new System.Net.XMPP.Jingle.JingleSessionManager.DelegateJingleSessionEventWithInfo(JingleSessionManager_OnSessionTransportInfoReceived);
            App.XMPPClient.JingleSessionManager.OnSessionTransportInfoAckReceived += new System.Net.XMPP.Jingle.JingleSessionManager.DelegateJingleSessionEventBool(JingleSessionManager_OnSessionTransportInfoAckReceived);
        }

        public void Dispose()
        {
            App.XMPPClient.JingleSessionManager.OnNewSession -= new System.Net.XMPP.Jingle.JingleSessionManager.DelegateJingleSessionEventWithInfo(JingleSessionManager_OnNewSession);
            App.XMPPClient.JingleSessionManager.OnNewSessionAckReceived -= new System.Net.XMPP.Jingle.JingleSessionManager.DelegateJingleSessionEventBool(JingleSessionManager_OnNewSessionAckReceived);
            App.XMPPClient.JingleSessionManager.OnSessionAcceptedAckReceived -= new System.Net.XMPP.Jingle.JingleSessionManager.DelegateJingleSessionEventBool(JingleSessionManager_OnSessionAcceptedAckReceived);
            App.XMPPClient.JingleSessionManager.OnSessionAcceptedReceived -= new System.Net.XMPP.Jingle.JingleSessionManager.DelegateJingleSessionEventWithInfo(JingleSessionManager_OnSessionAcceptedReceived);
            App.XMPPClient.JingleSessionManager.OnSessionTerminated -= new System.Net.XMPP.Jingle.JingleSessionManager.DelegateJingleSessionEvent(JingleSessionManager_OnSessionTerminated);
            App.XMPPClient.JingleSessionManager.OnSessionTransportInfoReceived -= new System.Net.XMPP.Jingle.JingleSessionManager.DelegateJingleSessionEventWithInfo(JingleSessionManager_OnSessionTransportInfoReceived);
            App.XMPPClient.JingleSessionManager.OnSessionTransportInfoAckReceived -= new System.Net.XMPP.Jingle.JingleSessionManager.DelegateJingleSessionEventBool(JingleSessionManager_OnSessionTransportInfoAckReceived);
       }

        AudioStreamSource source = null;
        IPAddress[] addresses = null;
        RTP.JingleMediaSession MediaSession = null;
        private bool m_bCallActive = false;

        public bool IsCallActive
        {
            get { return m_bCallActive; }
            set { m_bCallActive = value; }
        }

        Thread MicrophoneThread = null;
        Thread SpeakerThread = null;
        AudioClasses.ByteBuffer MicrophoneQueue = new ByteBuffer();


        public void StartCall(string strRemoteJID)
        {
            /// Start up our mic and speaker, start up our mixer
            /// 
            if (m_bCallActive == true)
                return;


            if (addresses.Length <= 0)
                throw new Exception("No IP addresses on System");

            int nPort = GetNextPort();
            IPEndPoint ep = new IPEndPoint(addresses[0], nPort);

            source = new AudioStreamSource();

            /// may need a lock here to make sure we have this session added to our list before the xmpp response gets back, though this should be many times faster than network traffic
            MediaSession = new JingleMediaSession(strRemoteJID, ep, App.XMPPClient);
            MediaSession.UseStun = true;
            MediaSession.AudioRTPStream.UseInternalTimersForPacketPushPull = false;
            MediaSession.ClearAllPayloads();

            MediaSession.AddKnownAudioPayload(KnownAudioPayload.G722_40); // Only g711, speex can't be encoded real time on the xoom (oops, this is the windows phone, we'll have to try that later)

            MediaSession.SendInitiateSession();
        }

        void JingleSessionManager_OnNewSession(string strSession, System.Net.XMPP.Jingle.JingleIQ iq, System.Net.XMPP.XMPPClient client)
        {
            App.XMPPClient.JingleSessionManager.TerminateSession(strSession, TerminateReason.Decline);
            //bool bAcceptNewCall = (bool)this.Dispatcher.Invoke(new DelegateAcceptSession(ShouldAcceptSession), strSession, jingle);

            //if (bAcceptNewCall == true)
            //{
            //    int nPort = GetNextPort();
            //    IPEndPoint ep = new IPEndPoint(addresses[0], nPort);

            //    JingleMediaSession session = new JingleMediaSession(strSession, jingle, ep, client);
            //    session.UseStun = UseStun;
            //    session.SendAcceptSession();
            //    SessionList.Add(strSession, session);
            //    ObservSessionList.Add(session);
            //}
            //else
            //{
            //    XMPPClient.JingleSessionManager.TerminateSession(strSession, TerminateReason.Decline);
            //}

        }

        void JingleSessionManager_OnNewSessionAckReceived(string strSession, System.Net.XMPP.Jingle.IQResponseAction response, System.Net.XMPP.XMPPClient client)
        {
            if ((MediaSession != null) && (MediaSession.Session == strSession))
            {
                MediaSession.GotNewSessionAck();
            }
        }

        void JingleSessionManager_OnSessionTransportInfoReceived(string strSession, System.Net.XMPP.Jingle.JingleIQ jingle, System.Net.XMPP.XMPPClient client)
        {
            if ((MediaSession != null) && (MediaSession.Session == strSession))
            {
                MediaSession.GotTransportInfo(jingle);

            }
        }

        void JingleSessionManager_OnSessionTransportInfoAckReceived(string strSession, IQResponseAction response, System.Net.XMPP.XMPPClient client)
        {
            if ((MediaSession != null) && (MediaSession.Session == strSession))
            {
                MediaSession.GotSendTransportInfoAck();
            }
        }



        void JingleSessionManager_OnSessionTerminated(string strSession, System.Net.XMPP.XMPPClient client)
        {
            if ((MediaSession != null) && (MediaSession.Session == strSession))
            {
                MediaSession.StopMedia(null);
                StopCall();
            }
        }

        void JingleSessionManager_OnSessionAcceptedReceived(string strSession, System.Net.XMPP.Jingle.JingleIQ jingle, System.Net.XMPP.XMPPClient client)
        {
            Console.WriteLine("Session {0} has accepted our invitation", strSession);
            if ((MediaSession != null) && (MediaSession.Session == strSession))
            {
                MediaSession.SessionAccepted(jingle, null);
                StartMedia();
            }
        }

        void JingleSessionManager_OnSessionAcceptedAckReceived(string strSession, System.Net.XMPP.Jingle.IQResponseAction response, System.Net.XMPP.XMPPClient client)
        {
            if (response.AcceptIQ == true)
            {
                Console.WriteLine("Session {0} has said OK to our Accept invitation", strSession);
                if ((MediaSession != null) && (MediaSession.Session == strSession))
                {
                    MediaSession.GotAcceptSessionAck(null);
                }
            }

        }



        delegate bool DelegateAcceptSession(string strSession, System.Net.XMPP.Jingle.Jingle jingle);

        bool ShouldAcceptSession(string strSession, System.Net.XMPP.Jingle.Jingle jingle)
        {
            ///TODO.. for now only allow outgoing calls
            //StartMicrophoneAndSpeaker();

            //if (AutoAnswer == true)
            //    return true;

            //if (MessageBox.Show(string.Format("Accept new Call from {0}", jingle.Initiator), "New Call", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            //    return true;
            return false;
        }


        private bool m_bAutoAnswer = false;

        public bool AutoAnswer
        {
            get { return m_bAutoAnswer; }
            set
            {
                if (m_bAutoAnswer != value)
                {
                    m_bAutoAnswer = value;
                }
            }
        }

        void StartMedia()
        {
            m_bCallActive = true;

            // Don't really need to use the mixer in android since we won't be conferencing people... go directly to the RTP buffer
            //PushPullObject thismember = AudioMixer.AddInputOutputSource(this, this);
            

            /// Start our speaker play thread
            SpeakerThread = new Thread(new ThreadStart(SpeakerThreadFunction));
            SpeakerThread.IsBackground = true;
            SpeakerThread.Name = "Speaker Write Thread";
            SpeakerThread.Start();


            /// Start our microphone read thread
            MicrophoneThread = new Thread(new ThreadStart(MicrophoneThreadFunction));
            MicrophoneThread.IsBackground = true;
            MicrophoneThread.Name = "Microphone Read Thread";
            MicrophoneThread.Start();

        }

        public void StopCall()
        {
            StopCall(true);
        }

        public MediaElement AudioStream = null;

        public event EventHandler OnCallStopped = null;

        void StopCall(bool bSendTerminate)
        {
            m_bCallActive = false;
            if (MediaSession != null)
            {
                App.XMPPClient.JingleSessionManager.TerminateSession(MediaSession.Session, TerminateReason.Gone);
                MediaSession = null;
            }

            if (OnCallStopped != null)
                OnCallStopped(this, new EventArgs());

        }


        void SafeStartMediaElement(object obj, EventArgs args)
        {
            if (AudioStream.CurrentState != MediaElementState.Playing)
            {
                AudioStream.BufferingTime = new TimeSpan(0, 0, 0);

                AudioStream.SetSource(source);
                AudioStream.Play();
            }
        }
        void SafeStopMediaElement(object obj, EventArgs args)
        {
            AudioStream.Stop();
        }
        public void SpeakerThreadFunction()
        {
            /// Send the data to our AudioStreamSource
            /// 
            TimeSpan tsPTime = TimeSpan.FromMilliseconds(MediaSession.AudioRTPStream.PTimeReceive);
            int nSamplesPerPacket = MediaSession.AudioRTPStream.AudioCodec.AudioFormat.CalculateNumberOfSamplesForDuration(tsPTime);
            int nBytesPerPacket = nSamplesPerPacket * MediaSession.AudioRTPStream.AudioCodec.AudioFormat.BytesPerSample;

            JingleMediaSession session = MediaSession;
            if (session == null)
                return;

            byte[] bDummySample = new byte[nBytesPerPacket];

            source.PacketSize = nBytesPerPacket;

            session.AudioRTPStream.IncomingRTPPacketBuffer.InitialPacketQueueMinimumSize = 4;
            session.AudioRTPStream.IncomingRTPPacketBuffer.PacketSizeShiftMax = 10;

            Deployment.Current.Dispatcher.BeginInvoke(new EventHandler(SafeStartMediaElement), null, null);

            int nMsTook = 0;
            /// Get first packet... have to wait for our rtp buffer to fill
            byte[] bData = session.AudioRTPStream.WaitNextPacketSample(true, MediaSession.AudioRTPStream.PTimeReceive*5, out nMsTook);
            if ((bData != null) && (bData.Length > 0))
                source.Write(bData);

            DateTime dtNextPacketExpected = DateTime.Now + tsPTime;

            System.Diagnostics.Stopwatch WaitPacketWatch = new System.Diagnostics.Stopwatch();
            int nDeficit = 0;
            while (m_bCallActive == true)
            {
                bData = session.AudioRTPStream.WaitNextPacketSample(true, MediaSession.AudioRTPStream.PTimeReceive, out nMsTook);
                if ((bData != null) && (bData.Length > 0))
                    source.Write(bData);

                //int nRemaining = MediaSession.AudioRTPStream.PTimeReceive - nMsTook;
                //if (nRemaining > 0)
                //    System.Threading.Thread.Sleep(nRemaining);

                //dtNextPacketExpected = dtLastPacket + tsPTime;
                //byte[] bData = session.AudioRTPStream.GetNextPacketSample(false);
                //if ((bData != null) && (bData.Length > 0))
                //    source.Write(bData);
                //else
                //    source.Write(bDummySample);

                TimeSpan tsRemaining = dtNextPacketExpected - DateTime.Now;
                int nMsRemaining = (int)tsRemaining.TotalMilliseconds;
                if (nMsRemaining > 0)
                {
                    nMsRemaining += nDeficit;
                    if (nMsRemaining > 0)
                        System.Threading.Thread.Sleep(nMsRemaining);
                    else
                    {
                        nDeficit = nMsRemaining;
                    }
                }
                else
                    nDeficit += nMsRemaining;

                dtNextPacketExpected += tsPTime;
            }

            Deployment.Current.Dispatcher.BeginInvoke(new EventHandler(SafeStopMediaElement), null, null);

        }

        bool UseEchoCanceller = false;

        public void MicrophoneThreadFunction()
        {
            StartMic();

            JingleMediaSession session = MediaSession;
            if (session == null)
                return;

            int nSamplesPerPacket = MediaSession.AudioRTPStream.AudioCodec.AudioFormat.CalculateNumberOfSamplesForDuration(TimeSpan.FromMilliseconds(MediaSession.AudioRTPStream.PTimeTransmit));
            int nBytesPerPacket = nSamplesPerPacket * MediaSession.AudioRTPStream.AudioCodec.AudioFormat.BytesPerSample;


            // Min size in ICS was 1280 or 40 ms


            TimeSpan tsPTime = TimeSpan.FromMilliseconds(MediaSession.AudioRTPStream.PTimeTransmit);
            DateTime dtNextPacketExpected = DateTime.Now + tsPTime;

            int nUnavailableAudioPackets = 0;
            while (m_bCallActive == true)
            {
                dtNextPacketExpected = DateTime.Now + tsPTime;
                if (MicrophoneQueue.Size >= nBytesPerPacket)
                {
                    byte[] buffer = MicrophoneQueue.GetNSamples(nBytesPerPacket);
                    session.AudioRTPStream.SendNextSample(buffer);
                }
                else
                {
                    nUnavailableAudioPackets++;
                }

                if (MicrophoneQueue.Size > nBytesPerPacket * 6)
                    MicrophoneQueue.GetNSamples(MicrophoneQueue.Size - nBytesPerPacket * 5);

                TimeSpan tsRemaining = dtNextPacketExpected - DateTime.Now;
                int nMsRemaining = (int)tsRemaining.TotalMilliseconds;
                if (nMsRemaining > 0)
                    System.Threading.Thread.Sleep(nMsRemaining);
            }

            StopMic();
        }

        byte[] buffer = new byte[16 * 40];
        void StartMic()
        {
            /// wnidows phone can only get mic at 100 ms intervals, not to good for speech
            Microphone mic = Microphone.Default;
            buffer = new byte[mic.GetSampleSizeInBytes(TimeSpan.FromMilliseconds(100)) * 4];
            mic.BufferDuration = TimeSpan.FromMilliseconds(100);
            mic.BufferReady += new EventHandler<EventArgs>(mic_BufferReady);
            mic.Start();
        }

        void StopMic()
        {
            Microphone mic = Microphone.Default;
            mic.BufferReady -= new EventHandler<EventArgs>(mic_BufferReady);
            mic.Stop();
        }

        void mic_BufferReady(object sender, EventArgs e)
        {
            Microphone mic = Microphone.Default;

            int nSize = mic.GetData(buffer);
            MicrophoneQueue.AppendData(buffer, 0, nSize);
        }

        
        static int FirstPort = 30000;
        static int LastPort = 30100;

        static int PortOn = 30000;

        static VoiceCall()
        {
            Random rand = new Random();
            PortOn = rand.Next(80) + FirstPort;
            if (PortOn % 2 != 0)
                PortOn++;
        }


        public static int GetNextPort()
        {
            int nRet = PortOn;
            PortOn += 2;
            if (PortOn > LastPort)
                PortOn = FirstPort;
            return nRet;
        }


        public IPAddress[] FindAddresses()
        {
            List<IPAddress> IPs = new List<IPAddress>();
       
            FindMyIP.MyIPAddress finder = new FindMyIP.MyIPAddress();
            IPAddress localaddress = finder.Find();
            if (localaddress != null)
                IPs.Add(localaddress);
            else
                IPs.Add(IPAddress.Parse("0.0.0.0"));

            /// We get a stun address later on, so just live with the local address here

            return IPs.ToArray();
        }

    }
}
