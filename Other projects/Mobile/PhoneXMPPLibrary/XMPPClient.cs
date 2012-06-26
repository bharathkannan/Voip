/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Net;

using SocketServer;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

using System.Runtime.Serialization;
using System.Collections.ObjectModel;

#if !MONO
using System.Windows.Threading;
#endif


namespace System.Net.XMPP
{
    public enum XMPPState
    {
        Unknown = 0,
        Connecting = 1,
        Connected = 2,
        Authenticating = 3,
        Authenticated = 4,
        AuthenticationFailed= 5,
        CanBind = 6,
        Binding = 7,
        Bound = 8,
        Ready = 9,
    }

    /// <summary>
    /// Don't seem to have nullable types, so use this for questions
    /// </summary>
    public enum Answer
    {
        Yes,
        No, 
        NoResponse,
    }

    public delegate void DelegateRosterItemAction(RosterItem item, XMPPClient client);

    public class XMPPClient : System.ComponentModel.INotifyPropertyChanged
    {
        public XMPPClient(SocketServer.ILogInterface loginterface) : this()
        {
            LogInterface = loginterface;
        }

        public XMPPClient()
        {
            StreamNegotiationLogic = new StreamNegotiationLogic(this);
            GenericIQLogic = new GenericIQLogic(this);
            RosterLogic = new RosterLogic(this);
            PresenceLogic = new PresenceLogic(this);
            GenericMessageLogic = new GenericMessageLogic(this);
            ServiceDiscoveryLogic = new ServiceDiscoveryLogic(this);
            JingleSessionManager = new Jingle.JingleSessionManager(this);
            StreamInitiationAndTransferLogic = new StreamInitiationAndTransferLogic(this);
            PersonalEventingLogic = new PersonalEventingLogic(this);

            lock (LogicLock)
            {
                ActiveServices.Add(StreamNegotiationLogic); /// Handle SASL authentication
                ActiveServices.Add(GenericMessageLogic);
                ActiveServices.Add(GenericIQLogic); /// Handle pings and other common messages
                ActiveServices.Add(RosterLogic); /// Handles getting our roster
                ActiveServices.Add(PresenceLogic);
                ActiveServices.Add(ServiceDiscoveryLogic);
                ActiveServices.Add(JingleSessionManager);
                ActiveServices.Add(StreamInitiationAndTransferLogic);
                ActiveServices.Add(PersonalEventingLogic);
            }

            this.OurServiceDiscoveryFeatureList.AddFeature(new feature("http://jabber.org/protocol/disco#items"));
            this.OurServiceDiscoveryFeatureList.AddFeature(new feature("http://jabber.org/protocol/disco#info"));
            this.OurServiceDiscoveryFeatureList.AddFeature(new feature("jabber:x:data"));

            // Jingle features
            this.OurServiceDiscoveryFeatureList.AddFeature(new feature("urn:xmpp:jingle:1"));
            this.OurServiceDiscoveryFeatureList.AddFeature(new feature("urn:xmpp:jingle:transports:ice-udp:0")); // not sure if we want ICE, makes the simple protocol difficult
            this.OurServiceDiscoveryFeatureList.AddFeature(new feature("urn:xmpp:jingle:transports:ice-udp:1"));
            this.OurServiceDiscoveryFeatureList.AddFeature(new feature("urn:xmpp:jingle:transports:raw-udp:0")); // raw udp much simpler
            this.OurServiceDiscoveryFeatureList.AddFeature(new feature("urn:xmpp:jingle:transports:raw-udp:1"));
            
            this.OurServiceDiscoveryFeatureList.AddFeature(new feature("urn:xmpp:jingle:apps:rtp:1"));
            this.OurServiceDiscoveryFeatureList.AddFeature(new feature("urn:xmpp:jingle:apps:rtp:audio"));
            this.OurServiceDiscoveryFeatureList.AddFeature(new feature("urn:xmpp:jingle:apps:rtp:video"));

            /// si features... not sure about these
            this.OurServiceDiscoveryFeatureList.AddFeature(new feature("http://jabber.org/protocol/si"));
            this.OurServiceDiscoveryFeatureList.AddFeature(new feature("http://jabber.org/protocol/si/profile/file-transfer"));
            this.OurServiceDiscoveryFeatureList.AddFeature(new feature("http://jabber.org/protocol/ibb"));
            this.OurServiceDiscoveryFeatureList.AddFeature(new feature("http://jabber.org/protocol/bytestreams"));


            this.OurServiceDiscoveryFeatureList.AddFeature(new feature("http://jabber.org/protocol/geoloc"));
            this.OurServiceDiscoveryFeatureList.AddFeature(new feature("http://jabber.org/protocol/geoloc+notify"));
            this.OurServiceDiscoveryFeatureList.AddFeature(new feature("http://jabber.org/protocol/tune"));
            this.OurServiceDiscoveryFeatureList.AddFeature(new feature("http://jabber.org/protocol/tune+notify"));
            this.OurServiceDiscoveryFeatureList.AddFeature(new feature("urn:xmpp:avatar:data"));
            this.OurServiceDiscoveryFeatureList.AddFeature(new feature("urn:xmpp:avatar:metadata"));
            //this.OurServiceDiscoveryFeatureList.AddFeature(new feature(""));

            FileTransferManager = new FileTransferManager(this);

        }

