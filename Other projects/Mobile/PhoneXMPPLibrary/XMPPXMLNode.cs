/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Net;

using System.Xml;
using System.Text.RegularExpressions;

namespace System.Net.XMPP
{
    /// Simple XML parser to pull data off the incoming stream
    /// XMLReader classes expect to have complete XML, or they block on the stream, which is unacceptable
    public class XMPPXMLNode
    {
        public XMPPXMLNode(string strXMLFragment)
        {
            ParseXMLNode(strXMLFragment);
        }

        string m_strOuterXML = "";
        public string OuterXML
        {
            get
            {
                return m_strOuterXML;
            }
        }

        string m_strName = "";
        public string Name
        {
            get
            {
                return m_strName;
            }
        }


        XmlNodeType m_XmlNodeType = XmlNodeType.None;
        public XmlNodeType NodeType
        {
            get
            {
                return m_XmlNodeType;
            }
        }

        public static Regex RegexDeclaration = new Regex(@"\<\?xml [^\<\>]* \?\>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        public static Regex RegexComment = new Regex(@"\< \s* !-- [^\<\>]* \>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        public static Regex RegexEndElement = new Regex(@"\<\/ (?<name>[^\<\>]+) \>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        public static Regex RegexStartElement = new Regex(@"\< (?<name>\S+) [^\<\>]* \>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        public static Regex RegexCompleteElement = new Regex(@"\< (?<name>\S+) [^\<\>]* \/\>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        void ParseXMLNode(string strXML)
        {
            m_strOuterXML = strXML;

            Match matchman = RegexDeclaration.Match(strXML);
            if (matchman.Success == true)
            {
                m_XmlNodeType |= XmlNodeType.XmlDeclaration;
                return;
            }

            matchman = RegexComment.Match(strXML);
            if (matchman.Success == true)
            {
                return;
            }

            matchman = RegexEndElement.Match(strXML);
            if (matchman.Success == true)
            {
                m_strName = matchman.Groups["name"].Value;
                m_XmlNodeType |= XmlNodeType.EndElement;
                return;
            }

            matchman = RegexCompleteElement.Match(strXML);
            if (matchman.Success == true)
            {
                m_strName = matchman.Groups["name"].Value;
                m_XmlNodeType |= XmlNodeType.EndElement;
                return;
            }

            matchman = RegexStartElement.Match(strXML);
            if (matchman.Success == true)
            {
                m_strName = matchman.Groups["name"].Value;
                return;
            }



            // Don't care about anything else
            return;



        }

        public string GetAttribute(string strAttributeName)
        {
            string strExp = string.Format("{0}=\"(?<value>.*?)\"", strAttributeName);
            Regex RegexAttribute = new Regex(strExp, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            Match matchman = RegexAttribute.Match(m_strOuterXML);

            if (matchman.Success == true)
            {
                string strValue = matchman.Groups["value"].Value;
                //strValue = strValue.Trim(' ', '\"');
                strValue = strValue.Trim();
                return strValue;
            }

            return "";
        }



    }
}
