/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Net;

using System.Xml;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Collections.Generic;

namespace System.Net.XMPP
{
    [XmlRoot(ElementName = "item")]
    public class PubSubItem : IXmlSerializable  /// Serialize our self since it doesn't seem to work correctly with our XElement in it
    {
        public PubSubItem()
        {
        }

        public PubSubItem(string strId)
        {
            Id = strId;
        }

        private string m_strId = null;
        [XmlAttribute(AttributeName="id")]
        public string Id
        {
            get { return m_strId; }
            set { m_strId = value; }
        }

        public XElement InnerItemXML = null;

        public void SetNodeFromObject(object obj)
        {
            string strObj = Utility.GetXMLStringFromObject(obj);
            InnerItemXML = Utility.GetXmlNode(strObj);
        }

        public T GetObjectFromXML<T>()
        {
            Type type = typeof(T);
            return (T) Utility.GetObjectFromElement(InnerItemXML, type);
        }
        #region IXmlSerializable Members

        public Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            m_strId = reader.GetAttribute("id");
            while (reader.Read() == true)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    string strElem = reader.ReadOuterXml();
                    strElem = strElem.Replace(@"xmlns=""http://jabber.org/protocol/pubsub""", ""); /// have to do this because can't figure out how to stop the above line from putting in the namespace.  It's not null so that namespace should be assumed, but isn't
                    if (strElem != null)
                        InnerItemXML = XElement.Parse(strElem);
                }
                reader.Read();
                break;
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            if (Id != null)
                writer.WriteAttributeString("id", Id);
            