        void LogMessage(string strMsg, params object [] msgparams)
        {
            if (LogInterface != null)
                LogInterface.LogMessage(this.JID, MessageImportance.Medium, strMsg, msgparams);
            else
               System.Diagnostics.Debug.WriteLine(strMsg, msgparams);
        }

        protected ILogInterface LogInterface = null;

        private StreamNegotiationLogic StreamNegotiationLogic = null;
        private GenericIQLogic GenericIQLogic = null;
        private RosterLogic RosterLogic = null;
        public PresenceLogic PresenceLogic = null;
        private GenericMessageLogic GenericMessageLogic = null;
        private ServiceDiscoveryLogic ServiceDiscoveryLogic = null;
        public Jingle.JingleSessionManager JingleSessionManager = null;
        internal StreamInitiationAndTransferLogic StreamInitiationAndTransferLogic = null;
        private PersonalEventingLogic PersonalEventingLogic = null;

        public ServiceDiscoveryFeatureList OurServiceDiscoveryFeatureList = new ServiceDiscoveryFeatureList();
        public ServiceDiscoveryFeatureList ServerServiceDiscoveryFeatureList = new ServiceDiscoveryFeatureList();
     

        public event EventHandler OnStateChanged = null;
        private XMPPState m_eXMPPState = XMPPState.Unknown;

        private XMPPAccount m_objXMPPAccount = new XMPPAccount();

        public XMPPAccount XMPPAccount
        {
            get { return m_objXMPPAccount; }
            set 
            { 
                m_objXMPPAccount = value;
                m_objXMPPAccount.HaveSuccessfullyConnectedAndAuthenticated = false;


                AvatarStorage.AccountFolder = this.JID.BareJID.Replace("@", "").Replace(" ", "").Replace("/", "_").Replace("\\", ""); ;
                this.AvatarImagePath = this.AvatarStorage.GetLastHashForJID(this.JID);

                FirePropertyChanged("XMPPAccount");
            }
        }


        private bool m_bAutoQueryServerFeatures = true;
        /// <summary>
        /// Automatically ask the server for the services it supports (service discovery)
        /// </summary>
        public bool AutoQueryServerFeatures
        {
            get { return m_bAutoQueryServerFeatures; }
            set { m_bAutoQueryServerFeatures = value; }
        }

        private System.Threading.Timer m_objReconnectTimer = null;
        private int m_nAutoReconnectAttempts = 0;
        private bool m_bAutoReconnect = false;
        /// <summary>
        /// Set to true if you want this client to attempt to auto-reconnect to the last good server if disconnected
        /// </summary>
        public bool AutoReconnect
        {
            get { return m_bAutoReconnect; }
            set { m_bAutoReconnect = value; }
        }

        void StartAutoReconnectTimer()
        {
            int DueTimeMs = 15000;
            if (m_nAutoReconnectAttempts > 2)
                DueTimeMs = 30000;
            else if (m_nAutoReconnectAttempts > 4)
                DueTimeMs = 60000;
            else if (m_nAutoReconnectAttempts > 6)
                DueTimeMs = 120000;
            if (m_nAutoReconnectAttempts > 8)
                DueTimeMs = 300000;
            m_nAutoReconnectAttempts++;
            
            LogMessage("XMPP Client attempting reconnect in {0} ms", DueTimeMs);
            m_objReconnectTimer = new System.Threading.Timer(new System.Threading.TimerCallback(OnTimeTryReconnect), this, DueTimeMs, System.Threading.Timeout.Infinite);
        }

