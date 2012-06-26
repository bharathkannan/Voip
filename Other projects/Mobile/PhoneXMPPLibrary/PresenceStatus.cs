/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Net;

using System.Runtime.Serialization;

#if !MONO
using System.Windows.Threading;
#endif

namespace System.Net.XMPP
{
    public enum PresenceType
    {
        available, // use Empty instead of this... there is no available value
        error,
        probe,
        subscribe,
        subscribed,
        unavailable,
        unsubscribe,
        unsubscribed
    }

    public enum PresenceShow
    {
        unknown,
        away,
        chat,
        dnd,
        xa
    }

    [DataContract]
    public class PresenceStatus : System.ComponentModel.INotifyPropertyChanged
    {
        public PresenceStatus()
        {
        }

        public override string ToString()
        {
            return string.Format("Presence: {0}, Show: {1}, Status: {2}", PresenceType, PresenceShow, Status);
        }

        private bool m_bIsDirty = true;

        public bool IsDirty
        {
            get { return m_bIsDirty; }
            set { m_bIsDirty = value; }
        }

        public bool IsOnline
        {
            get
            {
                if (m_ePresence == XMPP.PresenceType.available)
                    return true;
                else if (m_ePresence == XMPP.PresenceType.unavailable)
                    return false;

                return true;
            }
            set
            {
            }
        }

        private PresenceType m_ePresence = PresenceType.unavailable;
        [DataMember]
        public PresenceType PresenceType
        {
            get { return m_ePresence; }
            set
            {
                if (m_ePresence != value)
                {
                    m_ePresence = value;
                    FirePropertyChanged("PresenceType");
                    FirePropertyChanged("Presence");
                    FirePropertyChanged("PresenceColor");
                    FirePropertyChanged("PresenceBrush");
                    FirePropertyChanged("IsOnline");
                    IsDirty = true;
                }
            }
        }

#if !MONO
        public System.Windows.Media.Color PresenceColor
        {
            get
            {
                if (m_ePresenceShow == System.Net.XMPP.PresenceShow.unknown)
                    return System.Windows.Media.Color.FromArgb(0, 0, 0, 0);
                if (m_ePresenceShow == System.Net.XMPP.PresenceShow.dnd)
                    return System.Windows.Media.Colors.Red;
                else if (m_ePresenceShow == System.Net.XMPP.PresenceShow.away)
                    return System.Windows.Media.Colors.Orange;
                else if (m_ePresenceShow == System.Net.XMPP.PresenceShow.xa)
                {
#if WINDOWS_PHONE
                    if (string.Compare(Status, "online", StringComparison.CurrentCultureIgnoreCase) == 0)
#else
                    if (string.Compare(Status, "online", true) == 0)
#endif
                        return System.Windows.Media.Color.FromArgb(255, 64, 255, 64);
#if WINDOWS_PHONE
                    if (string.Compare(Status, "extended away", StringComparison.CurrentCultureIgnoreCase) == 0)
#else
                    if (string.Compare(Status, "extended away", true) == 0)
#endif
                        return System.Windows.Media.Colors.Orange;


                    return System.Windows.Media.Colors.Purple;
                }
                else if (m_ePresenceShow == System.Net.XMPP.PresenceShow.chat)
                    return System.Windows.Media.Color.FromArgb(255, 128, 255, 128);
                else
                    return System.Windows.Media.Colors.Purple;
            }
            set
            {
            }
        }

        public System.Windows.Media.Brush PresenceBrush
        {
            get
            {
                return new System.Windows.Media.SolidColorBrush(PresenceColor);
            }
            set
            {
            }
        }

#endif

        private PresenceShow m_ePresenceShow = PresenceShow.unknown;
        [DataMember]
        public PresenceShow PresenceShow
        {
            get { return m_ePresenceShow; }
            set 
            {
                if (m_ePresenceShow != value)
                {
                    m_ePresenceShow = value;
                    FirePropertyChanged("PresenceShow");
                    FirePropertyChanged("PresenceColor");
                    FirePropertyChanged("PresenceBrush");
                    FirePropertyChanged("IsOnline");
                    IsDirty = true;
                }
            }

        }

        private string m_strStatus = "unknown";
        [DataMember]
        public string Status
        {
            get { return m_strStatus; }
            set
            {
                if (m_strStatus != value)
                {
                    m_strStatus = value;
                    FirePropertyChanged("Status");
                    FirePropertyChanged("PresenceColor");
                    FirePropertyChanged("PresenceBrush");
                    FirePropertyChanged("IsOnline");
                    IsDirty = true;
                }
            }
        }

        private sbyte m_nPriority = 0;
        [DataMember]
        public sbyte Priority
        {
            get { return m_nPriority; }
            set
            {
                if (m_nPriority != value)
                {
                    m_nPriority = value;
                    FirePropertyChanged("Priority");
                    IsDirty = true;
                }
            }
        }


        #region INotifyPropertyChanged Members

        void FirePropertyChanged(string strName)
        {
            if (PropertyChanged != null)
            {
#if WINDOWS_PHONE
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(PropertyChanged, this, new System.ComponentModel.PropertyChangedEventArgs(strName));
#elif MONO
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(strName));
#else
   System.ComponentModel.PropertyChangedEventArgs args = new System.ComponentModel.PropertyChangedEventArgs(strName);
                System.ComponentModel.PropertyChangedEventHandler eventHandler = PropertyChanged;
                if (eventHandler == null)
                    return;

                Delegate[] delegates = eventHandler.GetInvocationList();
                // Walk thru invocation list
                foreach (System.ComponentModel.PropertyChangedEventHandler handler in delegates)
                {
                    DispatcherObject dispatcherObject = handler.Target as DispatcherObject;
                    // If the subscriber is a DispatcherObject and different thread
                    if (dispatcherObject != null && dispatcherObject.CheckAccess() == false)
                    {
                        // Invoke handler in the target dispatcher's thread
                        dispatcherObject.Dispatcher.Invoke(DispatcherPriority.DataBind, handler, this, args);
                    }
                    else // Execute handler as is
                        handler(this, args);
                }
#endif
            }
            

            //if (PropertyChanged != null)
            //    PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(strName));
        }
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged = null;

        #endregion
    }
}
