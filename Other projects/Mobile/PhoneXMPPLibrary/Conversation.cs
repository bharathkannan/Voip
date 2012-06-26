/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Net;

using System.Reflection;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

#if !MONO
using System.Windows.Threading;
#endif

using System.Collections.Generic;
using System.Xml.Serialization;

namespace System.Net.XMPP
{
    [DataContract]
    public class TextMessage : System.ComponentModel.INotifyPropertyChanged
    {
        public TextMessage()
        {
        }

        private JID m_objFrom = new JID();
        [DataMember]
        public JID From
        {
            get { return m_objFrom; }
            set
            {
                if (m_objFrom != value)
                {
                    m_objFrom = value;
                    FirePropertyChanged("From");
                }
            }
        }
        
        private JID m_objTo = new JID();
        [DataMember]
        public JID To
        {
            get { return m_objTo; }
            set
            {
                if (m_objTo != value)
                {
                    m_objTo = value;
                    FirePropertyChanged("To");
                }
            }
        }

        [XmlIgnore]
        public string RemoteEnd
        {
            get
            {
                if (Sent == true)
                    return To.FullJID;
                else
                    return From.FullJID;
            }
            set
            {
            }
        }

        private DateTime m_dtReceived = DateTime.Now;
        [DataMember]
        public DateTime Received
        {
            get { return m_dtReceived; }
            set
            {
                if (m_dtReceived != value)
                {
                    m_dtReceived = value;
                    FirePropertyChanged("Received");
                }
            }
        }

        private string m_strMessage = "";
        [DataMember]
        public string Message
        {
            get { return m_strMessage; }
            set
            {
                if (m_strMessage != value)
                {
                    m_strMessage = value;
                    FirePropertyChanged("Message");
                }
            }
        }


        private string m_strThread = "";
        [DataMember]
        public string Thread
        {
            get { return m_strThread; }
            set
            {
                if (m_strThread != value)
                {
                    m_strThread = value;
                    FirePropertyChanged("Thread");
                }
            }
        }

        private bool m_bSent = false;
        [DataMember]
        public bool Sent
        {
            get { return m_bSent; }
            set { m_bSent = value; }
        }

#if !MONO
        public System.Windows.Media.Brush TextColor
        {
            get
            {
                if (Sent == true)
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Purple);
                else
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 0xDF, 0x85, 0));
            }
            set
            {
            }
        }
#endif

        #region INotifyPropertyChanged Members

        void FirePropertyChanged(string strName)
        {
            if (PropertyChanged != null)
            {
#if WINDOWS_PHONE
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(PropertyChanged, this, new System.ComponentModel.PropertyChangedEventArgs(strName));
#elif MONO
                PropertyChanged(this, new ComponentModel.PropertyChangedEventArgs(strName));
#else
               System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(PropertyChanged, this, new System.ComponentModel.PropertyChangedEventArgs(strName));
#endif
            }
            

            //if (PropertyChanged != null)
            //    PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(strName));
        }
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged = null;

        #endregion
    }

    
    [DataContract]
    public class Conversation : System.ComponentModel.INotifyPropertyChanged
    {
        public Conversation()
        {
        }

        public Conversation(JID jid)
        {
            JID = jid;
        }

        private JID m_objJID = null;
        [XmlIgnore]
        public JID JID
        {
            get { return m_objJID; }
            set { m_objJID = value; }
        }

        delegate void DelegateClear();
        public void Clear()
        {
#if WINDOWS_PHONE
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(new DelegateClear(DoClear));
#else
            m_listMessages.Clear();
#endif

        }

        void DoClear()
        {
            m_listMessages.Clear();
        }

        delegate void DelegateAddMessage(TextMessage msg);
        public void AddMessage(TextMessage msg)
        {
#if WINDOWS_PHONE
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(new DelegateAddMessage(DoAddMessage), msg);
#else
            m_listMessages.Add(msg);
#endif
        }

        void DoAddMessage(TextMessage msg)
        {
            m_listMessages.Add(msg);
        }

        private ConversationState m_eConversationState = ConversationState.active;
        [XmlIgnore]
        public ConversationState ConversationState
        {
            get { return m_eConversationState; }
            set 
            {
                if (m_eConversationState != value)
                {
                    m_eConversationState = value;
                    FirePropertyChanged("ConversationState");
                }
            }
        }



#if WINDOWS_PHONE
        private ObservableCollection<TextMessage> m_listMessages = new ObservableCollection<TextMessage>();
        [DataMember]
        public ObservableCollection<TextMessage> Messages
        {
            get { return m_listMessages; }
            set { m_listMessages = value; }
        }
#elif MONO
        private ObservableCollection<TextMessage> m_listMessages = new ObservableCollection<TextMessage>();
        [DataMember]
        public ObservableCollection<TextMessage> Messages
        {
            get { return m_listMessages; }
            set { m_listMessages = value; }
        }
#else
        private ObservableCollectionEx<TextMessage> m_listMessages = new ObservableCollectionEx<TextMessage>();
        [DataMember]
        public ObservableCollectionEx<TextMessage> Messages
        {
            get { return m_listMessages; }
            set { m_listMessages = value; }
        }
#endif

        #region INotifyPropertyChanged Members

        void FirePropertyChanged(string strName)
        {
            if (PropertyChanged != null)
            {
#if WINDOWS_PHONE
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(PropertyChanged, this, new System.ComponentModel.PropertyChangedEventArgs(strName));
#elif MONO
                PropertyChanged(this, new ComponentModel.PropertyChangedEventArgs(strName));
#else
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(PropertyChanged, this, new System.ComponentModel.PropertyChangedEventArgs(strName));
#endif
            }
            //if (PropertyChanged != null)
            //    PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(strName));
        }
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged = null;

        #endregion

    }

    public enum ConversationState
    {
        none,
        active,
        paused,
        composing,
    }
}
