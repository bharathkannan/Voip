/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using System.Net.XMPP;
using System.Net.XMPP.Jingle;
using RTP;
using AudioClasses;

using System.Net;
using System.Net.NetworkInformation;
using System.ComponentModel;
using ImageAquisition;

namespace WPFXMPPClient
{
    /// <summary>
    /// Interaction logic for AudioMuxerWindow.xaml
    /// </summary>
    public partial class AudioMuxerWindow : Window, IAudioSink, IAudioSource, INotifyPropertyChanged
    {
        public AudioMuxerWindow()
        {
            InitializeComponent();
        }


        /// <summary>
        /// Our audio muxer.  Takes our microphone device and each incoming RTP stream as input, outputs
        /// to our speaker device and each outgoing RTP stream.  May also have a recording interface as ouput and tone/song generators as inputs
        /// Currently only supports HD voice because that's all the dmo's can echo cancel, but may add support for AAC if that becomes available
        /// </summary>
        AudioConferenceMixer AudioMixer = new AudioConferenceMixer(AudioFormat.SixteenBySixteenThousandMono);

        public AudioFileReader AudioFileReader
        {
            get { return this.AudioPlayer.AudioFileReader; }
        }


        ObservableCollectionEx<MediaSession> ObservSessionList = new ObservableCollectionEx<MediaSession>();
        Dictionary<string, MediaSession> SessionList = new Dictionary<string, MediaSession>();

        /// <summary>
        /// Our XMPP client
        /// </summary>
        XMPPClient XMPPClient = null;

        public void RegisterXMPPClient(XMPPClient client)
        {
            addresses = AudioMuxerWindow.FindAddresses();

            XMPPClient = client;
            XMPPClient.JingleSessionManager.OnNewSession += new System.Net.XMPP.Jingle.JingleSessionManager.DelegateJingleSessionEventWithInfo(JingleSessionManager_OnNewSession);
            XMPPClient.JingleSessionManager.OnNewSessionAckReceived += new JingleSessionManager.DelegateJingleSessionEventBool(JingleSessionManager_OnNewSessionAckReceived);
            XMPPClient.JingleSessionManager.OnSessionAcceptedAckReceived += new System.Net.XMPP.Jingle.JingleSessionManager.DelegateJingleSessionEventBool(JingleSessionManager_OnSessionAcceptedAckReceived);
            XMPPClient.JingleSessionManager.OnSessionAcceptedReceived += new System.Net.XMPP.Jingle.JingleSessionManager.DelegateJingleSessionEventWithInfo(JingleSessionManager_OnSessionAcceptedReceived);
            XMPPClient.JingleSessionManager.OnSessionTerminated += new System.Net.XMPP.Jingle.JingleSessionManager.DelegateJingleSessionEvent(JingleSessionManager_OnSessionTerminated);
            XMPPClient.JingleSessionManager.OnSessionTransportInfoReceived += new System.Net.XMPP.Jingle.JingleSessionManager.DelegateJingleSessionEventWithInfo(JingleSessionManager_OnSessionTransportInfoReceived);
            XMPPClient.JingleSessionManager.OnSessionTransportInfoAckReceived += new JingleSessionManager.DelegateJingleSessionEventBool(JingleSessionManager_OnSessionTransportInfoAckReceived);

            
            /// Get all our speaker and mic devices
            /// 
            MicrophoneDevices = ImageAquisition.NarrowBandMic.GetMicrophoneDevices();
            SpeakerDevices = ImageAquisition.NarrowBandMic.GetSpeakerDevices();

        }

       

        AudioClasses.AudioDevice[] MicrophoneDevices = null;
        AudioClasses.AudioDevice[] SpeakerDevices = null;

        ImageAquisition.NarrowBandMic Microphone = null;
        DirectShowFilters.SpeakerFilter Speaker = null;
        SocketServer.IMediaTimer ExpectPacketTimer = null;
        bool m_bAudioActive = false;


        // TODO.. need speaker play object/method
        ImageAquisition.AudioDeviceVolume MicrophoneVolume = null;
        ImageAquisition.AudioDeviceVolume SpeakerVolume = null;

