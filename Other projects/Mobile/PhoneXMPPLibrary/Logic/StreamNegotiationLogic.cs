/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Net;

using System.Xml;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

using System.Text.RegularExpressions;

namespace System.Net.XMPP
{
    public class Mechanism
    {
        public string Name {get; set;}
    }

    [Flags]
    public enum AuthMethod
    {
        NotSpecified = 0,
        MD5 = 1,
        Plain = 2,
        googletoken = 4,
    }


    public class StreamNegotiationLogic : Logic
    {
        public StreamNegotiationLogic(XMPPClient client)
            : base(client)
        {
        }

        /// <summary>
        ///  We've started our stream and now must negotiate features/authenticate
        /// </summary>
        public override void Start()
        {
            
            base.Start();
        }

        public void Reset()
        {
            m_nNonceCount = 1;
            AuthMethodsSupported = AuthMethod.Plain;
            FeatureTLS = false;
        }

        bool FeatureTLS = false;

        AuthMethod AuthMethodsSupported = AuthMethod.Plain;

        AuthMethod AuthMethodUsed = AuthMethod.NotSpecified;

        /// <summary>
        ///  
        /// </summary>
        /// <param name="stanza"></param>
        /// <returns></returns>
        public override bool NewXMLFragment(XMPPStanza stanza)
        {
            /// Looks like the crippled windows phone 7 libraries can't use output from xsd.exe, have to do this the hard way
            /// 
            //XDocument doc = XDocument.Load(new StringReader(stanza.XML));
            //XmlReader reader = XmlReader.Create(new StringReader(stanza.XML));


            stanza.XML = stanza.XML.Replace("stream:", "");  // no support for namespaces in windows phone 7, remove them

            XElement xmlElem = XElement.Parse(stanza.XML);

            if (XMPPClient.XMPPState >= XMPPState.Authenticated)
            {
                if (xmlElem.Name == "features")
                    return true;  /// If we hit this and parse the stream featurs a second time we re-authenticate.  Just return for now

                //if (xmlElem.Name == "stream")
                //{
                //     XMPPClient.XMPPState = XMPPState.CanBind;
                //}
                /// TODO.. see if this new stream supports bind
            }
          

            if (xmlElem.Name== "features")
            {
                //foreach (XElement node in xmlElem.Descendants())
                //{
                //    System.Diagnostics.Debug.WriteLine(node.Name);
                //}
                var Mechanisms = from mech in xmlElem.Descendants("{urn:ietf:params:xml:ns:xmpp-sasl}mechanism") 
                                 select new Mechanism
                                 {
                                     Name = mech.Value,
                                 };
                foreach (Mechanism mech in Mechanisms)
                {
                    if (mech.Name == "DIGEST-MD5")
                        AuthMethodsSupported |= AuthMethod.MD5;
                    else if (mech.Name == "PLAIN")
                        AuthMethodsSupported |= AuthMethod.Plain;
                  //  else if (mech.Name == "X-GOOGLE-TOKEN")
                    //    AuthMethodsSupported |= AuthMethod.googletoken;
                }

                if (AuthMethodsSupported == AuthMethod.NotSpecified)
                    throw new Exception("No acceptable authentication method was supplied");

                var tls = xmlElem.Descendants("{urn:ietf:params:xml:ns:xmpp-tls}starttls");
                if (tls.Count() > 0)
                    FeatureTLS = true;

                if ((FeatureTLS == true) && (XMPPClient.UseTLS == true))
                {
                    /// Tell the man we want to negotiate TLS
                    XMPPClient.SendRawXML(StartTLS);
                }
                else
                {
                    StartAuthentication();
                }

                return true;
            }
            else if (xmlElem.Name == "{urn:ietf:params:xml:ns:xmpp-tls}proceed")
            {
                XMPPClient.XMPPConnection.StartTLS();

                /// After starting TLS, start our normal digest authentication (or plain)
                /// 
                StartAuthentication();
            }
            else if (xmlElem.Name == "{urn:ietf:params:xml:ns:xmpp-sasl}challenge")
            {
                /// Build and send response
                /// 
                string strChallenge = xmlElem.Value;
                byte[] bData = Convert.FromBase64String(strChallenge);
                string strUnbasedChallenge = System.Text.UTF8Encoding.UTF8.GetString(bData, 0, bData.Length);

                //realm="ninethumbs.com",nonce="oFun3YWfVm/6nHCkNI/9a4XpcWIdQ5RH9E0IDVKH",qop="auth",charset=utf-8,algorithm=md5-sess

                //string strExampleResponse = "dXNlcm5hbWU9InRlc3QiLHJlYWxtPSJuaW5ldGh1bWJzLmNvbSIsbm9uY2U9InJaNjgreS9BeGp2SjJ6cjBCVUNxVUhQcG9ocFE4ZFkzR29JclpJcFkiLGNub25jZT0iVkdFRDNqNHUrUHE1M3IxYzNab2NhcGFzaWp1eTh2NjhoYXFzRC9IWjVKTT0iLG5jPTAwMDAwMDAxLGRpZ2VzdC11cmk9InhtcHAvbmluZXRodW1icy5jb20iLHFvcD1hdXRoLHJlc3BvbnNlPTdiM2MzOTVjZjU2MDA2Njg5MDg5MzdlYTk2YjEzZjI2LGNoYXJzZXQ9dXRmLTg=";
                //bData = Convert.FromBase64String(strExampleResponse);
                //string strUnbasedResponse = System.Text.UTF8Encoding.UTF8.GetString(bData, 0, bData.Length);
                //"username=\"test\",realm=\"ninethumbs.com\",nonce=\"rZ68+y/AxjvJ2zr0BUCqUHPpohpQ8dY3GoIrZIpY\",cnonce=\"VGED3j4u+Pq53r1c3Zocapasijuy8v68haqsD/HZ5JM=\",nc=00000001,digest-uri=\"xmpp/ninethumbs.com\",qop=auth,response=7b3c395cf5600668908937ea96b13f26,charset=utf-8";

                if (AuthMethodUsed == AuthMethod.MD5)
                {
                    string strRealm = XMPPClient.Domain;
                    Match matchrealm = Regex.Match(strUnbasedChallenge, @"realm=""([^""]+)""", RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);
                    if (matchrealm.Success == true)
                        strRealm = matchrealm.Groups[1].Value;

                    string strNonce = "";
                    Match matchnonce = Regex.Match(strUnbasedChallenge, @"nonce=""([^""]+)""", RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline);
                    if (matchnonce.Success == true)
                        strNonce = matchnonce.Groups[1].Value;


                    string strQop = "auth";
                    Match matchqop = Regex.Match(strUnbasedChallenge, @"qop=""([^""]+)""", RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);
                    if (matchqop.Success == true)
                        strQop = matchqop.Groups[1].Value;

                    string strAlgo = "md5-sess";
                    Match matchalgo = Regex.Match(strUnbasedChallenge, @"algorithm=([^\s,]+)", RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);
                    if (matchalgo.Success == true)
                        strAlgo = matchalgo.Groups[1].Value;

                    Random rand = new Random();
                    string strCnonce = rand.Next().ToString("X8").ToLower();
                    // Compute our MD5 response, then base64 it
                    string strResponse = GenerateMD5Response(strAlgo, XMPPClient.UserName, XMPPClient.Domain, XMPPClient.Password, strNonce, strCnonce);

                    string ResponseMessage = MD5Response.Replace("##RESPONSE##", strResponse);
                    XMPPClient.SendRawXML(ResponseMessage);
                }
                else if (AuthMethodUsed == AuthMethod.Plain)
                {
                    /// Send plain text stuff
                    /// 
                  
                }

            }
            else if (xmlElem.Name == "{urn:ietf:params:xml:ns:xmpp-sasl}success") /// Success
            {
                XMPPClient.XMPPState = XMPPState.Authenticated;

                if (XMPPClient.UseTLS == true)
                {
                    /// Start a new stream for some strange reason, but don't close the old one.
                    /// 
                    OpenStreamStanza open = new OpenStreamStanza(this.XMPPClient);
                    XMPPClient.SendRawXML(open.XML);
                }
                else
                {
                    XMPPClient.XMPPState = XMPPState.CanBind;
                }
            }
            else if (xmlElem.Name == "{urn:ietf:params:xml:ns:xmpp-sasl}failure")  /// Failed to authorize
            {
                XMPPClient.XMPPState = XMPPState.AuthenticationFailed;
            }


            return false;
        }

