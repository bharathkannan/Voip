/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Net;

using System.IO;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;

namespace System.Net.XMPP
{
    public class XMPPStream : Stream
    {
        
        public XMPPStream()
        {
        }

        protected long m_nPosition = 0;

        StringBuilder m_sbData = new StringBuilder();
        protected bool FoundStreamBeginning = false;


        
        public override string ToString()
        {
            return m_sbData.ToString();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Flush()
        {
            if (m_nPosition > 0)
                m_sbData.Remove(0, (int) m_nPosition);
            m_nPosition = 0;
        }

        public override long Length
        {
            get { return m_sbData.Length; }
        }

        public override long Position
        {
            get
            {
                return m_nPosition;
            }
            set
            {
                if (value > Length)
                    throw new Exception("Trying to seek past the end of data");
                if (m_nPosition < 0)
                    throw new Exception("Trying to seek before the beginning of data");
                m_nPosition = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int nRead = (int) ( ((Length - m_nPosition) < count) ? Length - m_nPosition : count);

            if (nRead <= 0)
                return nRead;
            //char [] data = new char[nRead];

            string strSBData = m_sbData.ToString();
            string strSubString = strSBData.Substring((int) m_nPosition, nRead);

            //m_sbData.CopyTo((int)m_nPosition, data, 0, nRead);
            for (int i = 0; i < nRead; i++)
            {
                buffer[offset + i] = (byte)strSubString[i];
            }

            m_nPosition += nRead;
            return nRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
                m_nPosition = offset;
            else if (origin == SeekOrigin.Current)
                m_nPosition += offset;
            else if (origin == SeekOrigin.End)
                m_nPosition = Length - offset;

            return m_nPosition;
        }

        public override void SetLength(long value)
        {
            m_sbData.Length = (int) value;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            string strText = System.Text.UTF8Encoding.UTF8.GetString(buffer, offset, count);
            m_sbData.Insert((int)m_nPosition, strText);
        }

        public void Append(string strData)
        {
            m_sbData.Append(strData);

        }



        protected string m_strTo = "";
        public string To
        {
            get
            {
                return m_strTo;
            }
            set
            {
                m_strTo = value;
            }
        }

        protected string m_strFrom = "";
        public string From
        {
            get
            {
                return m_strFrom;
            }
            set
            {
                m_strFrom = value;
            }
        }

        protected string m_strId = "";
        public string Id
        {
            get
            {
                return m_strId;
            }
            set
            {
                m_strId = value;
            }
        }

        protected string m_strVersion = "1.0";
        public string Version
        {
            get
            {
                return m_strVersion;
            }
            set
            {
                m_strVersion = value;
            }
        }

        protected string m_strLanguage = "en";
        public string Language
        {
            get
            {
                return m_strLanguage;
            }
            set
            {
                m_strLanguage = value;
            }
        }

      
        public void ParseStanzas(XMPPConnection connection, XMPPClient XMPPClient)
        {
           try
           {

              string CurrentNodeName = null;
              XMPPXMLNode objNode = null;

              while ( (objNode = ReadNextBlock(false)) != null)
              {
                 //System.Diagnostics.Debug.WriteLine("**ReadNextBlock got: {0}", objNode.Name);
                 if ((objNode.NodeType == XmlNodeType.XmlDeclaration) || (objNode.NodeType == XmlNodeType.Comment) || (objNode.NodeType == XmlNodeType.Whitespace))
                    continue;


                 if ((objNode.NodeType == XmlNodeType.EndElement) && (objNode.Name == "stream:stream"))
                 {
                    Flush();

                     /// We've been closed, tell whoever
                     /// 
                    connection.GracefulDisconnect();
                 }
                 else if (objNode.Name == "stream:stream")
                 {
                     System.Diagnostics.Debug.WriteLine("Got stream beggining fragment");
                    FoundStreamBeginning = true;
                    To = objNode.GetAttribute("to");
                    From = objNode.GetAttribute("from");
                    Id = objNode.GetAttribute("id");
                    Version = objNode.GetAttribute("version");
                    Language = objNode.GetAttribute("xml:lang");
                    CurrentNodeName = null;
                    //System.Diagnostics.Debug.WriteLine("Setting CurrentNodeName to null");

                    Flush();

                    if (XMPPClient.XMPPState == XMPPState.Authenticated)
                        XMPPClient.XMPPState = XMPPState.CanBind;

                 }
                 else if ( (objNode.NodeType == XmlNodeType.EndElement) && (CurrentNodeName == null) ) /// Must be a complete element
                 {
                     string strXML = FlushGet();
                     //System.Diagnostics.Debug.WriteLine("Got unpaired end fragment: {0}", strXML);

                     XMPPStanza stanza = new XMPPStanza(strXML);
                     connection.FireStanzaReceived(stanza);
                 }
                 else
                 {
                     if (CurrentNodeName == null)
                     {
                         CurrentNodeName = objNode.Name;
                         //System.Diagnostics.Debug.WriteLine("Setting CurrentNodeName to : {0}", CurrentNodeName);

                     }
                     else
                     {
                         if (objNode.Name == CurrentNodeName) /// Found the end tag
                         {
                             //System.Diagnostics.Debug.WriteLine("Found End tag to CurrentNodeName: {0}, setting to null", CurrentNodeName);
                             // Extract all the text up to this position
                             CurrentNodeName = null;

                             string strXML = FlushGet();

                             XMPPStanza stanza = new XMPPStanza(strXML);
                             connection.FireStanzaReceived(stanza);
                         }
                     }
                 }

              }
           }
           catch (Exception)
           {

           }
           finally
           {
           }
        }


        public string FlushGet()
        {
           string strAll = m_sbData.ToString();
           string strRet = strAll.Substring(0, (int) m_nPosition);
           m_sbData.Remove(0, (int) m_nPosition);
           m_nPosition = 0;
           return strRet;
        }

        public static Regex RegexXMLBlock = new Regex(@"\< [^\<\>]+  \>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        protected XMPPXMLNode ReadNextBlock(bool bRemove)
        {
           
           string strAll = m_sbData.ToString();
           strAll = strAll.TrimEnd();
           if (strAll.Length <= 0)
               return null;
           if (strAll[strAll.Length - 1] != '>') // make sure we have a full xml fragment before starting
               return null;

           Match match = RegexXMLBlock.Match(strAll, (int) m_nPosition);
           if (match.Success == true)
           {
              int nEndIndex = match.Index + match.Length;
              if (bRemove == true)
              {

                 m_sbData.Remove(0, nEndIndex);
                 m_nPosition = 0;
              }
              else
              {
                 m_nPosition = nEndIndex;
              }
              return new XMPPXMLNode(match.Value);
           }

           return null;
        }


    }
}
