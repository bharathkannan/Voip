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
using System.Runtime.Serialization;

using System.Text.RegularExpressions;


namespace System.Net.XMPP
{
    // We have iq's, responses, and messages, each with their own xml
    public enum IQType
    {
        get,
        set,
        result,
        error,
    }

    public class XMPPMessageBase
    {
        /// <summary>
        /// We need this default constructor in order to be able to serialize derived classes in Mono for android,
        /// all other versions of mono/.net don't have this limitation
        /// </summary>
        public XMPPMessageBase()
        {
        }
        public XMPPMessageBase(string strXML, string strNodeName)
        {
            NodeName = strNodeName;
            MessageXML = strXML;
        }

        private XElement m_objInitalXMLElement = null;
        [XmlIgnore()]
        public XElement InitalXMLElement
        {
            get { return m_objInitalXMLElement; }
            internal set { m_objInitalXMLElement = value; }
        }

        private string m_strNodeName = "unknown";
        [XmlIgnore()]
        public string NodeName
        {
            get { return m_strNodeName; }
            set { m_strNodeName = value; }
        }


        [XmlAttribute(AttributeName = "from")]  /// can't serialize JID as an attribute, so add this
        public string FromString
        {
            get
            {
                return m_objJIDFrom;
            }
            set
            {
                m_objJIDFrom = value;
            }
        }

        private JID m_objJIDFrom = new JID();
        [XmlIgnore()]
        public JID From
        {
            get { return m_objJIDFrom; }
            set { m_objJIDFrom = value; }
        }

        [XmlAttribute(AttributeName = "to")]
        public string TOString
        {
            get
            {
                return m_objJIDTo;
            }
            set
            {
                m_objJIDTo = value;
            }
        }

        private JID m_objJIDTo = new JID();
        [XmlIgnore()]
        public JID To
        {
            get { return m_objJIDTo; }
            set { m_objJIDTo = value; }
        }

        private string m_strID = Guid.NewGuid().ToString();
        [XmlAttribute(AttributeName = "id")]
        public string ID
        {
            get { return m_strID; }
            set { m_strID = value; }
        }


        private string m_strType = null;
        [XmlAttribute(AttributeName = "type")]
        public virtual string Type
        {
            get { return m_strType; }
            set { m_strType = value; }
        }

        private string m_strxmlns = "";
        /// <summary>
        /// The namespace to set in an outgoing message
        /// </summary>
        [XmlIgnore()]
        public string xmlns
        {
            get { return m_strxmlns; }
            set { m_strxmlns = value; }
        }

        private string m_strxmlnsfrom = "";
        /// <summary>
        /// The namespace in a parsed message
        /// </summary>
        [XmlIgnore()]
        public string xmlnsfrom
        {
            get { return m_strxmlnsfrom; }
            set { m_strxmlnsfrom = value; }
        }

        private string m_strInnerXML = "";
        [XmlIgnore()]
        public string InnerXML
        {
            get { return m_strInnerXML; }
            set { m_strInnerXML = value; }
        }

        /// <summary>
        /// When overriden in a derived class, gives the class an opportunity to build the InnerXML string from its data members
        /// </summary>
        public virtual void AddInnerXML(XElement elemMessage)
        {
            if ((InnerXML != null) && (InnerXML.Length > 0)) 
                elemMessage.Add(XElement.Parse(InnerXML));

        }

        /// <summary>
        /// When overriden in a derived class, gives the object an opportunity to parse the InnerXML string and set its data members
        /// </summary>
        public virtual void ParseInnerXML(XElement elem)
        {
            if (elem.FirstNode != null)
            {
                InnerXML = elem.FirstNode.ToString();
            }
        }

