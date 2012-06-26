/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Net;

using System.Xml;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Net.XMPP
{
    /// <summary>
    /// Manages adding and removing items from a pub sub node of type 'T'.  Node must already exist
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PubSubNodeManager<T> : Logic
    {
        public PubSubNodeManager(string strNode, XMPPClient client) : base(client)
        {
            Node = strNode;
        }

        private string m_strNode = "";
        public string Node
        {
          get { return m_strNode; }
          set { m_strNode = value; }
        }

        public void AddItem(string strItemId, T item)
        {
            PubSubIQ iq = new PubSubIQ();
            iq.Type = IQType.set.ToString();
            iq.To = new JID(string.Format("pubsub.{0}", XMPPClient.Domain));
            iq.From = XMPPClient.JID;
            iq.PubSub.Publish = new Publish();
            iq.PubSub.Publish.Node = Node;
            iq.PubSub.Publish.Item = new PubSubItem() { Id = strItemId};
            iq.PubSub.Publish.Item.SetNodeFromObject(item);

            Items.Add(item);
            ItemIdToObject.Add(strItemId, item);

            ListSentIQs.Add(iq);

            XMPPClient.SendObject(iq);
        }

        public string GetPubSubIdForItem(T item)
        {
            foreach (string strKey in ItemIdToObject.Keys)
            {
                if (ItemIdToObject[strKey].Equals(item) == true)
                    return strKey;
            }
            return null;
        }

        public void UpdateItem(string strItemId, T item)
        {
            PubSubIQ iq = new PubSubIQ();
            iq.Type = IQType.set.ToString();
            iq.To = new JID(string.Format("pubsub.{0}", XMPPClient.Domain));
            iq.From = XMPPClient.JID;
            iq.PubSub.Publish = new Publish();
            iq.PubSub.Publish.Node = Node;
            iq.PubSub.Publish.Item = new PubSubItem() { Id = strItemId};
            iq.PubSub.Publish.Item.SetNodeFromObject(item);

            ListSentIQs.Add(iq);

            XMPPClient.SendObject(iq);
        }

        public void DeleteItem(string strItemId, T item)
        {
            PubSubIQ iq = new PubSubIQ();
            iq.Type = IQType.set.ToString();
            iq.To = new JID(string.Format("pubsub.{0}", XMPPClient.Domain));
            iq.From = XMPPClient.JID;
            iq.PubSub.Retract = new Retract();
            iq.PubSub.Retract.Node = Node;
            iq.PubSub.Retract.Items = new PubSubItem[] { new PubSubItem() { Id = strItemId} };

            ListSentIQs.Add(iq);

            XMPPClient.SendObject(iq);
        }

        PubSubIQ IQGetAll = null;
        public void GetAllItems(string strSubId)
        {
            IQGetAll = new PubSubIQ();
            IQGetAll.Type = IQType.set.ToString();
            IQGetAll.To = new JID(string.Format("pubsub.{0}", XMPPClient.Domain));
            IQGetAll.From = XMPPClient.JID;
            IQGetAll.PubSub.Items = new PubSubItems() { Node = Node, subid=strSubId  };


            XMPPClient.SendObject(IQGetAll);
            
        }


        List<PubSubIQ> ListSentIQs = new List<PubSubIQ>();
        PubSubIQ FindSendingIQ(IQ incomingiq)
        {
            foreach (PubSubIQ nextiq in ListSentIQs)
            {
                if (nextiq.ID == incomingiq.ID)
                    return nextiq;
            }
            return null;
        }

        //public void Remove

        Dictionary<string, T> ItemIdToObject = new Dictionary<string, T>();
#if WINDOWS_PHONE
        private ObservableCollection<T> m_listItems = new ObservableCollection<T>();
        public ObservableCollection<T> Items
        {
            get { return m_listItems; }
            set { m_listItems = value; }
        }
#elif MONO
        private ObservableCollection<T> m_listItems = new ObservableCollection<T>();
        public ObservableCollection<T> Items
        {
            get { return m_listItems; }
            set { m_listItems = value; }
        }
#else
        private ObservableCollectionEx<T> m_listItems = new ObservableCollectionEx<T>();
        public ObservableCollectionEx<T> Items
        {
            get { return m_listItems; }
            set { m_listItems = value; }
        }
#endif

        public override bool NewIQ(IQ iq)
        {
            PubSubIQ SendingIQ = FindSendingIQ(iq);

            if (SendingIQ != null)
            {
                //PubSub iqrequest = ListSentIQs[iq.ID];
                ListSentIQs.Remove(SendingIQ);

                /// See if this was a retract request.  If it was and is successful, remove the item
                if (SendingIQ.PubSub.Retract != null)
                {
                    if (iq.Type == IQType.result.ToString())
                    {
                        string strItemId = SendingIQ.PubSub.Retract.Items[0].Id;
                        if (ItemIdToObject.ContainsKey(strItemId) == true)
                        {
                            T obj = ItemIdToObject[strItemId];
                            Items.Remove(obj);
                            ItemIdToObject.Remove(strItemId);
                        }
                    }
                }
                //else if (SendingIQ.PubSub.Publish != null)
                //{
                //    if (iq.Type == IQType.result.ToString())
                //    {
                //        string strItemId = SendingIQ.PubSub.Retract.Item.Id;
                //        if (ItemIdToObject.ContainsKey(strItemId) == true)
                //        {
                //            T obj = ItemIdToObject[strItemId];
                //            Items.Remove(obj);
                //            ItemIdToObject.Remove(strItemId);
                //        }
                //    }
                //}


                return true;
            }
            else if ((IQGetAll != null) && (IQGetAll.ID == iq.ID))
            {
                if (iq is PubSubIQ)
                {
                    PubSubIQ psem = iq as PubSubIQ;
                    if (psem != null)
                    {
                        if ((psem.PubSub.Items != null) && (psem.PubSub.Items.Items != null) && (psem.PubSub.Items.Items.Length > 0) )
                        {
                            m_listItems.Clear();
                            ItemIdToObject.Clear();

                            if (psem.PubSub.Items.Node == Node)
                            {
                                foreach (PubSubItem psi in psem.PubSub.Items.Items)
                                {
                                    T item = psi.GetObjectFromXML<T>();
                                    if (item != null)
                                    {
                                        Items.Add(item);
                                        ItemIdToObject.Add(psi.Id, item);

                                    }
                                }
                            }
                        }
                    }
                }
            }

            return base.NewIQ(iq);
        }

        public override bool NewMessage(Message iq)
        {
            if (iq is PubSubEventMessage)
            {
                PubSubEventMessage psem = iq as PubSubEventMessage;
                if (psem.Event != null)
                {

                    if ( (psem.Event.Items != null) && (psem.Event.Items.Node == Node) && (psem.Event.Items.Items != null))
                    {
                        foreach (PubSubItem psi in psem.Event.Items.Items)
                        {
                            T item = psi.GetObjectFromXML<T>();
                            if (item != null)
                            {
                                if (ItemIdToObject.ContainsKey(psi.Id) == false)
                                {
                                    Items.Add(item);
                                    ItemIdToObject.Add(psi.Id, item);
                                }
                                else  /// item with this id already exists, replace it with the new version
                                {
                                    T itemtoremove = ItemIdToObject[psi.Id];
                                    Items.Remove(itemtoremove);
                                    Items.Add(item);
                                    ItemIdToObject[psi.Id] = item;
                                }

                            }
                        }
                    }
                    if ((psem.Event.Retract != null) && (psem.Event.Retract.Node == Node) && (psem.Event.Retract.Items != null))
                    {
                        foreach (PubSubItem item  in psem.Event.Retract.Items)
                        {
                            string strRetract = item.Id;
                            if (ItemIdToObject.ContainsKey(strRetract) == true)
                            {
                                T itemtoremove = ItemIdToObject[strRetract];
                                Items.Remove(itemtoremove);
                                ItemIdToObject.Remove(strRetract);
                            }
                        }
                    }

                }
                 
            }
            return base.NewMessage(iq);
        }

    }
}
