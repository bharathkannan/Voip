using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Net.XMPP;
using System.Net.Cache;

namespace WPFXMPPClient
{
    /// <summary>
    /// Interaction logic for MapUserControl.xaml
    /// </summary>
    public partial class MapUserControl : UserControl
    {
        public MapUserControl()
        {
            InitializeComponent();
        }
        bool bLoaded = false;

        public string strURL  = "http://maps.googleapis.com/maps/api/staticmap?";

        public RosterItem OurRosterItem = null;
        public XMPPClient XMPPClient = null;
        public MapProperties MapProperties = new MapProperties();

        private bool m_SingleRosterItemMap = true;

        public List<int> ZoomLevels = new List<int>();

        public bool SingleRosterItemMap
        {
            get { return m_SingleRosterItemMap; }
            set { m_SingleRosterItemMap = value; }
        }

        private void InitializeValues()
        {
            ZoomLevels.Clear();
            for (int i = 1; i <= 21; i++)
            {
                ZoomLevels.Add(i);
            }
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeValues();
            ComboBoxZoom.ItemsSource = ZoomLevels;
            ComboBoxZoom.SelectedIndex = 15;
            ComboBoxMapType.ItemsSource = Enum.GetValues(typeof(MapType));
            ComboBoxMapType.SelectedIndex = 0;

            MapProperties.MapParameters = new MapParameters() { MapType = (MapType)ComboBoxMapType.SelectedValue };
            MapProperties.LocationParameters = new LocationParameters() { Zoom = (int)ComboBoxZoom.SelectedValue };

            //BuildURL();
            //LoadURL();
            if (XMPPClient != null)
            {
                XMPPClient.OnXMLReceived += new System.Net.XMPP.XMPPClient.DelegateString(XMPPClient_OnXMLReceived);
                XMPPClient.OnXMLSent += new System.Net.XMPP.XMPPClient.DelegateString(XMPPClient_OnXMLSent);
            }
            SetUpRosterItemNotifications();     
            ButtonLoadLocation_Click(null, e);
        }

        private geoloc ExtractGeoLoc(string strXML)
        {
            geoloc newGeoLoc = null;
              //<geoloc xmlns=\"http://jabber.org/protocol/geoloc\">
              //<lat>32.816849551194174</lat>
              //<lon>-96.757696867079247</lon>
              //<acurracy>0</acurracy>
              //<timestamp>2012-03-20T16:47:23.379-05:00</timestamp>
              //<geoloc>";
            return newGeoLoc;
        }

        void XMPPClient_OnXMLSent(XMPPClient client, string strXML)
        {

            if (strXML.Contains("GeoLoc"))
            {

            }

            // throw new NotImplementedException();
        }

        void XMPPClient_OnXMLReceived(XMPPClient client, string strXML)
        {
            if (strXML.Contains("GeoLoc"))
            {

            }
            // throw new NotImplementedException();
        }

