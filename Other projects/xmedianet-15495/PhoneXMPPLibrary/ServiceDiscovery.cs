/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Linq;
using System.Xml.Serialization;

namespace System.Net.XMPP
{
    [XmlRoot(ElementName="feature")]
    public class feature
    {
        public feature()
        {
        }

        public feature(string strVar)
        {
            Var = strVar;
        }
        private string m_strVar = "";

        [XmlAttribute(AttributeName = "var")]
        public string Var
        {
            get { return m_strVar; }
            set { m_strVar = value; }
        }

        public override bool Equals(object obj)
        {
            if (obj is feature)
            {
                feature sobj = obj as feature;
                if (sobj.Var == this.Var)
                    return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Var.GetHashCode();
        }

    }

    [XmlRoot(ElementName = "identity")]
    public class identity
    {
        public identity()
        {
        }
        public identity(string strCategory, string strType, string strName)
        {
            Category = strCategory;
            Type = strType;
            Name = strName;
        }

        private string m_strName;
        [XmlAttribute(AttributeName = "name")]
        public string Name
        {
            get { return m_strName; }
            set { m_strName = value; }
        }

        private string m_strCategory;
        [XmlAttribute(AttributeName = "category")]
        public string Category
        {
            get { return m_strCategory; }
            set { m_strCategory = value; }
        }

        private string m_strType;
        [XmlAttribute(AttributeName = "type")]
        public string Type
        {
            get { return m_strType; }
            set { m_strType = value; }
        }
    }

    public enum ItemType
    {
        NotQueried,
        Unknown,
        SOCKS5ByteStream,
        Directory,
        Conference,
        PubSub,
    }

    [XmlRoot(ElementName = "item")]
    public class item
    {
        public item()
        {
        }
        public item(string strJid, string strNode, string strName)
        {
            Node = strNode;
            JID = strJid;
            Name = strName;
        }

        private string m_strName;
        [XmlAttribute(AttributeName = "name")]
        public string Name
        {
            get { return m_strName; }
            set { m_strName = value; }
        }

        private string m_strJID;
        [XmlAttribute(AttributeName = "jid")]
        public string JID
        {
            get { return m_strJID; }
            set { m_strJID = value; }
        }

        private string m_strNode;
        [XmlAttribute(AttributeName = "node")]
        public string Node
        {
            get { return m_strNode; }
            set { m_strNode = value; }
        }


        /// <summary>
        /// Internal use so we can keep track if we've queried this item or not for what type it is
        /// </summary>
        [XmlIgnore()]
        public ItemType ItemType = ItemType.NotQueried;
    }

    public class ServiceDiscoveryFeatureList
    {
        public ServiceDiscoveryFeatureList()
        {
        }

        public bool HasFeature(string strFeature)
        {
            /// Make sure this feature doesn't exists
            /// 
            lock (m_LockFeatures)
            {
                foreach (feature fea in Features)
                {
                    if (fea.Var == strFeature)
                        return true;
                }

            }
            return false;
        }

        public void AddFeature(feature feature)
        {
            /// Make sure this feature doesn't exists
            /// 
            lock (m_LockFeatures)
            {
                foreach (feature fea in Features)
                {
                    if (fea.Equals(feature) == true)
                        return;
                }

                Features.Add(feature);
            }
        }


        public void RemoveFeature(feature feature)
        {
            /// Make sure this feature doesn't exists
            /// 
            lock (m_LockFeatures)
            {
                feature foundfeature = null;
                foreach (feature fea in Features)
                {
                    if (fea.Equals(feature) == true)
                    {
                        foundfeature = fea;
                        break;
                    }
                }

                if (foundfeature != null)
                   Features.Remove(foundfeature);
            }
        }

        public feature[] ToArray()
        {
            return Features.ToArray();
        }

        object m_LockFeatures = new object();

        List<feature> m_listFeatures = new List<feature>();

        public List<feature> Features
        {
            get { return m_listFeatures; }
            set { m_listFeatures = value; }
        }



        public item GetItem(string strItem)
        {
            foreach (item nextitem in Items)
            {
                if (nextitem.Name == strItem)
                    return nextitem;
            }

            return null;
        }

         public item GetItemByType(ItemType type)
        {
            foreach (item nextitem in Items)
            {
                if (nextitem.ItemType == type)
                    return nextitem;
            }

            return null;
        }

        List<item> m_listItems = new List<item>();
        public List<item> Items
        {
            get { return m_listItems; }
            set { m_listItems = value; }
        }
    }


    [XmlRoot(ElementName = "query", Namespace="http://jabber.org/protocol/disco#info")]
    public class ServiceDiscoveryInfoQuery
    {
        public ServiceDiscoveryInfoQuery()
        {
        }
        private feature[] m_afeatures = null;

        [XmlElement(ElementName="feature")]
        public feature[] Features
        {
            get { return m_afeatures; }
            set { m_afeatures = value; }
        }

        private identity[] m_aidentities = null;
        [XmlElement(ElementName = "identity")]
        public identity[] Identities
        {
            get { return m_aidentities; }
            set { m_aidentities = value; }
        }

    }

    [XmlRoot(ElementName = "query", Namespace = "http://jabber.org/protocol/disco#items")]
    public class ServiceDiscoveryItemQuery
    {
        public ServiceDiscoveryItemQuery()
        {
        }

        private string m_strNode = null;
        [XmlAttribute(AttributeName="node")]
        public string Node
        {
            get { return m_strNode; }
            set { m_strNode = value; }
        }

        private item[] m_aItems= null;
        [XmlElement(ElementName = "item")]
        public item[] Items
        {
            get { return m_aItems; }
            set { m_aItems = value; }
        }

     
    }

    [XmlRoot(ElementName = "iq")]
    public class ServiceDiscoveryIQ : IQ
    {
        public ServiceDiscoveryIQ()
            : base()
        {
        }
        public ServiceDiscoveryIQ(string strXML)
            : base(strXML)
        {
        }

        [XmlElement(ElementName = "query", Namespace = "http://jabber.org/protocol/disco#items")]
        public ServiceDiscoveryItemQuery ServiceDiscoveryItemQuery = null;

        [XmlElement(ElementName = "query", Namespace = "http://jabber.org/protocol/disco#info")]
        public ServiceDiscoveryInfoQuery ServiceDiscoveryInfoQuery = null;

  
    }

}
