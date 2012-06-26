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
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization;

using System.Runtime.InteropServices;

using System.Text.RegularExpressions;

using System.Speech.Synthesis;

namespace WPFXMPPClient
{
    /// <summary>
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
    public partial class ChatWindow : Window
    {
        public ChatWindow()
        {
            InitializeComponent();
        }

        public XMPPClient XMPPClient = null;
        public RosterItem OurRosterItem = null;

        System.Threading.Thread ThreadSpeak = null;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ThreadSpeak = new System.Threading.Thread(new System.Threading.ThreadStart(SpeakThread));
            ThreadSpeak.Name = "Speak Thread";
            ThreadSpeak.IsBackground = true;
            ThreadSpeak.Start();


            /// See if we have this conversation in storage if there are no messages
            if (OurRosterItem.HasLoadedConversation == false)
            {
                OurRosterItem.HasLoadedConversation = true;

                string strFilename = string.Format("{0}_conversation.item", OurRosterItem.JID.BareJID);

                string strPath = string.Format("{0}\\conversations", Environment.GetFolderPath(System.Environment.SpecialFolder.Personal));
                if (System.IO.Directory.Exists(strPath) == false)
                    System.IO.Directory.CreateDirectory(strPath);

                string strFullFileName = string.Format("{0}\\{1}", strPath, strFilename);
                FileStream location = null;
                if (File.Exists(strFullFileName) == true)
                {
                    try
                    {
                        location = new FileStream(strFullFileName, System.IO.FileMode.Open);
                        DataContractSerializer ser = new DataContractSerializer(typeof(System.Net.XMPP.Conversation));

                        OurRosterItem.Conversation = ser.ReadObject(location) as System.Net.XMPP.Conversation;
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


            OurRosterItem.HasNewMessages = false; /// We just viewed new messages
            this.DataContext = OurRosterItem;
            XMPPClient.OnNewConversationItem += new System.Net.XMPP.XMPPClient.DelegateNewConversationItem(XMPPClient_OnNewConversationItem);
            SetConversation();
        }

        Regex reghyperlink = new Regex(@"\w+\://\S+", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline);

        Paragraph MainParagraph = new Paragraph();

        public void SetConversation()
        {
            MainParagraph.Inlines.Clear();
            TextBlockChat.Document.Blocks.Clear();
            TextBlockChat.Document.Blocks.Add(MainParagraph);
            
            foreach (TextMessage msg in OurRosterItem.Conversation.Messages)
            {
                AddInlinesForMessage(msg);
            }

            this.TextBlockChat.ScrollToEnd();
        }

        const double FontSizeFrom = 10.0f;
        const double FontSizeMessage = 14.0f;
        void AddInlinesForMessage(TextMessage msg)
        {
            if (msg.Message == null)
                return;
            Span msgspan = new Span();
            string strRun = string.Format("{0} to {1} - {2}", msg.From, msg.To, msg.Received);
            Run runfrom = new Run(strRun);
            runfrom.Foreground = Brushes.Gray;
            runfrom.FontSize = FontSizeFrom;
            msgspan.Inlines.Add(runfrom);

            msgspan.Inlines.Add(new LineBreak());


            Span spanmsg = new Span();
            spanmsg.Foreground = Brushes.Gray;
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
                    Run runtext = new Run(strMessage.Substring(nMatchAt, (matchype.Index - nMatchAt)));
                    runtext.Foreground = msg.TextColor;
                    msgspan.Inlines.Add(runtext);
                }

                Hyperlink link = new Hyperlink();
                link.Inlines.Add(strMessage.Substring(matchype.Index, matchype.Length));
                link.Foreground = Brushes.Blue;
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
                Run runtext = new Run(strMessage.Substring(nMatchAt, (strMessage.Length - nMatchAt)));
                runtext.Foreground = msg.TextColor;
                msgspan.Inlines.Add(runtext);
            }
            msgspan.Inlines.Add(new LineBreak());

            this.MainParagraph.Inlines.Add(msgspan);
        }

        void link_Click(object sender, RoutedEventArgs e)
        {
            /// Navigate to this link
            /// 
            System.Diagnostics.Process.Start(((Hyperlink)sender).NavigateUri.ToString());
        }


