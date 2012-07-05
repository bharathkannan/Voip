/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

using System.Net.XMPP;
using System.Runtime.Serialization;
using System.IO.IsolatedStorage;

using Microsoft.Phone.Tasks;
using System.Text.RegularExpressions;

using System.Windows.Navigation;
using FindMyIP;
using System.Net.NetworkInformation;
using RTP;
using System.Net.XMPP.Jingle;
using AudioClasses;
using System.Threading;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework;
using System.Windows.Threading;

namespace XMPPClient
{
    public partial class ChatPage : PhoneApplicationPage
    {
        public ChatPage()
        {
            InitializeComponent();
            /*    var _Timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
               _Timer.Tick += (s, arg) =>
               {
                   FrameworkDispatcher.Update();

               };
               _Timer.Start();
             */
            photoChooserTask = new PhotoChooserTask();
            photoChooserTask.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);

        }

        PhotoChooserTask photoChooserTask = null;
        private void ButtonSendMessage_Click(object sender, EventArgs e)
        {
            SendMessage(this.TextBoxChatToSend.Text);
            this.TextBoxChatToSend.Text = "";
            this.Focus();
            //this.ListBoxConversation.Focus();
        }


        bool m_bInFileTransferMode = false;
        public bool InFileTransferMode
        {
            get { return m_bInFileTransferMode; }
            set { m_bInFileTransferMode = value; }
        }

        string m_strFileTransferSID = "";

        public string FileTransferSID
        {
            get { return m_strFileTransferSID; }
            set { m_strFileTransferSID = value; }
        }


        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            string strJID = NavigationContext.QueryString["JID"];

            bool bQuery = false;
            try
            {
                if (NavigationContext.QueryString["Refresh"] != null)
                    bQuery = true;
            }
            catch (Exception)
            {
            }

            this.VoiceCall.RegisterXMPPClient();

            OurRosterItem = App.XMPPClient.FindRosterItem(new JID(strJID));

            if (this.InFileTransferMode == true)
            {
                this.NavigationService.Navigate(new Uri("/FileTransferPage.xaml", UriKind.Relative));
                this.InFileTransferMode = false;
            }

            if ((OurRosterItem == null) || (App.XMPPClient.XMPPState != XMPPState.Ready))
            {

                NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
                return;
            }

            if (bQuery == true)
                NavigationService.RemoveBackEntry();


            /// See if we have this conversation in storage if there are no messages
            if (OurRosterItem.Conversation.Messages.Count <= 0)
            {

                string strFilename = string.Format("{0}_conversation.item", OurRosterItem.JID.BareJID);

                using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
                {

                    if (storage.FileExists(strFilename) == true)
                    {
                        // Load from storage
                        IsolatedStorageFileStream location = null;
                        try
                        {
                            location = new IsolatedStorageFileStream(strFilename, System.IO.FileMode.Open, storage);
                            if (location.Length > 0)
                            {
                                DataContractSerializer ser = new DataContractSerializer(typeof(System.Net.XMPP.Conversation));
                                OurRosterItem.Conversation = ser.ReadObject(location) as System.Net.XMPP.Conversation;
                            }
                        }
                        catch (Exception)
                        {
                        }
                        finally
                        {
                            if (location != null)
                                location.Close();
                        }
                    }

                }

            }


            OurRosterItem.HasNewMessages = false; /// We just viewed new messages
            /// 

            this.DataContext = OurRosterItem;
            this.TextBlockConversationTitle.Text = OurRosterItem.Name;
            this.TextBlockConversationTitle.LayoutUpdated += ScrollRichTextBoxToBottom;

            if (this.TextBlockChat.Blocks.Count <= 0)
                TextBlockChat.Blocks.Add(MainParagraph);

            SetConversation();

