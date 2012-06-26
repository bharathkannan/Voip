/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Net;

using System.Collections.Generic;
using System.Xml.Linq;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.Net.XMPP.Jingle;

namespace System.Net.XMPP.GoogleTalk
{

    [XmlRoot(ElementName = "description", Namespace = "http://www.google.com/session/phone")]
    public class Description
    {
        public Description()
        {
        }

        [XmlAttribute(AttributeName = "media")]
        public string media = null;

        [XmlElement(ElementName = "payload-type")]
        public List<Payload> Payloads = new List<Payload>();
    }

    [XmlRoot(ElementName = "transport")]
    public class Transport
    {
        public Transport()
        {
        }

        [XmlElement(ElementName = "candidate")]
        public List<Candidate> Candidates = new List<Candidate>();
    }

    [XmlRoot(ElementName = "session", Namespace = "http://www.google.com/session")]
    public class GoogleSession
    {
        public GoogleSession()
        {
        }

        public const string Initiate = "session-initiate";
        public const string Accept = "session-accept";
        public const string Terminate = "session-terminate";
        public const string TransportInfo = "transport-info";
        //public const string TransportAccept = "transport-accept";
        //public const string TransportReject = "transport-reject";
        //public const string TransportReplace = "transport-replace";

        [XmlAttribute(AttributeName = "type")]
        public string Action = Initiate;

        [XmlAttribute(AttributeName = "initiator")]
        public string Initiator = null;

        [XmlAttribute(AttributeName = "id")]
        public string SID = null;

        [XmlElement(ElementName = "description", Namespace = "http://www.google.com/session/phone")]
        public Description Description = new Description();

        [XmlElement(ElementName = "transport", Namespace = "http://www.google.com/transport/p2p")]
        public Transport Transport = new Transport();

    }

    /// <feature var='urn:xmpp:jingle:1'/>    
    /// <feature var='urn:xmpp:jingle:transports:ice-udp:0'/>    
    /// <feature var='urn:xmpp:jingle:transports:ice-udp:1'/>    
    /// <feature var='urn:xmpp:jingle:apps:rtp:1'/>    
    /// <feature var='urn:xmpp:jingle:apps:rtp:audio'/>    
    /// <feature var='urn:xmpp:jingle:apps:rtp:video'/>

    [XmlRoot(ElementName = "iq")]
    public class GoogleTalkIQ : IQ
    {
        public GoogleTalkIQ()
            : base()
        {
        }
        public GoogleTalkIQ(string strXML)
            : base(strXML)
        {
        }

        [XmlElement(ElementName = "session", Namespace = "http://www.google.com/session")]
        public GoogleSession Session = new GoogleSession();


    }

    /// <summary>
    /// Initiates a jingle session, returns and event when done 
    /// </summary>
    public class GoogleTalkLogic : Logic
    {
        public GoogleTalkLogic(XMPPClient client, string strSessionId, GoogleTalkSessionManager mgr)
            : base(client)
        {
            SessionId = strSessionId;
            GoogleTalkSessionManager = mgr;
        }

        public string SessionId = null;
        public string RemoteJID = null;
        GoogleTalkSessionManager GoogleTalkSessionManager = null;



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

                GoogleTalkSessionManager.FireNewSessionAckReceived(SessionId, response);
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

                GoogleTalkSessionManager.FireSessionAcceptedAck(SessionId, response);
                return true;
            }
            if ((TerminateSessionRequest != null) && (iq.ID == TerminateSessionRequest.ID))
            {
                /// Got an ack, analyze it an tell the client what is going on
                /// 
                GoogleTalkSessionManager.FireSessionTerminated(SessionId);
                IsCompleted = true;
                return true;
            }