        public static void SaveConversation(RosterItem item)
        {
            /// Save this conversation so it can be restored later... save it under the JID name
            string strFilename = string.Format("{0}_conversation.item", item.JID.BareJID);

            string strPath = string.Format("{0}\\conversations", Environment.GetFolderPath(System.Environment.SpecialFolder.Personal));
            if (System.IO.Directory.Exists(strPath) == false)
                System.IO.Directory.CreateDirectory(strPath);

            string strFullFileName = string.Format("{0}\\{1}", strPath, strFilename);
            FileStream location = null;
            try
            {
                location = new FileStream(strFullFileName, System.IO.FileMode.Create);

                DataContractSerializer ser = new DataContractSerializer(typeof(System.Net.XMPP.Conversation));
                ser.WriteObject(location, item.Conversation);
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


        void XMPPClient_OnNewConversationItem(RosterItem item, bool bReceived, TextMessage msg)
        {
            Dispatcher.Invoke(new System.Net.XMPP.XMPPClient.DelegateNewConversationItem(DoOnNewConversationItem), item, bReceived, msg);
        }

        [DllImport("user32.dll")]
        static extern bool FlashWindow(IntPtr hwnd, bool bInvert);

        void SpeakThread()
        {
            while (true)
            {
                string strSpeak = PhrasesToSpeak.WaitNext(5000);
                if (strSpeak != null)
                {
                    syn.Speak(strSpeak);
                }
            }
        }

        System.Speech.Synthesis.SpeechSynthesizer syn = new SpeechSynthesizer();
        SocketServer.EventQueueWithNotification<string> PhrasesToSpeak = new SocketServer.EventQueueWithNotification<string>();
        void DoOnNewConversationItem(RosterItem item, bool bReceived, TextMessage msg)
        {
            if (bReceived == true)
            {
                foreach (RosterItemPresenceInstance instance in  this.ListBoxInstances.Items)
                {
                    if (instance.FullJID.Equals(msg.From) == true)
                    {
                        this.ListBoxInstances.SelectedItem = instance;
                        break;
                    }
                }
            }

            if (item.JID.BareJID.Equals(OurRosterItem.JID.BareJID) == true)
            {
                AddInlinesForMessage(msg);

                if (bReceived == true)
                {
                    if (this.CheckBoxUseSpeech.IsChecked == true)
                    {
                        PhrasesToSpeak.Enqueue(msg.Message);
                        
                    }
                    else
                    {
                        System.Media.SoundPlayer player = new System.Media.SoundPlayer("Sounds/ding.wav");
                        player.Play();
                    }
                    IntPtr windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                    FlashWindow(windowHandle, true);

                }

                /// Clear our new message flag for this roster user as long as this window is open

                if (this.IsActive == true)
                {
                    OurRosterItem.HasNewMessages = false;
                }

                this.TextBlockChat.ScrollToEnd();
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            OurRosterItem.HasNewMessages = false;
            base.OnActivated(e);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            XMPPClient.OnNewConversationItem -= new System.Net.XMPP.XMPPClient.DelegateNewConversationItem(XMPPClient_OnNewConversationItem);
            SaveConversation(OurRosterItem);
            base.OnClosing(e);
        }

        public void Clear()
        {
            OurRosterItem.Conversation.Clear();
            SetConversation();
        }

        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            Clear();            
        }

        private void TextBoxChatToSend_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                DoSend();
            }

        }

        private void ButtonSend_Click(object sender, RoutedEventArgs e)
        {
            DoSend();
        }

        const string HideMe = "[minimize]";
        const string ClearMe = "[clear]";
        void DoSend()
        {
            /// Send to our selected item
            /// 
            string strText = this.TextBoxChatToSend.Text;
            if (this.ListBoxInstances.SelectedItems.Count > 0)
            {
                foreach (RosterItemPresenceInstance instance in this.ListBoxInstances.SelectedItems)
                {
                    if (strText == HideMe)
                    {
                        PrivacyService serv = XMPPClient.FindService(typeof(PrivacyService)) as PrivacyService;
                        if (serv != null)
                        {
                            serv.ForceUserToMinimizeMyWindow(instance.FullJID);
                        }
                    }
                    else if (strText == ClearMe)
                    {
                        PrivacyService serv = XMPPClient.FindService(typeof(PrivacyService)) as PrivacyService;
                        if (serv != null)
                        {
                            serv.ForceUserToClearMyHistory(instance.FullJID);
                        }
                    }
                    else
                    {
                        XMPPClient.SendChatMessage(strText, instance.FullJID);
                    }
                }
            }
            else
            {
                if (strText == HideMe)
                {
                    PrivacyService serv = XMPPClient.FindService(typeof(PrivacyService)) as PrivacyService;
                    if (serv != null)
                    {
                        serv.ForceUserToMinimizeMyWindow(OurRosterItem.LastFullJIDToGetMessageFrom);
                    }
                }
                else if (strText == ClearMe)
                {
                    PrivacyService serv = XMPPClient.FindService(typeof(PrivacyService)) as PrivacyService;
                    if (serv != null)
                    {
                        serv.ForceUserToClearMyHistory(OurRosterItem.LastFullJIDToGetMessageFrom);
                    }
                }
                else
                {
                    OurRosterItem.SendChatMessage(this.TextBoxChatToSend.Text, MessageSendOption.SendToLastRecepient);
                }
            }

            //XMPPClient.SendChatMessage(, OurRosterItem.JID);
            this.TextBoxChatToSend.Text = "";
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((e.ChangedButton == MouseButton.Left) && (e.ButtonState == MouseButtonState.Pressed))
                this.DragMove();
        }


        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            SaveConversation(OurRosterItem);
            this.Close();
        }

