/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Net;

using System.Collections.Generic;
using System.Xml.Linq;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace System.Net.XMPP.Jingle
{
    [XmlRoot(ElementName = "parameter")]
    public class PayloadParameter
    {
        public PayloadParameter()
        {
        }

        private string m_strName = null;
        [XmlAttribute(AttributeName = "name")]
        public string Name
        {
            get { return m_strName; }
            set { m_strName = value; }
        }

        private string m_strValue = null;
        [XmlAttribute(AttributeName = "value")]
        public string Value
        {
            get { return m_strValue; }
            set { m_strValue = value; }
        }
    }



    // An audio/video payload 
    [XmlRoot(ElementName = "payload-type")]
    public class Payload
    {
        public Payload()
        {
        }

        private int m_nPayloadId = 0;
        [XmlAttribute(AttributeName = "id")]
        public int PayloadId
        {
            get { return m_nPayloadId; }
            set { m_nPayloadId = value; }
        }
        
        private string m_strName = "speex";
        [XmlAttribute(AttributeName = "name")]
        public string Name
        {
            get { return m_strName; }
            set { m_strName = value; }
        }
        
        private string m_strChannels = null;
        [XmlAttribute(AttributeName = "channels")]
        public string Channels
        {
            get { return m_strChannels; }
            set { m_strChannels = value; }
        }

        private string m_strClockRate = "16000";
        [XmlAttribute(AttributeName = "clockrate")]
        public string ClockRate
        {
            get { return m_strClockRate; }
            set { m_strClockRate = value; }
        }

        
        private string m_strPtime = null;
        [XmlAttribute(AttributeName = "ptime")]
        public string Ptime
        {
            get { return m_strPtime; }
            set { m_strPtime = value; }
        }


        private string m_strMaxptime = null;
        [XmlAttribute(AttributeName = "maxptime")]
        public string Maxptime
        {
            get { return m_strMaxptime; }
            set { m_strMaxptime = value; }
        }

        [XmlElement(ElementName="parameter")]
        public List<PayloadParameter> Parameters = new List<PayloadParameter>();
    }

    [XmlRoot(ElementName = "candidate")]
    public class Candidate
    {
        public Candidate()
        {
        }

        [XmlAttribute(AttributeName = "foundation")]
        public string foundation = "0";

        [XmlAttribute(AttributeName = "component")]
        public int component = 1;

        [XmlAttribute(AttributeName = "protocol")]
        public string protocol = "udp";

        [XmlAttribute(AttributeName = "priority")]
        public int priority = 1231245;

        [XmlAttribute(AttributeName = "generation")]
        public int generation = 0;

        [XmlAttribute(AttributeName = "id")]
        public string id = null;

        [XmlAttribute(AttributeName = "ip")]
        public string ipaddress = "";

        [XmlAttribute(AttributeName = "address")]
        public string address
        {
            get
            {
                return ipaddress;
            }
            set
            {
                ipaddress = value;
            }
        }

        [XmlAttribute(AttributeName = "port")]
        public int port = 8080;

        [XmlAttribute(AttributeName = "type")]
        public string type = "host";

        [XmlAttribute(AttributeName = "rel-addr")]
        public string reladdr = null;

        [XmlAttribute(AttributeName = "rel-port")]
        public string relport = null;


        [XmlAttribute(AttributeName = "network")]
        public string network = "0";



        [XmlAttribute(AttributeName = "preference")]
        public string preference = "1";
      
        /// Google talk specific candidates
        [XmlAttribute(AttributeName = "username")]
        public string username = null;

        [XmlAttribute(AttributeName = "password")]
        public string password = null;

        [XmlAttribute(AttributeName = "name")]
        public string name = "rtp";





        [XmlIgnore()]
        public IPEndPoint IPEndPoint = null;

    }

    [XmlRoot(ElementName = "transport")]
    public class Transport
    {
        public Transport()
        {
        }

        [XmlAttribute(AttributeName = "pwd")]
        public string pwd = null;

        [XmlAttribute(AttributeName = "ufrag")]
        public string ufrag = null;

        [XmlElement(ElementName="candidate")]
        public List<Candidate> Candidates = new List<Candidate>();
    }

    [XmlRoot(ElementName = "description")]
    public class Description
    {
        public Description()
        {
        }

        [XmlAttribute(AttributeName="media")]
        public string media = null;

        [XmlElement(ElementName = "payload-type")]
        public List<Payload> Payloads = new List<Payload>();
    }

    [XmlRoot(ElementName = "content")]
    public class Content
    {
        public Content()
        {
        }

        [XmlAttribute(AttributeName="creator")]
        public string Creator = null;
        [XmlAttribute(AttributeName = "name")]
        public string Name = null;

        [XmlElement(ElementName = "description", Namespace = "urn:xmpp:jingle:apps:rtp:1")]
        public Description Description = new Description();

        [XmlElement(ElementName = "transport", Namespace = "urn:xmpp:jingle:transports:ice-udp:1")]
        public Transport ICETransport = null;

        [XmlElement(ElementName = "transport", Namespace = "urn:xmpp:jingle:transports:raw-udp:1")]
        public Transport RawUDPTransport = null;

        [XmlElement(ElementName = "transport", Namespace = "http://www.google.com/transport/p2p")]
        public Transport GoogleTransport = null;

    }

    [XmlRoot(ElementName = "mute")]
    public class Mute
    {
        public Mute()
        {
        }

        [XmlAttribute(AttributeName = "creator")]
        public string Creator = "responder";
        [XmlAttribute(AttributeName = "name")]
        public string Name = "voice";
    }

    public enum JingleStateChange
    {
        None,
        Active,
        Hold,
        Ringing,
        Mute,
        Unmute
    }

    public enum TerminateReason
    {
        Decline,
        Busy,
        Success,
        UnsupportedTransports,
        FailedTransports,

        AlternativeSession,
        Cancel,
        ConnectivityError,
        Expired,
        FailedApplication,
        FailedTransport,
        GeneralError,
        Gone,
        IncompatibleParameters,
        MediaError,
        SecurityError,
        Timeout,
        UnsupportedApplication,
        Unknown,
    }

    public class Reason
    {
        public Reason()
        {
        }
        
        public Reason(TerminateReason reason)
        {
            TerminateReason = reason;
        }

        /// <summary>
        /// The reason the session was terminated.   Have to do this the long way because of lack of choice support on windows phone
        /// </summary>
        /// 
        [XmlIgnore()]
        public TerminateReason TerminateReason
        {
            get 
            {
                if (Success != null) return TerminateReason.Success;
                else if (Busy != null) return TerminateReason.Busy;
                else if (Decline != null) return TerminateReason.Decline;
                else if (UnsupportedTransports != null) return TerminateReason.UnsupportedTransports;
                else if (FailedTransport != null) return TerminateReason.FailedTransports;
                else if (AlternativeSession != null) return TerminateReason.AlternativeSession;
                else if (Cancel != null) return TerminateReason.Cancel;
                else if (ConnectivityError != null) return TerminateReason.ConnectivityError;
                else if (Expired != null) return TerminateReason.Expired;
                else if (FailedApplication != null) return TerminateReason.FailedApplication;
                else if (GeneralError != null) return TerminateReason.GeneralError;
                else if (Gone != null) return TerminateReason.Gone;
                else if (IncompatibleParameters != null) return TerminateReason.IncompatibleParameters;
                else if (MediaError != null) return TerminateReason.MediaError;
                else if (SecurityError != null) return TerminateReason.SecurityError;
                else if (Timeout != null) return TerminateReason.Timeout;
                else if (UnsupportedApplication != null) return TerminateReason.UnsupportedApplication;

                else return TerminateReason.Unknown;
            }
            set 
            {
                Busy = null;
                Decline = null;
                Success = null;
                Unknown = null;
                UnsupportedTransports = null;
                FailedTransport = null;
                AlternativeSession = null;
                Cancel = null;
                ConnectivityError = null;
                Expired = null;
                FailedApplication = null;
                GeneralError = null;
                Gone = null;
                IncompatibleParameters = null;
                MediaError = null;
                SecurityError = null;
                Timeout = null;
                UnsupportedApplication = null;

                if (value == TerminateReason.Success) Success = "";
                else if (value == TerminateReason.Busy) Busy = "";
                else if (value == TerminateReason.Decline) Decline = "";
                else if (value == TerminateReason.UnsupportedTransports) Decline = "";
                else if (value == TerminateReason.FailedTransports) FailedTransport = "";
                else if (value == TerminateReason.AlternativeSession) AlternativeSession = "";
                else if (value == TerminateReason.Cancel) Cancel = "";
                else if (value == TerminateReason.ConnectivityError) ConnectivityError = "";
                else if (value == TerminateReason.Expired) Expired = "";
                else if (value == TerminateReason.FailedApplication) FailedApplication = "";
                else if (value == TerminateReason.GeneralError) GeneralError = "";
                else if (value == TerminateReason.Gone) Gone = "";
                else if (value == TerminateReason.IncompatibleParameters) IncompatibleParameters = "";
                else if (value == TerminateReason.MediaError) MediaError = "";
                else if (value == TerminateReason.SecurityError) SecurityError = "";
                else if (value == TerminateReason.Timeout) Timeout = "";
                else if (value == TerminateReason.UnsupportedApplication) UnsupportedApplication = "";
                else Unknown = "";
            }
        }

        [XmlElement("busy")]
        string Busy = null;
        [XmlElement("decline")]
        string Decline = null;
        [XmlElement("unknown")]
        string Unknown = null;
        [XmlElement("success")]
        string Success = "";
        [XmlElement("unsupported-transports")]
        string UnsupportedTransports = null;
        [XmlElement("failed-transport")]
        string FailedTransport = null;

        [XmlElement("alternative-session")]
        string AlternativeSession = null;
        [XmlElement("failed-cancel")]
        string Cancel = null;
        [XmlElement("connectivity-error")]
        string ConnectivityError = null;
        [XmlElement("expired")]
        string Expired = null;
        [XmlElement("failed-application")]
        string FailedApplication = null;
        [XmlElement("general-error")]
        string GeneralError = null;
        [XmlElement("gone")]
        string Gone = null;
        [XmlElement("incompatible-parameters")]
        string IncompatibleParameters = null;
        [XmlElement("media-error")]
        string MediaError = null;
        [XmlElement("security-error")]
        string SecurityError = null;
        [XmlElement("timeout")]
        string Timeout = null;
        [XmlElement("unsupported-applications")]
        string UnsupportedApplication = null;

    }


    [XmlRoot(ElementName = "jingle", Namespace = "urn:xmpp:jingle:1")]
    public class Jingle
    {
        public Jingle()
        {
        }

        public const string SessionInitiate = "session-initiate";
        public const string Initiate = "initiate";
        public const string SessionAccept = "session-accept";
        public const string Accept = "accept";
        public const string SessionTerminate = "session-terminate";
        public const string Terminate = "terminate";
        public const string ContentAdd = "content-add";
        public const string ContentModify = "content-modify";
        public const string ContentReject = "content-reject";
        public const string ContentAccept = "content-accept";
        public const string DescriptionInfo = "description-info";
        public const string SessionInfo = "session-info";

        public const string TransportInfo = "transport-info";
        public const string TransportAccept = "transport-accept";
        public const string TransportReject = "transport-reject";
        public const string TransportReplace = "transport-replace";

        [XmlAttribute(AttributeName = "action")]
        public string Action = SessionInitiate;

        [XmlAttribute(AttributeName = "initiator")]
        public string Initiator = null;

        [XmlAttribute(AttributeName = "sid")]
        public string SID = null;

        [XmlElement(ElementName = "content")]
        public Content Content = new Content();

        [XmlElement(ElementName = "active", Namespace="urn:xmpp:jingle:apps:rtp:info:1")]
        public string Active = null;

        [XmlElement(ElementName = "hold", Namespace = "urn:xmpp:jingle:apps:rtp:info:1")]
        public string Hold = null;

        [XmlElement(ElementName = "ringing", Namespace = "urn:xmpp:jingle:apps:rtp:info:1")]
        public string Ringing = null;

        [XmlElement(ElementName = "mute", Namespace = "urn:xmpp:jingle:apps:rtp:info:1")]
        public Mute Mute = null;

        [XmlElement(ElementName = "unmute", Namespace = "urn:xmpp:jingle:apps:rtp:info:1")]
        public Mute Unmute = null;

        [XmlIgnore()]
        public JingleStateChange JingleStateChange
        {
            get
            {
                if (Active != null)
                    return JingleStateChange.Active;
                else if (Hold != null)
                    return JingleStateChange.Hold;
                else if (Ringing != null)
                    return JingleStateChange.Ringing;
                else if (Mute != null)
                    return JingleStateChange.Mute;
                else if (Unmute != null)
                    return JingleStateChange.Unmute;
                return JingleStateChange.None;
            }
            set
            {
                Active = null;
                Hold = null;
                Ringing = null;
                Mute = null;
                Unmute = null;

                if (value == XMPP.Jingle.JingleStateChange.Active)
                    Active = "";
                else if (value == XMPP.Jingle.JingleStateChange.Hold)
                    Hold = "";
                else if (value == XMPP.Jingle.JingleStateChange.Mute)
                    Mute = new Mute();
                else if (value == XMPP.Jingle.JingleStateChange.Ringing)
                    Ringing = "";
                else if (value == XMPP.Jingle.JingleStateChange.Unmute)
                    Unmute = new Mute();
            }
        }

        [XmlElement(ElementName="reason")]
        public Reason Reason = null;

    }

     /// <feature var='urn:xmpp:jingle:1'/>    
     /// <feature var='urn:xmpp:jingle:transports:ice-udp:0'/>    
     /// <feature var='urn:xmpp:jingle:transports:ice-udp:1'/>    
     /// <feature var='urn:xmpp:jingle:apps:rtp:1'/>    
     /// <feature var='urn:xmpp:jingle:apps:rtp:audio'/>    
     /// <feature var='urn:xmpp:jingle:apps:rtp:video'/>

    [XmlRoot(ElementName = "iq")]
    public class JingleIQ : IQ
    {
        public JingleIQ()
            : base()
        {
        }
        public JingleIQ(string strXML)
            : base(strXML)
        {
        }

        [XmlElement(ElementName = "jingle", Namespace = "urn:xmpp:jingle:1")]
        public Jingle Jingle = new Jingle();

     
    }

    /// <summary>
    /// Initiates a jingle session, returns and event when done 
    /// </summary>
    public class JingleSessionLogic : Logic
    {
        public JingleSessionLogic(XMPPClient client, string strSessionId, JingleSessionManager mgr)
            : base(client)
        {
            SessionId = strSessionId;
            JingleSessionManager = mgr;
        }

        public string SessionId = null;
        public string RemoteJID = null;
        JingleSessionManager JingleSessionManager = null;

        

        public override bool NewIQ(IQ iq)
        {
            if ((OutgoingRequestMessage != null) && (iq.ID == OutgoingRequestMessage.ID))
            {
                /// Got an ack, analyze it an tell the client what is going on
                IQResponseAction response = new IQResponseAction();
                if (iq.Type != IQType.result.ToString())
                {
                    response.AcceptIQ = false;
                    response.Error = iq.Error;
                }

                JingleSessionManager.FireNewSessionAckReceived(SessionId, response);
                return true;
            }
            if ((AcceptSessionMessage != null) && (iq.ID == AcceptSessionMessage.ID))
            {
                /// Client accept our session

                IQResponseAction response = new IQResponseAction();
                if (iq.Type != IQType.result.ToString())
                {
                    response.AcceptIQ = false;
                    response.Error = iq.Error;
                }

                JingleSessionManager.FireSessionAcceptedAck(SessionId, response);
                return true;
            }
            if ((TransportInfoRequest != null) && (iq.ID == TransportInfoRequest.ID))
            {
                /// Client accept our session

                IQResponseAction response = new IQResponseAction();
                if (iq.Type != IQType.result.ToString())
                {
                    response.AcceptIQ = false;
                    response.Error = iq.Error;
                }

                JingleSessionManager.FireSessionTransportInfoAck(SessionId, response);
                return true;
            }
            if ((TerminateSessionRequest != null) && (iq.ID == TerminateSessionRequest.ID))
            {
                /// Got an ack, analyze it an tell the client what is going on
                /// 
                JingleSessionManager.FireSessionTerminated(SessionId);
                IsCompleted = true;
                return true;
            }
            

            return false;
        }

        public void NewJingleIQ(JingleIQ jingleiq)
        {
            /// New message coming in, see if it is session accept or session terminate
            /// 
            if (jingleiq.Type == IQType.error.ToString())
            {
                JingleSessionManager.FireSessionTerminated(this.SessionId);
                return;
            }

            if (jingleiq.Type == IQType.set.ToString())
            {
                if ((jingleiq.Jingle.Action == Jingle.SessionAccept)||(jingleiq.Jingle.Action == Jingle.Accept))
                {
                    IQ iqresponse = new IQ();
                    iqresponse.ID = jingleiq.ID;
                    iqresponse.From = XMPPClient.JID;
                    iqresponse.To = jingleiq.From;
                    iqresponse.Type = IQType.result.ToString();
                    XMPPClient.SendXMPP(iqresponse);

                    JingleSessionManager.FireSessionAcceptedReceived(this.SessionId, jingleiq);
                }
                else if ((jingleiq.Jingle.Action == Jingle.SessionTerminate)||(jingleiq.Jingle.Action == Jingle.Terminate))
                {
                    /// Tell the user we've been terminated.  Ack and finish this logic
                    /// 
                    IQ iqresponse = new IQ();
                    iqresponse.ID = jingleiq.ID;
                    iqresponse.From = XMPPClient.JID;
                    iqresponse.To = jingleiq.From;
                    iqresponse.Type = IQType.result.ToString();

                    XMPPClient.SendXMPP(iqresponse);
                    JingleSessionManager.FireSessionTerminated(this.SessionId);

                    this.IsCompleted = true;
                }
                else if (jingleiq.Jingle.Action == Jingle.ContentAdd)
                {
                    /// TODO, notify user of this
                    /// 
                    IQ iqresponse = new IQ();
                    iqresponse.ID = jingleiq.ID;
                    iqresponse.From = XMPPClient.JID;
                    iqresponse.To = jingleiq.From;
                    iqresponse.Type = IQType.result.ToString();

                    XMPPClient.SendXMPP(iqresponse);
                }
                else if (jingleiq.Jingle.Action == Jingle.ContentModify)
                {
                    /// TODO, notify user of this
                    /// 
                    IQ iqresponse = new IQ();
                    iqresponse.ID = jingleiq.ID;
                    iqresponse.From = XMPPClient.JID;
                    iqresponse.To = jingleiq.From;
                    iqresponse.Type = IQType.result.ToString();

                    XMPPClient.SendXMPP(iqresponse);
                }
                else if (jingleiq.Jingle.Action == Jingle.ContentReject)
                {
                    /// TODO, notify user of this
                    /// 
                    IQ iqresponse = new IQ();
                    iqresponse.ID = jingleiq.ID;
                    iqresponse.From = XMPPClient.JID;
                    iqresponse.To = jingleiq.From;
                    iqresponse.Type = IQType.result.ToString();

                    XMPPClient.SendXMPP(iqresponse);
                }
                else if (jingleiq.Jingle.Action == Jingle.ContentAccept)
                {
                    /// TODO, notify user of this
                    /// 
                    IQ iqresponse = new IQ();
                    iqresponse.ID = jingleiq.ID;
                    iqresponse.From = XMPPClient.JID;
                    iqresponse.To = jingleiq.From;
                    iqresponse.Type = IQType.result.ToString();

                    XMPPClient.SendXMPP(iqresponse);
                }
                else if (jingleiq.Jingle.Action == Jingle.DescriptionInfo)
                {
                    /// TODO, notify user of this
                    /// 
                    IQ iqresponse = new IQ();
                    iqresponse.ID = jingleiq.ID;
                    iqresponse.From = XMPPClient.JID;
                    iqresponse.To = jingleiq.From;
                    iqresponse.Type = IQType.result.ToString();

                    XMPPClient.SendXMPP(iqresponse);
                }
                else if (jingleiq.Jingle.Action == Jingle.SessionInfo)
                {
                    //if (jingleiq.Jingle.JingleStateChange != JingleStateChange.None)

                    /// Some type of status message, just ack
                    IQ iqresponse = new IQ();
                    iqresponse.ID = jingleiq.ID;
                    iqresponse.From = XMPPClient.JID;
                    iqresponse.To = jingleiq.From;
                    iqresponse.Type = IQType.result.ToString();

                    XMPPClient.SendXMPP(iqresponse);

                    /// TODO, notify client of state change
                    /// 
                    System.Diagnostics.Debug.WriteLine("Got a Jingle state changed event of {0}", jingleiq.Jingle.JingleStateChange);
                }
                else if (jingleiq.Jingle.Action == Jingle.TransportInfo)
                {

                    IQ iqresponse = new IQ();
                    iqresponse.ID = jingleiq.ID;
                    iqresponse.From = XMPPClient.JID;
                    iqresponse.To = jingleiq.From;
                    iqresponse.Type = IQType.result.ToString();
                    XMPPClient.SendXMPP(iqresponse);

                    JingleSessionManager.FireSessionTransportInfoReceived(this.SessionId, jingleiq);
                }
                else if (jingleiq.Jingle.Action == Jingle.TransportAccept)
                {
                    /// TODO, notify user of this
                    /// 
                    IQ iqresponse = new IQ();
                    iqresponse.ID = jingleiq.ID;
                    iqresponse.From = XMPPClient.JID;
                    iqresponse.To = jingleiq.From;
                    iqresponse.Type = IQType.result.ToString();

                    XMPPClient.SendXMPP(iqresponse);
                }
                else if (jingleiq.Jingle.Action == Jingle.TransportReject)
                {
                    /// TODO, notify user of this
                    /// 
                    IQ iqresponse = new IQ();
                    iqresponse.ID = jingleiq.ID;
                    iqresponse.From = XMPPClient.JID;
                    iqresponse.To = jingleiq.From;
                    iqresponse.Type = IQType.result.ToString();

                    XMPPClient.SendXMPP(iqresponse);
                }
                else if (jingleiq.Jingle.Action == Jingle.TransportReplace)
                {
                    /// TODO, notify user of this
                    /// 
                    IQ iqresponse = new IQ();
                    iqresponse.ID = jingleiq.ID;
                    iqresponse.From = XMPPClient.JID;
                    iqresponse.To = jingleiq.From;
                    iqresponse.Type = IQType.result.ToString();

                    XMPPClient.SendXMPP(iqresponse);
                }
                else
                {
                    IQ iqresponse = new IQ();
                    iqresponse.ID = jingleiq.ID;
                    iqresponse.From = XMPPClient.JID;
                    iqresponse.To = jingleiq.From;
                    iqresponse.Type = IQType.error.ToString();
                    iqresponse.Error = new Error("<Unknown action />");

                    XMPPClient.SendXMPP(iqresponse);
                }
            }


        }

        JingleIQ IncomingRequestMessage = null;
        internal void NewIncomingSessionRequest(JingleIQ iq)
        {
            /// Handle this in a different thread so the we can still process xmpp messages but the user can reply at will
            /// 

            System.Threading.ThreadPool.QueueUserWorkItem(new Threading.WaitCallback(FireNewSessionThreaded), iq);
        }

        void FireNewSessionThreaded(object obj)
        {
            JingleIQ iq = obj as JingleIQ;
            IncomingRequestMessage = iq;
            RemoteJID = iq.From;


            IQ iqresponse = new IQ();
            iqresponse.ID = IncomingRequestMessage.ID;
            iqresponse.From = XMPPClient.JID;
            iqresponse.To = IncomingRequestMessage.From;

            iqresponse.Type = IQType.result.ToString();
            XMPPClient.SendXMPP(iqresponse);

            JingleSessionManager.FireNewSession(this.SessionId, iq);
            
        }

        JingleIQ OutgoingRequestMessage = null;
        internal void InitiateSession(Jingle jingleinfo, string strJIDTo)
        {

            if (OutgoingRequestMessage != null) /// we've already started a session, the user needs to create a new one
                throw new Exception(string.Format("Cannot initiate a session that already exists, Session [{0}] has already sent out an initiate session message, client must create a new session", this.SessionId));

            RemoteJID = strJIDTo;

            OutgoingRequestMessage = new JingleIQ();
            OutgoingRequestMessage.From = XMPPClient.JID;
            OutgoingRequestMessage.To = RemoteJID;
            OutgoingRequestMessage.Type = IQType.set.ToString();
            OutgoingRequestMessage.Jingle = jingleinfo;
            OutgoingRequestMessage.Jingle.Action = Jingle.SessionInitiate;
            OutgoingRequestMessage.Jingle.Initiator = XMPPClient.JID;
            if (OutgoingRequestMessage.Jingle.SID == null)
               OutgoingRequestMessage.Jingle.SID = Guid.NewGuid().ToString();

            XMPPClient.SendObject(OutgoingRequestMessage);
        }

        JingleIQ AcceptSessionMessage = null;
        internal void AcceptSession(Jingle jingleinfo)
        {
            if (AcceptSessionMessage != null) /// we've already started a session, the user needs to create a new one
                throw new Exception(string.Format("Cannot accept a session that already exists, Session [{0}] has already been accepted, client must create a new session", this.SessionId));

            AcceptSessionMessage = new JingleIQ();
            AcceptSessionMessage.From = XMPPClient.JID;
            AcceptSessionMessage.To = RemoteJID;
            AcceptSessionMessage.Type = IQType.set.ToString();
            AcceptSessionMessage.Jingle = jingleinfo;
            AcceptSessionMessage.Jingle.Action = Jingle.SessionAccept;
            //AcceptSessionMessage.Jingle.Initiator = XMPPClient.JID;
            AcceptSessionMessage.Jingle.SID = this.SessionId;

            XMPPClient.SendObject(AcceptSessionMessage);
        }

        internal void SendJingle(Jingle jingleinfo)
        {
            JingleIQ iq = new JingleIQ();
            iq.From = XMPPClient.JID;
            iq.To = RemoteJID;
            iq.Type = IQType.set.ToString();
            iq.Jingle = jingleinfo;
            iq.Jingle.SID = this.SessionId;

            XMPPClient.SendObject(iq);
        }

        JingleIQ TransportInfoRequest = null;
        internal void SendTransportInfo(Jingle jingle)
        {
            TransportInfoRequest = new JingleIQ();
            TransportInfoRequest.From = XMPPClient.JID;
            TransportInfoRequest.To = RemoteJID;
            TransportInfoRequest.Type = IQType.set.ToString();
            TransportInfoRequest.Jingle = jingle;
            TransportInfoRequest.Jingle.Action = Jingle.TransportInfo;
            TransportInfoRequest.Jingle.SID = this.SessionId;

            XMPPClient.SendObject(TransportInfoRequest);

        }

        JingleIQ TerminateSessionRequest = null;
        internal void TerminateSession(TerminateReason reason)
        {
            TerminateSessionRequest = new JingleIQ();
            TerminateSessionRequest.From = XMPPClient.JID;
            TerminateSessionRequest.To = RemoteJID;
            TerminateSessionRequest.Type = IQType.set.ToString();
            TerminateSessionRequest.Jingle.Action = Jingle.SessionTerminate;
            TerminateSessionRequest.Jingle.Initiator = XMPPClient.JID;
            TerminateSessionRequest.Jingle.SID = this.SessionId;
            TerminateSessionRequest.Jingle.Reason = new Reason(reason);

            XMPPClient.SendObject(TerminateSessionRequest);

        }

        /// Example Negotiation Below, from XEP-0167
        /// <jingle xmlns='urn:xmpp:jingle:1' action='session-initiate' initiator='romeo@montague.lit/orchard' sid='a73sjjvkla37jfea'>    
        ///     <content creator='initiator' name='voice'>      
        ///     <description xmlns='urn:xmpp:jingle:apps:rtp:1' media='audio'>        
        ///         <payload-type id='96' name='speex' clockrate='16000'/>        
        ///         <payload-type id='97' name='speex' clockrate='8000'/>        
        ///         <payload-type id='18' name='G729'/>        
        ///         <payload-type id='0' name='PCMU'/>        
        ///         <payload-type id='103' name='L16' clockrate='16000' channels='2'/>        
        ///         <payload-type id='98' name='x-ISAC' clockrate='8000'/>      
        ///     </description>      
        ///     <transport xmlns='urn:xmpp:jingle:transports:ice-udp:1' pwd='asd88fgpdd777uzjYhagZg' ufrag='8hhy'>
        ///         <candidate 
        ///             component='1'   
        ///             foundation='1' 
        ///             generation='0' 
        ///             id='el0747fg11' 
        ///             ip='10.0.1.1' 
        ///             network='1' 
        ///             port='8998' 
        ///             priority='2130706431' 
        ///             protocol='udp'
        ///             type='host'/>        
        ///             
        ///         <candidate component='1'
        ///             foundation='2'
        ///             generation='0'
        ///             id='y3s2b30v3r'
        ///             ip='192.0.2.3'
        ///             network='1'
        ///             port='45664'
        ///             priority='1694498815'
        ///             protocol='udp'
        ///             rel-addr='10.0.1.1'
        ///             rel-port='8998'
        ///             type='srflx'/>      
        ///         </transport>    
        ///     </content>  
        /// </jingle>

        ///<iq from='juliet@capulet.lit/balcony'    
        ///id='ih28sx61'    
        ///to='romeo@montague.lit/orchard'    
        ///type='result'/>

        ///<iq from='juliet@capulet.lit/balcony'    
        ///     id='i91fs6d5'    
        ///         to='romeo@montague.lit/orchard'    type='set'>  
        ///         <jingle xmlns='urn:xmpp:jingle:1'      
        ///             action='session-accept'          
        ///             initiator='romeo@montague.lit/orchard'          
        ///             responder='juliet@capulet.lit/balcony'          
        ///             sid='a73sjjvkla37jfea'>    
        ///             
        ///             <content creator='initiator' name='voice'>      
        ///                 <description xmlns='urn:xmpp:jingle:apps:rtp:1' media='audio'>        
        ///                     <payload-type id='97' name='speex' clockrate='8000'/>        
        ///                     <payload-type id='18' name='G729'/>      
        ///                 </description>      
        ///             
        ///                 <transport xmlns='urn:xmpp:jingle:transports:ice-udp:1'                 
        ///                 pwd='YH75Fviy6338Vbrhrlp8Yh'                 
        ///                 ufrag='9uB6'>        
        ///               
        ///                     <candidate component='1'                   
        ///                         foundation='1'                   
        ///                         generation='0'                   
        ///                         id='or2ii2syr1'                   
        ///                         ip='192.0.2.1'                   
        ///                         network='0'                   
        ///                         port='3478'                   
        ///                         priority='2130706431'                   
        ///                         protocol='udp'                   
        ///                         type='host'/>      
        ///                 </transport>    
        ///             </content>  
        ///         </jingle>
        /// </iq>


        /// <iq from='romeo@montague.lit/orchard'    
        ///     id='i91fs6d5'    
        ///     to='juliet@capulet.lit/balcony'    
        ///     type='result'/>
    }

    /// <summary>
    /// Used to ack or reject incoming IQs
    /// </summary>
    public class IQResponseAction
    {
        public IQResponseAction()
        {
        }

        private bool m_bAcceptIQ = true;

        public bool AcceptIQ
        {
            get { return m_bAcceptIQ; }
            set { m_bAcceptIQ = value; }
        }

        private Error m_objError = new Error() { Type = "cancel" };

        public Error Error
        {
            get { return m_objError; }
            set { m_objError = value; }
        }
    }

    /// <summary>
    /// This is the class the client interacts with when it wants to start a new jingle session or be notified when a new incoming session is encountered
    /// (XMPPClient.JingleSessionManager)
    /// </summary>
    public class JingleSessionManager : Logic
    {
        public JingleSessionManager(XMPPClient client) : base(client)
        {
            
        }

        object SessionLock = new object();
        Dictionary<string, JingleSessionLogic> Sessions = new Dictionary<string, JingleSessionLogic>();

        public override bool NewIQ(IQ iq)
        {
            if (iq is JingleIQ) //Our XMPPMessageFactory created a jingle message
            {
                /// See if this is a JINGLE message for our session 
                /// 
                JingleIQ jingleiq = iq as JingleIQ;
                string strSessionId = jingleiq.Jingle.SID;

                JingleSessionLogic sessionlogic = null;
                lock (SessionLock)
                {
                    if (Sessions.ContainsKey(strSessionId) == true)
                        sessionlogic = Sessions[strSessionId];
                }

                if (sessionlogic != null)
                {
                    sessionlogic.NewJingleIQ(jingleiq);

                    if (sessionlogic.IsCompleted == true)
                        RemoveSesion(strSessionId);

                    return true;
                }

                if ( (jingleiq.Jingle.Action == Jingle.SessionInitiate) || (jingleiq.Jingle.Action == Jingle.Initiate) )/// A new jingle session has been requested that we didn't initiate.  Start it's own "JingleLogic"
                {
                    JingleSessionLogic newrequestlogic = new JingleSessionLogic(this.XMPPClient, strSessionId, this);
                    lock (SessionLock)
                    {
                        Sessions.Add(jingleiq.Jingle.SID, newrequestlogic);
                    }
                    newrequestlogic.NewIncomingSessionRequest(jingleiq);

                    if (newrequestlogic.IsCompleted == true)
                        RemoveSesion(strSessionId);

                    return true;
                }
            }
            else
            {
                /// May be a simple response to one of our jingle objects, send the message to each one to see
                /// 

                List<JingleSessionLogic> sessions = new List<JingleSessionLogic>();
                lock (SessionLock)
                {
                    foreach (string strSessionId in Sessions.Keys)
                        sessions.Add(Sessions[strSessionId]);
                }

                foreach (JingleSessionLogic session in sessions)
                {
                    bool bHandled = session.NewIQ(iq);
                    if (bHandled == true)
                    {
                        if (session.IsCompleted == true)
                            RemoveSesion(session.SessionId);

                        return true;
                    }
                }

            }

            return false;
        }


        void RemoveSesion(string strSessionId)
        {
            lock (SessionLock)
            {
                if (Sessions.ContainsKey(strSessionId) == true)
                    Sessions.Remove(strSessionId);
            }
        }
        

        public string InitiateNewSession(JID jidto, Jingle jingleinfo)
        {
            /// Create a new session logic and send out the intial session create request
            /// 

            string strSessionId = Guid.NewGuid().ToString();
            JingleSessionLogic session = new JingleSessionLogic(this.XMPPClient, strSessionId, this);
            jingleinfo.SID = strSessionId;
            lock (SessionLock)
            {
                Sessions.Add(strSessionId, session);
            }

            session.InitiateSession(jingleinfo, jidto);

            return strSessionId;
        }

        public void SendAcceptSession(string strSessionId, Jingle jingleinfo)
        {
            /// Create a new session logic and send out the intial session create request
            /// 
            JingleSessionLogic session = null;
            lock (SessionLock)
            {
                if (Sessions.ContainsKey(strSessionId) == true)
                {
                    session = Sessions[strSessionId];
                }
            }
            if (session != null)
            {
                session.AcceptSession(jingleinfo);
            }
        }

        public void TerminateSession(string strSessionId, TerminateReason reason)
        {
            JingleSessionLogic session = null;
            lock (SessionLock)
            {
                if (Sessions.ContainsKey(strSessionId) == true)
                {
                    session = Sessions[strSessionId];
                }
            }
            if (session != null)
            {
                session.TerminateSession(reason);
            }

        }

        public void SendTransportInfo(string strSessionId, Jingle jingle)
        {
            JingleSessionLogic session = null;
            lock (SessionLock)
            {
                if (Sessions.ContainsKey(strSessionId) == true)
                {
                    session = Sessions[strSessionId];
                }
            }
            if (session != null)
            {
                session.SendTransportInfo(jingle);
            }

        }

        public void SendJingle(string strSessionId, Jingle jingleinfo)
        {
            /// Create a new session logic and send out the intial session create request
            /// 
            JingleSessionLogic session = null;
            lock (SessionLock)
            {
                if (Sessions.ContainsKey(strSessionId) == true)
                {
                    session = Sessions[strSessionId];
                }
            }
            if (session != null)
            {
                session.SendJingle(jingleinfo);
            }
        }

        /// <summary>
        /// Populates a Jingle message object with the basic stuff needed to initiate an audio call with speex and mu-law support.
        /// RTP payloads are hard coded and the transport is hard-coded to ICE.  
        /// </summary>
        /// <param name="strLocalIP">The local IP address that the client is listening on for audio</param>
        /// <param name="nLocalPort">The local port that the client is listening on for audio</param>
        /// <returns>A Jingle object that can be further modified and supplied to InitiateNewSession()</returns>
        public static Jingle CreateBasicOutgoingAudioRequest(string strLocalIP, int nLocalPort)
        {
            Jingle jinglecontent = new Jingle();
            jinglecontent.Content = new Content();
            jinglecontent.Action = Jingle.SessionInitiate;
            jinglecontent.Content.Description = new Description();
            jinglecontent.Content.Description.media = "audio";


            jinglecontent.Content.Description.Payloads.Add(new Payload() { PayloadId = 96, Channels = "1", ClockRate = "16000", Name = "speex" });
            jinglecontent.Content.Description.Payloads.Add(new Payload() { PayloadId = 97, Channels = "1", ClockRate = "8000", Name = "speex" });
            jinglecontent.Content.Description.Payloads.Add(new Payload() { PayloadId = 0, Channels = "1", ClockRate = "8000", Name = "PCMU" });

            /// If you don't want to use ICE UDP, new a different transport object
            jinglecontent.Content.ICETransport = new Transport();
            jinglecontent.Content.ICETransport.Candidates.Add(new Candidate() { ipaddress = strLocalIP, port = nLocalPort });

            return jinglecontent;
        }

        /// <summary>
        /// Create a simple audio session jingle accept
        /// </summary>
        /// <param name="strLocalIP">The local ip address that the client is listening on for media</param>
        /// <param name="nLocalPort">The local port the client is listening on for media</param>
        /// <param name="payloads">A list of payloads that are acceptable, or null to use the default paylodas (speex, PCMU)</param>
        /// <returns>A Jingle message object that can be supplied to </returns>
        public static Jingle CreateBasicAudioSessionAccept(string strLocalIP, int nLocalPort, Payload [] payloads)
        {
            Jingle jinglecontent = new Jingle();
            jinglecontent.Content = new Content();
            jinglecontent.Action = Jingle.SessionAccept;
            jinglecontent.Content.Description = new Description();
            jinglecontent.Content.Description.media = "audio";


            if (payloads != null)
            {
                foreach (Payload payload in payloads)
                    jinglecontent.Content.Description.Payloads.Add(payload);
            }
            else
            {
                jinglecontent.Content.Description.Payloads.Add(new Payload() { PayloadId = 96, Channels = "1", ClockRate = "16000", Name = "speex" });
                jinglecontent.Content.Description.Payloads.Add(new Payload() { PayloadId = 97, Channels = "1", ClockRate = "8000", Name = "speex" });
                jinglecontent.Content.Description.Payloads.Add(new Payload() { PayloadId = 0, Channels = "1", ClockRate = "8000", Name = "PCMU" });
            }

            /// If you don't want to use ICE UDP, new a different transport object
            jinglecontent.Content.ICETransport = new Transport();
            jinglecontent.Content.ICETransport.Candidates.Add(new Candidate() { ipaddress = strLocalIP, port = nLocalPort });

            return jinglecontent;
        }

        public delegate void DelegateJingleSessionEventWithInfo(string strSession, JingleIQ iq, XMPPClient client);
        public delegate void DelegateJingleSessionEvent(string strSession, XMPPClient client);
        public delegate void DelegateJingleSessionEventBool(string strSession, IQResponseAction response, XMPPClient client);

        public event DelegateJingleSessionEventWithInfo OnNewSession = null;
        public event DelegateJingleSessionEventBool OnNewSessionAckReceived = null;

        public event DelegateJingleSessionEventWithInfo OnSessionAcceptedReceived = null;
        public event DelegateJingleSessionEventBool OnSessionAcceptedAckReceived = null;

        public event DelegateJingleSessionEventWithInfo OnSessionTransportInfoReceived = null;
        public event DelegateJingleSessionEventBool OnSessionTransportInfoAckReceived = null;

        public event DelegateJingleSessionEvent OnSessionTerminated = null;



        internal void FireNewSession(string strSession, JingleIQ iq)
        {
            if (OnNewSession != null)
                OnNewSession(iq.Jingle.SID, iq, XMPPClient);
        }

        internal void FireNewSessionAckReceived(string strSessionId, IQResponseAction response)
        {
            if (OnNewSessionAckReceived != null)
                OnNewSessionAckReceived(strSessionId, response, XMPPClient);
        }


        internal void FireSessionAcceptedReceived(string strSession, JingleIQ iq)
        {
            if (OnSessionAcceptedReceived != null)
                OnSessionAcceptedReceived(strSession, iq, XMPPClient);
        }

        internal void FireSessionAcceptedAck(string strSessionId, IQResponseAction response)
        {
            if (OnSessionAcceptedAckReceived != null)
                OnSessionAcceptedAckReceived(strSessionId, response, XMPPClient);
        }


        internal void FireSessionTransportInfoReceived(string strSession, JingleIQ iq)
        {
            if (OnSessionTransportInfoReceived != null)
                OnSessionTransportInfoReceived(strSession, iq, XMPPClient);
        }

        internal void FireSessionTransportInfoAck(string strSessionId, IQResponseAction response)
        {
            if (OnSessionTransportInfoAckReceived != null)
                OnSessionTransportInfoAckReceived(strSessionId, response, XMPPClient);
        }

        internal void FireSessionTerminated(string strSession)
        {
            if (OnSessionTerminated != null)
                OnSessionTerminated(strSession, XMPPClient);
        }

    }
}
