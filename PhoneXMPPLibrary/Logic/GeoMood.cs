/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Net;

using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization;


///<feature var='http://jabber.org/protocol/geoloc'/>    
///<feature var='http://jabber.org/protocol/geoloc+notify'/>    
///<feature var='http://jabber.org/protocol/tune'/>    
///<feature var='http://jabber.org/protocol/tune+notify'/>

namespace System.Net.XMPP
{

    
    [DataContract]
    [XmlRoot(ElementName = "tune", Namespace = "http://jabber.org/protocol/tune")]
    public class TuneItem
    {
        public TuneItem()
        {
        }

        public override string ToString()
        {
            if (Title == null)
                return "";
            return string.Format("{0} - {1}", Title, Artist);
        }

        private string m_strArtist = null;
        [XmlElement(ElementName = "artist")]
        [DataMember]
        public string Artist
        {
            get { return m_strArtist; }
            set { m_strArtist = value; }
        }

        private int m_nLength = 0;
        [XmlElement(ElementName = "length")]
        [DataMember]
        public int Length
        {
            get { return m_nLength; }
            set { m_nLength = value; }
        }

        private string m_strSource = null;
        [XmlElement(ElementName = "source")]
        [DataMember]
        public string Source
        {
            get { return m_strSource; }
            set { m_strSource = value; }
        }

        private string m_strTitle = null;
        [XmlElement(ElementName = "title")]
        [DataMember]
        public string Title
        {
            get { return m_strTitle; }
            set { m_strTitle = value; }
        }

        private string m_strTrack = null;
        [XmlElement(ElementName = "track")]
        [DataMember]
        public string Track
        {
            get { return m_strTrack; }
            set { m_strTrack = value; }
        }
    }




    [DataContract]
    [XmlRoot(ElementName = "geoloc", Namespace = "http://jabber.org/protocol/geoloc")]
    public class geoloc
    {
        public geoloc() : base()
        {
        }


        private double m_fLatitude = 0.0f;
        [XmlElement(ElementName = "lat")]
        [DataMember]
        public double lat
        {
            get { return m_fLatitude; }
            set { m_fLatitude = value; }
        }

        private double m_fLongitude = 0.0f;
        [XmlElement(ElementName = "lon")]
        [DataMember]
        public double lon
        {
            get { return m_fLongitude; }
            set { m_fLongitude = value; }
        }

        private string m_strLocality = null;
        [XmlElement(ElementName = "locality")]
        [DataMember]
        public string Locality
        {
            get { return m_strLocality; }
            set { m_strLocality = value; }
        }

        private string m_strCountry = null;
        [XmlElement(ElementName = "country")]
        [DataMember]
        public string Country
        {
            get { return m_strCountry; }
            set { m_strCountry = value; }
        } 

        private int m_nAccuracy = 0;
        [XmlElement(ElementName = "acurracy")]
        [DataMember]
        public int Accuracy
        {
            get { return m_nAccuracy; }
            set { m_nAccuracy = value; }
        }

        private DateTime m_dtTimeStamp = DateTime.Now;
        [XmlElement(ElementName = "timestamp")]
        [DataMember]
        public DateTime TimeStamp
        {
            get { return m_dtTimeStamp; }
            set { m_dtTimeStamp = value; }
        }

        [XmlIgnore()]
        public bool IsDirty = true;

    }

    [DataContract]
    [XmlRoot(ElementName = "x", Namespace = "jabber:x:avatar")]
    public class IQAvatar
    {
        public IQAvatar()
        {
        }

        private string m_strHash = null;
        [XmlElement(ElementName = "hash")]
        public string Hash
        {
            get { return m_strHash; }
            set { m_strHash = value; }
        }

    }

    [DataContract]
    [XmlRoot(ElementName = "query", Namespace = "jabber:x:avatar")]
    public class IQAvatarQuery
    {
        public IQAvatarQuery()
        {
        }

        private byte[] m_bData = null;
        [XmlElement(ElementName="data", DataType = "base64Binary")]
        public byte[] Data
        {
            get { return m_bData; }
            set { m_bData = value; }
        }
        

    }


    [DataContract]
    [XmlRoot(ElementName = "data", Namespace = "urn:xmpp:avatar:data")]
    public class avatardata
    {
        public avatardata()
        {
        }

        private byte [] m_bImageData = null;
        [DataMember]
        [XmlText(DataType = "base64Binary")]
        public byte [] ImageData
        {
            get { return m_bImageData; }
            set { m_bImageData = value; }
        }

    }

    [DataContract]
    [XmlRoot(ElementName = "info", Namespace = "urn:xmpp:avatar:data")]
    public class imageinfo
    {
        public imageinfo()
        {
        }

        private string m_strID = null;
        [XmlAttribute(AttributeName="id")]
        public string ID
        {
            get { return m_strID; }
            set { m_strID = value; }
        }

        private int m_nHeight = 0;
        [XmlAttribute(AttributeName = "height")]
        public int Height
        {
            get { return m_nHeight; }
            set { m_nHeight = value; }
        }

        private int m_nWidth = 0;
        [XmlAttribute(AttributeName = "width")]
        public int Width
        {
            get { return m_nWidth; }
            set { m_nWidth = value; }
        }

        private int m_nBytes = 0;
        [XmlAttribute(AttributeName = "bytes")]
        public int ByteLength
        {
            get { return m_nBytes; }
            set { m_nBytes = value; }
        }

        private string m_strContentType = "image/png";
        [XmlAttribute(AttributeName = "type")]
        public string ContentType
        {
            get { return m_strContentType; }
            set { m_strContentType = value; }
        }
    }

    [DataContract]
    [XmlRoot(ElementName = "metadata", Namespace = "urn:xmpp:avatar:metadata")]
    public class avatarmetadata
    {
        public avatarmetadata()
        {
        }

        private imageinfo m_objImageInfo = new imageinfo();
        [XmlElement(ElementName = "info")]
        public imageinfo ImageInfo
        {
            get { return m_objImageInfo; }
            set { m_objImageInfo = value; }
        }
       

    }

}