        /// TODO.. add a method so the user can interactively prevent re-connects

        void OnTimeTryReconnect(object obj)
        {
            m_objReconnectTimer = null;
            if ( (XMPPConnection == null) || (XMPPConnection.Connected == false))
               Connect();
        }



        /// <summary>
        /// The XMPP State machine.  SHould be 'Ready' before message are exchanged by external programs
        /// </summary>
        public XMPPState XMPPState
        {
            get { return m_eXMPPState; }
            set 
            {
                if (m_eXMPPState != value)
                {
                    m_eXMPPState = value;
                    LogMessage("Setting state to {0}", m_eXMPPState);

                    StateChanged();
                }
            }
        }


        public bool UseTLS
        {
            get { return m_objXMPPAccount.UseTLSMethod; }
            set { m_objXMPPAccount.UseTLSMethod = value; }
        }

        public bool UseOldStyleTLS
        {
            get { return m_objXMPPAccount.UseOldSSLMethod; }
            set { m_objXMPPAccount.UseOldSSLMethod = value; }
        }

        private bool m_bRetrieveRoster = true;

        public bool RetrieveRoster
        {
            get { return m_bRetrieveRoster; }
            set { m_bRetrieveRoster = value; }
        }

        protected virtual void StateChanged()
        {
            // Fire state changed event
            if (OnStateChanged != null)
                OnStateChanged(this, new EventArgs());

            if (XMPPState == System.Net.XMPP.XMPPState.CanBind)
            {
                Ready = false;
                // We can now bind to our resource
                GenericIQLogic.Start();
            }
            else if (XMPPState == System.Net.XMPP.XMPPState.Bound)
            {
                Ready = false;
                if (RetrieveRoster == true)
                   RosterLogic.Start();

                if (AutoQueryServerFeatures == true)
                {
                    ServiceDiscoveryLogic.QueryServiceInfo();
                    ServiceDiscoveryLogic.QueryServiceItems();
                }

                XMPPState = System.Net.XMPP.XMPPState.Ready;
            }
            else if (XMPPState == System.Net.XMPP.XMPPState.Unknown)  // We be logged out
            {
                Ready = false;
                this.StreamNegotiationLogic.Reset();
                this.ServerServiceDiscoveryFeatureList.Features.Clear();
                if (this.XMPPAccount.LastPrescence == null)
                    this.XMPPAccount.LastPrescence = new PresenceStatus();

                this.XMPPAccount.LastPrescence.PresenceType = PresenceType.unavailable;
                this.XMPPAccount.LastPrescence.PresenceShow = PresenceShow.xa;
                FirePropertyChanged("PresenceStatus");

                foreach (RosterItem item in RosterItems)
                {
                    item.SetDisconnected();
                }
            }
            else if (XMPPState == System.Net.XMPP.XMPPState.Ready)
            {
                Ready = true;
                XMPPAccount.HaveSuccessfullyConnectedAndAuthenticated = true;
                ConnectHandle.Set();
                if (this.AutomaticallyDownloadAvatars == true)
                    PresenceLogic.RequestOurVCARD();
            }
            else
            {
                Ready = false;
            }

            if (XMPPState == System.Net.XMPP.XMPPState.Ready)
                ConnectHandle.Set();
            else
                ConnectHandle.Reset();

            FirePropertyChanged("XMPPState");
        }

       
        public event EventHandler OnRetrievedRoster = null;

        internal void FireGotRoster()
        {
            if (OnRetrievedRoster != null)
                OnRetrievedRoster(this, new EventArgs());

            FireListChanged(1);

            this.XMPPAccount.LastPrescence.PresenceType = PresenceType.available;
            this.XMPPAccount.LastPrescence.PresenceShow = PresenceShow.chat;
            this.XMPPAccount.LastPrescence.Status = "online";
            UpdatePresence();
        }


        public void UpdatePresence()
        {
            if (this.XMPPAccount.LastPrescence.IsDirty == true)
            {
                PresenceLogic.SetPresence(this.XMPPAccount.LastPrescence, this.XMPPAccount.Capabilities, this.AvatarImagePath);
                this.XMPPAccount.LastPrescence.IsDirty = false;
            }
        }

