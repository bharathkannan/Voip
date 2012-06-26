using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net.XMPP;
using System.ComponentModel;

namespace LocationClasses
{
    public class BuddyPosition : INotifyPropertyChanged
    {
        public BuddyPosition(RosterItem item)
        {
            RosterItem = item;
            ((INotifyPropertyChanged)item).PropertyChanged += new PropertyChangedEventHandler(BuddyPosition_PropertyChanged);
        }

        void BuddyPosition_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "GeoLoc")
            {
                /// New geolocation, add it to our list
                /// 
                GeoCoordinate coord = new GeoCoordinate(RosterItem.GeoLoc.lon, RosterItem.GeoLoc.lat, RosterItem.GeoLoc.TimeStamp);
                CoordinateList.Add(coord);
                FirePropertyChanged("Count");
            }
        }

        private RosterItem m_objRosterItem = null;

        public RosterItem RosterItem
        {
            get { return m_objRosterItem; }
            set 
            { 
                m_objRosterItem = value;
                FirePropertyChanged("RosterItem");
            }
        }

        public void ClearCoordinates()
        {
            m_listCoordinateList.Clear();
            FirePropertyChanged("Count");
        }

        private List<GeoCoordinate> m_listCoordinateList = new List<GeoCoordinate>();

        public List<GeoCoordinate> CoordinateList
        {
            get { return m_listCoordinateList; }
            set { m_listCoordinateList = value; }
        }

        public int Count
        {
            get
            {
                return CoordinateList.Count;
            }
            set
            {
            }
        }



        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        void FirePropertyChanged(string strName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(strName));
        }

        #endregion
    }
}