        void StartAuthentication()
        {
            if ((AuthMethodsSupported & AuthMethod.MD5) == AuthMethod.MD5)
            {
                AuthMethodUsed = AuthMethod.MD5;
                XMPPClient.SendRawXML(MD5Auth);
            }
            else if ( ((AuthMethodsSupported & AuthMethod.Plain) == AuthMethod.Plain) && (XMPPClient.UseTLS == true))
            {
                AuthMethodUsed = AuthMethod.Plain;

                byte[] bUserName = System.Text.UTF8Encoding.UTF8.GetBytes(XMPPClient.UserName);
                byte[] bPassword = System.Text.UTF8Encoding.UTF8.GetBytes(XMPPClient.Password);
                byte[] bText = new byte[2 + bUserName.Length + bPassword.Length];
                bText[0] = 0;
                Array.Copy(bUserName, 0, bText, 1, bUserName.Length);
                bText[bUserName.Length + 1] = 0;
                Array.Copy(bPassword, 0, bText, 1+bUserName.Length+1, bPassword.Length);

                string strBase64Text = Convert.ToBase64String(bText);
                string ResponseMessage = PlainAuth.Replace("##RESPONSE##", strBase64Text);
                XMPPClient.SendRawXML(ResponseMessage);
            }
            // Needs some type of google token retrieved from http or something
            //else if (((AuthMethodsSupported & AuthMethod.googletoken) == AuthMethod.googletoken) && (XMPPClient.UseTLS == true))
            //{
            //    AuthMethodUsed = AuthMethod.googletoken;

            //    string strText = string.Format("\0{0}@{1}\0{2}", XMPPClient.UserName, XMPPClient.Domain, XMPPClient.Password);
            //    byte[] bText = System.Text.UTF8Encoding.UTF8.GetBytes(strText);
            //    string strBase64Text = Convert.ToBase64String(bText);
            //    string ResponseMessage = PlainAuth.Replace("##RESPONSE##", strBase64Text);
            //    XMPPClient.SendRawXML(ResponseMessage);
            //}
        }