        IPAddress[] addresses = null;
        System.Windows.Threading.DispatcherTimer UpdateTimer = null;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateTimer = new System.Windows.Threading.DispatcherTimer(TimeSpan.FromSeconds(1), System.Windows.Threading.DispatcherPriority.Normal, new EventHandler(UpdateGuiTimer), this.Dispatcher);
            UpdateTimer.Start();

            this.ListViewAudioSessions.ItemsSource = ObservSessionList;

            if ((MicrophoneDevices != null) && (MicrophoneDevices.Length > 0))
            {
                this.ComboBoxMicDevices.ItemsSource = MicrophoneDevices;
                this.ComboBoxMicDevices.SelectedItem = MicrophoneDevices[0];
                MicrophoneVolume = new ImageAquisition.AudioDeviceVolume(MicrophoneDevices[0]);
                this.SliderMicVolume.DataContext = MicrophoneVolume;
            }
            if ((SpeakerDevices != null) && (SpeakerDevices.Length > 0))
            {
                this.ComboBoxSpeakerDevices.ItemsSource = SpeakerDevices;
                this.ComboBoxSpeakerDevices.SelectedItem = SpeakerDevices[0];
                SpeakerVolume = new ImageAquisition.AudioDeviceVolume(SpeakerDevices[0]);
                this.SliderSpeakerVolume.DataContext = SpeakerVolume;
            }

            AnswerTypeInformation normal = new AnswerTypeInformation() { AnswerType = AnswerType.Normal, Description = "Normal Answer" };
            AnswerTypeInformation dnd = new AnswerTypeInformation() { AnswerType = AnswerType.DND, Description = "Do Not Disturb" };
            AnswerTypeInformation conference = new AnswerTypeInformation() { AnswerType = AnswerType.AcceptToConference, Description = "Auto Add to Conference" };


            AudioPlayer.XMPPClient = this.XMPPClient;

            this.DataContext = this;
        }