        public void AddToRoster(JID jid, string strName, string strGroup)
        {
            RosterLogic.AddToRoster(jid, strName, strGroup);
            PresenceLogic.SubscribeToPresence(jid);
        }

        public RosterItem FindRosterItem(JID jid)
        {
            return RosterItems.FindRosterItem(jid);
        }

        public RosterItem FindRosterItemHandle(string strHandle)
        {
            return RosterItems.FindRosterItemHandle(strHandle);
        }


        private RosterItemList m_listRosterItems = new RosterItemList();
        public RosterItemList RosterItems
        {
            get { return m_listRosterItems; }
            protected set { m_listRosterItems = value; }
        }

        public void AsyncFireListChanged()
        {
            System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(FireListChanged), 1);
        }

        public event EventHandler OnRosterItemsChanged = null;

        public void FireListChanged(object objnull)
        {
            FirePropertyChanged("RosterItems");
            if (OnRosterItemsChanged != null)
               OnRosterItemsChanged(this, new EventArgs());
        }



        /// <summary>
        /// The XMPP Server IP or hostname to connect to - different from domain
        /// </summary>
        /// 
        public string Server
        {
            get { return m_objXMPPAccount.Server; }
            set { m_objXMPPAccount.Server = value; }
        }

        /// <summary>
        /// The XMPP Server domain
        /// </summary>
        /// 
        
        public string Domain
        {
            get { return JID.Domain; }
            set
            {
                if (m_objXMPPAccount.JID.Domain != value)
                {
                    m_objXMPPAccount.JID.Domain = value;
                    FirePropertyChanged("Domain");
                    FirePropertyChanged("JID");
                }
            }
        }

        public int Port
        {
            get { return m_objXMPPAccount.Port; }
            set { m_objXMPPAccount.Port = value; }
        }

        
        public string UserName
        {
            get { return JID.User; }
            set
            {
                if (m_objXMPPAccount.JID.User != value)
                {
                    m_objXMPPAccount.JID.User = value;
                    FirePropertyChanged("UserName");
                    FirePropertyChanged("JID");
                }
            }
        }
        
        public string Password
        {
            get { return m_objXMPPAccount.Password; }
            set { m_objXMPPAccount.Password = value; }
        }

        public string Resource
        {
            get { return m_objXMPPAccount.JID.Resource; }
            set
            {
                if (m_objXMPPAccount.JID.Resource != value)
                {
                    m_objXMPPAccount.JID.Resource = value;
                    FirePropertyChanged("Resource");
                    FirePropertyChanged("JID");
                }
            }
        }

        internal XMPPConnection XMPPConnection = null;


        public JID JID
        {
            get { return m_objXMPPAccount.JID; }
            set 
            {
                if (m_objXMPPAccount.JID != value)
                {
                    m_objXMPPAccount.JID = value;
                    FirePropertyChanged("JID");
                }
            }
        }

        public PresenceStatus PresenceStatus
        {
            get { return XMPPAccount.LastPrescence; }
            set 
            {
                if (XMPPAccount.LastPrescence != value)
                {
                    XMPPAccount.LastPrescence = value;
                    FirePropertyChanged("PresenceStatus");
                }
            }
        }

        private bool m_bAutoAcceptPresence = false;

        public bool AutoAcceptPresenceSubscribe
        {
            get { return m_bAutoAcceptPresence; }
            set { m_bAutoAcceptPresence = value; }
        }

        public delegate void DelegateShouldSubscribeUser(PresenceMessage pres);
        public event DelegateShouldSubscribeUser OnUserSubscriptionRequest = null;

        internal Answer ShouldSubscribeUser(PresenceMessage pres)
        {
            if (AutoAcceptPresenceSubscribe == true)
                return Answer.Yes;

            // TODO... should automatically let users see our presence if we added them to our roster then subscribed to them
            // (In other words, called AddToRoster(...))

            if (OnUserSubscriptionRequest != null)
               OnUserSubscriptionRequest(pres);

            // Dispatcher calls can't return in object any more, so the user will have to call back with
            // their response
            return Answer.NoResponse;
        }

        public void AcceptUserPresence(PresenceMessage pres, string strNickName, string strGroup)
        {
            PresenceLogic.AcceptUserPresence(pres, strNickName, strGroup);
        }
        public void DeclineUserPresence(PresenceMessage pres)
        {
            PresenceLogic.DeclineUserPresence(pres);
        }