        const string StartTLS = @"<starttls xmlns=""urn:ietf:params:xml:ns:xmpp-tls""/>";
        const string MD5Auth = @"<auth xmlns=""urn:ietf:params:xml:ns:xmpp-sasl"" mechanism=""DIGEST-MD5"" />";
        //const string PlainAuth = @"<auth xmlns=""urn:ietf:params:xml:ns:xmpp-sasl"" mechanism=""PLAIN"" >##RESPONSE##</auth>";
        const string PlainAuth = @"<auth xmlns=""urn:ietf:params:xml:ns:xmpp-sasl"" mechanism=""PLAIN"" xmlns:ga=""http://www.google.com/talk/protocol/auth"" ga:client-uses-full-bind-result=""true"" >##RESPONSE##</auth>";
        const string MD5Response = @"<response xmlns=""urn:ietf:params:xml:ns:xmpp-sasl"">##RESPONSE##</response>";

        int m_nNonceCount = 1;
        private string GenerateMD5Response(string strAuthMethod, string AuthName, string strRealm, string strPassword, string strNonce, string strCnonce)
        {
            string digesturi = string.Format("xmpp/{0}", strRealm);

            byte[] bA1 = MD5Core.GetHash(string.Format("{0}:{1}:{2}", AuthName, strRealm, strPassword));
            string strHA1 = HexStringFromByte(bA1, false).ToLower();

            // if md5-sess, uncomment the following line
            if (strAuthMethod == "md5-sess")
            {
                string strNonces = string.Format(":{0}:{1}", strNonce, strCnonce);
                byte[] bNonces = System.Text.UTF8Encoding.UTF8.GetBytes(strNonces);
                byte[] bA1AndNonces = new byte[bA1.Length + bNonces.Length];
                Array.Copy(bA1, 0, bA1AndNonces, 0, bA1.Length);
                Array.Copy(bNonces, 0, bA1AndNonces, bA1.Length, bNonces.Length);

                byte[] bEncryptedA1 = MD5Core.GetHash(bA1AndNonces);
                strHA1 = HexStringFromByte(bEncryptedA1, false).ToLower();
                /// Works differently then sip here, doesn't use the hex string, uses the byte array
                //strHA1 = EncryptValue(string.Format("{0}:{1}:{2}", strHA1, strNonce, strCnonce));
            }

            string strHA2 = EncryptValue(string.Format("AUTHENTICATE:{0}", digesturi));
            string strResponse = EncryptValue(string.Format("{0}:{1}:{2}:{3}:{4}:{5}", new object[] { strHA1, strNonce, m_nNonceCount.ToString("X8"), strCnonce, "auth", strHA2 }));
            


            //realm="ninethumbs.com",nonce="K1HU68gvGGhprpzHozN7uHT+czu4nAkWUwPoku5c",qop="auth",charset=utf-8,algorithm=md5-sess
            //Ex:  "username=\"test\",realm=\"ninethumbs.com\",nonce=\"rZ68+y/AxjvJ2zr0BUCqUHPpohpQ8dY3GoIrZIpY\",cnonce=\"VGED3j4u+Pq53r1c3Zocapasijuy8v68haqsD/HZ5JM=\",nc=00000001,digest-uri=\"xmpp/ninethumbs.com\",qop=auth,response=7b3c395cf5600668908937ea96b13f26,charset=utf-8";
            //      username="test",realm="ninethumbs.com",nonce="ulGBZ80fK+bxFGfZSbp5Q6QCMZ26Ze100BpymIju",cnonce="68a0560c",nc=00000001,qop=auth,digest-uri="xmpp/ninethumbs.com",response="518d6f9a97f6c5e597caa0ef08297fc1",charset=utf-8
            string strTotalResponse = string.Format(@"username=""{0}"",realm=""{1}"",nonce=""{2}"",cnonce=""{3}"",nc={4},qop=auth,digest-uri=""{5}"",response={6},charset=utf-8",
                AuthName, strRealm, strNonce, strCnonce, m_nNonceCount.ToString("X8"), digesturi, strResponse);

            m_nNonceCount++;

            // Now, Base64 it
            byte [] bResponse = System.Text.UTF8Encoding.UTF8.GetBytes(strTotalResponse);

            return Convert.ToBase64String(bResponse);
        }

        /// <summary>
        /// Does an MD5 encryption of this value
        /// </summary>
        /// <param name="strValue"></param> 
        /// <returns></returns>
        public static string EncryptValue(string strValue)
        {
            byte [] bMD5 = MD5Core.GetHash(strValue);

            return HexStringFromByte(bMD5, false).ToLower();
        }

        public static string HexStringFromByte(byte[] aBytes, bool Spaces)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder(aBytes.Length * 3 + 10);
            foreach (byte b in aBytes)
            {
                string strHex = Convert.ToString(b, 16);
                if (strHex.Length == 1)
                    strHex = "0" + strHex;
                if (Spaces)
                    strHex += " ";
                builder.Append(strHex);
            }

            string strRet = builder.ToString();
            strRet = strRet.TrimEnd();
            strRet = strRet.ToUpper();
            return strRet;
        }

    }
}