        void UpdateGuiTimer(object obj, EventArgs args)
        {
            MediaSession[] sessions = ObservSessionList.ToArray();
            foreach (MediaSession session in sessions)
            {
                session.CallDuration = TimeSpan.MaxValue;
                session.Statistics = "";
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((e.ChangedButton == MouseButton.Left) && (e.ButtonState == MouseButtonState.Pressed))
                this.DragMove();

        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = System.Windows.Visibility.Hidden;
        }

        public void CloseAllSessions()
        {
            /// Stop all calls
            /// 
            MediaSession[] sessions = this.ObservSessionList.ToArray();
            foreach (MediaSession session in sessions)
                CloseSession(session);
        }

        protected override void OnClosed(EventArgs e)
        {
            CloseAllSessions();
            base.OnClosed(e);
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Visibility = System.Windows.Visibility.Hidden;
        }

        ToneGenerator OurToneGenerator = new ToneGenerator(350, 440);
        //TonGenerator noise2 = new TonGenerator(1004, 1004);

        void StartMicrophoneAndSpeaker(AudioFormat format)
        {
            if (this.IsLoaded == false)
            {
                this.Show();
            }
                 

            if (Microphone != null)
                return;
            AudioDevice micdevice = this.ComboBoxMicDevices.SelectedItem as AudioDevice;
            AudioDevice speakdevice = this.ComboBoxSpeakerDevices.SelectedItem as AudioDevice;

            if ((micdevice == null) || (speakdevice == null))
                return;

            PushPullObject thismember = AudioMixer.AddInputOutputSource(this, this);
            PushPullObject FileMixer = AudioMixer.AddInputOutputSource(AudioFileReader, null);
            
            PushPullObject tonemember = AudioMixer.AddInputOutputSource(OurToneGenerator, OurToneGenerator);
            OurToneGenerator.IsSourceActive = false;
            //thismember.SourceExcludeList.Clear();  // clear so we can hear our mic
            thismember.SourceExcludeList.Add(tonemember.AudioSource); /// we don't want to hear the tone we're sending

            System.Windows.Interop.WindowInteropHelper helper = new System.Windows.Interop.WindowInteropHelper(Window.GetWindow(this));


            if (SpeakerVolume != null)
            {
                SpeakerVolume.Dispose();
                SpeakerVolume = null;
            }
            if (MicrophoneVolume != null)
            {
                MicrophoneVolume.Dispose();
                MicrophoneVolume = null;
            }

            MicrophoneVolume = new ImageAquisition.AudioDeviceVolume(micdevice);
            this.SliderMicVolume.DataContext = MicrophoneVolume;

            SpeakerVolume = new ImageAquisition.AudioDeviceVolume(speakdevice);
            this.SliderSpeakerVolume.DataContext = SpeakerVolume;

            Microphone = new ImageAquisition.NarrowBandMic(micdevice, speakdevice.Guid, helper.Handle);
            Microphone.AGC = UseAEC; // don't use gain control when AEC is disabled, so we can hear mics better
            Microphone.UseKinectArray = false;
            Speaker = new DirectShowFilters.SpeakerFilter(speakdevice.Guid, 20, format, helper.Handle);
            Speaker.Start();
 
            if (UseAEC == true)
            {
                Microphone.Start();
            }
            else
            {
                Microphone.StartNoEchoCancellation();
            } m_bAudioActive = true;
            AudioMixer.Start();
        }

        void StopMicrophoneAndSpeaker()
        {
            AudioMixer.Stop();
            Microphone.Stop();
            Speaker.Stop();
            Microphone = null;
            Speaker.Dispose();
            Speaker = null;
        }

        /// <summary>
        ///  See if we have an active call to this jid, if not, start one
        /// </summary>
        /// <param name="item"></param>
        public void InitiateOrShowCallTo(JID jidto)
        {
            foreach (MediaSession currsess in ObservSessionList)
            {
                if (currsess.RemoteJID == jidto)
                    return;
            }

            if ((MicrophoneDevices == null) || (MicrophoneDevices.Length <= 0))
            {
                MessageBox.Show("You have no microphone device, can't start a call", "No Microphone Device Found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if ((SpeakerDevices == null) || (SpeakerDevices.Length <= 0))
            {
                MessageBox.Show("You have no speaker device, can't start a call", "No Speaker Device Found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
     

            StartMicrophoneAndSpeaker(AudioFormat.SixteenBySixteenThousandMono);

            if (addresses.Length <= 0)
                throw new Exception("No IP addresses on System");

            int nPort = GetNextPort();
            IPEndPoint ep = new IPEndPoint(addresses[0], nPort);

            MediaSession jinglesession = null;

            /// Determine if we are calling via jingle, or google voice
            /// (For now only make outgoing calls via jingle, accept incomgin google voice)


            /// may need a lock here to make sure we have this session added to our list before the xmpp response gets back, though this should be many times faster than network traffic
            jinglesession = new JingleMediaSession(jidto, ep, XMPPClient);
            jinglesession.AudioRTPStream.RecvResampler = new BetterAudioResampler();
            jinglesession.AudioRTPStream.SendResampler = new BetterAudioResampler();


            try
            {
                if (jinglesession.RosterItem != null)
                    jinglesession.RosterItem.PropertyChanged += new PropertyChangedEventHandler(RosterItem_PropertyChanged);
                string strSession = jinglesession.SendInitiateSession();
                ObservSessionList.Add(jinglesession);
                SessionList.Add(strSession, jinglesession);
            }
            catch (Exception ex)
            {
                /// Should never happen
            }
        }

        void RosterItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            /// See if this roster item goes off line, if it does, Terminate it's media session
            /// 
            RosterItem item = sender as RosterItem;
            if (item != null)
            {
                if (item.Presence.PresenceType != PresenceType.available)
                {
                    /// Find this session and remove it
                    /// 
                    JingleMediaSession foundsession = null;
                    foreach (JingleMediaSession session in ObservSessionList)
                    {
                        if (session.RosterItem == item)
                        {
                            foundsession = session;
                            break;
                        }
                    }

                    if (foundsession != null)
                    {
                        CloseSession(foundsession);
                    }
                }
            }
        }

        void JingleSessionManager_OnNewSession(string strSession, System.Net.XMPP.Jingle.JingleIQ iq, XMPPClient client)
        {
            foreach (MediaSession currsess in ObservSessionList)
            {
                if (currsess.RemoteJID == iq.From) /// Don't allow sessions from the same person twice
                {
                    XMPPClient.JingleSessionManager.TerminateSession(strSession, TerminateReason.Decline);
                    return;
                }
            }

            int nPort = GetNextPort();
            IPEndPoint ep = new IPEndPoint(addresses[0], nPort);
            JingleMediaSession session = new JingleMediaSession(strSession, iq, KnownAudioPayload.G722 | KnownAudioPayload.Speex16000 | KnownAudioPayload.Speex8000 | KnownAudioPayload.G711, ep, client);
            session.AudioRTPStream.RecvResampler = new BetterAudioResampler();
            session.AudioRTPStream.SendResampler = new BetterAudioResampler();

            if (session.RosterItem != null)
                session.RosterItem.PropertyChanged += new PropertyChangedEventHandler(RosterItem_PropertyChanged);

            try
            {
                SessionList.Add(strSession, session);
                ObservSessionList.Add(session);
                session.StartIncoming(iq);
                System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(DoUserAcceptCall), session);
            }
            catch (Exception ex)
            {
                SessionList.Remove(strSession);
                ObservSessionList.Remove(session);

                if (session.RosterItem != null)
                    session.RosterItem.PropertyChanged -= new PropertyChangedEventHandler(RosterItem_PropertyChanged);

                /// No compatible codecs probably
                XMPPClient.JingleSessionManager.TerminateSession(strSession, TerminateReason.MediaError);
                return;
            }


        }

        void DoUserAcceptCall(object obj)
        {
            JingleMediaSession session = obj as JingleMediaSession;
            bool bAcceptNewCall = (bool)this.Dispatcher.Invoke(new DelegateAcceptSession(ShouldAcceptSession), session.Session, session.InitialJingle);

            if (bAcceptNewCall == true)
            {
                session.UserAcceptSession();
            }
            else
            {
                SessionList.Remove(session.Session);
                ObservSessionList.Remove(session);

                if (session.RosterItem != null)
                    session.RosterItem.PropertyChanged -= new PropertyChangedEventHandler(RosterItem_PropertyChanged);

                XMPPClient.JingleSessionManager.TerminateSession(session.Session, TerminateReason.Decline);
            }

        }

        void JingleSessionManager_OnNewSessionAckReceived(string strSession, IQResponseAction response, XMPPClient client)
        {
            if (SessionList.ContainsKey(strSession) == true)
            {
                MediaSession session = SessionList[strSession];
                session.GotNewSessionAck();
            }
        }

        void JingleSessionManager_OnSessionTransportInfoReceived(string strSession, System.Net.XMPP.Jingle.JingleIQ jingleiq, XMPPClient client)
        {
            if (SessionList.ContainsKey(strSession) == true)
            {
                MediaSession session = SessionList[strSession];
                session.GotTransportInfo(jingleiq);
            }
        }

        void JingleSessionManager_OnSessionTransportInfoAckReceived(string strSession, IQResponseAction response, XMPPClient client)
        {
            if (SessionList.ContainsKey(strSession) == true)
            {
                MediaSession session = SessionList[strSession];
                session.GotSendTransportInfoAck();
            }
        }

        delegate void DelegateWindow(Window win);
        void SafeCloseWindow(Window win)
        {
            if ((win != null) && (win.IsLoaded == true) )
                win.Close();
        }

        void JingleSessionManager_OnSessionTerminated(string strSession, XMPPClient client)
        {
            if (IncomingCallWindows.ContainsKey(strSession) == true) /// Call is still waiting on user acceptance
            {
                IncomingCallWindow win = IncomingCallWindows[strSession];
                IncomingCallWindows.Remove(strSession);
                
                /// Close the window
                /// 
                this.Dispatcher.Invoke(new DelegateWindow(SafeCloseWindow), win);
            }

            if (SessionList.ContainsKey(strSession) == true)
            {
                MediaSession session = SessionList[strSession];
                session.StopMedia(AudioMixer);
                SessionList.Remove(strSession);
                ObservSessionList.Remove(session);

                if (session.RosterItem != null)
                    session.RosterItem.PropertyChanged -= new PropertyChangedEventHandler(RosterItem_PropertyChanged);

                this.Dispatcher.Invoke(new DelegateAcceptSession(SessionEnded), strSession, null);

            }
        }
     
        void JingleSessionManager_OnSessionAcceptedReceived(string strSession, System.Net.XMPP.Jingle.JingleIQ jingle, XMPPClient client)
        {
            Console.WriteLine("Session {0} has accepted our invitation", strSession);
            if (SessionList.ContainsKey(strSession) == true)
            {
                MediaSession session = SessionList[strSession];
                session.SessionAccepted(jingle, AudioMixer);
            }
        }
     
        void JingleSessionManager_OnSessionAcceptedAckReceived(string strSession, System.Net.XMPP.Jingle.IQResponseAction response, XMPPClient client)
        {
            if (response.AcceptIQ == true)
            {
                Console.WriteLine("Session {0} has said OK to our Accept invitation", strSession);
                if (SessionList.ContainsKey(strSession) == true)
                {
                    MediaSession session = SessionList[strSession];
                    session.GotAcceptSessionAck(AudioMixer);
                    
                }
            }
            
        }

     
     

        delegate bool DelegateAcceptSession(string strSession, IQ iq);

        Dictionary<string, IncomingCallWindow> IncomingCallWindows = new Dictionary<string, IncomingCallWindow>();
        bool ShouldAcceptSession(string strSession, IQ iq)
        {
            StartMicrophoneAndSpeaker(AudioFormat.SixteenBySixteenThousandMono);

            this.Activate();
            System.Media.SoundPlayer player = new System.Media.SoundPlayer("Sounds/enter.wav");
            player.Play();


            if (AutoAnswer == true)
                return true;

            // If there is another incoming call window for this session, close it first
            if (IncomingCallWindows.ContainsKey(strSession) == true)
            {
                IncomingCallWindow win = IncomingCallWindows[strSession];
                IncomingCallWindows.Remove(strSession);
                SafeCloseWindow(win);
            }


            IncomingCallWindow IncomingCall = new IncomingCallWindow();
            IncomingCall.IncomingCallFrom = XMPPClient.FindRosterItem(iq.From);
            IncomingCallWindows.Add(strSession, IncomingCall);
            IncomingCall.ShowDialog();
            if (IncomingCallWindows.ContainsKey(strSession) == true)
                IncomingCallWindows.Remove(strSession);
            if (IncomingCall.Accepted == true)
                return true;

            //if (MessageBox.Show(string.Format("Accept new Call from {0}", iq.From), "New Call", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            //    return true;
            return false;
        }

        bool SessionEnded(string strSession, IQ iq)
        {
            System.Media.SoundPlayer player = new System.Media.SoundPlayer("Sounds/leave.wav");
            player.Play();
            return true;
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
                    FirePropertyChanged("AutoAnswer");
                }
            }
        }


        #region IAudioSink Members

        /// <summary>
        ///  Push to our speaker
        /// </summary>
        /// <param name="sample"></param>
        public void PushSample(MediaSample sample, object objSource)
        {
            lock (SpeakerLock)
            {
                // send this sample to our speakers, please
                if (Speaker != null)
                {
                    Speaker.PushSample(sample, this);
                }
            }
        }

        private bool m_bSpeakerMute = false;

        public bool SpeakerMute
        {
            get { return m_bSpeakerMute; }
            set
            {
                if (m_bSpeakerMute != value)
                {
                    m_bSpeakerMute = value;
                    FirePropertyChanged("SpeakerMute");
                }
            }
        }

        public bool IsSinkActive
        {
            get
            {
                return !m_bSpeakerMute;
            }
            set
            {
            }
        }

        double m_fSinkAmplitudeMultiplier = 1.0f;
        public double SinkAmplitudeMultiplier
        {
            get { return m_fSinkAmplitudeMultiplier; }
            set { m_fSinkAmplitudeMultiplier = value; }
        }

        #endregion

        #region IAudioSource Members

        //Pull a sample from our mic to send to the conference
        public MediaSample PullSample(AudioFormat format, TimeSpan tsDuration)
        {
            lock (MicLock)
            {
                if (Microphone != null)
                {
                    return Microphone.PullSample(format, tsDuration);
                }
                else
                    return null;
            }
        }


        public bool m_bMuted = false;
        public bool IsSourceActive
        {
            get
            {
                return !m_bMuted;
            }
            set
            {
                if (m_bMuted != value)
                {
                    m_bMuted = !value;
                    FirePropertyChanged("IsSourceActive");
                }
            }
        }
        public bool Muted
        {
            get
            {
                return m_bMuted;
            }
            set
            {
                if (m_bMuted != value)
                {
                    m_bMuted = value;
                    FirePropertyChanged("Muted");
                }
            }
        }

        double m_fSourceAmplitudeMultiplier = 1.0f;
        public double SourceAmplitudeMultiplier
        {
            get { return m_fSourceAmplitudeMultiplier; }
            set { m_fSourceAmplitudeMultiplier = value; }
        }
        

        #endregion

        static int FirstPort = 30000;
        static int LastPort = 30100;

        static int PortOn = 30000;

        static AudioMuxerWindow()
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


        public static IPAddress[] FindAddresses()
        {
            List<IPAddress> IPs = new List<IPAddress>();
            /// See what interfaces can connect to our itpcluster
            /// 

            IPAddress BindAddress = IPAddress.Any;
            NetworkInterface[] infs = NetworkInterface.GetAllNetworkInterfaces();


            foreach (NetworkInterface inf in infs)
            {
                try
                {
                    IPInterfaceProperties props = inf.GetIPProperties();
                    if (props == null)
                        continue;

                    IPv4InterfaceProperties ip4 = props.GetIPv4Properties();
                    if (ip4 == null)  /// TODO.. allow for IPV6 interfaces
                        continue;
                    if (ip4.IsAutomaticPrivateAddressingActive == true)
                        continue;
                    foreach (UnicastIPAddressInformation addrinfo in props.UnicastAddresses)
                    {

                        if (addrinfo.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                            continue;

                        //addrinfo.SuffixOrigin == SuffixOrigin.OriginDhcp
                        //addrinfo.PrefixOrigin == PrefixOrigin.Dhcp

                        if (addrinfo.PrefixOrigin == PrefixOrigin.WellKnown)
                            continue; /// ignore well known IP addresses


                        if (addrinfo.Address.Equals(IPAddress.Any) == false)
                        {

                            if (addrinfo.Address.Equals(IPAddress.Parse("127.0.0.1")) == false)
                                IPs.Add(new IPAddress(addrinfo.Address.GetAddressBytes()));
                        }

                    }
                }
                catch (Exception)
                {
                }
            }

            return IPs.ToArray();
        }

        private void ButtonClose_Click_1(object sender, RoutedEventArgs e)
        {
            MediaSession session = ((FrameworkElement)sender).DataContext as MediaSession;
            CloseSession(session);
        }
        
        void CloseSession(MediaSession session)
        {
            if (session != null)
            {
                session.StopMedia(AudioMixer);
                XMPPClient.JingleSessionManager.TerminateSession(session.Session, TerminateReason.Gone);
                SessionList.Remove(session.Session);
                ObservSessionList.Remove(session);

                if (session.RosterItem != null)
                    session.RosterItem.PropertyChanged -= new PropertyChangedEventHandler(RosterItem_PropertyChanged);

            }
        }

        private void ButtonStartMixer_Click(object sender, RoutedEventArgs e)
        {
            StartMicrophoneAndSpeaker(AudioFormat.SixteenBySixteenThousandMono);
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        void FirePropertyChanged(string strName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(strName));
        }
        #endregion

        object MicLock = new object();
        object SpeakerLock = new object();
        void ResetMicAndSpeakers()
        {

            AudioDevice micdevice = this.ComboBoxMicDevices.SelectedItem as AudioDevice;
            AudioDevice speakdevice = this.ComboBoxSpeakerDevices.SelectedItem as AudioDevice;
            if ((micdevice == null) || (speakdevice == null))
                return;


            if (SpeakerVolume != null)
            {
                SpeakerVolume.Dispose();
                SpeakerVolume = null;
            }
            if (MicrophoneVolume != null)
            {
                MicrophoneVolume.Dispose();
                MicrophoneVolume = null;
            }

            MicrophoneVolume = new ImageAquisition.AudioDeviceVolume(micdevice);
            this.SliderMicVolume.DataContext = MicrophoneVolume;

            SpeakerVolume = new ImageAquisition.AudioDeviceVolume(speakdevice);
            this.SliderSpeakerVolume.DataContext = SpeakerVolume;


            if (Microphone == null) /// Haven't start yet, no need to restart
                return;

            System.Windows.Interop.WindowInteropHelper helper = new System.Windows.Interop.WindowInteropHelper(Window.GetWindow(this));

            lock (MicLock)
            {
                lock (SpeakerLock)
                {
                    Microphone.Stop();
                    Speaker.Stop();
                    Microphone = null;
                    Speaker.Dispose();
                    Speaker = null;


                    Speaker = new DirectShowFilters.SpeakerFilter(speakdevice.Guid, 30, AudioFormat.SixteenBySixteenThousandMono, helper.Handle);
                    Microphone = new ImageAquisition.NarrowBandMic(micdevice, speakdevice.Guid, helper.Handle);
                    Microphone.AGC = UseAEC;
                    Microphone.UseKinectArray = false;
                    Speaker.Start();
                    if (UseAEC == true)
                    {
                        Microphone.Start();
                    }
                    else
                    {
                        Microphone.StartNoEchoCancellation();
                    }
                }
            }
        }

        private bool m_bUseAEC = true;

        public bool UseAEC
        {
            get { return m_bUseAEC; }
            set { m_bUseAEC = value; }
        }

        private void ComboBoxSpeakerDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ResetMicAndSpeakers();
    
        }

        private void ComboBoxMicDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ResetMicAndSpeakers();
        }

