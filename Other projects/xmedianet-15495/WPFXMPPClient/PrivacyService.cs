/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net.XMPP;
using System.Xml.Serialization;
using System.Xml.Linq;


namespace WPFXMPPClient
{
    public enum PrivacyCommand
    {
        clearchathistory,
        hidechatwindow,
    }

    [XmlRoot(ElementName="message")]
    public class PrivacyMessage : Message
    {
        public PrivacyMessage()
        : base()
        {
            this.Type = "privacy";
            Delivered = null;
        }
        public PrivacyMessage(string strXML)
            : base(strXML)
        {
            this.Type = "privacy";
        }

        private PrivacyCommand m_ePrivacyCommand = PrivacyCommand.clearchathistory;
        [XmlElement("command")]
        public PrivacyCommand PrivacyCommand
        {
            get { return m_ePrivacyCommand; }
            set { m_ePrivacyCommand = value; }
        }


    }

    /// <summary>
    ///  This is an example of how to implement a custom service.  Most users would probably object to this service when using our client, 
    ///  because it forces the client to clear the current chat history or to minimize the window.  Since this is just an example, we'll allow
    ///  it.  These commands will be activated by sending a "[clear]" or "[minimize]" message.
    ///  
    ///  In this example, the PrivacyService is both the service and message parser
    ///  
    /// </summary>
    public class PrivacyService : Logic, IXMPPMessageBuilder
    {
        public PrivacyService(XMPPClient client)
            : base(client)
        {
            if (client.OurServiceDiscoveryFeatureList.HasFeature(ServiceString) == false) //register this with service discover
                client.OurServiceDiscoveryFeatureList.AddFeature(new feature(ServiceString));

            /// Register this object as a message parser, and add this logic to our XMPP client instance
            /// Other user services don't have to follow this pattern, then can add these two steps where ever
            XMPPClient.XMPPMessageFactory.AddMessageBuilder(this);
            XMPPClient.AddLogic(this);
        }



        public const string ServiceString = "http://example.where/privacy/v1.0";

        #region IXMPPMessageBuilder Members

        public Message BuildMessage(System.Xml.Linq.XElement elem, string strXML)
        {
            XAttribute attrType = elem.Attribute("type");
            if (attrType != null)
            {
                if (attrType.Value == "privacy")
                {
                    PrivacyMessage query = Utility.ParseObjectFromXMLString(strXML, typeof(PrivacyMessage)) as PrivacyMessage;
                    return query;

                }
            }

            return null;
        }

        public IQ BuildIQ(System.Xml.Linq.XElement elem, string strXML)
        {
            return null;
        }

        #endregion


        public void ForceUserToClearMyHistory(JID jiduser)
        {
            PrivacyMessage msg = new PrivacyMessage();
            msg.From = XMPPClient.JID;
            msg.To = jiduser;
            msg.PrivacyCommand = PrivacyCommand.clearchathistory;
            XMPPClient.SendObject(msg);
        }

        public void ForceUserToMinimizeMyWindow(JID jiduser)
        {
            PrivacyMessage msg = new PrivacyMessage();
            msg.From = XMPPClient.JID;
            msg.To = jiduser;
            msg.PrivacyCommand = PrivacyCommand.hidechatwindow;
            XMPPClient.SendObject(msg);
        }


        public event DelegateRosterItemAction OnMustClearUserHistory = null;
        public event DelegateRosterItemAction OnMustHideMyChatWindow = null;

        public override bool NewMessage(Message msg)
        {
            if (msg is PrivacyMessage)
            {

                PrivacyMessage pmsg = msg as PrivacyMessage;
                RosterItem item = XMPPClient.FindRosterItem(msg.From);
                if ( (pmsg.PrivacyCommand == PrivacyCommand.clearchathistory) && (item != null) )
                {
                    if (OnMustClearUserHistory != null)
                        OnMustClearUserHistory(item, XMPPClient);
                }
                else if ((pmsg.PrivacyCommand == PrivacyCommand.hidechatwindow) && (item != null))
                {
                    if (OnMustHideMyChatWindow != null)
                        OnMustHideMyChatWindow(item, XMPPClient);
                }

                return true;
            }

            return false;
        }


        #region IXMPPMessageBuilder Members


        public PresenceMessage BuildPresence(XElement elem, string strXML)
        {
            return null;
        }

        #endregion
    }


}
