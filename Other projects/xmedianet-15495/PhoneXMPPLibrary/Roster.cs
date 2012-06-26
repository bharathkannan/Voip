/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Net;

using System.Xml.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

#if !MONO
using System.Windows.Threading;
#endif

using System.Runtime.Serialization;

namespace System.Net.XMPP
{
    /// <summary>
    /// An instance of this class is made for every presence full jid.  We need this
    /// since we can login from many different places, and we want to be aware of all instances of this roster item
    /// </summary>
    public class RosterItemPresenceInstance : System.ComponentModel.INotifyPropertyChanged
    {
        public RosterItemPresenceInstance(JID fulljid)
        {
            FullJID = fulljid;
        }

        private JID m_jidFullJID = null;

        public JID FullJID
        {
            get { return m_jidFullJID; }
            set { m_jidFullJID = value; }
        }

        private PresenceStatus m_objPresence = new PresenceStatus();

        public PresenceStatus Presence
        {
            get { return m_objPresence; }
            set
            {
                if (m_objPresence != value)
                {
                    m_objPresence = value;
                    FirePropertyChanged("Presence");
                }
            }
        }

        private bool m_bCanClientDoAudio = false;

        public bool CanClientDoAudio
        {
            get { return m_bCanClientDoAudio; }
            set
            {
               m_bCanClientDoAudio = value;
            }
        }


        private bool m_bIsGoogleClient = false;

        public bool IsKnownGoogleClient
        {
            get { return m_bIsGoogleClient; }
            set { m_bIsGoogleClient = value; }
        }

#if !MONO
        public System.Windows.Visibility VisibilityCanClientDoAudio
        {
            get
            {
                if (m_bCanClientDoAudio == true)
                    return Windows.Visibility.Visible;
                return Windows.Visibility.Collapsed;
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
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(strName));
#else
                System.ComponentModel.PropertyChangedEventArgs args = new System.ComponentModel.PropertyChangedEventArgs(strName);
                System.ComponentModel.PropertyChangedEventHandler eventHandler = PropertyChanged;
                if (eventHandler == null)
                    return;

                Delegate[] delegates = eventHandler.GetInvocationList();
                // Walk thru invocation list
                foreach (System.ComponentModel.PropertyChangedEventHandler handler in delegates)
                {
                    DispatcherObject dispatcherObject = handler.Target as DispatcherObject;
                    // If the subscriber is a DispatcherObject and different thread
                    if (dispatcherObject != null && dispatcherObject.CheckAccess() == false)
                    {
                        // Invoke handler in the target dispatcher's thread
                        dispatcherObject.Dispatcher.Invoke(DispatcherPriority.DataBind, handler, this, args);
                    }
                    else // Execute handler as is
                        handler(this, args);
                }
                //System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(PropertyChanged, this, new System.ComponentModel.PropertyChangedEventArgs(strName));
#endif
            }
        }
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged = null;

