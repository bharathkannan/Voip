/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Net;

using System.Xml;
using System.Xml.Serialization;

namespace System.Net.XMPP
{

    [XmlRoot(ElementName = "N")]
    public class Name
    {
        public Name()
        {
        }

        private string m_strGivenName = null;
        [XmlElement(ElementName = "GIVEN")]
        public string GivenName
        {
            get { return m_strGivenName; }
            set { m_strGivenName = value; }
        }

        private string m_strFamilyName = null;
        [XmlElement(ElementName = "FAMILY")]
        public string FamilyName
        {
            get { return m_strFamilyName; }
            set { m_strFamilyName = value; }
        }

    }
    [XmlRoot(ElementName = "ADR")]
    public class Address
    {
        public Address()
        {
        }
        private string m_strCountry = null;
        [XmlElement(ElementName="CTRY")]
        public string Country
        {
            get { return m_strCountry; }
            set { m_strCountry = value; }
        }

        private string m_strLocality = null;
        [XmlElement(ElementName = "LOCALITY")]
        public string Locality
        {
            get { return m_strLocality; }
            set { m_strLocality = value; }
        }
        
        private string m_strHome = null;
        [XmlElement(ElementName = "HOME")]
        public string Home
        {
            get { return m_strHome; }
            set { m_strHome = value; }
        }
    }

    [XmlRoot(ElementName = "PHOTO")]
    public class Photo
    {
        public Photo()
        {
        }

        private string m_strType = "image/png";
        [XmlElement(ElementName = "TYPE")]
        public string Type
        {
            get { return m_strType; }
            set { m_strType = value; }
        }

        private byte[] m_bBytes = null;
        [XmlElement(ElementName="BINVAL", DataType="base64Binary")]
        public byte[] Bytes
        {
            get { return m_bBytes; }
            set { m_bBytes = value; }
        }
    }

    [XmlRoot(ElementName = "vCard", Namespace = "vcard-temp")]
    public class vcard
    {
        public vcard()
        {
        }

        private Name m_objName = null;
        [XmlElement(ElementName = "N")]
        public Name Name
        {
            get { return m_objName; }
            set { m_objName = value; }
        }

        private string m_strNickName = null;
        [XmlElement(ElementName = "NICKNAME")]
        public string NickName
        {
            get { return m_strNickName; }
            set { m_strNickName = value; }
        }

        private Photo m_objPhoto = null;
        [XmlElement(ElementName = "PHOTO")]
        public Photo Photo
        {
            get { return m_objPhoto; }
            set { m_objPhoto = value; }
        }

        private Address m_objAddress = null;
        [XmlElement(ElementName = "ADR")]
        public Address Address
        {
            get { return m_objAddress; }
            set { m_objAddress = value; }
        }


    }
}
