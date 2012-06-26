/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.ComponentModel;

namespace System.Net.XMPP
{
    public class PubSubConfigForm : DataForm
    {
        public PubSubConfigForm() : base()
        {
            FormType = "http://jabber.org/protocol/pubsub#node_config";
        }

        private string m_strNodeName = "";

        public string NodeName
        {
            get { return m_strNodeName; }
            set { m_strNodeName = value; }
        }


        const string accessmodelvalues = "authorize, open, presence, roster, whitelist";
        const string accessmodellabels =
                @"Subscription requests must be approved and only subscribers may retrieve items,
                Anyone may subscribe and retrieve items, 
                Anyone with a presence subscription of both or from may subscribe and retrieve items,
                Anyone in the specified roster group(s) may subscribe and retrieve items, 
                Only those on a whitelist may subscribe and retrieve items";

        private string m_strAccessModel = "open";
        [ListSingleFormField("pubsub#access_model", "Who may subscribe and retrieve items", true,
            accessmodelvalues, accessmodellabels)]
        public string AccessModel
        {
            get { return m_strAccessModel; }
            set { m_strAccessModel = value; }
        }



        private string m_strBodyXSLT = null;
        [TextSingleFormField("pubsub#body_xslt", "The URL of an XSL transformation which can be applied to payloads in order to generate an appropriate message body element.", false)]
        public string BodyXSLT
        {
            get { return m_strBodyXSLT; }
            set { m_strBodyXSLT = value; }
        }


        const string childrenassociationpolicyvalues = "all, owners, whitelist";
        const string childrenassociationpolicylabels = 
                @"Anyone may associate leaf nodes with the collection,
                Only collection node owners may associate leaf nodes with the collection,
                Only those on a whitelist may associate leaf nodes with the collection";

        private string m_strChildrenAssociationPolicy = null;
        [ListSingleFormField("pubsub#children_association_policy", "Who may associate leaf nodes with a collection", false,
            childrenassociationpolicyvalues, childrenassociationpolicylabels)]
        public string ChildrenAssociationPolicy
        {
            get { return m_strChildrenAssociationPolicy; }
            set { m_strChildrenAssociationPolicy = value; }
        }


        private List<string> m_listChildrenAssociationWhiteList = new List<string>();
        [JIDMultiFormField("pubsub#children_association_whitelist", "The list of JIDs that may associate leaf nodes with a collection", false)]
        public List<string> ChildrenAssociationWhiteList
        {
            get { return m_listChildrenAssociationWhiteList; }
            set { m_listChildrenAssociationWhiteList = value; }
        }


        private List<string> m_listChildNodes = new List<string>();
        [TextMultiFormField("pubsub#children", "The child nodes (leaf or collection) associated with a collection", false)]
        public List<string> ChildNodes
        {
            get { return m_listChildNodes; }
            set { m_listChildNodes = value; }
        }

        private string m_strChildrenMax = "";
        [TextSingleFormField("pubsub#children_max", "The maximum number of child nodes that can be associated with a collection", false)]
        public string ChildrenMax
        {
            get { return m_strChildrenMax; }
            set { m_strChildrenMax = value; }
        }

        private List<string>m_listCollection = new List<string>();
        [TextMultiFormField("pubsub#collection", "The collection(s) with which a node is affiliated", false)]
        public List<string> Collection
        {
          get { return m_listCollection; }
          set { m_listCollection = value; }
        }

        private List<string> m_listContact = new List<string>();
        [JIDMultiFormField("pubsub#contact", "The JIDs of those to contact with questions", false)]
        public List<string> Contact
        {
          get { return m_listContact; }
          set { m_listContact = value; }
        }

        private string m_strDataFormXslt = null;
        [TextSingleFormField("pubsub#dataform_xslt", "'The URL of an XSL transformation which can be applied to the payload format in order to generate a valid Data Forms result that the client could display using a generic Data Forms rendering engine", false)]
        public string DataFormXslt
        {
          get { return m_strDataFormXslt; }
          set { m_strDataFormXslt = value; }
        }