        [XmlIgnore()] /// stop recursion
        public virtual string MessageXML
        {
            get
            {
                if ((xmlns != null) && (xmlns.Length > 0))
                {
                    XNamespace xn = xmlns;
                    XDocument doc = new XDocument();

                    XElement elemMessage = new XElement(xn + NodeName);

                    doc.Add(elemMessage);

                    if (Type.Length > 0)
                        elemMessage.Add(new XAttribute("type", m_strType));
                    if (ID.Length > 0)
                        elemMessage.Add(new XAttribute("id", ID));
                    if (From != null)
                        elemMessage.Add(new XAttribute("from", From));
                    if (To != null)
                        elemMessage.Add(new XAttribute("to", To));

                    AddInnerXML(elemMessage);

                    /// Rebuild our InitialXMLElement
                    InitalXMLElement = XElement.Parse(doc.ToString());
                    return doc.ToString();
                }
                else
                {
                    InitalXMLElement = new XElement(NodeName);

                    if (Type.Length > 0)
                        InitalXMLElement.Add(new XAttribute("type", m_strType));
                    if (ID.Length > 0)
                        InitalXMLElement.Add(new XAttribute("id", ID));
                    if (From != null)
                        InitalXMLElement.Add(new XAttribute("from", From));
                    if (To != null)
                        InitalXMLElement.Add(new XAttribute("to", To));

                    AddInnerXML(InitalXMLElement);

                    return InitalXMLElement.ToString();
                }
 
            }
            set
            {
                if ((value == null) || (value.Length <= 0))
                    return;

                /// Parse the xml fragment
                /// 
                InitalXMLElement = XElement.Parse(value);
                XAttribute attrType = InitalXMLElement.Attribute("type");
                if (attrType != null) Type = attrType.Value;

                XAttribute attrId = InitalXMLElement.Attribute("id");
                if (attrId != null) ID = attrId.Value;

                XAttribute attrFrom = InitalXMLElement.Attribute("from");
                if (attrFrom != null) From = attrFrom.Value;

                XAttribute attrTo = InitalXMLElement.Attribute("to");
                if (attrTo != null) To = attrTo.Value;

                XAttribute attrxmlns = InitalXMLElement.Attribute("xmlns");
                if (attrxmlns != null) xmlnsfrom = attrxmlns.Value;

                ParseInnerXML(InitalXMLElement);
            }
        }
    }

    public enum ErrorType
    {
        [XmlEnum(null)]
        unknown,
        [XmlEnum("bad-request")]
        badrequest,
        [XmlEnum("feature-not-implemented")]
        featurenotimplemented,
        [XmlEnum("forbidden")]
        forbidden,
        [XmlEnum("gone")]
        gone,
        [XmlEnum("internal-server-error")]
        internalservererror,
        [XmlEnum("item-not-found")]
        itemnotfound,
        [XmlEnum("jid-malformed")]
        jidmalformed,
        [XmlEnum("not-acceptable")]
        notacceptable,
        [XmlEnum("not-authorized")]
        notauthorized,
        [XmlEnum("not-allowed")]
        notallowed,
        [XmlEnum("policy-violation")]
        policyviolation,
        [XmlEnum("recipient-unavailable")]
        recipientunavailable,
        [XmlEnum("redirect")]
        redirect,
        [XmlEnum("registration-required")]
        registrationrequired,
        [XmlEnum("remote-server-not-found")]
        remoteservernotfound,
        [XmlEnum("remote-server-timeout")]
        remoteservertimeout, 
        [XmlEnum("resource-constraint")]
        resourceconstraint,
        [XmlEnum("service-unavailable")]
        serviceunavailable,
        [XmlEnum("subscription-required")]
        subscriptionrequired, 
        [XmlEnum("undefined-condition")]
        undefinedcondition,
        [XmlEnum("unexpected-request")]
        unexpectedrequest,

        [XmlEnum("bad-format")]
        badformat,
        [XmlEnum("bad-namespace-prefix")]
        badnamespaceprefix,
        [XmlEnum("conflict")]
        conflict,
        [XmlEnum("connection-timeout")]
        connectiontimeout,
        [XmlEnum("hostgone")]
        hostgone,
        [XmlEnum("host-unknown")]
        hostunknown,
        [XmlEnum("improper-addressing")]
        improperaddressing,
        [XmlEnum("invalid-from")]
        invalidfrom,
        [XmlEnum("invalid-id")]
        invalidid,
        [XmlEnum("invalid-namespace")]
        invalidnamespace,
        [XmlEnum("invalid-xml")]
        invalidxml,
        [XmlEnum("not-well-formed")]
        notwellformed,
        [XmlEnum("remote-connection-failed")]
        remoteconnectionfailed,
        [XmlEnum("reset")]
        reset,
        [XmlEnum("restricted-xml")]
        restrictedxml,
        [XmlEnum("see-other-host")]
        seeotherhost,
        [XmlEnum("system-shutdown")]
        systemshutdown,
        [XmlEnum("unsupported-encoding")]
        unsupportedencoding,
        [XmlEnum("unsupported-stanza-type")]
        unsupportedstanzatype,
        [XmlEnum("unsupported-version")]
        unsupportedversion
    }

    
    public class ErrorDescription : IXmlSerializable
    {
        public ErrorDescription()
        {
        }