        internal void FireConnectAttemptFinished(bool bConnected)
        {
            if (bConnected == true)
            {
                m_nAutoReconnectAttempts = 0;

            }
            else
            {
                if ( (AutoReconnect == true) &&  (XMPPAccount.HaveSuccessfullyConnectedAndAuthenticated == true) )
                {
                    StartAutoReconnectTimer();
                }
            }
        }

        public event EventHandler OnServerDisconnect = null;
        internal void FireDisconnectedFromServer()
        {
            if (OnServerDisconnect != null)
                OnServerDisconnect(this, new EventArgs());

            if ((AutoReconnect == true) && (XMPPAccount.HaveSuccessfullyConnectedAndAuthenticated == true))
            {
                StartAutoReconnectTimer();
            }
        }

        public void Connect()
        {
            Connect(null);
        }

        public void Connect(ILogInterface log)
        {
            LogMessage("Calling XMPPClient::Connect()");
            string strCurrentAccountName = this.JID.BareJID.Replace("@", "").Replace(" ", "").Replace("/", "_").Replace("\\", "");
            AvatarStorage.AccountFolder = strCurrentAccountName;
            if (this.XMPPAccount.AccountName == null)
                this.XMPPAccount.AccountName = strCurrentAccountName;

            this.RosterItems.Clear();

            XMPPConnection = new XMPPConnection(this, log);
            XMPPConnection.OnStanzaReceived += new System.Net.XMPP.XMPPConnection.DelegateStanza(XMPPConnection_OnStanzaReceived);
            XMPPConnection.Connect();
        }

        public System.Threading.ManualResetEvent ConnectHandle = new System.Threading.ManualResetEvent(false);

        public void Disconnect()
        {
            LogMessage("Calling XMPPClient::Disconnect()");
            if (XMPPConnection != null)
            {
                XMPPConnection.Disconnect();
                XMPPConnection = null;
            }
        }

        public bool Ready
        {
            get
            {
                if (this.XMPPState == XMPP.XMPPState.Ready)
                    return true;

                return false;
            }
            set
            {
                FirePropertyChanged("Ready");
            }
        }

        public bool Connected
        {
            get
            {
                if (XMPPConnection == null)
                    return false;
                return XMPPConnection.Connected;
            }
            set
            { 
            }
        }

        public void SendRawXML(string strXML)
        {
            try
            {
                XMPPConnection.Send(strXML);
            }
            catch (Exception ex)
            {
                LogMessage("Exception sending raw XML: {0}", ex);
                throw new Exception("Exception sending raw XML", ex);
            }
        }

        /// <summary>
        /// Sends an iq, message, or presence message
        /// </summary>
        /// <param name="iq"></param>
        public void SendXMPP(XMPPMessageBase iq)
        {
            try
            {
                XMPPConnection.Send(iq.MessageXML);
            }
            catch (Exception ex)
            {
                LogMessage("Exception sending XMPP class: {0}", ex);
                throw new Exception("Exception sending XMPP class", ex);
            }
        }

        public void SendObject(object objXMLSerializable)
        {
            try
            {
                XMPPConnection.Send(Utility.GetXMLStringFromObject(objXMLSerializable));
            }
            catch (Exception ex)
            {
                LogMessage("Exception sending object: {0}", ex);
                throw new Exception("Exception sending object", ex);
            }
        }

        /// <summary>
        /// Adds a logic handler to our list so it will receive IQ and message events
        /// </summary>
        /// <param name="log"></param>
        public void AddLogic(Logic log)
        {
            lock (LogicLock)
            {
                ActiveServices.Add(log);
            }
        }

        /// <summary>
        /// Removes a logic handler from our list.  Logic handlers are automatically removed if they set their state to completed
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        public bool RemoveLogic(Logic log)
        {
            bool bRet = false;
            lock (LogicLock)
            {
                if (ActiveServices.Contains(log) == true)
                {
                    ActiveServices.Remove(log);
                    bRet = true;
                }
            }
            return bRet;
        }

        public Logic FindService(Type type)
        {
            lock (LogicLock)
            {
                foreach (Logic log in ActiveServices)
                {
                    if (log.GetType() == type)
                        return log;
                }
            }
            return null;
        }