            if (InnerItemXML != null)
                writer.WriteRaw(InnerItemXML.ToString());
        }

        #endregion
    }

    [XmlRoot(ElementName = "publish")]
    public class Publish
    {
        public Publish()
        {
        }

        private string m_strNode = null;
        [XmlAttribute(AttributeName="node")]
        public string Node
        {
            get { return m_strNode; }
            set { m_strNode = value; }
        }

        PubSubItem m_objItem = null;
        [XmlElement(ElementName="item")]
        public PubSubItem Item
        {
            get { return m_objItem; }
            set { m_objItem = value; }
        }

    }

    [XmlRoot(ElementName = "retract")]
    public class Retract
    {
        public Retract()
        {
        }

        private string m_strNode = null;
        [XmlAttribute(AttributeName = "node")]
        public string Node
        {
            get { return m_strNode; }
            set { m_strNode = value; }
        }

        [XmlElement(ElementName = "item")]
        public PubSubItem[] Items = null;
    }


    [XmlRoot(ElementName = "subscribe")]
    public class Subscribe
    {
        public Subscribe()
        {
        }

        private string m_strNode = null;
        [XmlAttribute(AttributeName = "node")]
        public string Node
        {
            get { return m_strNode; }
            set { m_strNode = value; }
        }

        [XmlAttribute(AttributeName = "jid")]
        public string JID = null;

       

    }

    [XmlRoot(ElementName = "subscription")]
    public class Subscription
    {
        public Subscription()
        {
        }

        [XmlAttribute(AttributeName="node")]
        public string Node = null;
        [XmlAttribute(AttributeName = "jid")]
        public string JID = null;
        [XmlAttribute(AttributeName = "subscription")]
        public string subscription = null;
        [XmlAttribute(AttributeName = "subid")]
        public string subid = null;
    }

    [XmlRoot(ElementName="affiliation")]
    public class Affiliation
    {
        public Affiliation()
        {
        }

        [XmlAttribute(AttributeName = "node")]
        public string Node = null;
        [XmlAttribute(AttributeName = "affiliation")]
        public string affiliation = null;
    }


    [XmlRoot(ElementName = "create")]
    public class Create
    {
        public Create()
        {
        }

        [XmlAttribute(AttributeName = "node")]
        public string Node = null;
    }

    [XmlRoot(ElementName = "delete")]
    public class Delete
    {
        public Delete()
        {
        }

        [XmlAttribute(AttributeName = "node")]
        public string Node = null;
    }

    [XmlRoot(ElementName = "configure")]
    public class Configure
    {
        public Configure()
        {
        }

        //public XElement X = null; /// x form data... TODO... make those classes serializable, redo DataForm.cs
    }


    [XmlRoot(ElementName = "default")]
    public class Default
    {
        public Default()
        {
        }

        //public XElement X = null; /// x form data... TODO... make those classes serializable, redo DataForm.cs
    }

    [XmlRoot(ElementName = "items")]
    public class PubSubItems
    {
        public PubSubItems()
        {
        }


        [XmlAttribute(AttributeName = "node")]
        public string Node = null;
        [XmlAttribute(AttributeName = "subid")]
        public string subid = null;

        protected PubSubItem[] m_objItems = null;
        [XmlElement(ElementName="item")]
        public PubSubItem[] Items
        {
            get { return m_objItems; }
            set { m_objItems = value; }
        }

        [XmlElement(ElementName = "retract")]
        public Retract Retract = null;


    }

    [XmlRoot(ElementName = "pubsub")]
    public class PubSub
    {
        public PubSub()
        {
        }

        [XmlElement(ElementName = "publish")]
        public Publish Publish = null;

        [XmlElement(ElementName = "subscribe")]
        public Subscribe Subscribe = null;

        [XmlElement(ElementName = "subscription")]
        public Subscription Subscription = null;

        [XmlElement(ElementName = "retract")]
        public Retract Retract = null;

        [XmlElement(ElementName = "create")]
        public Create Create = null;
        
        [XmlElement(ElementName = "delete")]
        public Delete Delete = null;
        
        [XmlElement(ElementName = "configure")]
        public Configure Configure = null;

        [XmlElement(ElementName = "default")]
        public Default Default = null;
        
        [XmlElement(ElementName = "items")]
        public PubSubItems Items = null;


        //[XmlArray(ElementName = "affiliations")]
        //[XmlArrayItem(ElementName = "affiliation")]
        [XmlElement(ElementName = "affiliation")]
        public Affiliation[] Affiliations = null;

    }

    [XmlRoot(ElementName = "iq")]
    public class PubSubIQ : IQ
    {
        public PubSubIQ()
            : base()
        {
            this.Type = IQType.set.ToString();
        }
      

        public PubSubIQ(string strXML)
            : base(strXML)
        {
        }

        private PubSub m_objPubSub = new PubSub();
        [XmlElement(ElementName = "pubsub", Namespace = "http://jabber.org/protocol/pubsub")]
        public PubSub PubSub
        {
          get { return m_objPubSub; }
          set { m_objPubSub = value; }
        }
    }


    [XmlRoot(ElementName = "event", Namespace = "http://jabber.org/protocol/pubsub#event")]
    public class Event
    {
        public Event()
        {
        }
       
        [XmlElement(ElementName = "items")]
        public PubSubItems Items = null;

        [XmlElement(ElementName = "retract")]
        public Retract Retract = null;

        [XmlElement(ElementName = "create")]
        public Create Create = null;

        [XmlElement(ElementName = "delete")]
        public Delete Delete = null;

        [XmlElement(ElementName = "configure")]
        public Configure Configure = null;

        [XmlElement(ElementName = "default")]
        public Default Default = null;


    }

    [XmlRoot(ElementName = "message")]
    public class PubSubEventMessage : Message
    {
        public PubSubEventMessage()
            : base()
        {
        }
        public PubSubEventMessage(string strXML)
            : base(strXML)
        {
        }

        [XmlElement(ElementName = "event", Namespace = "http://jabber.org/protocol/pubsub#event")]
        public Event Event = null;


        //public override void AddInnerXML(System.Xml.Linq.XElement elemMessage)
        //{
        //    XElement pubsubevent = new XElement("{http://jabber.org/protocol/pubsub#event}event");
        //    elemMessage.Add(pubsubevent);

        //    XElement itemsnode = new XElement("{http://jabber.org/protocol/pubsub#event}items");
        //    itemsnode.Add(new XAttribute("node", Node));
        //    pubsubevent.Add(itemsnode);

        //    foreach (PubSubItem item in Items)
        //    {
        //        item.AddXML(itemsnode);
        //    }

        //    base.AddInnerXML(elemMessage);
        //}

        //public override void ParseInnerXML(System.Xml.Linq.XElement elemMessage)
        //{
        //    /// Extract pubsub element, publish element, then item element
        //    /// 

        //    //<event xmlns="http://jabber.org/protocol/pubsub#event">
        //    //  <items node="GroceryList">
        //    //    <retract id="c62a3de9-4d88-493f-bbcb-3338dd18b7f4" />
        //    //  </items>
        //    //</event>

        //    Items.Clear();

        //    foreach (XElement pubsub in elemMessage.Descendants("{http://jabber.org/protocol/pubsub#event}event"))
        //    {
        //        XElement publish = pubsub.FirstNode as XElement;
        //        if ((publish != null) && (publish.Name == "{http://jabber.org/protocol/pubsub#event}items"))
        //        {
        //            if (publish.Attribute("node") != null)
        //                Node = publish.Attribute("node").Value;

        //            foreach (XElement elemitem in publish.Descendants("{http://jabber.org/protocol/pubsub#event}item"))
        //            {
        //                PubSubItem newitem = new PubSubItem();
        //                if (elemitem.Attributes("id") != null)
        //                    newitem.Id = elemitem.Attribute("id").Value;

        //                newitem.ParseXML(publish);
        //                Items.Add(newitem);
        //            }
        //            foreach (XElement elemitem in publish.Descendants("{http://jabber.org/protocol/pubsub#event}retract"))
        //            {
        //                if (elemitem.Attributes("id") != null)
        //                {
        //                    RetractIds.Add(elemitem.Attribute("id").Value);
        //                }
        //            }
        //        }
        //        break;
        //    }


        //    base.ParseInnerXML(elemMessage);
        //}
    }
}