        private void SetUpRosterItemNotifications()
        {
            if (XMPPClient == null)
                return;

            foreach (RosterItem rosterItem in XMPPClient.RosterItems)
            {
                rosterItem.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(rosterItem_PropertyChanged);
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if ((e.Key == Key.F5))
            {
//                BuildURL();
//                LoadURL();
                ButtonLoadLocation_Click(null, null);
            }
           
            base.OnPreviewKeyDown(e);
        }

        void rosterItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                // WebBrowserMap.Navigate(strURL);
                if (e.PropertyName == "GeoLoc")
                {

                    if (SingleRosterItemMap)
                    {
                        RosterItem item = sender as RosterItem;

                        //if (item.JID.User == "brianbonnett")
                        {
                            if (item.GeoLoc.lat == 0.0 && item.GeoLoc.lon == 0.0)
                                return;

                            BuildURL();

                            //strURL = BuildURLForRosterItem(item, "blue", "B");
                            Console.WriteLine(String.Format("{0}: {1}, {2}", item.JID.User, item.GeoLoc.lat, item.GeoLoc.lon));
                            // MessageBox.Show("updating location!");
                            if (Paths.ContainsKey(item) == false)
                                Paths.Add(item, new List<geoloc>());
                            Paths[item].Add(item.GeoLoc);

                            TextBoxURL.Text = strURL;
                            TextBoxTimeStamp.Text = String.Format("{0}'s Location ({1}): {2}, {3}", item.JID.BareJID, item.GeoLoc.TimeStamp, item.GeoLoc.lat, item.GeoLoc.lon);
                            RosterItems.Add(item);
                            LocationsList.ItemsSource = RosterItems;
                            LoadURL();


                            return;
                        }
                    }
                    else
                    {

                        // throw new NotImplementedException();
                        //if (e.PropertyName == "GeoLoc")
                        {
                            string strURLUpdated = BuildURLForAllRosterItems();
                            if (String.Compare(strURLUpdated, strURL, true) == 0)
                            {
                                strURL = strURLUpdated;
                                LoadURL();
                            }
                        }
                    }
                }
            })
                  );

        }

        List<RosterItem> RosterItems = new List<RosterItem>();

        private void LoadURL()
        {
            //LoadImageFromURL();
            this.Dispatcher.Invoke(new Action(() =>
            {
                LoadImageFromURL();
                // WebBrowserMap.Navigate(strURL);
            })
            );

             
        }

        private void LoadImageFromURL()
        {
            BitmapImage _image = new BitmapImage();
            _image.BeginInit();
            //if (bLoaded == false)
            {
                _image.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                _image.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.None;
                
                //_image.CacheOption = BitmapCacheOption.None;
                //_image.UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
                //_image.CacheOption = BitmapCacheOption.OnLoad;
                //_image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            }

            _image.UriSource = new Uri(strURL, UriKind.RelativeOrAbsolute);
            _image.EndInit();
            MapImage.Source = _image;

            if (bLoaded == false)
            {
                bLoaded = true;
                LoadImageFromURL();
            }

          

            //BitmapImage bmpImage = new BitmapImage();
            ////string mapURL = "http://maps.googleapis.com/maps/api/staticmap?" + "center=" + lat + "," + lng + "&" + "size=500x400&markers=size:mid%7Ccolor:red%7C" + location + "&zoom=" + zoom + "&maptype=" + mapType + "&sensor=false";
            //bmpImage.BeginInit();
            //bmpImage.UriSource = new Uri(strURL);
            //bmpImage.EndInit();

            //MapImage.Source = bmpImage;
        }


        private string BuildURLForAllRosterItems()
        {
            string strGoogleMapsApiURL = "http://maps.googleapis.com/maps/api/staticmap?";
            strGoogleMapsApiURL += String.Format("&zoom={0}", MapProperties.LocationParameters.Zoom);

            strGoogleMapsApiURL += String.Format("&maptype={0}", MapProperties.MapParameters.MapType);

            strGoogleMapsApiURL += String.Format("&size=800x800");
            strGoogleMapsApiURL += String.Format("&sensor=false");
            string strMyLatLon = String.Format("{0},{1}", this.XMPPClient.GeoLocation.lat, this.XMPPClient.GeoLocation.lon);
            string strMyColor = "red";
            string strMyLabel = "A";
            if (this.XMPPClient.JID != null && this.XMPPClient.JID.User != null && this.XMPPClient.JID.User.Length >= 1)
                strMyLabel = this.XMPPClient.JID.User[0].ToString();
            
            if (strMyLatLon != "0,0")
                strGoogleMapsApiURL += String.Format("&center={0}", strMyLatLon);
            if (strMyLatLon != "0,0")
                strGoogleMapsApiURL += String.Format("&markers=color:{0}%7Clabel:{1}%7C{2}", strMyColor, strMyLabel, strMyLatLon);

                // BuildMarkerForRosterItem(rosterItem, strColor, strLabel);

            string strMarkers = "";
            List<string> colors = new List<string>() { "red", "blue", "yellow", "green" };
            List<string> labels = new List<string>() { "B", "C", "D", "E", "F", "G", "H", "I", "J" };

            int nColorIndex = 0;
            int nLabelIndex = 0;

            foreach (RosterItem rosterItem in XMPPClient.RosterItems)
            {
                string strColor = "";
                string strLabel = "";
                if (!(nColorIndex < colors.Count()))
                    nColorIndex++;
                if (!(nLabelIndex < labels.Count()))
                    nLabelIndex++;

                strColor = colors[nColorIndex];
                strLabel = labels[nLabelIndex];
               
                //string strLabel = labels[nColorIndex];

                if (rosterItem != null && rosterItem.JID != null && rosterItem.JID.User != null && rosterItem.JID.User.Length >= 1)
                    strLabel = rosterItem.JID.User[0].ToString();

                strMarkers += BuildMarkerForRosterItem(rosterItem, strColor, strLabel);


               


            }

            strGoogleMapsApiURL += strMarkers;

           
            return strGoogleMapsApiURL;
        }

        private string BuildMarkerForRosterItem(RosterItem rosterItem, string strColor, string strLabel)
        {
            string strLatLon = String.Format("{0},{1}", rosterItem.GeoLoc.lat, rosterItem.GeoLoc.lon);
            return String.Format("&markers=color:{0}%7Clabel:{1}%7C{2}", strColor, strLabel, strLatLon);

        }

        private string BuildURLForRosterItem(RosterItem rosterItem, string strColor, string strLabel)
        {
            string strGoogleMapsApiURL = "http://maps.googleapis.com/maps/api/staticmap?";

            if (rosterItem != null)
            {
                string strLatLon = String.Format("{0},{1}", rosterItem.GeoLoc.lat, rosterItem.GeoLoc.lon);
                // OurRosterItem.GeoString;

                strGoogleMapsApiURL += String.Format("center={0}", strLatLon);

                strGoogleMapsApiURL += BuildMarkerForRosterItem(rosterItem, strColor, strLabel);

                TextBoxTimeStamp.Text = String.Format("{0}'s Location ({1}): ", rosterItem.JID.ToString(), rosterItem.GeoLoc.TimeStamp);
                TextBoxGeoLoc.Text = strLatLon;

                //strURL = strGoogleMapsApiURL;
                //TextBoxURL.Text = strURL;
                // center=Williamsburg,Brooklyn,NY
                // &zoom=13
                // &size=800x800&
                // markers=color:blue%7Clabel:S%7C11211%7C11206%7C11222&sensor=false";

            }
            return strGoogleMapsApiURL;
        }

        public void Refresh()
        {
            ButtonLoadLocation_Click(null, null);
        }

        private void BuildURL()
        {
            string strGoogleMapsApiURL = "http://maps.googleapis.com/maps/api/staticmap?";
           
            if (OurRosterItem != null)
            {
                if (OurRosterItem.GeoLoc.lat == 0.0 && OurRosterItem.GeoLoc.lon == 0.0)
                    return;

                string strLatLon = String.Format("{0},{1}", OurRosterItem.GeoLoc.lat, OurRosterItem.GeoLoc.lon);
                    // OurRosterItem.GeoString;

                strGoogleMapsApiURL += String.Format("center={0}", strLatLon);
                strGoogleMapsApiURL += String.Format("&zoom={0}", MapProperties.LocationParameters.Zoom);
                strGoogleMapsApiURL += String.Format("&maptype={0}", MapProperties.MapParameters.MapType);
                strGoogleMapsApiURL += String.Format("&size=800x800");
                strGoogleMapsApiURL += BuildMarkerForRosterItem(OurRosterItem, "red", "");
                    // += String.Format("&markers=color:blue%7Clabel:B%7C{0}", strLatLon);
                strGoogleMapsApiURL += String.Format("&sensor=false");

                strURL = strGoogleMapsApiURL;
                TextBoxURL.Text = strURL;

                TextBoxTimeStamp.Text = String.Format("Location ({0}): ", OurRosterItem.GeoLoc.TimeStamp);
                TextBoxGeoLoc.Text = strLatLon;
            // center=Williamsburg,Brooklyn,NY
            // &zoom=13
            // &size=800x800&
            // markers=color:blue%7Clabel:S%7C11211%7C11206%7C11222&sensor=false";

            }
           
        }

        private void ButtonLoadURL_Click(object sender, RoutedEventArgs e)
        {
            strURL = TextBoxURL.Text;
            LoadURL();

        }

        private void ButtonLoadLocation_Click(object sender, RoutedEventArgs e)
        {
           // ButtonLoadLocationAll_Click(sender, e);

            BuildURL();
            LoadURL();
        }

        private void ButtonLoadLocationAll_Click(object sender, RoutedEventArgs e)
        {
            string strURLUpdated = BuildURLForAllRosterItems();
            if (String.Compare(strURLUpdated, strURL, true) != 0)
            {
                strURL = strURLUpdated;
                LoadURL();
            }
        }

        private Dictionary<RosterItem, List<geoloc>> m_Paths = new Dictionary<RosterItem,List<geoloc>>();

        public Dictionary<RosterItem, List<geoloc>> Paths
        {
            get { return m_Paths; }
            set { m_Paths = value; }
        }

        private void HyperlinkRosterItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonViewMessages_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MapProperties.LocationParameters.Zoom = (int)ComboBoxZoom.SelectedValue;
            }
            catch (Exception ex)
            {

            }

            //if (ComboBoxMapType.SelectedValue.ToString() == "roadmap")
            //    MapProperties.MapParameters.MapType = MapType.roadmap;
            //else if (ComboBoxMapType.SelectedValue.ToString() == "satellite")
            //    MapProperties.MapParameters.MapType = MapType.satellite;

            //else if (ComboBoxMapType.SelectedValue.ToString() == "terrain")
            //    MapProperties.MapParameters.MapType = MapType.terrain;

            //else if (ComboBoxMapType.SelectedValue.ToString() == "hybrid")
            //    MapProperties.MapParameters.MapType = MapType.hybrid;

            MapProperties.MapParameters.MapType = (MapType)ComboBoxMapType.SelectedValue;

            ButtonLoadLocation_Click(sender, e);
        }

        public void ZoomIn(int delta)
        {
            MapProperties.LocationParameters.Zoom += delta;

            if (MapProperties.LocationParameters.Zoom < 0)
                MapProperties.LocationParameters.Zoom = 0;
            if (MapProperties.LocationParameters.Zoom > 21)
                MapProperties.LocationParameters.Zoom = 21;

            Refresh();
        }

        public void ZoomOut(int delta)
        {
            MapProperties.LocationParameters.Zoom -= delta;

            if (MapProperties.LocationParameters.Zoom < 0)
                MapProperties.LocationParameters.Zoom = 0;
            if (MapProperties.LocationParameters.Zoom > 21)
                MapProperties.LocationParameters.Zoom = 21;

            Refresh();
        }

        private void MapImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            //MapProperties.LocationParameters.Zoom = Convert.ToInt32(Math.Max(MapProperties.LocationParameters.Zoom + (0.1F * e.Delta / 120.0F), 0.01F));

            //if (MapProperties.LocationParameters.Zoom < 0)
            //    MapProperties.LocationParameters.Zoom = 0;
            //if (MapProperties.LocationParameters.Zoom > 21)
            //    MapProperties.LocationParameters.Zoom = 21;

            //Refresh();
        }
                   
    }

    public class MapProperties
    {
        private string m_URL = "http://maps.googleapis.com/maps/api/staticmap?";

        public string URL
        {
            get { return m_URL; }
            set { m_URL = value; }
        }

        private LocationParameters m_LocationParameters = new LocationParameters();

        public LocationParameters LocationParameters
        {
            get { return m_LocationParameters; }
            set { m_LocationParameters = value; }
        }

        private MapParameters m_MapParameters = new MapParameters();

        public MapParameters MapParameters
        {
            get { return m_MapParameters; }
            set { m_MapParameters = value; }
        }

        // The markers parameter takes set of value assignments (marker descriptors) of the following format:
        // markers=markerStyles|markerLocation1| markerLocation2|... etc.

        private List<string> m_Markers = new List<string>();

        public List<string> Markers
        {
            get { return m_Markers; }
            set { m_Markers = value; }
        }

        private bool m_Sensor = false;

        public bool Sensor
        {
            get { return m_Sensor; }
            set { m_Sensor = value; }
        }

     

    }

    // Location Parameters:

    // center (required if markers not present) defines the center of the map, equidistant from all edges of the map. This parameter takes a location as either a comma-separated {latitude,longitude} pair (e.g. "40.714728,-73.998672") or a string address (e.g. "city hall, new york, ny") identifying a unique location on the face of the earth. For more information, see Locations below.
    // zoom (required if markers not present) defines the zoom level of the map, which determines the magnification level of the map. This parameter takes a numerical value corresponding to the zoom level of the region desired. For more information, see zoom levels below.
    //      Maps on Google Maps have an integer "zoom level" which defines the resolution of the current view. Zoom levels between 0 (the lowest zoom level, 
    //      in which the entire world can be seen on one map) to 21+ (down to individual buildings) are possible within the default roadmap maps view.
    // center=texas&size=500x300&zoom=12&sensor=false

    public class LocationParameters
    {
        
        private string m_Center = "";

        public string Center
        {
            get { return m_Center; }
            set { m_Center = value; }
        }

        // possible values are 0 to 21
        private int m_Zoom = 15;

        public int Zoom
        {
            get { return m_Zoom; }
            set { m_Zoom = value; }
        }

        public const int ZoomMin = 0;
        public const int ZoomMax = 21;

        public override string ToString()
        {
            // only print if value is not blank
            string retStr = "";
            if (Center != null && Center.Length > 0)
                retStr += String.Format("center={0}", Center);
            retStr += String.Format("zoom={0}", Zoom);

            return retStr;
        }
    }

    //Map Parameters:

    //size (required) defines the rectangular dimensions of the map image. This parameter takes a string of the form {horizontal_value}x{vertical_value}. For example, 500x400 defines a map 500 pixels wide by 400 pixels high. Maps smaller than 180 pixels in width will display a reduced-size Google logo. This parameter is affected by the scale parameter, described below; the final output size is the product of the size and scale values.
    //scale (optional) affects the number of pixels that are returned. scale=2 returns twice as many pixels as scale=1 while retaining the same coverage area and level of detail (i.e. the contents of the map don't change). This is useful when developing for high-resolution displays, or when generating a map for printing. The default value is 1. Accepted values are 2 and 4 (4 is only available to Maps API for Business customers.) See Scale Values for more information.
    // (Default scale value is 1; accepted values are 1, 2, and (for Maps API for Business customers only) 4).

    //format (optional) defines the format of the resulting image. By default, the Static Maps API creates PNG images. There are several possible formats including GIF, JPEG and PNG types. Which format you use depends on how you intend to present the image. JPEG typically provides greater compression, while GIF and PNG provide greater detail. For more information, see Image Formats.
    //maptype (optional) defines the type of map to construct. There are several possible maptype values, including roadmap, satellite, hybrid, and terrain. For more information, see Static Maps API Maptypes below.
    //language (optional) defines the language to use for display of labels on map tiles. Note that this parameter is only supported for some country tiles; if the specific language requested is not supported for the tile set, then the default language for that tileset will be used.
    //region (optional) defines the appropriate borders to display, based on geo-political sensitivities. Accepts a region code specified as a two-character ccTLD ('top-level domain') value.

    //    The table below shows the maximum allowable values for the size parameter at each scale value.

    //API	                        scale=1	    scale=2	                                scale=4
    //Free	                        640x640	    640x640 (returns 1280x1280 pixels)	    Not available.
    //Google Maps API for Business	2048x2048	1024x1024 (returns 2048x2048 pixels)	512x512 (returns 2048x2048 pixels)


    public class MapParameters
    {
        private SizeParameters m_Size = new SizeParameters();

        public SizeParameters Size
        {
            get { return m_Size; }
            set { m_Size = value; }
        }

        private int m_Scale = 2;

        public int Scale
        {
            get { return m_Scale; }
            set { m_Scale = value; }
        }

        private MapFormat m_MapFormat = MapFormat.png;

        public MapFormat MapFormat
        {
            get { return m_MapFormat; }
            set { m_MapFormat = value; }
        }

        private MapType m_MapType = MapType.roadmap;

        public MapType MapType
        {
            get { return m_MapType; }
            set { m_MapType = value; }
        }

        public override string ToString()
        {
            string retStr = "";

            retStr += String.Format("size={0}", Size.ToString());
            retStr += String.Format("scale={0}", Scale);
            if (MapFormat == WPFXMPPClient.MapFormat.jpg_baseline)
                retStr += String.Format("format={0}", "jpg-baseline");
            else
                retStr += String.Format("format={0}", MapFormat.ToString());
            retStr += String.Format("maptype={0}", MapType.ToString());

            return retStr;
        }
 
    }

    public enum MapType
    {
        roadmap, // default
        satellite, 
        hybrid,
        terrain
    }

    // png8 or png (default) specifies the 8-bit PNG format.
    // png32 specifies the 32-bit PNG format.
    // gif specifies the GIF format.
    // jpg specifies the JPEG compression format.
    // jpg-baseline specifies a non-progressive JPEG compression format.

    public enum MapFormat
    {
        png, 
        png32,
        gif,
        jpg,
        // jpg-baseline
        jpg_baseline
    }

    public enum MarkerSize
    {

    }
    public class SizeParameters
    {
        private int m_Horizontal = 400;

        public int Horizontal
        {
            get { return m_Horizontal; }
            set { m_Horizontal = value; }
        }

        private int m_Vertical = 400;

        public int Vertical
        {
            get { return m_Vertical; }
            set { m_Vertical = value; }
        }

        public override string ToString()
        {
            return String.Format("{0}x{1}", Horizontal, Vertical);
        }


    }

   
}
;