        public IQ SendRecieveIQ(IQ iq, int nTimeoutMS)
        {
            return SendRecieveIQ(iq, nTimeoutMS, SerializationMethod.MessageXMLProperty);
        }
        /// <summary>
        /// Sends an IQ and returns the response.  This is called from separate thread since it waits
        /// </summary>
        /// <param name="iq">The IQ to send</param>
        /// <param name="nTimeoutMS">The timeout in ms to wait for a areponse</param>
        /// <returns>The IQ that was received, or null if one was not received within the timeout period</returns>
        public IQ SendRecieveIQ(IQ iq, int nTimeoutMS, SerializationMethod method)
        {
            SendRecvIQLogic iqlog = new SendRecvIQLogic(this, iq);
            iqlog.SerializationMethod = method;
            AddLogic(iqlog);

            iqlog.SendReceive(nTimeoutMS);

            RemoveLogic(iqlog);

            return iqlog.RecvIQ;
        }

        /// <summary>
        /// Sends a chat message to a user
        /// </summary>
        /// <param name="msg"></param>
        public void SendChatMessage(string strMessage, JID jidto)
        {
            TextMessage txtmsg = new TextMessage();
            txtmsg.Received = DateTime.Now;
            txtmsg.From = JID;
            txtmsg.To = jidto;
            txtmsg.Message = strMessage;
            GenericMessageLogic.SendChatMessage(txtmsg);
        }

        internal void SendChatMessage(TextMessage msg)
        {
            GenericMessageLogic.SendChatMessage(msg);
        }

        public ServiceDiscoveryIQ QueryServiceDiscovery(JID jidto, string strNode)
        {
            ServiceDiscoveryIQ iqrequest = new ServiceDiscoveryIQ();
            iqrequest.From = this.JID;
            iqrequest.To = jidto;
            iqrequest.Type = IQType.get.ToString();
            iqrequest.ServiceDiscoveryItemQuery = new ServiceDiscoveryItemQuery();
            iqrequest.ServiceDiscoveryItemQuery.Node = strNode;

            IQ iqresponse = SendRecieveIQ(iqrequest, 10000);
            if (iqresponse is ServiceDiscoveryIQ)
            {
                return iqresponse as ServiceDiscoveryIQ;

            }

            return null;
        }

        public delegate void DelegateNewConversationItem(RosterItem item, bool bReceived, TextMessage msg);
        public event DelegateNewConversationItem OnNewConversationItem = null;
        internal void FireNewConversationItem(RosterItem item, bool bReceived, TextMessage msg)
        {
            if (OnNewConversationItem != null)
                OnNewConversationItem(item, bReceived, msg);
        }

        public delegate void DelegateConversationState(RosterItem rosteritem, ConversationState newstate);
        public event DelegateConversationState OnConversationStateChanged = null;
        internal void FireNewConversationState(RosterItem rosteritem, ConversationState newstate)
        {
            if (OnConversationStateChanged != null)
                OnConversationStateChanged(rosteritem, newstate);
        }

        private XMPPMessageFactory m_objXMPPMessageFactory = new XMPPMessageFactory();

        public XMPPMessageFactory XMPPMessageFactory
        {
            get { return m_objXMPPMessageFactory; }
        }

        protected virtual void XMPPConnection_OnStanzaReceived(XMPPStanza stanza, object objFrom)
        {
            bool bHandled = false;
            /// See if any of our handlers 
            /// 
            List<Logic> RemoveList = new List<Logic>();
            lock (LogicLock)
            {
                foreach (Logic log in ActiveServices)
                {
                    bHandled = log.NewXMLFragment(stanza);
                    if (log.IsCompleted == true)
                        RemoveList.Add(log);
                    if (bHandled == true)
                        break;
                    
                }

                foreach (Logic log in RemoveList)
                    ActiveServices.Remove(log);
            }

            if (bHandled == true)
                return;

            /// Now see if this stanza is a higher level IQ or message
            /// 
            try
            {
               XElement elem = XElement.Parse(stanza.XML);
               XMPPMessageBase msg = null;
               if (elem.Name == "iq")
               {
                   msg = XMPPMessageFactory.BuildIQ(elem, stanza.XML);
               }
               else if (elem.Name == "message")
               {
                   msg = XMPPMessageFactory.BuildMessage(elem, stanza.XML);
               }
               else if (elem.Name == "presence")
               {
                   msg = XMPPMessageFactory.BuildPresence(elem, stanza.XML);
                   //msg = new PresenceMessage(stanza.XML);
               }
                   /// TODO.. log IQ, MESSAGE or PRESENCE event, maybe have an event handler
                   /// 
                if (msg != null)
                {
                    if (msg is IQ)
                        bHandled = OnIQ(msg as IQ);
                    else if (msg is Message)
                        bHandled = OnMessage(msg as Message);
                    else if (msg is PresenceMessage)
                        bHandled = OnPresence(msg as PresenceMessage);
               }
            }
            catch(Exception  ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }

            if (bHandled == true)
                return;

            
        }