            return false;
        }

        public void NewJingleIQ(GoogleTalkIQ gtiq)
        {
            /// New message coming in, see if it is session accept or session terminate
            /// 
            if (gtiq.Type == IQType.error.ToString())
            {
                GoogleTalkSessionManager.FireSessionTerminated(this.SessionId);
                return;
            }

            if (gtiq.Type == IQType.set.ToString())
            {
                if (gtiq.Type == GoogleSession.Accept)
                {
                    IQ iqresponse = new IQ();
                    iqresponse.ID = gtiq.ID;
                    iqresponse.From = XMPPClient.JID;
                    iqresponse.To = gtiq.From;
                    iqresponse.Type = IQType.result.ToString();
                    XMPPClient.SendXMPP(iqresponse);

                    GoogleTalkSessionManager.FireSessionAcceptedReceived(this.SessionId, gtiq);
                }
                else if (gtiq.Type == GoogleSession.Terminate)
                {
                    /// Tell the user we've been terminated.  Ack and finish this logic
                    /// 
                    IQ iqresponse = new IQ();
                    iqresponse.ID = gtiq.ID;
                    iqresponse.From = XMPPClient.JID;
                    iqresponse.To = gtiq.From;
                    iqresponse.Type = IQType.result.ToString();

                    XMPPClient.SendXMPP(iqresponse);
                    GoogleTalkSessionManager.FireSessionTerminated(this.SessionId);

                    this.IsCompleted = true;
                }
                else if (gtiq.Type == GoogleSession.TransportInfo)
                {

                    IQ iqresponse = new IQ();
                    iqresponse.ID = gtiq.ID;
                    iqresponse.From = XMPPClient.JID;
                    iqresponse.To = gtiq.From;
                    iqresponse.Type = IQType.result.ToString();
                    XMPPClient.SendXMPP(iqresponse);

                    GoogleTalkSessionManager.FireSessionTransportInfoReceived(this.SessionId, gtiq);
                }
                else
                {
                    IQ iqresponse = new IQ();
                    iqresponse.ID = gtiq.ID;
                    iqresponse.From = XMPPClient.JID;
                    iqresponse.To = gtiq.From;
                    iqresponse.Type = IQType.error.ToString();
                    iqresponse.Error = new Error("<Unknown action />");

                    XMPPClient.SendXMPP(iqresponse);
                }
            }


        }

        GoogleTalkIQ IncomingRequestMessage = null;
        internal void NewIncomingSessionRequest(GoogleTalkIQ iq)
        {
            IncomingRequestMessage = iq;
            RemoteJID = iq.From;


            IQ iqresponse = new IQ();
            iqresponse.ID = IncomingRequestMessage.ID;
            iqresponse.From = XMPPClient.JID;
            iqresponse.To = IncomingRequestMessage.From;

            iqresponse.Type = IQType.result.ToString();
            XMPPClient.SendXMPP(iqresponse);

            GoogleTalkSessionManager.FireNewSession(this.SessionId, iq);

        }

        GoogleTalkIQ OutgoingRequestMessage = null;
        internal void InitiateSession(GoogleSession jingleinfo, string strJIDTo)
        {

            if (OutgoingRequestMessage != null) /// we've already started a session, the user needs to create a new one
                throw new Exception(string.Format("Cannot initiate a session that already exists, Session [{0}] has already sent out an initiate session message, client must create a new session", this.SessionId));

            RemoteJID = strJIDTo;

            OutgoingRequestMessage = new GoogleTalkIQ();
            OutgoingRequestMessage.From = XMPPClient.JID;
            OutgoingRequestMessage.To = RemoteJID;
            OutgoingRequestMessage.Type = IQType.set.ToString();
            OutgoingRequestMessage.Session = jingleinfo;
            OutgoingRequestMessage.Session.Action = GoogleSession.Initiate;
            OutgoingRequestMessage.Session.Initiator = XMPPClient.JID;
            if (OutgoingRequestMessage.Session.SID == null)
                OutgoingRequestMessage.Session.SID = Guid.NewGuid().ToString();

            XMPPClient.SendObject(OutgoingRequestMessage);
        }

        GoogleTalkIQ AcceptSessionMessage = null;
        internal void AcceptSession(GoogleSession jingleinfo)
        {
            if (AcceptSessionMessage != null) /// we've already started a session, the user needs to create a new one
                throw new Exception(string.Format("Cannot accept a session that already exists, Session [{0}] has already been accepted, client must create a new session", this.SessionId));

            AcceptSessionMessage = new GoogleTalkIQ();
            AcceptSessionMessage.From = XMPPClient.JID;
            AcceptSessionMessage.To = RemoteJID;
            AcceptSessionMessage.Type = IQType.set.ToString();
            AcceptSessionMessage.Session = jingleinfo;
            AcceptSessionMessage.Session.Action = GoogleSession.Accept;
            AcceptSessionMessage.Session.Initiator = XMPPClient.JID;
            AcceptSessionMessage.Session.SID = this.SessionId;

            XMPPClient.SendObject(AcceptSessionMessage);
        }

        internal void SendGoogleSession(GoogleSession jingleinfo)
        {
            GoogleTalkIQ iq = new GoogleTalkIQ();
            iq.From = XMPPClient.JID;
            iq.To = RemoteJID;
            iq.Type = IQType.set.ToString();
            iq.Session = jingleinfo;
            iq.Session.SID = this.SessionId;

            XMPPClient.SendObject(iq);
        }


        GoogleTalkIQ TerminateSessionRequest = null;
        internal void TerminateSession(TerminateReason reason)
        {
            TerminateSessionRequest = new GoogleTalkIQ();
            TerminateSessionRequest.From = XMPPClient.JID;
            TerminateSessionRequest.To = RemoteJID;
            TerminateSessionRequest.Type = IQType.set.ToString();
            TerminateSessionRequest.Session.Action = GoogleSession.Terminate;
            TerminateSessionRequest.Session.Initiator = XMPPClient.JID;
            TerminateSessionRequest.Session.SID = this.SessionId;
            //TerminateSessionRequest.Jingle.Reason = new Reason(reason);

            XMPPClient.SendObject(TerminateSessionRequest);

        }
    }



    /// <summary>
    /// This is the class the client interacts with when it wants to start a new google voice session or be notified when a new incoming session is encountered
    /// </summary>
    public class GoogleTalkSessionManager : Logic
    {
        public GoogleTalkSessionManager(XMPPClient client)
            : base(client)
        {

        }

        object SessionLock = new object();
        Dictionary<string, GoogleTalkLogic> Sessions = new Dictionary<string, GoogleTalkLogic>();

        public override bool NewIQ(IQ iq)
        {
            if (iq is GoogleTalkIQ) //Our XMPPMessageFactory created a jingle message
            {
                /// See if this is a JINGLE message for our session 
                /// 
                GoogleTalkIQ jingleiq = iq as GoogleTalkIQ;
                string strSessionId = jingleiq.Session.SID;

                GoogleTalkLogic sessionlogic = null;
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

                if (jingleiq.Session.Action == GoogleSession.Initiate)
                {
                    GoogleTalkLogic newrequestlogic = new GoogleTalkLogic(this.XMPPClient, strSessionId, this);
                    lock (SessionLock)
                    {
                        Sessions.Add(jingleiq.Session.SID, newrequestlogic);
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

                List<GoogleTalkLogic> sessions = new List<GoogleTalkLogic>();
                lock (SessionLock)
                {
                    foreach (string strSessionId in Sessions.Keys)
                        sessions.Add(Sessions[strSessionId]);
                }

                foreach (GoogleTalkLogic session in sessions)
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


        public string InitiateNewSession(JID jidto, GoogleSession sessioninfo)
        {
            /// Create a new session logic and send out the intial session create request
            /// 

            string strSessionId = Guid.NewGuid().ToString();
            GoogleTalkLogic session = new GoogleTalkLogic(this.XMPPClient, strSessionId, this);
            sessioninfo.SID = strSessionId;
            lock (SessionLock)
            {
                Sessions.Add(strSessionId, session);
            }

            session.InitiateSession(sessioninfo, jidto);

            return strSessionId;
        }

        public void SendAcceptSession(string strSessionId, GoogleSession sessioninfo)
        {
            /// Create a new session logic and send out the intial session create request
            /// 
            GoogleTalkLogic session = null;
            lock (SessionLock)
            {
                if (Sessions.ContainsKey(strSessionId) == true)
                {
                    session = Sessions[strSessionId];
                }
            }
            if (session != null)
            {
                session.AcceptSession(sessioninfo);
            }
        }

        public void TerminateSession(string strSessionId, TerminateReason reason)
        {
            GoogleTalkLogic session = null;
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

        public void SendJingle(string strSessionId, GoogleSession sessioninfo)
        {
            /// Create a new session logic and send out the intial session create request
            /// 
            GoogleTalkLogic session = null;
            lock (SessionLock)
            {
                if (Sessions.ContainsKey(strSessionId) == true)
                {
                    session = Sessions[strSessionId];
                }
            }
            if (session != null)
            {
                session.SendGoogleSession(sessioninfo);
            }
        }

        /// <summary>
        /// Populates a Jingle message object with the basic stuff needed to initiate an audio call with speex and mu-law support.
        /// RTP payloads are hard coded and the transport is hard-coded to ICE.  
        /// </summary>
        /// <param name="strLocalIP">The local IP address that the client is listening on for audio</param>
        /// <param name="nLocalPort">The local port that the client is listening on for audio</param>
        /// <returns>A Jingle object that can be further modified and supplied to InitiateNewSession()</returns>
        public static GoogleSession CreateBasicOutgoingAudioRequest(string strLocalIP, int nLocalPort)
        {
            GoogleSession sessioncontent = new GoogleSession();
            sessioncontent.Action = GoogleSession.Initiate;
            sessioncontent.Description = new Description();
            sessioncontent.Description.media = "audio";


            sessioncontent.Description.Payloads.Add(new Payload() { PayloadId = 9, Channels = "1", ClockRate = "16000", Name = "G722" });
            sessioncontent.Description.Payloads.Add(new Payload() { PayloadId = 96, Channels = "1", ClockRate = "16000", Name = "speex" });
            sessioncontent.Description.Payloads.Add(new Payload() { PayloadId = 97, Channels = "1", ClockRate = "8000", Name = "speex" });
            sessioncontent.Description.Payloads.Add(new Payload() { PayloadId = 0, Channels = "1", ClockRate = "8000", Name = "PCMU" });

            /// If you don't want to use ICE UDP, new a different transport object
            sessioncontent.Transport = new Transport();
            sessioncontent.Transport.Candidates.Add(new Candidate() { ipaddress = strLocalIP, port = nLocalPort });

            return sessioncontent;
        }

        /// <summary>
        /// Create a simple audio session jingle accept
        /// </summary>
        /// <param name="strLocalIP">The local ip address that the client is listening on for media</param>
        /// <param name="nLocalPort">The local port the client is listening on for media</param>
        /// <param name="payloads">A list of payloads that are acceptable, or null to use the default paylodas (speex, PCMU)</param>
        /// <returns>A Jingle message object that can be supplied to </returns>
        public static GoogleSession CreateBasicAudioSessionAccept(string strLocalIP, int nLocalPort, Payload[] payloads)
        {
            GoogleSession sessioncontent = new GoogleSession();
            sessioncontent.Action = GoogleSession.Accept;
            sessioncontent.Description = new Description();
            sessioncontent.Description.media = "audio";


            if (payloads != null)
            {
                foreach (Payload payload in payloads)
                    sessioncontent.Description.Payloads.Add(payload);
            }
            else
            {
                sessioncontent.Description.Payloads.Add(new Payload() { PayloadId = 9, Channels = "1", ClockRate = "16000", Name = "G722" });
                sessioncontent.Description.Payloads.Add(new Payload() { PayloadId = 96, Channels = "1", ClockRate = "16000", Name = "speex" });
                sessioncontent.Description.Payloads.Add(new Payload() { PayloadId = 97, Channels = "1", ClockRate = "8000", Name = "speex" });
                sessioncontent.Description.Payloads.Add(new Payload() { PayloadId = 0, Channels = "1", ClockRate = "8000", Name = "PCMU" });
            }

            /// If you don't want to use ICE UDP, new a different transport object
            sessioncontent.Transport = new Transport();
            sessioncontent.Transport.Candidates.Add(new Candidate() { ipaddress = strLocalIP, port = nLocalPort });

            return sessioncontent;
        }

        public delegate void DelegateJingleSessionEventWithInfo(string strSession, GoogleTalkIQ iq , XMPPClient client);
        public delegate void DelegateJingleSessionEvent(string strSession, XMPPClient client);
        public delegate void DelegateJingleSessionEventBool(string strSession, IQResponseAction response, XMPPClient client);

        public event DelegateJingleSessionEventWithInfo OnNewSession = null;
        public event DelegateJingleSessionEventBool OnNewSessionAckReceived = null;

        public event DelegateJingleSessionEventWithInfo OnSessionAcceptedReceived = null;
        public event DelegateJingleSessionEventBool OnSessionAcceptedAckReceived = null;

        public event DelegateJingleSessionEventWithInfo OnSessionTransportInfoReceived = null;

        public event DelegateJingleSessionEvent OnSessionTerminated = null;



        internal void FireNewSession(string strSession, GoogleTalkIQ iq)
        {
            if (OnNewSession != null)
                OnNewSession(strSession, iq, XMPPClient);
        }

        internal void FireNewSessionAckReceived(string strSessionId, IQResponseAction response)
        {
            if (OnNewSessionAckReceived != null)
                OnNewSessionAckReceived(strSessionId, response, XMPPClient);
        }




        internal void FireSessionAcceptedReceived(string strSession, GoogleTalkIQ iq)
        {
            if (OnSessionAcceptedReceived != null)
                OnSessionAcceptedReceived(strSession, iq, XMPPClient);
        }

        internal void FireSessionAcceptedAck(string strSessionId, IQResponseAction response)
        {
            if (OnSessionAcceptedAckReceived != null)
                OnSessionAcceptedAckReceived(strSessionId, response, XMPPClient);
        }


        internal void FireSessionTransportInfoReceived(string strSession, GoogleTalkIQ iq)
        {
            if (OnSessionTransportInfoReceived != null)
                OnSessionTransportInfoReceived(strSession, iq, XMPPClient);
        }


        internal void FireSessionTerminated(string strSession)
        {
            if (OnSessionTerminated != null)
                OnSessionTerminated(strSession, XMPPClient);
        }

    }

}