        private bool m_bDeliverNotifications = true;
        [BoolFormField("pubsub#deliver_notifications", "Whether to deliver event notifications", true)]
        public bool DeliverNotifications
        {
          get { return m_bDeliverNotifications; }
          set { m_bDeliverNotifications = value; }
        }

        private Nullable<bool> m_bDeliverPayloads = null;
        [BoolFormField("pubsub#deliver_payloads", "Whether to deliver payloads with event notifications; applies only to leaf nodes", false)]
        public Nullable<bool> DeliverPayloads
        {
          get { return m_bDeliverPayloads; }
          set { m_bDeliverPayloads = value; }
        }

        private string m_strNodeDescription = null;
        [TextSingleFormField("pubsub#description", "A description of the node", false)]
        public string NodeDescription
        {
          get { return m_strNodeDescription; }
          set { m_strNodeDescription = value; }
        }

        private string m_strItemExpire = null;
        [TextSingleFormField("pubsub#item_expire", "Number of seconds after which to automatically purge items", false)]
        public string ItemExpire
        {
          get { return m_strItemExpire; }
          set { m_strItemExpire = value; }
        }

        const string itemreplyvalues = "owner, publisher";
        const string itemreplylabels = "Statically specify a replyto of the node owner(s), Dynamically specify a replyto of the item publisher";
        private string m_strItemReply = null;
        [ListSingleFormField("pubsub#itemreply", "Whether owners or publisher should receive replies to items", false, itemreplyvalues, itemreplylabels)]
        public string ItemReply
        {
          get { return m_strItemReply; }
          set { m_strItemReply = value; }
        }

        private string m_strLanguage = null;
        [ListSingleFormField("pubsub#language", "The default language of the node", false, null, null)]
        public string Language
        {
          get { return m_strLanguage; }
          set { m_strLanguage = value; }
        }


        private string m_strMaxItems = null;
        [TextSingleFormField("pubsub#max_items", "The maximum number of items to persist", false)]
        public string MaxItems
        {
          get { return m_strMaxItems; }
          set { m_strMaxItems = value; }
        }
        
        private string m_strMaxPayloadSize = null;
        [TextSingleFormField("pubsub#max_payload_size", "The maximum payload size in bytes", false)]
        public string MaxPayloadSize
        {
          get { return m_strMaxPayloadSize; }
          set { m_strMaxPayloadSize = value; }
        }

        const string nodetypevalues =  "leaf, collection";
        const string nodetypelabels = "The node is a leaf node (default), The node is a collection node";
        private string m_strNodeType = "leaf";
        [ListSingleFormField("pubsub#node_type", "Whether the node is a leaf (default) or a collection", true, nodetypevalues, nodetypelabels)]
        public string NodeType
        {
            get { return m_strNodeType; }
            set { m_strNodeType = value; }
        }

        const string notificationtypevalues = "normal, headline";
        const string notificationtypelabels = "Messages of type normal, Messages of type headline";
        private string m_strNotficationType = null;
        [ListSingleFormField("pubsub#notification_type", "Specify the delivery style for notifications", false, notificationtypevalues, notificationtypelabels)]
        public string NotficationType
        {
            get { return m_strNotficationType; }
            set { m_strNotficationType = value; }
        }

        private Nullable<bool> m_bNotifyConfig = null;
        [BoolFormField("pubsub#notify_config", "Whether to notify subscribers when the node configuration changes", false)]
        public Nullable<bool> NotifyConfig
        {
            get { return m_bNotifyConfig; }
            set { m_bNotifyConfig = value; }
        }

        private Nullable<bool> m_bNotifyDelete = null;
        [BoolFormField("pubsub#notify_delete", "Whether to notify subscribers when the node is deleted", false)]
        public Nullable<bool> NotifyDelete
        {
            get { return m_bNotifyDelete; }
            set { m_bNotifyDelete = value; }
        }

