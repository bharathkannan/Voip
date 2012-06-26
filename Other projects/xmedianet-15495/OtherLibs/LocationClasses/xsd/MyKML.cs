using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace LocationClasses
{
    public class GeoCoordinate
    {
        public GeoCoordinate()
        {
        }
        public GeoCoordinate(double fLon, double fLat, DateTime timestamp)
        {
            Longitude = fLon;
            Latitude = fLat;
            TimeStamp = timestamp;
        }
         
        public override string  ToString()
        {
 	        return string.Format("{0},{1},{2}", Longitude, Latitude, Altitude);
        }

        public double Longitude = 0.0f;
        public double Latitude = 0.0f;
        public double Altitude = 0.0f;
        public double Heading = 0.0f;
        public double Tilt = 0.0f;
        public double Range = 0.0f;
        public DateTime TimeStamp;
    }

    [XmlRoot(ElementName="LineString")]
    public class LineString
    {
        public LineString()
        {
        }

        [XmlElement(ElementName="tessellate")]
        public bool Tessellate = true;
        [XmlElement(ElementName="coordinates")]
        public string Coordinates = null;
    }

    [XmlRoot(ElementName="Point")]
    public class Point
    {
        public Point()
        {
        }

        [XmlElement(ElementName="coordinates")]
        public string Coordinate = null;
    }

    [XmlRoot(ElementName = "TimeStamp", Namespace="http://www.google.com/kml/ext/2.2")]
    public class TimeStamp
    {
        public TimeStamp()
        {
        }

        [XmlElement(ElementName="when")]
        public DateTime When = DateTime.Now;
    }

    [XmlRoot(ElementName = "LookAt")]
    public class LookAt
    {
        public LookAt()
        {
        }

        public LookAt(GeoCoordinate coord)
        {
            GeoCoordinate = coord;
        }

        [XmlIgnore]
        public GeoCoordinate GeoCoordinate = new GeoCoordinate();
        
        [XmlElement(ElementName = "longitude")]
        public double Longitude
        {
            get
            {
                return GeoCoordinate.Longitude;
            }
            set
            {
                GeoCoordinate.Longitude = value;
            }
        }

        [XmlElement(ElementName = "latitude")]
        public double Latitude
        {
            get
            {
                return GeoCoordinate.Latitude;
            }
            set
            {
                GeoCoordinate.Latitude= value;
            }
        }

        [XmlElement(ElementName = "altitude")]
        public double Altitude
        {
            get
            {
                return GeoCoordinate.Altitude;
            }
            set
            {
                GeoCoordinate.Altitude = value;
            }
        }

        [XmlElement(ElementName = "heading")]
        public double Heading
        {
            get
            {
                return GeoCoordinate.Heading;
            }
            set
            {
                GeoCoordinate.Heading = value;
            }
        }

        [XmlElement(ElementName = "tilt")]
        public double Tilt
        {
            get
            {
                return GeoCoordinate.Tilt;
            }
            set
            {
                GeoCoordinate.Tilt = value;
            }
        }

        [XmlElement(ElementName = "range")]
        public double Range
        {
            get
            {
                return GeoCoordinate.Range;
            }
            set
            {
                GeoCoordinate.Range = value;
            }
        }

        [XmlElement(ElementName = "TimeStamp", Namespace="http://www.google.com/kml/ext/2.2")]
        public TimeStamp TimeStamp
        {
            get
            {
                if (GeoCoordinate.TimeStamp > DateTime.MinValue)
                    return new TimeStamp() { When = GeoCoordinate.TimeStamp};
                return null;
            }
            set
            {
                GeoCoordinate.TimeStamp = value.When;
            }
        }

    }

    [XmlRoot(ElementName = "Placemark")]
    public class Placemark
    {
        public Placemark()
        {
        
        }
        
        public Placemark(string strName, GeoCoordinate coord)
        {
            Name = strName;
            Point = new Point();
            Point.Coordinate = coord.ToString();
            LookAt = new LookAt(coord);
        }


        public Placemark(string strName, IEnumerable<GeoCoordinate> LinePoints)
        {
            Name = strName;
            LineString = new LocationClasses.LineString();
            StringBuilder sb = new StringBuilder();
            foreach (GeoCoordinate pt in LinePoints)
            {
                sb.AppendFormat("{0} ", pt.ToString());
            }
            LineString.Coordinates = sb.ToString();
        }

        [XmlElement(ElementName="name")]
        public string Name = null;
        [XmlElement(ElementName="visibility")]
        public bool Visibility = false;

        public LookAt LookAt = null;
        [XmlElement(ElementName = "LineString")]
        public LineString LineString = null;

        [XmlElement(ElementName = "Point")]
        public Point Point = null;

    }

    [XmlRoot(ElementName = "Document")]
    public class Document
    {
        public Document()
        {
        }
        [XmlElement(ElementName = "name")]
        public string Name = "";
        [XmlElement(ElementName = "open")]
        public bool Open = true;
        [XmlElement(ElementName = "Placemark")]
        public List<Placemark> Placemarks = new List<Placemark>();

    }

    [XmlRoot(ElementName = "kml", Namespace="http://www.opengis.net/kml/2.2")]
    public class MyKML
    {
        public MyKML()
        {
        }

        [XmlElement(ElementName = "Document")]
        public Document Document = new Document();
    }
}