        #endregion
    }

    public enum MessageSendOption
    {
        SendPriority,
        SendToLastRecepient,
        SendToAll,
    }

    public class RosterItem : System.ComponentModel.INotifyPropertyChanged
    {
        public RosterItem(XMPPClient client, JID jid)
        {
            XMPPClient = client;
            this.JID = jid;
            
            /// Set our avatar to our last known avatar
            /// 
            this.AvatarImagePath = XMPPClient.AvatarStorage.GetLastHashForJID(this.JID);
        }

        public RosterItem()
        {
        }

        public override bool Equals(object obj)
        {
            if (obj is RosterItem)
                return JID.Equals(((RosterItem)obj).JID);
            return false;
        }


        private XMPPClient m_objXMPPClient = null;
        public XMPPClient XMPPClient
        {
          get { return m_objXMPPClient; }
          set { m_objXMPPClient = value; }
        }

        public override string ToString()
        {
               return string.Format("{0} ({1}), {2}", JID, Name, Presence);
        }

        private JID m_objJID = null;

        public JID JID
        {
            get { return m_objJID; }
            set 
            { 
                m_objJID = value;
                Conversation.JID = value;
            }
        }
        private string m_strName = "";

        public string Name
        {
            get 
            {
                int nInstances = m_listClientInstances.Count;
                if (nInstances <= 1)
                    return m_strName;
                else
                    return string.Format("{0} ({1} places)", m_strName, nInstances);
            }
            set
            {
                if (m_strName != value)
                {
                    m_strName = value;
                    FirePropertyChanged("Name");
                }
            }
        }

        private geoloc m_objGeoLoc = new geoloc() { lat = 0.0f, lon = 0.0f };
        /// <summary>
        ///  The current location of this roster item
        /// </summary>
        public geoloc GeoLoc
        {
            get { return m_objGeoLoc; }
            set 
            {
                if (m_objGeoLoc != value)
                {
                    m_objGeoLoc = value;
                    FirePropertyChanged("GeoLoc");
                    FirePropertyChanged("GeoString");
                    FirePropertyChanged("GeoVisible");
                }
            }
        }

        public string GeoString
        {
            get
            {
                if ((m_objGeoLoc.lat == 0) && (m_objGeoLoc.lon == 0))
                    return "unknown location";
                else
                    return string.Format("lat: {0:N2}, lon: {1:N2}", m_objGeoLoc.lat, m_objGeoLoc.lon);
            }
            set
            {
                
            }
        }

#if !MONO
        public System.Windows.Visibility GeoVisible
        {
            get
            {
                lock (m_lockClientInstances)
                {
                    if ((m_objGeoLoc.lat == 0) && (m_objGeoLoc.lon == 0))
                        return Windows.Visibility.Collapsed;
                    else
                        return Windows.Visibility.Visible;
                    
                }
            }
            set { }

        }
#endif

        private TuneItem m_objTune = new TuneItem();
        /// <summary>
        /// The tune this roster item is listening to
        /// </summary>
        public TuneItem Tune
        {
            get { return m_objTune; }
            set 
            { 
                m_objTune = value;
                FirePropertyChanged("Tune");
                FirePropertyChanged("TuneString");
            }
        }

        public string TuneString
        {
            get
            {
                return m_objTune.ToString();
            }
            set
            {

            }
        }


        private vcard m_objvCard = new vcard();

        public vcard vCard
        {
            get { return m_objvCard; }
            set 
            { 
                m_objvCard = value; 

                /// See if we have a new photo
                /// 
                if ((m_objvCard.Photo != null) && (m_objvCard.Photo.Bytes != null))
                {
                    /// compute the hash, save the file, set the image
                    /// 
                    string strHash = XMPPClient.AvatarStorage.WriteAvatar(m_objvCard.Photo.Bytes);
                    this.AvatarImagePath = strHash;
                    XMPPClient.AvatarStorage.SetJIDHash(this.JID, strHash);
                }
            }
        }


        string m_strLastFullJIDToGetMessageFrom = null;
        public JID LastFullJIDToGetMessageFrom
        {
            get
            {
                if (m_strLastFullJIDToGetMessageFrom != null)
                    return m_strLastFullJIDToGetMessageFrom;

                if (ClientInstances.Count > 0)
                {
                    return ClientInstances[0].FullJID;
                }
                return this.JID;
            }
            set
            {
                m_strLastFullJIDToGetMessageFrom = value;
            }
        }

        public void AddSendTextMessage(TextMessage msg)
        {
            Conversation.AddMessage(msg);
        }
        public void AddRecvTextMessage(TextMessage msg)
        {
            LastFullJIDToGetMessageFrom = msg.From;
            Conversation.AddMessage(msg);
        }

        public void SendChatMessage(string strMessage, MessageSendOption option)
        {
            if (option == MessageSendOption.SendToAll)
            {
                SendChatMessageToAllAvailableInstances(strMessage);
                return;
            }

            TextMessage txtmsg = new TextMessage();
            txtmsg.Received = DateTime.Now;
            txtmsg.From = XMPPClient.JID;

            if (option == MessageSendOption.SendPriority)
               txtmsg.To = this.JID.BareJID;
            else
               txtmsg.To = LastFullJIDToGetMessageFrom;

            txtmsg.Message = strMessage;
            XMPPClient.SendChatMessage(txtmsg);
        }

        public void SendChatMessageToAllAvailableInstances(string strMessage)
        {
            foreach (RosterItemPresenceInstance instance in ClientInstances)
            {
                TextMessage txtmsg = new TextMessage();
                txtmsg.Received = DateTime.Now;
                txtmsg.From = XMPPClient.JID;
                txtmsg.To = instance.FullJID;
                txtmsg.Message = strMessage;
                XMPPClient.SendChatMessage(txtmsg);
            }
        }

   
     
        private string m_strSubscription = "";

        public string Subscription
        {
            get { return m_strSubscription; }
            set
            {
                if (m_strSubscription != value)
                {
                    m_strSubscription = value;
                    FirePropertyChanged("Subscription");
                }
            }
        }

        private PresenceStatus m_objPresence = new PresenceStatus();

        public PresenceStatus Presence
        {
            get { return m_objPresence; }
            set
            {
                if (m_objPresence != value)
                {
                    m_objPresence = value;
                    FirePropertyChanged("Presence");
                }
            }
        }

        public void SetDisconnected()
        {
            lock (m_lockClientInstances)
            {
                m_listClientInstances.Clear();
            }
            Presence.PresenceType = PresenceType.unavailable;
            Presence.PresenceShow = PresenceShow.unknown;
        }

        public void SetPresence(PresenceMessage pres)
        {
            /// Hack for now to show who can do audio
            /// 
            bool bCanDoAudio = false;
            bool IsKnownGoogleClient = false;
            if (pres.Capabilities != null)
            {
                if (pres.Capabilities.Extensions != null)
                {
                    if (pres.Capabilities.Extensions.IndexOf("voice-v1") >= 0)
                        bCanDoAudio = true;
                }

                if (pres.Capabilities.Node != null)
                {
                    if (pres.Capabilities.Node == "http://www.android.com/gtalk/client/caps2")
                        IsKnownGoogleClient = true;
                    else if (pres.Capabilities.Node == "http://talkgadget.google.com/client/caps")
                        IsKnownGoogleClient = true;
                }
            }

            if ((pres.From.Resource != null) && (pres.From.Resource.Length > 0))
            {
                RosterItemPresenceInstance instance = FindInstance(pres.From);
                if (instance != null)
                {
                    instance.Presence = pres.PresenceStatus;
                    if (pres.PresenceStatus.PresenceType == PresenceType.unavailable)
                    {
                        lock (m_lockClientInstances)
                        {
                            m_listClientInstances.Remove(instance);
                        }

                        FirePropertyChanged("ClientInstances");
                        FirePropertyChanged("Name");
                    }
                }
                else
                {
                    instance = new RosterItemPresenceInstance(pres.From);
                    instance.Presence = pres.PresenceStatus;
                    instance.CanClientDoAudio = bCanDoAudio;
                    instance.IsKnownGoogleClient = IsKnownGoogleClient;
                    lock (m_lockClientInstances)
                    {
                        m_listClientInstances.Add(instance);
                    }
                    FirePropertyChanged("ClientInstances");
                    FirePropertyChanged("Name");
                }
            }


            /// Get the precense of the most available and latest client instance
            /// 
            PresenceStatus beststatus = pres.PresenceStatus;
            if (pres.PresenceStatus.PresenceType != PresenceType.available)
            {
                lock (m_lockClientInstances)
                {
                    foreach (RosterItemPresenceInstance instance in m_listClientInstances)
                    {
                        if (instance.Presence.PresenceType == PresenceType.available)
                        {
                            beststatus = instance.Presence;
                            break;
                        }
                    }
                }
            }

            Presence = beststatus;

            
            //System.Diagnostics.Debug.WriteLine(item.ToString());
            XMPPClient.FireListChanged(1);
        }

#if !MONO
        public System.Windows.Visibility AudioVisible
        {
            get 
            {
                lock (m_lockClientInstances)
                {
                    foreach (RosterItemPresenceInstance instance in m_listClientInstances)
                    {
                        if (instance.CanClientDoAudio == true)
                            return Windows.Visibility.Visible;
                    }
                    return Windows.Visibility.Collapsed;
                }
            }
            set { }
         
        }
#endif

        /// <summary>
        /// TODO, make this a generic feature search
        /// </summary>
        /// <returns></returns>
        public RosterItemPresenceInstance FindAudioPresenceInstance()
        {
            lock (m_lockClientInstances)
            {
                foreach (RosterItemPresenceInstance instance in m_listClientInstances)
                {
                    if (instance.CanClientDoAudio == true)
                        return instance;
                }
            }
            return null;
        }

        public RosterItemPresenceInstance FindInstance(JID jid)
        {
            lock (m_lockClientInstances)
            {
                foreach (RosterItemPresenceInstance instance in m_listClientInstances)
                {
                    if (jid.Equals(instance.FullJID) == true)
                        return instance;
                }
            }
            return null;
        }


        object m_lockClientInstances = new object();

#if WINDOWS_PHONE
        private ObservableCollection<RosterItemPresenceInstance> m_listClientInstances = new ObservableCollection<RosterItemPresenceInstance>();
        public ObservableCollection<RosterItemPresenceInstance> ClientInstances
        {
            get { return m_listClientInstances; }
            set { m_listClientInstances = value; }
        }
#elif MONO
        private ObservableCollection<RosterItemPresenceInstance> m_listClientInstances = new ObservableCollection<RosterItemPresenceInstance>();
        public ObservableCollection<RosterItemPresenceInstance> ClientInstances
        {
            get { return m_listClientInstances; }
            set { m_listClientInstances = value; }
        }
#else
        private ObservableCollectionEx<RosterItemPresenceInstance> m_listClientInstances = new ObservableCollectionEx<RosterItemPresenceInstance>();
        public ObservableCollectionEx<RosterItemPresenceInstance> ClientInstances
        {
            get { return m_listClientInstances; }
            set { m_listClientInstances = value; }
        }
#endif



        private string m_strGroup = "Unknown";

        public string Group
        {
            get { return m_strGroup; }
            set 
            {
                if (m_strGroup != value)
                {
                    m_strGroup = value;
                    FirePropertyChanged("Group");
                }
            }
        }


        private List<string> m_listGroups = new List<string>();

        public List<string> Groups
        {
            get { return m_listGroups; }
            set 
            {
                if (m_listGroups != value)
                {
                    m_listGroups = value;
                    FirePropertyChanged("Groups");
                }
            }
        }


        public bool HasLoadedConversation = false;

        private Conversation m_objConversation = new Conversation("null@null");

        public Conversation Conversation
        {
            get { return m_objConversation; }
            set { m_objConversation = value; }
        }

        private bool m_bHasNewMessages = false;

        public bool HasNewMessages
        {
            get { return m_bHasNewMessages; }
            set
            {
                if (m_bHasNewMessages != value)
                {
                    m_bHasNewMessages = value;
                    FirePropertyChanged("HasNewMessages");
                    FirePropertyChanged("NewMessagesVisible");
                }
            }
        }

#if !MONO
        public System.Windows.Visibility NewMessagesVisible
        {
            get
            {
                if (m_bHasNewMessages == true)
                    return System.Windows.Visibility.Visible;
                else
                    return System.Windows.Visibility.Collapsed;
            }
            set { }
        }
#endif

        private string m_strImagePath = null;

        public string AvatarImagePath
        {
            get { return m_strImagePath; }
            set 
            {
                if (m_strImagePath != value)
                {
                    m_strImagePath = value;
                    if (m_strImagePath != null)
                    {
                        XMPPClient.AvatarStorage.SetJIDHash(this.JID, m_strImagePath);
                        FirePropertyChanged("Avatar");
                    }
                }
                else
                {
                }
            }
        }

#if !MONO
        /// <summary>
        /// Must keep this bitmapimage as a class member or it won't appear.  Not sure why it's going out of scope
        /// when it should be referenced by WPF
        /// </summary>
        System.Windows.Media.Imaging.BitmapImage OurImage = null;
        public System.Windows.Media.ImageSource Avatar
        {
            get
            {

                if (m_strImagePath != null)
                    OurImage = XMPPClient.AvatarStorage.GetAvatarImage(m_strImagePath);

                if (OurImage == null)
                {
                    Uri uri  = null;
                    uri = new Uri("Avatars/darkavatar.png", UriKind.Relative);
                    OurImage = new System.Windows.Media.Imaging.BitmapImage(uri);
                }


                return OurImage;
            }
        }
#endif


        public rosteritem Node { get; set; }

        #region INotifyPropertyChanged Members

        void FirePropertyChanged(string strName)
        {
            if (PropertyChanged != null)
            {
#if WINDOWS_PHONE
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(PropertyChanged, this, new System.ComponentModel.PropertyChangedEventArgs(strName));
#elif MONO
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(strName));
#else
                System.ComponentModel.PropertyChangedEventArgs args = new System.ComponentModel.PropertyChangedEventArgs(strName);
                System.ComponentModel.PropertyChangedEventHandler eventHandler = PropertyChanged;
                if (eventHandler == null)
                    return;

                Delegate[] delegates = eventHandler.GetInvocationList();
                // Walk thru invocation list
                foreach (System.ComponentModel.PropertyChangedEventHandler handler in delegates)
                {
                    DispatcherObject dispatcherObject = handler.Target as DispatcherObject;
                    // If the subscriber is a DispatcherObject and different thread
                    if (dispatcherObject != null && dispatcherObject.CheckAccess() == false)
                    {
                        // Invoke handler in the target dispatcher's thread
                        dispatcherObject.Dispatcher.Invoke(DispatcherPriority.DataBind, handler, this, args);
                    }
                    else // Execute handler as is
                        handler(this, args);
                }
                //System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(PropertyChanged, this, new System.ComponentModel.PropertyChangedEventArgs(strName));
#endif
            }
        }
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged = null;

        #endregion
    }

    public class RosterItemList : System.Collections.Specialized.INotifyCollectionChanged, IEnumerable<RosterItem>
    {
        public RosterItemList()
            : base()
        {
        }

        protected List<RosterItem> items = new List<RosterItem>();

        public void Add(RosterItem item)
        {
            items.Add(item);
#if WINDOWS_PHONE
            FireCollectionChanged(this, new Collections.Specialized.NotifyCollectionChangedEventArgs(Collections.Specialized.NotifyCollectionChangedAction.Add, item, -1));
#else
            FireCollectionChanged(this, new Collections.Specialized.NotifyCollectionChangedEventArgs(Collections.Specialized.NotifyCollectionChangedAction.Add, item));
#endif
        }

        public void Clear()
        {
            if (items.Count > 0)
            {
                items.Clear();
                FireCollectionChanged(this, new Collections.Specialized.NotifyCollectionChangedEventArgs(Collections.Specialized.NotifyCollectionChangedAction.Reset));
            }
        }

        public void Remove(RosterItem item)
        {
            int nPos = items.IndexOf(item);
            items.Remove(item);
            FireCollectionChanged(this, new Collections.Specialized.NotifyCollectionChangedEventArgs(Collections.Specialized.NotifyCollectionChangedAction.Remove, item, nPos));
        }

        void FireCollectionChanged(object obj, Collections.Specialized.NotifyCollectionChangedEventArgs args)
        {
            if (CollectionChanged != null)
            {

#if WINDOWS_PHONE
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(new System.Collections.Specialized.NotifyCollectionChangedEventHandler(SafeFireCollectionChanged), this, args); 
#elif MONO
                CollectionChanged(obj, args);
#else

                System.Collections.Specialized.NotifyCollectionChangedEventHandler eventHandler = CollectionChanged;
                if (eventHandler == null)
                    return;

                Delegate[] delegates = eventHandler.GetInvocationList();
                // Walk thru invocation list
                foreach (System.Collections.Specialized.NotifyCollectionChangedEventHandler handler in delegates)
                {
                    DispatcherObject dispatcherObject = handler.Target as DispatcherObject;
                    // If the subscriber is a DispatcherObject and different thread
                    if (dispatcherObject != null && dispatcherObject.CheckAccess() == false)
                    {
                        // Invoke handler in the target dispatcher's thread
                        dispatcherObject.Dispatcher.Invoke(DispatcherPriority.DataBind, handler, this, args);
                    }
                    else // Execute handler as is
                        handler(this, args);
                }
                //System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(new System.Collections.Specialized.NotifyCollectionChangedEventHandler(SafeFireCollectionChanged),
                //    this, args); 
#endif

            }
        }

    
        void SafeFireCollectionChanged(object obj, Collections.Specialized.NotifyCollectionChangedEventArgs args)
        {
            CollectionChanged(obj, args);
        }

        public RosterItem FindRosterItemHandle(string strHandle)
        {
            foreach (RosterItem item in items)
            {
                if (strHandle == item.Name)
                    return item;
            }

            return null;
        }

        public RosterItem FindRosterItem(JID jid)
        {
            foreach (RosterItem item in items)
            {
                if (jid.BareJID == item.JID.BareJID)
                    return item;
            }

            return null;
        }

        public RosterItem[] ToArray()
        {
            return items.ToArray();
        }


        public int Count
        {
            get
            {
                return items.Count;
            }
        }

        public RosterItem this[int nIndex]
        {
            get
            {
                return items[nIndex];
            }
            set
            {
                items[nIndex] = value;
            }
        }

        #region INotifyCollectionChanged Members

        public event Collections.Specialized.NotifyCollectionChangedEventHandler CollectionChanged = null;

        #endregion

        #region IEnumerable<RosterItem> Members

        public IEnumerator<RosterItem> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        Collections.IEnumerator Collections.IEnumerable.GetEnumerator()
        {
            return items.GetEnumerator();
        }

        #endregion

    }
}
