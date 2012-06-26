/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Net;

using System.Runtime.Serialization;
namespace System.Net.XMPP
{
    [DataContract]
    public class XMPPAccount : System.ComponentModel.INotifyPropertyChanged
    {
        public XMPPAccount()
        {
        }

        public override string ToString()
        {
            return AccountName;
        }

        private bool m_bHaveSuccessfullyConnectedAndAuthenticated = false;

        public bool HaveSuccessfullyConnectedAndAuthenticated
        {
            get { return m_bHaveSuccessfullyConnectedAndAuthenticated; }
            set { m_bHaveSuccessfullyConnectedAndAuthenticated = value; }
        }

        private string m_strAccountName = "Unknown Account";
        [DataMember]
        public string AccountName
        {
            get { return m_strAccountName; }
            set 
            { 
                m_strAccountName = value;
                HaveSuccessfullyConnectedAndAuthenticated = false;
                FirePropertyChanged("AccountName");
            }
        }

        
        public string m_strAvatarHash = null;
        [DataMember]
        public string AvatarHash
        {
            get { return m_strAvatarHash; }
            set 
            { 
                m_strAvatarHash = value; 
            }
        }

        
        public string m_strServer = "talk.google.com";
        [DataMember]
        public string Server
        {
            get { return m_strServer; }
            set
            {
                HaveSuccessfullyConnectedAndAuthenticated = false;
                m_strServer = value; 
            }
        }

        
        public string m_strPassword = "";
        [DataMember]
        public string Password
        {
            get { return m_strPassword; }
            set
            {
                HaveSuccessfullyConnectedAndAuthenticated = false;
                m_strPassword = value; 
            }
        }

        public int m_nPort = 5222;
        [DataMember]
        public int Port
        {
          get { return m_nPort; }
          set 
          {
              HaveSuccessfullyConnectedAndAuthenticated = false;
              m_nPort = value; 
          }

        }

        private bool m_bUseOldSSLMethod = false;
        [DataMember]
        public bool UseOldSSLMethod
        {
            get { return m_bUseOldSSLMethod; }
            set 
            {
                HaveSuccessfullyConnectedAndAuthenticated = false;
                m_bUseOldSSLMethod = value; 
            }
        }

        
        public bool m_bUseTLSMethod = true;
        [DataMember]
        public bool UseTLSMethod
        {
            get { return m_bUseTLSMethod; }
            set 
            {
                HaveSuccessfullyConnectedAndAuthenticated = false;
                m_bUseTLSMethod = value; 
            }
        }
        
        public JID m_objJID = new JID();
        [DataMember]
        public JID JID
        {
            get { return m_objJID; }
            set 
            {
                HaveSuccessfullyConnectedAndAuthenticated = false;
                m_objJID = value; 
            }
        }

        public string User
        {
            get
            {
                return m_objJID.User;
            }
            set
            {
                m_objJID.User = value;
                HaveSuccessfullyConnectedAndAuthenticated = false;
            }
        }

        public string Domain
        {
            get
            {
                return m_objJID.Domain;
            }
            set
            {
                m_objJID.Domain = value;
                HaveSuccessfullyConnectedAndAuthenticated = false;
            }
        }

        public string Resource
        {
            get
            {
                return m_objJID.Resource;
            }
            set
            {
                m_objJID.Resource = value;
                HaveSuccessfullyConnectedAndAuthenticated = false;
            }
        }


        private bool m_bUseSOCKSProxy = false;
        [DataMember]
        public bool UseSOCKSProxy
        {
            get { return m_bUseSOCKSProxy; }
            set { m_bUseSOCKSProxy = value; }
        }

        private string m_strProxyName = "";
        [DataMember]
        public string ProxyName
        {
            get { return m_strProxyName; }
            set { m_strProxyName = value; }
        }

        private int m_nProxyPort = 8080;
        [DataMember]
        public int ProxyPort
        {
            get { return m_nProxyPort; }
            set { m_nProxyPort = value; }
        }

        private int m_nSOCKSVersion = 5;
        [DataMember]
        public int SOCKSVersion
        {
            get { return m_nSOCKSVersion; }
            set { m_nSOCKSVersion = value; }
        }

        PresenceStatus m_eLastPrescence = new PresenceStatus() { PresenceType = PresenceType.available, PresenceShow = PresenceShow.chat, Status = "online", IsDirty = true };
        [DataMember]
        public PresenceStatus LastPrescence
        {
            get 
            { 
                if (m_eLastPrescence == null)
                    m_eLastPrescence = new PresenceStatus() { PresenceType = PresenceType.available, PresenceShow = PresenceShow.chat, Status = "online", IsDirty=true };
                return m_eLastPrescence; 
            }
            set 
            { 
                if ((value != null) && (value != m_eLastPrescence))
                {
                    m_eLastPrescence = value;
                    m_eLastPrescence.IsDirty = true;
                    FirePropertyChanged("LastPrescence");
                }
            }
        }

        /// <summary>
        /// Capabilities of a client.  This may be set if the user wants to advertise caps in their presence messages
        /// (XEP-0115)
        /// </summary>
        private Capabilities m_objCapabilities = null;
        public Capabilities Capabilities
        {
            get { return m_objCapabilities; }
            set { m_objCapabilities = value; }
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
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(PropertyChanged, this, new System.ComponentModel.PropertyChangedEventArgs(strName));
#endif
            }

            //if (PropertyChanged != null)
            //    PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(strName));
        }
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged = null;

        #endregion

    }
}