        protected virtual bool OnIQ(IQ iq)
        {
            bool bHandled = false;
            Logic[] LogicList = null;
            List<Logic> RemoveList = new List<Logic>();

            lock (LogicLock)
            {
                LogicList = ActiveServices.ToArray();
            }

            foreach (Logic log in LogicList)
            {
                bHandled = log.NewIQ(iq);

                if (log.IsCompleted == true)
                    RemoveList.Add(log);
                if (bHandled == true)
                    break;

            }

            lock (LogicLock)
            {
                foreach (Logic log in RemoveList)
                {
                    if (ActiveServices.Contains(log) == true)
                        ActiveServices.Remove(log);
                }
            }
            return bHandled;
        }

        protected virtual bool OnMessage(Message msg)
        {
            bool bHandled = false;
            Logic[] LogicList = null;
            List<Logic> RemoveList = new List<Logic>();

            lock (LogicLock)
            {
                LogicList = ActiveServices.ToArray();
            }

            foreach (Logic log in LogicList)
            {
                bHandled = log.NewMessage(msg);

                if (log.IsCompleted == true)
                    RemoveList.Add(log);
                if (bHandled == true)
                    break;

            }

            lock (LogicLock)
            {
                foreach (Logic log in RemoveList)
                {
                    if (ActiveServices.Contains(log) == true)
                        ActiveServices.Remove(log);
                }
            }
            return bHandled;
        }

        protected virtual bool OnPresence(PresenceMessage pres)
        {
            bool bHandled = false;
            Logic[] LogicList = null;
            List<Logic> RemoveList = new List<Logic>();

            lock (LogicLock)
            {
                LogicList = ActiveServices.ToArray();
            }

            foreach (Logic log in LogicList)
            {
                bHandled = log.NewPresence(pres);

                if (log.IsCompleted == true)
                    RemoveList.Add(log);
                if (bHandled == true)
                    break;
            }

            lock (LogicLock)
            {
                foreach (Logic log in RemoveList)
                {
                    if (ActiveServices.Contains(log) == true)
                        ActiveServices.Remove(log);
                }
            }
            return bHandled;
        }

        protected List<Logic> ActiveServices = new List<Logic>();
        protected object LogicLock = new object();


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


        public delegate void DelegateString(XMPPClient client, string strXML);
        public event DelegateString OnXMLSent = null;
        public event DelegateString OnXMLReceived = null;

        internal virtual void FireXMLSent(string strXML)
        {
            System.Diagnostics.Debug.WriteLine("-->" + strXML);

            if (OnXMLSent != null)
                OnXMLSent(this, strXML);
        }

        internal virtual void FireXMLReceived(string strXML)
        {
            System.Diagnostics.Debug.WriteLine("<--" + strXML);

            if (OnXMLReceived != null)
                OnXMLReceived(this, strXML);
        }

        public FileTransferManager FileTransferManager = null;

        private geoloc m_objGeoLocation = new geoloc() { lat = 0.0f, lon = 0.0f };
        string m_strGeoLocation = "Unknown Location";

        public geoloc GeoLocation
        {
            get { return m_objGeoLocation; }
            set 
            { 
                m_objGeoLocation = value; 
            }
        }
        public string GeoLocationString
        {
            get
            {
                return m_strGeoLocation;
            }
            set
            {
                m_strGeoLocation = value;
                FirePropertyChanged("GeoLocationString");
            }
        }            

        public void SetGeoLocation(double fLat, double fLon)
        {
            m_objGeoLocation.lat = fLat;
            m_objGeoLocation.lon = fLon;
            if ((m_objGeoLocation.lat == 0.0f) && (m_objGeoLocation.lon == 0.0f))
                GeoLocationString = "Unknown Location";
            else
                GeoLocationString = string.Format("{0:N3}, {1:N3}", m_objGeoLocation.lat, m_objGeoLocation.lon);

            FirePropertyChanged("GeoLocation");
            
            this.PersonalEventingLogic.PublishGeoInfo(m_objGeoLocation);
        }

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

