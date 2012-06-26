/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Net;


using System.Xml;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

namespace System.Net.XMPP
{
    public class GenericIQLogic : Logic
    {
        public GenericIQLogic(XMPPClient client)
            : base(client)
        {
            BindIQ = new IQ();
            BindIQ.Type = IQType.set.ToString();
            BindIQ.To = null;
            BindIQ.From = null;
        }

        private string m_strInnerXML = "";

        public string InnerXML
        {
            get { return m_strInnerXML; }
            set { m_strInnerXML = value; }
        }

        public override void Start()
        {
            base.Start();
            Bind();
        }

        public const string BindXML = @"<bind xmlns=""urn:ietf:params:xml:ns:xmpp-bind""><resource>##RESOURCE##</resource></bind>";
        
        IQ BindIQ = null;

        void Bind()
        {
            BindIQ.InnerXML = BindXML.Replace("##RESOURCE##", XMPPClient.JID.Resource);

            XMPPClient.XMPPState = XMPPState.Binding;
            XMPPClient.SendXMPP(BindIQ);
        }
        
        
        public override bool NewIQ(IQ iq)
        {
            try
            {
                if (iq.ID == BindIQ.ID)
                {
                    /// Extract our jid incase it changed
                    /// <iq type="result" id="bind_1" to="ninethumbs.com/7b5005e1"><bind xmlns="urn:ietf:params:xml:ns:xmpp-bind"><jid>test@ninethumbs.com/hypnotoad</jid></bind></iq>
                    /// 
                    if (iq.Type == IQType.result.ToString())
                    {
                        /// bound, now do toher things
                        /// 
                        XElement elembind = XElement.Parse(iq.InnerXML);
                        XElement nodejid = elembind.FirstNode as XElement;
                        if ((nodejid != null) && (nodejid.Name == "{urn:ietf:params:xml:ns:xmpp-bind}jid"))
                        {
                            XMPPClient.JID = nodejid.Value;
                        }
                        XMPPClient.XMPPState = XMPPState.Bound;
                    }
                    return true;
                }

                if ((iq.InnerXML != null) && (iq.InnerXML.Length > 0))
                {

                    XElement elem = XElement.Parse(iq.InnerXML);
                    if (elem.Name == "{urn:xmpp:ping}ping")
                    {
                        iq.Type = IQType.result.ToString();
                        iq.To = iq.From;
                        iq.From = XMPPClient.JID.BareJID;
                        iq.InnerXML = "";
                        XMPPClient.SendXMPP(iq);
                    }

                }
            }
            catch (Exception)
            {
            }
            return false;
        }

    }

    /// <summary>
    /// The method that is used to get xml from the object
    /// </summary>
    public enum SerializationMethod
    {
        /// <summary>
        /// Use the XMLSerializer to get xml from the object
        /// </summary>
        XMLSerializeObject,

        /// <summary>
        /// Use the virtual MessageXML property to xml from the object
        /// </summary>
        MessageXMLProperty,
    }

    public class SendRecvIQLogic : Logic
    {
        public SendRecvIQLogic(XMPPClient client, IQ iq)
            : base(client)
        {
            SendIQ = iq;
        }

        private string m_strInnerXML = "";

        public string InnerXML
        {
            get { return m_strInnerXML; }
            set { m_strInnerXML = value; }
        }

        private int m_nTimeoutMs = 10000;

        public int TimeoutMs
        {
            get { return m_nTimeoutMs; }
            set { m_nTimeoutMs = value; }
        }

        private SerializationMethod m_eSerializationMethod = SerializationMethod.MessageXMLProperty;

        public SerializationMethod SerializationMethod
        {
            get { return m_eSerializationMethod; }
            set { m_eSerializationMethod = value; }
        }
 

        public bool SendReceive(int nTimeoutMs)
        {
            TimeoutMs = nTimeoutMs;
            if (SerializationMethod == XMPP.SerializationMethod.MessageXMLProperty)
                XMPPClient.SendXMPP(SendIQ);
            else
                XMPPClient.SendObject(SendIQ);

            Success = GotIQEvent.WaitOne(TimeoutMs);
            return Success;
        }



        System.Threading.ManualResetEvent GotIQEvent = new System.Threading.ManualResetEvent(false);
        IQ m_objSendIQ = null;

        public IQ SendIQ
        {
            get { return m_objSendIQ; }
            set { m_objSendIQ = value; }
        }

        private IQ m_objRecvIQ = null;

        public IQ RecvIQ
        {
            get { return m_objRecvIQ; }
            set { m_objRecvIQ = value; }
        }

        public override bool NewIQ(IQ iq)
        {
            try
            {
                if (iq.ID == SendIQ.ID)
                {
                    RecvIQ = iq;
                    IsCompleted = true;
                    Success = true;
                    GotIQEvent.Set();

                    return true;
                }
            }
            catch (Exception)
            {
            }
            return false;
        }

    }
}