        public ErrorDescription(string strDesc)
        {
            Description = strDesc;
        }

        public ErrorDescription(ErrorType type)
        {
            ErrorType = type;
        }

        public static string GetEnumDescription(ErrorType value)
        {
            Type type = typeof(ErrorType);

            System.Reflection.FieldInfo [] fis = type.GetFields();
            foreach(System.Reflection.FieldInfo fi in fis) 
            {
                if (fi.Name == value.ToString())
                {
                    XmlEnumAttribute[] attributes = (XmlEnumAttribute[]) fi.GetCustomAttributes(typeof(XmlEnumAttribute), false);
                    foreach (XmlEnumAttribute attr in attributes)
                    {
                        return attr.Name;
                    }
                }
            }
            return null;
        }

        public static ErrorType FindEnumFromString(string strValue)
        {
            Type type = typeof(ErrorType);

            System.Reflection.FieldInfo[] fis = type.GetFields();
            foreach (System.Reflection.FieldInfo fi in fis)
            {
                XmlEnumAttribute[] attributes = (XmlEnumAttribute[])fi.GetCustomAttributes(typeof(XmlEnumAttribute), false);
                foreach (XmlEnumAttribute attr in attributes)
                {
                    if (attr.Name == strValue)
                        return (ErrorType) fi.GetRawConstantValue();
                    break;
                }
            }
            return ErrorType.unknown;
        }

        private string m_strDescription = null;

        public string Description
        {
            get 
            { 
                return m_strDescription; 
            }
            set 
            { 
                m_strDescription = value; 
            }
        }

        public override string ToString()
        {
            return m_strDescription;
        }

        public ErrorType ErrorType
        {
            get
            {
                return FindEnumFromString(m_strDescription);
            }
            set
            {
                Description = GetEnumDescription(value);
                if (Description == null)
                    Description = value.ToString();
            }
        }

        public void Write(XElement elemError)
        {
            XElement elemnode = new XElement(Description);
            elemError.Add(elemnode);
        }

        #region IXmlSerializable Members

        public XmlSchema  GetSchema()
        {
 	        return null;
        }

        private string m_strInnerErrorText = null;

        public string InnerErrorText
        {
            get { return m_strInnerErrorText; }
            set { m_strInnerErrorText = value; }
        }

        public void  ReadXml(XmlReader reader)
        {
            Description = reader.Name;
            InnerErrorText = reader.ReadInnerXml();
        }

        public void  WriteXml(XmlWriter writer)
        {
            
            if (Description != null)
 	           writer.WriteElementString(Description, "");
        }

        public static implicit operator ErrorDescription(string strDesc)
        {
            return new ErrorDescription(strDesc);
        }

        public static implicit operator string(ErrorDescription objError)
        {
            if (objError == null)
                return null;
            return objError.Description;
        }


        #endregion
    }

      // Build this straight from xml
    [XmlRoot(ElementName = "error")]
    public class Error
    {
        public Error()
        {
        }

        public Error(ErrorType type)
        {
            this.ErrorDescription = new ErrorDescription(type);
        }

        public Error(string strError)
        {
            this.ErrorDescription = new ErrorDescription(strError);
        }


        private string m_strType = null;
        [XmlAttribute(AttributeName = "type")]
        [DataMember]
        public string Type
        {
            get { return m_strType; }
            set { m_strType = value; }
        }

        private string m_strCode = null;
        [XmlAttribute(AttributeName = "code")]
        [DataMember]
        public string Code
        {
            get { return m_strCode; }
            set { m_strCode = value; }
        }

        [XmlAnyElement()]
        public ErrorDescription ErrorDescription = null;

//#if WINDOWS_PHONE
//#else
//        public ErrorDescription ErrorDescription  = null;
//#endif      

    }

    //<iq type="get" id="791-126" from="ninethumbs.com" to="ninethumbs.com/411f8597"><ping xmlns="urn:xmpp:ping"/></iq>
#if !WINDOWS_PHONE
    [XmlRoot(ElementName = "iq")]
#endif
    public class IQ : XMPPMessageBase
    {
        public IQ(string strXML) : base(strXML, "iq")
        {
        }
        public IQ()
            : base(null, "iq")
        {
            this.Type = IQType.get.ToString();
        }

