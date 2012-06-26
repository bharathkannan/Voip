using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace GroceryList
{

    [DataContract]
    [XmlRoot(ElementName = "groceryitem", Namespace=null)]
    public class GroceryItem
    {
        public GroceryItem()
        {
        }

        private string m_strName = null;
        [XmlElement(ElementName = "name")]
        [DataMember]
        public string Name
        {
            get { return m_strName; }
            set { m_strName = value; }
        }

        private bool m_bIsAccountedFor = false;
        [XmlElement(ElementName = "isaccountedfor")]
        [DataMember]
        public bool IsAccountedFor
        {
            get { return m_bIsAccountedFor; }
            set { m_bIsAccountedFor = value; }
        }

        private string m_strPerson = "";
        /// <summary>
        /// The person who has last modified this item
        /// </summary>
        [XmlElement(ElementName = "person")]
        [DataMember]
        public string Person
        {
            get { return m_strPerson; }
            set { m_strPerson = value; }
        }

        private string m_strPrice = "";
        /// <summary>
        /// The price that was paid or will be paid
        /// </summary>
        [XmlElement(ElementName = "price")]
        [DataMember]
        public string Price
        {
            get { return m_strPrice; }
            set { m_strPrice = value; }
        }

        private string m_strItemId = Guid.NewGuid().ToString();
        [XmlElement(ElementName = "itemid")]
        [DataMember]
        public string ItemId
        {
            get { return m_strItemId; }
            set { m_strItemId = value; }
        }
    }
}
