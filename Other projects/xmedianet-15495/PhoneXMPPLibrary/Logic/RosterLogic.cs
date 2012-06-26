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
using System.Collections.Generic;


namespace System.Net.XMPP
{
    public enum subscription
    {
        both,
        from,
        none,
        remove,
        to
    }

    [XmlRoot(ElementName = "item", Namespace = "jabber:iq:roster")]
    public class rosteritem
    {
        public rosteritem()
        {
        }

        private string m_strJid = null;
        [XmlAttribute(AttributeName="jid")]
        public string JID
        {
            get { return m_strJid; }
            set { m_strJid = value; }
        }
        
        private string m_strName = null;
        [XmlAttribute(AttributeName = "name")]
        public string Name
        {
            get { return m_strName; }
            set { m_strName = value; }
        }

        private string m_strAsk = null;
        [XmlAttribute(AttributeName = "ask")]
        public string Ask
        {
            get { return m_strAsk; }
            set { m_strAsk = value; }
        }

        private string m_strApproved = null;
        [XmlAttribute(AttributeName = "approved")]
        public string Approved
        {
            get { return m_strApproved; }
            set { m_strApproved = value; }
        }

        /// <summary>
        /// Keep as string so we can leave out if desired
        /// </summary>
        private string m_strSubscription = null;
        [XmlAttribute(AttributeName = "subscription")]
        public string Subscription
        {
            get { return m_strSubscription; }
            set { m_strSubscription = value; }
        }

        private string [] m_strGroup = new string[] {};
        [XmlElement(ElementName="group")] /// Todo... should be an array
        public string [] Groups
        {
            get { return m_strGroup; }
            set { m_strGroup = value; }
        }

    }
    
    [XmlRoot(ElementName = "query", Namespace = "jabber:iq:roster")]
    public class rosterquery
    {
        public rosterquery()
        {
        }

        private string m_strVersion = null;
        [XmlAttribute(AttributeName = "ver")]
        public string Version
        {
            get { return m_strVersion; }
            set { m_strVersion = value; }
        }

        private rosteritem[] m_listRosterItems = new rosteritem[] { };
        [XmlElement(ElementName = "item")] 
        public rosteritem[] RosterItems
        {
            get { return m_listRosterItems; }
            set { m_listRosterItems = value; }
        }


    }

    [XmlRoot(ElementName = "iq")]
    public class RosterIQ : IQ
    {
        public RosterIQ()
            : base()
        {
        }
        public RosterIQ(string strXML)
            : base(strXML)
        {
        }

        private rosterquery m_objQuery = new rosterquery();
        [XmlElement(ElementName = "query", Namespace = "jabber:iq:roster")] 
        public rosterquery Query
        {
            get { return m_objQuery; }
            set { m_objQuery = value; }
        }

        [XmlIgnore()]
        public override string MessageXML
        {
            get
            {
                return Utility.GetXMLStringFromObject(this);

            }
            set
            {
                if ((value == null) || (value.Length <= 0))
                    return;

                RosterIQ iq = Utility.ParseObjectFromXMLString(value, typeof(RosterIQ)) as RosterIQ;
                if (iq != null)
                {
                    this.ID = iq.ID;
                    this.Type = iq.Type;
                    this.From = iq.From;
                    this.To = iq.To;

                    this.Query = iq.Query;
                }


            }

        }
       
    }
  

    /// <summary>
    /// Responsible for querying our roster and adding and removing users from our roster
    /// This logic never completes, it's always there in case we want to query the roster again
    /// </summary>
    public class RosterLogic : Logic
    {
        public RosterLogic(XMPPClient client)
            : base(client)
        {
            RosterIQ = new RosterIQ();
            RosterIQ.Type = IQType.get.ToString();
            RosterIQ.To = null;
            RosterIQ.From = null;
            
        }

        //<iq xmlns="jabber:client" type="get" id="aab8a" >
        //    <query xmlns="jabber:iq:roster"/>
        //</iq>

        public const string Query = @"<query xmlns=""jabber:iq:roster""/>";

        RosterIQ RosterIQ = null;
        public override void  Start()
        {
            XMPPClient.SendObject(RosterIQ);
        }


        public void SubscribeToUser()
        {
        }


