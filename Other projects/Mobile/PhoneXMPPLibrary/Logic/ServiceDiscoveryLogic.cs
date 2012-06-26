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


    /// <summary>
    /// Responsible for setting our sessions' presence
    /// </summary>
    public class ServiceDiscoveryLogic : Logic
    {
        public ServiceDiscoveryLogic(XMPPClient client)
            : base(client)
        {
        }



        ServiceDiscoveryIQ IQInfoRequest = null;

        public void QueryServiceInfo()
        {
            IQInfoRequest = new ServiceDiscoveryIQ();
            IQInfoRequest.ServiceDiscoveryInfoQuery = new ServiceDiscoveryInfoQuery();
            IQInfoRequest.From = XMPPClient.JID;
            IQInfoRequest.To = XMPPClient.Domain;
            IQInfoRequest.Type = IQType.get.ToString();
            XMPPClient.SendObject(IQInfoRequest);
        }

        ServiceDiscoveryIQ IQItemRequest = null;
        public void QueryServiceItems()
        {
            IQItemRequest = new ServiceDiscoveryIQ();
            IQItemRequest.ServiceDiscoveryItemQuery = new ServiceDiscoveryItemQuery();
            IQItemRequest.From = XMPPClient.JID;
            IQItemRequest.To = XMPPClient.Domain;
            IQItemRequest.Type = IQType.get.ToString();
            XMPPClient.SendObject(IQItemRequest);
        }
     

        // Look for subscribe message to subscribe to presence
        public override bool NewIQ(IQ iq)
        {
    
            //// XEP-0030
            ///<iq type='get' from='romeo@montague.net/orchard' to='plays.shakespeare.lit' id='info1'>  
            ///   <query xmlns='http://jabber.org/protocol/disco#info'/>
            ///</iq>

            if ( (iq is ServiceDiscoveryIQ) && (iq.Type == IQType.get.ToString()) )
            {
                ServiceDiscoveryIQ response = new ServiceDiscoveryIQ();
                response.To = iq.From;
                response.From = XMPPClient.JID;
                response.ID = iq.ID;
                response.Type = IQType.result.ToString();
                response.ServiceDiscoveryInfoQuery = new ServiceDiscoveryInfoQuery();
                response.ServiceDiscoveryInfoQuery.Features = XMPPClient.OurServiceDiscoveryFeatureList.ToArray();
                
                XMPPClient.SendObject(response);
                return true;
            }
            else if ( (IQInfoRequest != null) && (IQInfoRequest.ID == iq.ID))
            {
                if (iq is ServiceDiscoveryIQ)
                {
                    ServiceDiscoveryIQ response = iq as ServiceDiscoveryIQ;

                    if ((response.ServiceDiscoveryInfoQuery != null) && (response.ServiceDiscoveryInfoQuery.Features != null) && (response.ServiceDiscoveryInfoQuery.Features.Length > 0))
                    {
                        XMPPClient.ServerServiceDiscoveryFeatureList.Features.Clear();
                        XMPPClient.ServerServiceDiscoveryFeatureList.Features.AddRange(response.ServiceDiscoveryInfoQuery.Features);
                    }
                    if ((response.ServiceDiscoveryInfoQuery != null) && (response.ServiceDiscoveryInfoQuery.Identities != null) && (response.ServiceDiscoveryInfoQuery.Identities.Length > 0))
                    {
                    }
                }
                return true;
            }
            else if ((IQItemRequest != null) && (IQItemRequest.ID == iq.ID))
            {
                if (iq is ServiceDiscoveryIQ)
                {
                    ServiceDiscoveryIQ response = iq as ServiceDiscoveryIQ;

                    if ((response.ServiceDiscoveryItemQuery != null) && (response.ServiceDiscoveryItemQuery.Items != null) && (response.ServiceDiscoveryItemQuery.Items.Length > 0))
                    {
                        XMPPClient.ServerServiceDiscoveryFeatureList.Items.AddRange(response.ServiceDiscoveryItemQuery.Items);
                        System.Threading.ThreadPool.QueueUserWorkItem(QueryItemsForProxy);
                    }
                }
                return true;
            }

            return base.NewIQ(iq);
        }

        void QueryItemsForProxy(object obj)
        {
            item [] items = XMPPClient.ServerServiceDiscoveryFeatureList.Items.ToArray();
            foreach (item nextitem in items)
            {
                QueryItemType(nextitem);
            }
        }

        void QueryItemType(item item)
        {
            if (item.ItemType == ItemType.NotQueried)
            {
                ServiceDiscoveryIQ iqqueryproxy = new ServiceDiscoveryIQ();
                iqqueryproxy.From = XMPPClient.JID;
                iqqueryproxy.To = item.JID;
                iqqueryproxy.ServiceDiscoveryInfoQuery = new ServiceDiscoveryInfoQuery();
                iqqueryproxy.Type = IQType.get.ToString();


                IQ iqret = XMPPClient.SendRecieveIQ(iqqueryproxy, 15000, SerializationMethod.XMLSerializeObject);
                if ((iqret != null) && (iqret is ServiceDiscoveryIQ))
                {
                    ServiceDiscoveryIQ response = iqret as ServiceDiscoveryIQ;
                    if ((response.ServiceDiscoveryInfoQuery != null) && (response.ServiceDiscoveryInfoQuery.Identities != null) && (response.ServiceDiscoveryInfoQuery.Identities.Length > 0))
                    {
                        if ((response.ServiceDiscoveryInfoQuery.Identities[0].Category == "proxy") &&
                            (response.ServiceDiscoveryInfoQuery.Identities[0].Type == "bytestreams"))
                        {
                            item.ItemType = ItemType.SOCKS5ByteStream;
                            return;
                        }
                        if (response.ServiceDiscoveryInfoQuery.Identities[0].Category == "pubsub")
                        {
                            item.ItemType = ItemType.PubSub;
                            return;
                        }
                        if (response.ServiceDiscoveryInfoQuery.Identities[0].Category == "directory")
                        {
                            item.ItemType = ItemType.Directory;
                            return;
                        }
                        if (response.ServiceDiscoveryInfoQuery.Identities[0].Category == "conference")
                        {
                            item.ItemType = ItemType.Conference;
                            return;
                        }
                    }
                }


                item.ItemType = ItemType.Unknown;
            }
        }

    }
}