        private void AudioViewerControl_Loaded(object sender, RoutedEventArgs e)
        {
            /// Set the sources on this audio control
            /// 
            WPFImageWindows.AudioViewerControl audiocontrol = sender as WPFImageWindows.AudioViewerControl;
            if (audiocontrol == null)
                return;

            MediaSession currsess = audiocontrol.DataContext as MediaSession;
            if (currsess == null)
                return;

            //currsess.AudioRTPStream.RenderSink
            currsess.AudioRTPStream.RenderSink = audiocontrol.AudioDisplayFilter;
            WPFImageWindows.AudioSource source = new WPFImageWindows.AudioSource(currsess.AudioRTPStream);
            audiocontrol.Sources.Add(source);
            audiocontrol.AudioDisplayFilter.AddSource(source);

        }

        private void CheckBoxUseAEC_Checked(object sender, RoutedEventArgs e)
        {
            ResetMicAndSpeakers();
        }

        private void CheckBoxUseAEC_Unchecked(object sender, RoutedEventArgs e)
        {
            ResetMicAndSpeakers();
        }

        private void ButtonResetStats_Click(object sender, RoutedEventArgs e)
        {
            MediaSession currsess = ((FrameworkElement)sender).DataContext as MediaSession;
            if (currsess == null)
                return;
            currsess.AudioRTPStream.IncomingRTPPacketBuffer.Reset();
        }

        private void ButtonPlaySong_Click(object sender, RoutedEventArgs e)
        {
         
        }

        private void ButtonRandom_Click(object sender, RoutedEventArgs e)
        {
           
        }
    }

    public enum AnswerType
    {
        Normal,
        DND,
        AcceptToConference,
        AcceptToHold,
    }

    public class AnswerTypeInformation
    {
        public AnswerTypeInformation()
        {
        }

        private AnswerType m_eAnswerType = AnswerType.Normal;

        public AnswerType AnswerType
        {
            get { return m_eAnswerType; }
            set { m_eAnswerType = value; }
        }

        private string m_strDescription = "";

        public string Description
        {
            get { return m_strDescription; }
            set { m_strDescription = value; }
        }
        
    }
  
}