        private void ButtonCopy_Click(object sender, RoutedEventArgs e)
        {
            /// Copy all the selected text 
            /// 
            StringBuilder sb = new StringBuilder();
            //foreach (TextMessage msg in this.ListBoxConversation.SelectedItems)
            //{
            //    if (msg.Sent == false)
            //       sb.AppendFormat("From {0} at {1}\r\n{2}", msg.From, msg.Received, msg.Message);
            //    else
            //        sb.AppendFormat("To {0} at {1}\r\n{2}", msg.To, msg.Received, msg.Message);
            //}
            Clipboard.SetText(sb.ToString());
        }

        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {

        }

        private void ButtonSendFile_Click(object sender, RoutedEventArgs e)
        {
            // Find the file to send

            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            if (dlg.ShowDialog() == true)
            {
                string strFileName = dlg.FileName;

                if ((this.ListBoxInstances.SelectedItems.Count <= 0) && (this.ListBoxInstances.Items.Count > 0))
                    this.ListBoxInstances.SelectedIndex = 0;

                if (this.ListBoxInstances.SelectedItems.Count > 0)
                {
                    foreach (RosterItemPresenceInstance instance in this.ListBoxInstances.SelectedItems)
                    {
                        /// Just send it to 1 recepient for now, must have a full jid
                        string strSendID = XMPPClient.FileTransferManager.SendFile(strFileName, instance.FullJID);

                        foreach (Window wind in Application.Current.Windows)
                        {
                            if (wind is MainWindow)
                            {
                                ((MainWindow)wind).ShowFileTransfer();
                                break;
                            }
                        }

                        break;
                    }
                }
                

            }
        }

        private void ButtonSendScreenCapture_Click(object sender, RoutedEventArgs e)
        {
            List<Window> RestoreList = new List<Window>();
            foreach (Window win in Application.Current.Windows)
            {
                if (win.WindowState != System.Windows.WindowState.Minimized)
                {
                    win.WindowState = System.Windows.WindowState.Minimized;
                    RestoreList.Add(win);
                }
            }

            byte [] bPng = WPFImageWindows.ScreenGrabUtility.GetScreenPNG();

            foreach (Window win in RestoreList)
            {
                win.WindowState = System.Windows.WindowState.Normal;
            }

            if (bPng != null)
            {
                string strFileName = string.Format("sc_{0}.png", Guid.NewGuid());

                if ((this.ListBoxInstances.SelectedItems.Count <= 0) && (this.ListBoxInstances.Items.Count > 0))
                    this.ListBoxInstances.SelectedIndex = 0;

                if (this.ListBoxInstances.SelectedItems.Count > 0)
                {
                    foreach (RosterItemPresenceInstance instance in this.ListBoxInstances.SelectedItems)
                    {
                        /// Just send it to 1 recepient for now, must have a full jid
                        string strSendID = XMPPClient.FileTransferManager.SendFile(strFileName, bPng, instance.FullJID);

                        foreach (Window wind in Application.Current.Windows)
                        {
                            if (wind is MainWindow)
                            {
                                ((MainWindow)wind).ShowFileTransfer();
                                break;
                            }
                        }

                        break;
                    }
                }
                
            }
        }


        public void DownloadFinished(string strRequestId, string strLocalFileName, RosterItem itemfrom)
        {
            System.Diagnostics.Process.Start(strLocalFileName);
        }

        private void TextBlockChat_TouchDown(object sender, TouchEventArgs e)
        {
            /// scroll our window
            /// 
            
        }

        private void TextBlockChat_TouchMove(object sender, TouchEventArgs e)
        {

        }

        private void ButtonStartAudioCall_Click(object sender, RoutedEventArgs e)
        {
            RosterItemPresenceInstance item = ((FrameworkElement)sender).DataContext as RosterItemPresenceInstance;
            if (item == null)
                return;

            foreach (Window win in Application.Current.Windows)
            {
                if (win is AudioMuxerWindow)
                {
                    ((AudioMuxerWindow)win).InitiateOrShowCallTo(item.FullJID);
                    break;
                }
            }
        }

    
    }
}