        public void SetTune(string strTitle, string strArtist)
        {
            this.m_objTune.Title = strTitle;
            this.m_objTune.Artist = strArtist;

            FirePropertyChanged("Tune");
            FirePropertyChanged("TuneString");

            this.PersonalEventingLogic.PublishTuneInfo(this.m_objTune);
        }


        public AvatarStorage AvatarStorage = new AvatarStorage("default");

        public void SetAvatar(byte[] bImageData, int nWidth, int nHeight, string strContentType)
        {
            this.AvatarImagePath = AvatarStorage.WriteAvatar(bImageData);
            this.AvatarStorage.SetJIDHash(this.JID, this.AvatarImagePath);
            /// Need to update our presence with our avatar has
            /// 
            this.PresenceStatus.IsDirty = true; // set manually because only gets dirty on type and status, not hash
            UpdatePresence();

            /// Set this in our save data for our account

            //PersonalEventingLogic.PublishAvatarData(bImageData, nWidth, nHeight);
        }

        public void UpdatevCard()
        {
            /// Update our vcard first, then our photo in our presence
            /// 
            this.PresenceLogic.UdpateVCARD(this.vCard);


            if (this.vCard.Photo != null)
            {
                this.AvatarImagePath = AvatarStorage.WriteAvatar(this.vCard.Photo.Bytes);
                this.AvatarStorage.SetJIDHash(this.JID, this.AvatarImagePath);
                this.PresenceStatus.IsDirty = true; // set manually because only gets dirty on type and status, not hash
                UpdatePresence();
            }

        }

        private bool m_bAutomaticallyDownloadAvatars = true;

        public bool AutomaticallyDownloadAvatars
        {
            get { return m_bAutomaticallyDownloadAvatars; }
            set { m_bAutomaticallyDownloadAvatars = value; }
        }

        // commented out because this uses the old method, and we do it automatically now (If set)
        //public void DownloadAvatar(JID jidfrom, string strItem)
        //{
        //    PersonalEventingLogic.DownloadDataNode(jidfrom, "urn:xmpp:avatar:data", strItem);
        //}

        private vcard m_objvCard = new vcard();

        public vcard vCard
        {
            get { return m_objvCard; }
            set 
            { 
                m_objvCard = value; 
                if ( (m_objvCard != null) && (m_objvCard.Photo != null) && (m_objvCard.Photo.Bytes != null) )
                {
                    string strHash = AvatarStorage.WriteAvatar(m_objvCard.Photo.Bytes);
                    AvatarImagePath = strHash;
                    this.AvatarStorage.SetJIDHash(this.JID, this.AvatarImagePath);
                }
            }
        }

        internal string AvatarImagePath
        {
            get { return m_objXMPPAccount.AvatarHash; }
            set
            {
                if (m_objXMPPAccount.AvatarHash != value)
                {
                    m_objXMPPAccount.AvatarHash = value;
                    if (m_objXMPPAccount.AvatarHash  != null)
                        FirePropertyChanged("Avatar");
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

                if (m_objXMPPAccount.AvatarHash != null)
                    OurImage = AvatarStorage.GetAvatarImage(m_objXMPPAccount.AvatarHash);

                if (OurImage == null)
                {
                    Uri uri = null;
#if WINDOWS_PHONE
                    uri = new Uri("[Application Name];component/Avatars/avatar.png", UriKind.Relative);
#else
                    uri = new Uri("Avatars/avatar.png", UriKind.Relative);
#endif
                    OurImage = new System.Windows.Media.Imaging.BitmapImage(uri);
                }


                return OurImage;
            }
        }

#endif

        public void PublishVCARD()
        {
            /// Could put this in a logic class so it can check the response
            IQ iq = new IQ();
            iq.From = JID;
            iq.To = null;
            iq.Type = IQType.set.ToString();
            iq.InnerXML = Utility.GetXMLStringFromObject(vCard);

            SendXMPP(iq);
        }

    }

    internal class AskObject
    {
       public string RequestId {get; set;}
       public string FileName { get; set; }
       public int FileSize { get; set; }
       public JID from { get; set; }

    }

    internal class ProgressObject
    {
        public string RequestId { get; set; }
        public int Bytes { get; set; }
        public int Total { get; set; }
        public JID from { get; set; }

    }

}