        private Nullable<bool> m_bNotifyRetract = null;
        [BoolFormField("pubsub#notify_retract", "Whether to notify subscribers when items are removed from the node", false)]
        public Nullable<bool> NotifyRetract
        {
            get { return m_bNotifyRetract; }
            set { m_bNotifyRetract = value; }
        }

        public Nullable<bool> m_bNotifySubscribe = null;
        [BoolFormField("pubsub#notify_sub", "Whether to notify owners about new subscribers and unsubscribes", false)]
        public Nullable<bool> NotifySubscribe
        {
            get { return m_bNotifySubscribe; }
            set { m_bNotifySubscribe = value; }
        }

        private Nullable<bool> m_bPersistItems = null;
        [BoolFormField("pubsub#persist_items", "Whether to persist items to storage", false)]
        public Nullable<bool> PersistItems
        {
            get { return m_bPersistItems; }
            set { m_bPersistItems = value; }
        }

        private Nullable<bool> m_bPresenceBasedDelivery = null;
        [BoolFormField("pubsub#presence_based_delivery", "Whether to deliver notifications to available users only", false)]
        public Nullable<bool> PresenceBasedDelivery
        {
            get { return m_bPresenceBasedDelivery; }
            set { m_bPresenceBasedDelivery = value; }
        }

        const string publishmodelvalues = "publishers, subscribers, open";
        const string publishmodellabels = "Only publishers may publish, Subscribers may publish, Anyone may publish";
        private string m_strPublishModel = null;
        [ListSingleFormField("pubsub#publish_model", "The publisher model", false, publishmodelvalues, publishmodellabels)]
        public string PublishModel
        {
            get { return m_strPublishModel; }
            set { m_strPublishModel = value; }
        }

        private Nullable<bool> m_bPurgeOffline = null;
        [BoolFormField("pubsub#purge_offline", "Whether to purge all items when the relevant publisher goes offline", false)]
        public Nullable<bool> PurgeOffline
        {
            get { return m_bPurgeOffline; }
            set { m_bPurgeOffline = value; }
        }

        private List<string> m_bRosterGroupsAllowed = new List<string>();
        [ListMultiFormField("pubsub#roster_groups_allowed", "The roster group(s) allowed to subscribe and retrieve items", false, null, null)]
        public List<string> RosterGroupsAllowed
        {
            get { return m_bRosterGroupsAllowed; }
            set { m_bRosterGroupsAllowed = value; }
        }

        const string slpivalues = "never, on_sub, on_sub_and_presence";
        const string slpilabels = "never, When a new subscription is processed, When a new subscription is processed and whenever a subscriber comes online";
        private string m_strSendLastPublishedItem = null;
        [ListSingleFormField("pubsub#send_last_published_item", "When to send the last published item", false, slpivalues, slpilabels)]
        public string SendLastPublishedItem
        {
            get { return m_strSendLastPublishedItem; }
            set { m_strSendLastPublishedItem = value; }
        }

        private Nullable<bool> m_bTempSub = null;
        [BoolFormField("pubsub#tempsub", "Whether to make all subscriptions temporary, based on subscriber presence", false)]
        public Nullable<bool> TempSub
        {
            get { return m_bTempSub; }
            set { m_bTempSub = value; }
        }

        private bool m_bAllowSubscribe = true;
        [BoolFormField("pubsub#subscribe", "Whether to allow subscriptions", true)]
        public bool AllowSubscribe
        {
            get { return m_bAllowSubscribe; }
            set { m_bAllowSubscribe = value; }
        }

        private string m_strTitle = null;
        [TextSingleFormField("pubsub#title", "A friendly name for the node", false)]
        public new string Title
        {
          get { return m_strTitle; }
          set { m_strTitle = value; }
        }

        private string m_strType = null;
        [TextSingleFormField("pubsub#type", "The type of node data, usually specified by the namespace of the payload (if any)", false)]
        public new string Type
        {
            get { return m_strType; }
            set { m_strType = value; }
        }

        //public string m_strSubscriptionDepth = "all";



    }
}