            App.XMPPClient.OnNewConversationItem += new System.Net.XMPP.XMPPClient.DelegateNewConversationItem(XMPPClient_OnNewConversationItem);

        }


        Regex reghyperlink = new Regex(@"\w+\://\S+", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline);

        Paragraph MainParagraph = new Paragraph();

        public void SetConversation()
        {
            //  TextBlockChat.Selection.Select(MainParagraph.ContentStart, MainParagraph.ContentEnd);
            //TextBlockChat.Selection.Text = "";

            foreach (Paragraph graph in TextBlockChat.Blocks)
            {
                graph.Inlines.Clear();

            }

            TextBlockChat.InvalidateArrange();
            //MainParagraph.Inlines.Clear();
            //MainParagraph.SetValue



            foreach (TextMessage msg in OurRosterItem.Conversation.Messages)
            {
                if (msg.Message == null)
                    msg.Message = "";
                AddInlinesForMessage(msg);
            }


        }

        private void ScrollRichTextBoxToBottom(object sender, EventArgs e)
        {
            ScrollChat.ScrollToVerticalOffset(this.TextBlockChat.ActualHeight + 100);
        }


        const double FontSizeFrom = 10.0f;
        const double FontSizeMessage = 14.0f;
        void AddInlinesForMessage(TextMessage msg)
        {
            Span msgspan = new Span();
            string strRun = string.Format("{0} to {1} - {2}", msg.From.User, msg.To.User, msg.Received);
            Run runfrom = new Run();
            runfrom.Text = strRun;
            runfrom.Foreground = new SolidColorBrush(Colors.Gray);
            runfrom.FontSize = FontSizeFrom;
            msgspan.Inlines.Add(runfrom);

            msgspan.Inlines.Add(new LineBreak());


            Span spanmsg = new Span();
            runfrom.Foreground = new SolidColorBrush(Colors.Gray);
            spanmsg.FontSize = FontSizeMessage;
            msgspan.Inlines.Add(spanmsg);

            /// Look for hyperlinks in our run
            /// 
            string strMessage = msg.Message;
            int nMatchAt = 0;
            Match matchype = reghyperlink.Match(strMessage, nMatchAt);
            while (matchype.Success == true)
            {
                string strHyperlink = matchype.Value;

                /// Add everything before this as a normal run
                /// 
                if (matchype.Index > nMatchAt)
                {
                    Run runtext = new Run();
                    runtext.Text = strMessage.Substring(nMatchAt, (matchype.Index - nMatchAt));
                    runtext.Foreground = msg.TextColor;
                    msgspan.Inlines.Add(runtext);
                }

                Hyperlink link = new Hyperlink();
                link.Inlines.Add(strMessage.Substring(matchype.Index, matchype.Length));
                link.Foreground = new SolidColorBrush(Colors.Blue);
                link.TargetName = "_blank";
                try
                {
                    link.NavigateUri = new Uri(strMessage.Substring(matchype.Index, matchype.Length));
                }
                catch (Exception)
                {
                }
                link.Click += new RoutedEventHandler(link_Click);
                msgspan.Inlines.Add(link);

                nMatchAt = matchype.Index + matchype.Length;

                if (nMatchAt >= (strMessage.Length - 1))
                    break;

                matchype = reghyperlink.Match(strMessage, nMatchAt);
            }

            /// see if we have any remaining text
            /// 
            if (nMatchAt < strMessage.Length)
            {
                Run runtext = new Run();
                runtext.Text = strMessage.Substring(nMatchAt, (strMessage.Length - nMatchAt));
                runtext.Foreground = msg.TextColor;
                msgspan.Inlines.Add(runtext);
            }
            msgspan.Inlines.Add(new LineBreak());

            this.MainParagraph.Inlines.Add(msgspan);
        }

        void link_Click(object sender, RoutedEventArgs e)
        {
            /// Navigate to this link
            if (MessageBox.Show("Are you sure you want to navigate to the link?", "Confirm Navigate", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                WebBrowserTask task = new WebBrowserTask();
                task.Uri = ((Hyperlink)sender).NavigateUri;
                task.Show();
            }

        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (this.VoiceCall.IsCallActive == true)
            {
                if (MessageBox.Show("A call is active, if you navigate away from this page the call will be stopped?", "Leave Page and End Call?", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                }
                else
                {
                    e.Cancel = true;
                    return;
                }
            }

            base.OnNavigatingFrom(e);
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            VoiceCall.StopCall();
            VoiceCall.Dispose();

            App.XMPPClient.OnNewConversationItem -= new System.Net.XMPP.XMPPClient.DelegateNewConversationItem(XMPPClient_OnNewConversationItem);
            if (OurRosterItem != null)
                SaveConversation(OurRosterItem);
        }

        public static void SaveConversation(RosterItem item)
        {
            /// Save this conversation so it can be restored later... save it under the JID name

            string strFilename = string.Format("{0}_conversation.item", item.JID.BareJID);



            using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
            {

                // Load from storage
                IsolatedStorageFileStream location = new IsolatedStorageFileStream(strFilename, System.IO.FileMode.Create, storage);
                DataContractSerializer ser = new DataContractSerializer(typeof(System.Net.XMPP.Conversation));

                try
                {
                    ser.WriteObject(location, item.Conversation);
                }
                catch (Exception)
                {
                }
                location.Close();
            }

        }

        void XMPPClient_OnNewConversationItem(RosterItem item, bool bReceived, TextMessage msg)
        {

            if (msg.Message.IndexOf("Call from ") == 0 && this.hack == false)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() => ButtonStartVoice.Content = "Accept Voice Call");
                string ipen = msg.Message.Substring(10);
                int pos = ipen.IndexOf(':');
                me = new MediaPart(AudioStream, null);
                me.remote = new IPEndPoint(IPAddress.Parse(ipen.Substring(0, pos)), Convert.ToInt32(ipen.Substring(pos + 1)));
            }
            else if (msg.Message.IndexOf("Call ok") == 0 && this.hack == true)
            {
                string ipen = msg.Message.Substring(8);
                int pos = ipen.IndexOf(':');
                me.remote = new IPEndPoint(IPAddress.Parse(ipen.Substring(0, pos)), Convert.ToInt32(ipen.Substring(pos + 1)));
                Deployment.Current.Dispatcher.BeginInvoke(() => { ButtonStartVoice.Content = "End Voice Call"; });
                Datastore.Add(me.localEp, "localEp");
                Datastore.Add(me.stream, "stream");
                Datastore.Add(me.remote, "remoteEp");
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                this.NavigationService.Navigate(new Uri("/TestPAge.xaml?", UriKind.Relative)));
                

            }






            Dispatcher.BeginInvoke(new System.Net.XMPP.XMPPClient.DelegateNewConversationItem(DoOnNewConversationItem), item, bReceived, msg);
        }

        void DoOnNewConversationItem(RosterItem item, bool bReceived, TextMessage msg)
        {

            if (item.JID.BareJID == OurRosterItem.JID.BareJID)
            {
                /// Clear our new message flag for this roster user as long as this window is open
                item.HasNewMessages = false;

                AddInlinesForMessage(msg);



                this.TextBlockChat.Focus();


                //TextPointer myTextPointer1 = MainParagraph.ContentStart.GetPositionAtOffset(20);
                //TextPointer myTextPointer1 = MainParagraph.ContentEnd.GetPositionAtOffset(0, LogicalDirection.Backward);
                //TextPointer myTextPointer2 = MainParagraph.ContentEnd.GetPositionAtOffset(0, LogicalDirection.Backward);
                this.TextBlockChat.Selection.Select(this.TextBlockChat.ContentEnd, this.TextBlockChat.ContentEnd);

                ScrollChat.ScrollToVerticalOffset(this.TextBlockChat.ActualHeight + 100);
                //this.ListBoxConversation.UpdateLayout();
                //if (this.ListBoxConversation.Items.Count > 0)
                //   this.ListBoxConversation.ScrollIntoView(this.ListBoxConversation.Items[this.ListBoxConversation.Items.Count - 1]);

            }
        }

        RosterItem OurRosterItem;

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void ListBoxConversation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ButtonClearMessages_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to clear these items?", "Confirm Clear", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                OurRosterItem.Conversation.Clear();
                this.Focus();

                /// Can't seem a way to clear the window besides reloading it
                //SetConversation();
                NavigationService.Navigate(new Uri(string.Format("/ChatPage.xaml?JID={0}&Refresh=True&Unload={1}", this.OurRosterItem.JID, Guid.NewGuid()), UriKind.Relative));

            }
        }

        public string LastFullJIDBeforeMSDecidedToScrewUs = null;

        private void ButtonSendPhoto_Click(object sender, EventArgs e)
        {
            try
            {
                if (OurRosterItem.LastFullJIDToGetMessageFrom.Resource.Length <= 0)
                {
                    if (OurRosterItem.ClientInstances.Count > 0)
                    {
                        LastFullJIDBeforeMSDecidedToScrewUs = OurRosterItem.ClientInstances[0].FullJID;
                    }
                }
                else
                    LastFullJIDBeforeMSDecidedToScrewUs = OurRosterItem.LastFullJIDToGetMessageFrom;

                App.XMPPClient.Disconnect(); // We're going to get disconnected any way
                photoChooserTask.ShowCamera = true;
                photoChooserTask.Show();
            }
            catch (System.InvalidOperationException)
            {
                MessageBox.Show("Unable to select a photo");
            }

        }

        void photoChooserTask_Completed(object sender, PhotoResult e)
        {
            App.XMPPClient.Connect();
            App.WaitReady();
            System.Threading.Thread.Sleep(2000);

            if (e.TaskResult == TaskResult.OK)
            {
                byte[] bStream = new byte[e.ChosenPhoto.Length];
                e.ChosenPhoto.Read(bStream, 0, bStream.Length);

                this.InFileTransferMode = true;
                this.FileTransferSID = App.XMPPClient.FileTransferManager.SendFile(e.OriginalFileName, bStream, LastFullJIDBeforeMSDecidedToScrewUs);

                //Code to display the photo on the page in an image control named myImage.
                //System.Windows.Media.Imaging.BitmapImage bmp = new System.Windows.Media.Imaging.BitmapImage();
                //bmp.SetSource(e.ChosenPhoto);
                //myImage.Source = bmp;
            }

            /// know bug occurs sometimes when re-opening this app, this fixes it:
            /// http://social.msdn.microsoft.com/Forums/en-US/windowsphone7series/thread/e079d99e-d2ac-47ab-a87e-f4ce0d3660d5
            NavigationService.Navigated += new NavigatedEventHandler(navigateCompleted);

        }

        void navigateCompleted(object sender, EventArgs e)
        {
            //Do the delayed navigation from the main page
            this.NavigationService.Navigate(new Uri("/FileTransferPage.xaml", UriKind.Relative));
            NavigationService.Navigated -= new NavigatedEventHandler(navigateCompleted);
        }


        void SendMessage(string strMessage)
        {
            if (App.XMPPClient.Connected == false)
            {
                if (MessageBox.Show("Client was disconnected, would you like to re-connect?", "Connection Lost", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    App.XMPPClient.Connect();
                return;
            }

            App.XMPPClient.SendChatMessage(strMessage, OurRosterItem.JID);
            this.TextBoxChatToSend.Text = "";
            this.Focus();
        }

        private void TextBoxChatToSend_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string strText = this.TextBoxChatToSend.Text;
                SendMessage(strText);
                //this.ListBoxConversation.Focus();
            }
        }

        VoiceCall VoiceCall = new VoiceCall();
        MediaPart me;


        private void ButtonStartVoice_Click(object sender, RoutedEventArgs e)
        {
        /*    me = new MediaPart(AudioStream, null);
            me.InitCall();
            Datastore.Add(me.localEp, "localEp");
            Datastore.Add(me.stream, "stream");
            Datastore.Add(me.localEp,"remoteEp");
            this.NavigationService.Navigate(new Uri("/TestPAge.xaml?", UriKind.Relative));
         */
            this.hack = false;
            if (ButtonStartVoice.Content.ToString() == "Start Voice Call")
            {
                this.hack = true;
                me = new MediaPart(AudioStream, null);
                me.InitCall();
                SendMessage("Call from " + me.localEp.ToString());
                this.Focus();
            }
            else if (ButtonStartVoice.Content.ToString() == "Accept Voice Call")
            {
                me.InitCall();
                this.Focus();
                Datastore.Add(me.localEp, "localEp");
                Datastore.Add(me.stream, "stream");
                Datastore.Add(me.remote, "remoteEp");
                Deployment.Current.Dispatcher.BeginInvoke(() => { ButtonStartVoice.Content = "End Voice Call"; });
                SendMessage("Call ok " + me.localEp.ToString());
                this.NavigationService.Navigate(new Uri("/TestPAge.xaml?", UriKind.Relative));

            }

            
        }
        /*

            if (ButtonStartVoice.Content.ToString() == "Start Voice Call")
            {
                AudioStream.Stop();
                VoiceCall.AudioStream = this.AudioStream;
                VoiceCall.OnCallStopped -= new EventHandler(VoiceCall_OnCallStopped);
                VoiceCall.OnCallStopped += new EventHandler(VoiceCall_OnCallStopped);
                VoiceCall.StartCall(OurRosterItem.LastFullJIDToGetMessageFrom);
                ButtonStartVoice.Content = "Stop Call";
            }
            else
            {
                VoiceCall.StopCall();
            }
            */


        void VoiceCall_OnCallStopped(object sender, EventArgs e)
        {
            this.Dispatcher.BeginInvoke(new EventHandler(SafeStopCall), null, null);
        }

        void SafeStopCall(object obj, EventArgs args)
        {
            ButtonStartVoice.Content = "Start Voice Call";
        }

        private void TextBoxChatToSend_TextChanged(object sender, TextChangedEventArgs e)
        {

        }




        //public static class Extensions
        //{
        //    public static T GetChildByType<T>(this UIElement element, Func<T, bool> condition)
        //        where T : UIElement
        //    {
        //        List<T> results = new List<T>();
        //        GetChildrenByType<T>(element, condition, results);
        //        if (results.Count > 0)
        //            return results[0];
        //        else
        //            return null;
        //    }

        //    private static void GetChildrenByType<T>(UIElement element, Func<T, bool> condition, List<T> results)
        //        where T : UIElement
        //    {
        //        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
        //        {
        //            UIElement child = VisualTreeHelper.GetChild(element, i) as UIElement;
        //            if (child != null)
        //            {
        //                T t = child as T;
        //                if (t != null)
        //                {
        //                    if (condition == null)
        //                        results.Add(t);
        //                    else if (condition(t))
        //                        results.Add(t);
        //                }
        //                GetChildrenByType<T>(child, condition, results);
        //            }
        //        }
        //    }
        //} 





        #region Myedit

        bool hack = false;
        RTPAudioStream stream = null;
        AudioClasses.ByteBuffer MicrophoneQueue = new ByteBuffer();
        IPAddress myip;
        Boolean IsCallActive;
        IPEndPoint localEp, StunEp;
        Thread SpeakerThread, MicrophoneThread;
        AudioStreamSource source = null;
        IPEndPoint remote;
 //       public MediaElement AudioStream1;

        public void InitCall()
        {
            myip = IPAddress.Parse("172.16.41.174");
            localEp = new IPEndPoint(myip, 3001);
   //         AudioStream1 = this.mediaElement1;
       //     AudioStream.Stop();
            InitializeStream();
            FindStunAddress();
        }

        //Find STUN Address

        public void FindStunAddress()
        {
           // StunEp = stream.GetSTUNAddress(new DnsEndPoint("stun.ekiga.net", 3478), 4000);
          //  localEp = stream.FindIpPort();
        }

        //InitStream
        public void InitializeStream()
        {
            stream = new RTPAudioStream(0, null);
            stream.Bind(localEp);
            stream.AudioCodec = new G722CodecWrapper();
            stream.UseInternalTimersForPacketPushPull = false;
        }

        //Mediastart

        public void StartCall()
        {
            //stream init
            IsCallActive = true;
            stream.Start(remote, 50, 50);
            source = new AudioStreamSource();


            //stream start recv
            SpeakerThread = new Thread(new ThreadStart(SpeakerThreadFunction));
            SpeakerThread.Name = "Speaker Thread";
            SpeakerThread.Start();

            MicrophoneThread = new Thread(new ThreadStart(MicrophoneThreadFunction));
            MicrophoneThread.Name = "Microphone Thread";
            MicrophoneThread.Start();
        }

        //Speaker Thread

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
            source = new AudioStreamSource();
           TimeSpan tsPTime = TimeSpan.FromMilliseconds(stream.PTimeReceive);
            int nSamplesPerPacket = stream.AudioCodec.AudioFormat.CalculateNumberOfSamplesForDuration(tsPTime);
            int nBytesPerPacket = nSamplesPerPacket * stream.AudioCodec.AudioFormat.BytesPerSample;
            byte[] bDummySample = new byte[nBytesPerPacket];
            source.PacketSize = nBytesPerPacket;
            stream.IncomingRTPPacketBuffer.InitialPacketQueueMinimumSize = 4;
            stream.IncomingRTPPacketBuffer.PacketSizeShiftMax = 10;
            int nMsTook = 0;

            
               Deployment.Current.Dispatcher.BeginInvoke(new EventHandler(SafeStartMediaElement), null, null);
        //       while (true) { }
            /// Get first packet... have to wait for our rtp buffer to fill
            byte[] bData = stream.WaitNextPacketSample(true, stream.PTimeReceive * 5, out nMsTook);
            if ((bData != null) && (bData.Length > 0))
            {
                source.Write(bData);
            }


            DateTime dtNextPacketExpected = DateTime.Now + tsPTime;

            System.Diagnostics.Stopwatch WaitPacketWatch = new System.Diagnostics.Stopwatch();
            int nDeficit = 0;
            while (IsCallActive == true)
            {
                bData = stream.WaitNextPacketSample(true, stream.PTimeReceive, out nMsTook);
                if ((bData != null) && (bData.Length > 0))
                {
                    source.Write(bData);
                }

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

        //Microphone Thread
        public void MicrophoneThreadFunction()
        {
            StartMic();
            int nSamplesPerPacket = stream.AudioCodec.AudioFormat.CalculateNumberOfSamplesForDuration(TimeSpan.FromMilliseconds(stream.PTimeTransmit));
            int nBytesPerPacket = nSamplesPerPacket * stream.AudioCodec.AudioFormat.BytesPerSample;
            TimeSpan tsPTime = TimeSpan.FromMilliseconds(stream.PTimeTransmit);
            DateTime dtNextPacketExpected = DateTime.Now + tsPTime;
            int nUnavailableAudioPackets = 0;
            while (IsCallActive == true)
            {
                dtNextPacketExpected = DateTime.Now + tsPTime;
                if (MicrophoneQueue.Size >= nBytesPerPacket)
                {
                    byte[] buffer = MicrophoneQueue.GetNSamples(nBytesPerPacket);
                    stream.SendNextSample(buffer);
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
                {

                    System.Threading.Thread.Sleep(nMsRemaining);
                }
            }
            StopMic();
        }
        byte[] buffer = new byte[16 * 40];
        void StartMic()
        {
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







        #endregion
    }

}