        private Error m_objError = null;
        [XmlElement(ElementName = "error")]
        public Error Error
        {
            get { return m_objError; }
            set { m_objError = value; }
        }

        /// <summary>
        ///  This is the old way of building... Until we fully serialize everything we're stuck with leaving this in
        /// </summary>
        /// <param name="elemMessage"></param>
        public override void AddInnerXML(XElement elemMessage)
        {
            if (Error != null)
            {
                XElement elemError = new XElement("error");
                if (Error.Type != null)
                    elemError.Add(new XAttribute("type", Error.Type));
                if (Error.ErrorDescription != null) 
                {
                    Error.ErrorDescription.Write(elemError);
                }
                elemMessage.Add(elemError);
            }
            base.AddInnerXML(elemMessage);

        }

        /// <summary>
        ///  This is the old way of parsing... Until we fully serialize everything we're stuck with leaving this in
        /// </summary>
        public override void ParseInnerXML(XElement elem)
        {
            foreach (XElement elemerror in elem.Descendants("error"))
            {
                this.Error = new Error();
                if (elemerror.FirstNode != null)
                {
                    try
                    {
                        this.Error.ErrorDescription = ((XElement)elemerror.FirstNode).Name.ToString();
                    }
                    catch (Exception)
                    {
                        // todo.. make all nodes use object streaming instead of parsing if possible on all platforms
                    }
                }
                break;
            }
            base.ParseInnerXML(elem);
        }
    }



#if !WINDOWS_PHONE
    [XmlRoot(ElementName = "message")]
#endif
    public class Message : XMPPMessageBase
    {
        public Message()
            : base(null, "message")
        {
        }

        public Message(string strXML)
            : base(strXML, "message")
        {
        }

        public override void AddInnerXML(XElement elemMessage)
        {
            if (Delivered > DateTime.MinValue)
            {
                elemMessage.Add(new XElement("delay", new XAttribute("stamp", Delivered.ToString()), new XAttribute("from", From), new XAttribute("xmlns", "urn:xmpp:delay")));
            }
            if ((InnerXML != null) && (InnerXML.Length > 0))
                elemMessage.Add(XElement.Parse(InnerXML));

            base.AddInnerXML(elemMessage);
        }

        public override void ParseInnerXML(XElement elem)
        {
            foreach (XElement node in elem.Nodes())
            {
                if (node.Name == "{urn:xmpp:delay}delay")
                {
                    XAttribute attrts = node.Attribute("stamp");
                    if (attrts != null)
                    {
                        Delivered = DateTime.Parse(attrts.Value);
                    }
                }
            }

            base.ParseInnerXML(elem);
        }

     

        private DateTime ?m_dtDelivered = null;

        public DateTime? Delivered
        {
            get { return m_dtDelivered; }
            set { m_dtDelivered = value; }
        }

    }

    [XmlRoot(ElementName = "message")]
    public class ChatMessage : Message
    {

        public ChatMessage(string strXML)
            : base(strXML)
        {
        }

        public override void AddInnerXML(XElement elemMessage)
        {
            if ((Body != null) && (Body.Length > 0))
            {
                elemMessage.Add(new XElement("body", new XText(Body)));
            }
            base.AddInnerXML(elemMessage);
        }

        public override void ParseInnerXML(XElement elem)
        {
            foreach (XElement node in elem.Nodes())
            {
                if (node.Name == "body")
                {
                    Body = node.Value;
                }
                else if (node.Name == "{http://jabber.org/protocol/chatstates}active")
                {
                    ConversationState = ConversationState.active;
                }
                else if (node.Name == "{http://jabber.org/protocol/chatstates}paused")
                {
                    ConversationState = ConversationState.paused;
                }
                else if (node.Name == "{http://jabber.org/protocol/chatstates}composing")
                {
                    ConversationState = ConversationState.composing;
                }
            }
            base.ParseInnerXML(elem);
        }

        ConversationState m_eConversationState = ConversationState.none;

        public ConversationState ConversationState
        {
            get { return m_eConversationState; }
            set { m_eConversationState = value; }
        }

        private string m_strBody = null;

        public string Body
        {
            get { return m_strBody; }
            set { m_strBody = value; }
        }
    }




}
