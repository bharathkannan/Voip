/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Net;

using System.Xml;

namespace System.Net.XMPP
{
    // An XML message right under the <stream> node
    // Derived classes include message, iq and presence
    public class XMPPStanza
    {
        public XMPPStanza(XMPPClient client)
        {
        }

        public XMPPStanza(string strXML)
        {
            m_strXML = strXML;
          //  Node = new XMPPXMLNode(m_strXML);
        }
        //public XMPPXMLNode Node = null;

        
        protected string m_strXML = "";
        public string XML
        {
            get
            {
                return m_strXML;
            }
            set
            {
                m_strXML = value;
                //Node = new XMPPXMLNode(m_strXML);  
            }
        }

    }

    public class OpenStreamStanza : XMPPStanza
    {
        public OpenStreamStanza(XMPPClient client) : base (client)
        {
            m_strXML =
@"<?xml version=""1.0""?>
<stream:stream xmlns:stream=""http://etherx.jabber.org/streams"" version=""1.0"" xmlns=""jabber:client"" to=""##TO##"" xml:lang=""en"" xmlns:xml=""http://www.w3.org/XML/1998/namespace"" >";
//            m_strXML = m_strXML.Replace("##FROM##", client.JID.BareJID);
            m_strXML = m_strXML.Replace("##TO##", client.Domain);

        }
    }
}
