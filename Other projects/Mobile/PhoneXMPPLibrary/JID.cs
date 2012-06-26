/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace System.Net.XMPP
{
    [DataContract]
    public class JID
    {
        public JID()
        {
        }

        public JID(string strJID)
        {
            FullJID = strJID;
        }

        public override string ToString()
        {
            return FullJID;
        }

        public static Regex RegexJID = new Regex(@" (?<naked> ((?<user>\S+)@)*(?<domain>[^/]+) ) (/(?<resource>.*))*", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        private string m_strUser = "";
        [DataMember]
        [XmlIgnore()]
        public string User
        {
          get 
          { 
              return m_strUser; 
          }
          set 
          { 
              m_strUser = value; 
              RebuildJID();
          }
        }

        private string m_strDomain = "";
        [DataMember]
        [XmlIgnore()]
        public string Domain
        {
          get 
          { 
              return m_strDomain; 
          }
          set 
          { 
              m_strDomain = value; 
              RebuildJID();
          }
        }

        private string m_strResource = "";
        [DataMember]
        [XmlIgnore()]
        public string Resource
        {
          get 
          { 
              return m_strResource; 
          }
          set 
          { 
              m_strResource = value; 
              RebuildJID();
          }
        }

        private string m_strFullJID = "";
        [XmlText()]
        public string FullJID
        {
          get 
          { 
              return m_strFullJID; 
          }
          set 
          { 
              m_strFullJID = value; 
              ParseJID();
          }
        }

        private string m_strBareJID = "";
        [XmlIgnore()]
        public string BareJID
        {
          get 
          { 
              return m_strBareJID; 
          }
          set 
          { 
              m_strBareJID = value; 
              ParseNakedJID();
          }
        }

        private void RebuildJID()
        {
            if (m_strResource == null)
                m_strResource = "";
            if (m_strUser == null)
                m_strUser = "";
            if (m_strDomain == null)
                m_strDomain = "";


            if (m_strResource.Length > 0)
            {
                if (m_strUser.Length > 0)
                {
                    m_strFullJID = string.Format("{0}@{1}/{2}", m_strUser, m_strDomain, m_strResource);
                    m_strBareJID = string.Format("{0}@{1}", m_strUser, m_strDomain);
                }
                else
                {
                    m_strFullJID = string.Format("{0}/{1}", m_strDomain, m_strResource);
                    m_strBareJID = string.Format("{0}", m_strDomain);
                }
            }
            else
            {
                if (m_strUser.Length > 0)
                {
                m_strFullJID = string.Format("{0}@{1}", m_strUser, m_strDomain);
                m_strBareJID = string.Format("{0}@{1}", m_strUser, m_strDomain);
                    }
                                else
                {
                    m_strFullJID = string.Format("{0}/{1}", m_strDomain, m_strResource);
                    m_strBareJID = string.Format("{0}", m_strDomain);
                }

            }
        }

        /// <summary>
        ///  Full JID has changed, parse for everything
        /// </summary>
        private void ParseJID()
        {
           m_strBareJID = "";
           m_strDomain = "";
           m_strUser = "";
           m_strResource = "";
           if (m_strFullJID == null)
               m_strFullJID = "";

           Match match = RegexJID.Match(m_strFullJID);
           if (match.Success == true)
           {
               m_strDomain = match.Groups["domain"].Value;
               m_strUser = match.Groups["user"].Value;
               m_strBareJID = match.Groups["naked"].Value;
               if ( (match.Groups["resource"] != null) && (match.Groups["resource"].Value != null) )
                  m_strResource = match.Groups["resource"].Value;
           }
        }

        /// <summary>
        /// Naked jid has changed... reset the user and domain, then rebuild the full jid
        /// </summary>
        private void ParseNakedJID()
        {
           m_strDomain = "";
           m_strUser = "";
           Match match = RegexJID.Match(m_strBareJID);
           if (match.Success == true)
           {
               m_strDomain = match.Groups["domain"].Value;
               m_strUser = match.Groups["user"].Value;
               RebuildJID();
           }
        }

        public static implicit operator JID(string strJID)
        {
            return new JID(strJID);
        }
        
        public static implicit operator string(JID objJID)
        {
            if (objJID == null)
                return null;
            return objJID.FullJID;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            string strValue = obj.ToString();
            return FullJID.Equals(strValue);
        }

        public override int GetHashCode()
        {
            return FullJID.GetHashCode();
        }


    }
}
