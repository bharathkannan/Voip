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

namespace WPFXMPPClient
{
    /// <summary>
    /// Interaction logic for SendRawXMLWindow.xaml
    /// </summary>
    public partial class SendRawXMLWindow : Window, System.ComponentModel.INotifyPropertyChanged
    {
        public SendRawXMLWindow()
        {
            InitializeComponent();
        }

        XMPPClient XMPPClient = null;
        public void SetXMPPClient(XMPPClient client)
        {
            this.DataContext = this;
            XMPPClient = client;
            XMPPClient.OnXMLReceived += new System.Net.XMPP.XMPPClient.DelegateString(XMPPClient_OnXMLReceived);
            XMPPClient.OnXMLSent += new System.Net.XMPP.XMPPClient.DelegateString(XMPPClient_OnXMLSent);
        }

        Paragraph MainParagraph = new Paragraph();
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            textBoxLog.Document.Blocks.Add(MainParagraph);
        }

        


        private string m_strLogText = "";

        public string LogText
        {
            get { return m_strLogText; }
            set { m_strLogText = value; }
        }

        public const int MaxLogSize = 4096 * 8;
        public const int ReduceLogBy = 4096;

        void XMPPClient_OnXMLSent(XMPPClient client, string strXML)
        {
            AddNewStanza(strXML, true);
        }

        void XMPPClient_OnXMLReceived(XMPPClient client, string strXML)
        {
            AddNewStanza(strXML, false);
        }

        void AddNewStanza(string strText, bool bSent)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(AddStanza), new TextStanza() { XML = strText, Sent = bSent });
        }

        delegate void DelegateObject(object obj);
        void AddStanza(object obj)
        {
            this.Dispatcher.Invoke(new DelegateObject(DoAddStanza), obj);
        }

        void DoAddStanza(object obj)
        {
            TextStanza stanza = obj as TextStanza;

            Run run = new Run(stanza.XML);
            if (stanza.Sent == true)
                run.Foreground = Brushes.Orange;
            else
                run.Foreground = Brushes.Purple;

            MainParagraph.Inlines.Add(run);
            MainParagraph.Inlines.Add(new LineBreak());
            if (this.IsLoaded == true)
            {
                this.textBoxLog.ScrollToEnd();
            }

        }

      

        private void buttonSend_Click(object sender, RoutedEventArgs e)
        {
            string strSend = this.textBoxSend.Text;
            XMPPClient.SendRawXML(strSend);

            this.textBoxSend.Text = "";
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = System.Windows.Visibility.Hidden;
            base.OnClosing(e);
        }


        
        void FirePropertyChanged(string strName)
        {
            if (PropertyChanged != null)
            {
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(PropertyChanged, this, new System.ComponentModel.PropertyChangedEventArgs(strName));
            }
        }
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged = null;

        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            this.MainParagraph.Inlines.Clear();
        }


    }

    public class TextStanza
    {
        public string XML { get; set; }
        public bool Sent { get; set; }
    }
}