        public override bool NewIQ(IQ iq)
        {
            if (!(iq is RosterIQ))
                return false;

            RosterIQ rostiq = iq as RosterIQ;

            if (iq.ID == RosterIQ.ID)
            {
                //<iq type="result" id="aab8a" to="test@ninethumbs.com/hypnotoad">
                   //<query xmlns="jabber:iq:roster">
                       ///<item jid="brianbonnett@ninethumbs.com" name="BrianBonnett" subscription="both">
                       ///    <group>Friends</group>
                       ///</item>
                   ///</query>
                ///</iq>
                ///
                

                this.Success = false;
                if (iq.Type == IQType.result.ToString())
                {
                    if ( (rostiq.Query != null) && (rostiq.Query.RosterItems != null) )
                    {
                        foreach (rosteritem item in rostiq.Query.RosterItems)
                        {
                            RosterItem ros = new RosterItem(XMPPClient, item.JID);

                            if (item.Ask == "subscribe")
                            {
                                /// Need to do subscribe to this user's presence some how
                                /// 
                            }

                            /// See if we already have this roster item
                            /// 
                            RosterItem existingitem = XMPPClient.FindRosterItem(ros.JID);
                            if (existingitem != null)
                            {
                                existingitem.Name = (item.Name != null) ? item.Name : ros.JID.BareJID;
                                existingitem.Subscription = (item.Subscription != null) ? item.Subscription : "";
                                existingitem.Node = item;
                                existingitem.Groups.Clear();
                                /// Get the group for this item
                                if (item.Groups != null)
                                {
                                    foreach (string strGroup in item.Groups)
                                    {
                                        ros.Group = strGroup;
                                        ros.Groups.Add(strGroup);
                                    }
                                }
                            }
                            else
                            {
                                ros.Name = (item.Name != null) ? item.Name : ros.JID.BareJID;
                                ros.Subscription = (item.Subscription != null) ? item.Subscription : "";
                                ros.Node = item;
                                XMPPClient.RosterItems.Add(ros);
                                /// Get the group for this item
                                if (item.Groups != null)
                                {
                                    foreach (string strGroup in item.Groups)
                                    {
                                        ros.Group = strGroup;
                                        if (ros.Groups.Contains(strGroup) == false)
                                            ros.Groups.Add(strGroup);
                                    }
                                }
                            }
                        }

                    }

                    this.Success = true;
                    XMPPClient.FireGotRoster();
                }

                this.IsCompleted = false;
                
                return true;
            }
            else if (iq.Type == "set")
            {
                //<iq type="set" id="640-356" to="test2@ninethumbs.com/phone"><query xmlns="jabber:iq:roster"><item jid="test@ninethumbs.com" ask="subscribe" subscription="from"/></query></iq>

                if ( (rostiq.Query != null) && (rostiq.Query.RosterItems != null) )
                {
                    foreach (rosteritem item in rostiq.Query.RosterItems)
                    {
                        RosterItem ros = new RosterItem(XMPPClient, item.JID)
                        {
                            XMPPClient = XMPPClient,
                            Name = item.Name,
                            Subscription = item.Subscription,
                            Node = item,
                        };

                        if (XMPPClient.FindRosterItem(ros.JID) == null)
                        {

                            XMPPClient.RosterItems.Add(ros);

                            if (item.Groups != null)
                            {
                                foreach (string strGroup in item.Groups)
                                {
                                    ros.Group = strGroup;
                                    if (ros.Groups.Contains(strGroup) == false)
                                        ros.Groups.Add(strGroup);
                                }
                            }

                            XMPPClient.AsyncFireListChanged();
                        }

                        if (item.Subscription == subscription.from.ToString())  /// should only have a from subscription if we've added the roster item
                        {
                            //if (XMPPClient.AutoAcceptPresenceSubscribe
                            /// subscribe to presence of this one
                            XMPPClient.PresenceLogic.SubscribeToPresence(ros.JID.BareJID);
                        }

                    }
 
                    iq.Type = IQType.result.ToString();
                    iq.To = iq.From;
                    iq.From = XMPPClient.JID;
                    iq.InnerXML = null;
                    XMPPClient.SendXMPP(iq);

                    this.Success = true;
                    return true;
                }

            }


            return false;
        }

        //public const string AddRosterQuery = @"<query xmlns=""jabber:iq:roster""><item jid=""##JID##"" name=""##NAME##""><group>##GROUP##</group></item></query>";

        public void AddToRoster(JID jid, string strName, string strGroup)
        {
             //<iq from='juliet@example.com/balcony'
             //      id='ph1xaz53'
             //      type='set'>
             //    <query xmlns='jabber:iq:roster'>
             //      <item jid='nurse@example.com'
             //            name='Nurse'>
             //        <group>Servants</group>
             //      </item>
             //    </query>
             //  </iq>

            //string strAddQuery = AddRosterQuery.Replace("##JID##", jid.BareJID);
            //strAddQuery = strAddQuery.Replace("##NAME##", strName);
            //strAddQuery = strAddQuery.Replace("##GROUP##", strGroup);

            RosterIQ AddRosterIQ = new RosterIQ();
            AddRosterIQ.Type = IQType.set.ToString();
            AddRosterIQ.To = null;
            AddRosterIQ.From = XMPPClient.JID;
            rosteritem newitem = new rosteritem();
            newitem.Name = strName;

            JID newjid = jid;
            if (newjid.User.Length <= 0)
            {
                newjid.User = newjid.Domain;
                newjid.Domain = XMPPClient.JID.Domain;
            }

            newitem.JID = newjid;
            newitem.Subscription = subscription.none.ToString();
            newitem.Groups = new string[] { strGroup };

            AddRosterIQ.Query.RosterItems = new rosteritem[] { newitem };

            

            IQ IQResponse = XMPPClient.SendRecieveIQ(AddRosterIQ, 10000);
            if (IQResponse != null)
            {
            }

        }
        
        //public const string DeleteRosterQuery = @"<query xmlns=""jabber:iq:roster""><item jid=""##JID##"" subscription=""remove"" /></query>";

        public void DeleteFromRoster(JID jid)
        {
        
         //<iq from='juliet@example.com/balcony'
         //      id='hm4hs97y'
         //      type='set'>
         //    <query xmlns='jabber:iq:roster'>
         //      <item jid='nurse@example.com'
         //            subscription='remove'/>
         //    </query>
         //  </iq>

            //string strDeleteQuery = DeleteRosterQuery.Replace("##JID##", jid.BareJID);

            RosterIQ DeleteRosterIQ = new RosterIQ();
            //DeleteRosterIQ.InnerXML = strDeleteQuery;
            DeleteRosterIQ.Type = IQType.set.ToString();
            DeleteRosterIQ.To = null;
            DeleteRosterIQ.From = XMPPClient.JID;
            //DeleteRosterIQ.xmlns = "jabber:client";

            rosteritem deleteitem = new rosteritem();
            deleteitem.JID = jid;
            deleteitem.Subscription = subscription.remove.ToString();

            DeleteRosterIQ.Query.RosterItems = new rosteritem[] { deleteitem };


            IQ IQResponse = XMPPClient.SendRecieveIQ(DeleteRosterIQ, 10000);
            if (IQResponse != null)
            {
            }

        }

    }
}
