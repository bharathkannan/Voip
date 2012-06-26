/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml;
using System.Xml.Serialization;
using System.Xml.Linq;

namespace System.Net.XMPP
{
    /// <summary>
    /// Handles services for personnel event.  Music, avatars and geo location go here
    /// </summary>
    public class PersonalEventingLogic : Logic
    {
        public PersonalEventingLogic(XMPPClient client)
            : base(client)
        {
        }

        List<string> ListSentIQs = new List<string>();

        public bool PublishTuneInfo(TuneItem item)
        {
            //string strTuneXML = Utility.GetXMLStringFromObject(item);


            PubSubIQ iq = new PubSubIQ();
            iq.Type = IQType.set.ToString();
            iq.To = null; /// null for personal eventing pub sub
            iq.From = XMPPClient.JID;
            iq.PubSub.Publish = new Publish() { Node = "http://jabber.org/protocol/tune", Item = new PubSubItem() };
            iq.PubSub.Publish.Item.SetNodeFromObject(item);

            ListSentIQs.Add(iq.ID);

            XMPPClient.SendObject(iq);
            return true;
        }

        public bool PublishGeoInfo(geoloc item)
        {
            string strGeoInfo = Utility.GetXMLStringFromObject(item);

            PubSubIQ iq = new PubSubIQ();
            iq.Type = IQType.set.ToString();
            iq.To = null; /// null for personal eventing pub sub
            iq.From = XMPPClient.JID;
            iq.PubSub.Publish = new Publish() { Node = "http://jabber.org/protocol/geoloc", Item = new PubSubItem()};
            iq.PubSub.Publish.Item.SetNodeFromObject(item);
            iq.PubSub.Publish.Item.Id = "lastlocation";

            ListSentIQs.Add(iq.ID);
            try
            {
                XMPPClient.SendObject(iq);
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

         public bool PublishAvatarData(byte [] bImageData, int nWidth, int nHeight)
        {
             // publish avatar data node
            avatardata data = new avatardata();
            data.ImageData = bImageData;
            //string strAvatarInfo = Utility.GetXMLStringFromObject(data);

            string strHash = XMPPClient.AvatarStorage.WriteAvatar(bImageData);

            PubSubIQ iq = new PubSubIQ();
            iq.Type = IQType.set.ToString();
            iq.To = null; /// null for personal eventing pub sub
            iq.From = XMPPClient.JID;
            iq.PubSub.Publish = new Publish() { Node = "urn:xmpp:avatar:data", Item = new PubSubItem() };
            iq.PubSub.Publish.Item.SetNodeFromObject(data);

            ListSentIQs.Add(iq.ID);
            XMPPClient.SendObject(iq);


             // publish avatar meta data node
            avatarmetadata metadata = new avatarmetadata();
            metadata.ImageInfo.ByteLength = bImageData.Length;
            metadata.ImageInfo.Width = nWidth;
            metadata.ImageInfo.Height = nHeight;
            //string strAvatarMetaData = Utility.GetXMLStringFromObject(metadata);

            PubSubIQ iqmeta = new PubSubIQ();
            iqmeta.Type = IQType.set.ToString();
            iqmeta.To = null; /// null for personal eventing pub sub
            iqmeta.From = XMPPClient.JID;
            iqmeta.PubSub.Publish = new Publish() { Node = "urn:xmpp:avatar:metadata", Item = new PubSubItem() {Id = strHash } };
            iqmeta.PubSub.Publish.Item.SetNodeFromObject(metadata);

            ListSentIQs.Add(iqmeta.ID);
            XMPPClient.SendObject(iqmeta);

            return true;
        }

         public void DownloadDataNode(JID jidto, string strNodeName, string strItem)
         {

             PubSubIQ iq = new PubSubIQ();
             iq.Type = IQType.set.ToString();
             iq.To = null; /// null for personal eventing pub sub
             iq.From = XMPPClient.JID;
             iq.PubSub.Publish = new Publish() { Node = strNodeName, Item = new PubSubItem() { Id = strNodeName } };

             ListSentIQs.Add(iq.ID);
             XMPPClient.SendObject(iq);
         }


        public override bool NewIQ(IQ iq)
        {
            if (ListSentIQs.Contains(iq.ID) == true)
            {
                ListSentIQs.Remove(iq.ID);
                return true;
            }

            return base.NewIQ(iq);
        }

        public override bool NewMessage(Message iq)
        {
            /// Look for pubsub events
            /// 
            if (iq is PubSubEventMessage)
            {
                PubSubEventMessage psem = iq as PubSubEventMessage;
                if (psem.Event != null)
                {
                    if ((psem.Event.Items != null) && (psem.Event.Items.Items != null) && (psem.Event.Items.Items.Length > 0))
                    {
                        PubSubItem psitem = psem.Event.Items.Items[0];
                        XElement elem = psitem.InnerItemXML as XElement;

                        if (psem.Event.Items.Node == "http://jabber.org/protocol/tune")
                        {
                            TuneItem item = psitem.GetObjectFromXML<TuneItem>();
                            if (item != null)
                            {
                                /// find the roster item, set the tune item
                                RosterItem rosteritem = XMPPClient.FindRosterItem(iq.From);
                                if (rosteritem != null)
                                {
                                    rosteritem.Tune = item;
                                }
                            }
                        }
                        else if (psem.Event.Items.Node == "http://jabber.org/protocol/geoloc")
                        {
                            geoloc item = psitem.GetObjectFromXML<geoloc>();
                            if (item != null)
                            {
                                /// find the roster item, set the tune item
                                RosterItem rosteritem = XMPPClient.FindRosterItem(iq.From);
                                if (rosteritem != null)
                                {
                                    rosteritem.GeoLoc = item;
                                }
                            }
                        }
                        else if (psem.Event.Items.Node == "http://jabber.org/protocol/mood")
                        {
                        }
                        else if (psem.Event.Items.Node == "urn:xmpp:avatar:metadata") /// item avatar metadata
                        {
                            /// We have update avatar info for this chap, we should then proceed to get the avatar data
                            /// 
                            foreach (PubSubItem objItem in psem.Event.Items.Items)
                            {
                                avatarmetadata meta = psitem.GetObjectFromXML<avatarmetadata>();
                                if (meta != null)
                                {
                                    /// Request this node ? maybe we get it automatically?
                                    /// 

                                }
                                /// Not sure why they would have more than 1 avatar item, so we'll ignore fom now
                                /// 
                                break;
                            }

                        }
                        else if (psem.Event.Items.Node == "urn:xmpp:avatar:data") /// item avatar
                        {
                            /// We have update avatar info for this chap, we should then proceed to get the avatar data
                            /// 
                            /// Works, but let's comment out for now to focus on more supported avatar methods
                            //foreach (PubSubItem objItem in psem.Items)
                            //{
                            //    avatardata data = Utility.ParseObjectFromXMLString(objItem.InnerItemXML, typeof(avatardata)) as avatardata;
                            //    if (data != null)
                            //    {
                            //        string strHash = XMPPClient.AvatarStorage.WriteAvatar(data.ImageData);
                            //        RosterItem item = XMPPClient.FindRosterItem(psem.From);
                            //        if (item != null)
                            //        {
                            //            item.AvatarImagePath = strHash;
                            //        }
                            //    }
                            //    /// Not sure why they would have more than 1 avatar item, so we'll ignore fom now
                            //    /// 
                            //    break;
                            //}

                        }

                    }
                }

            }

            return base.NewMessage(iq);
        }
    }
}
