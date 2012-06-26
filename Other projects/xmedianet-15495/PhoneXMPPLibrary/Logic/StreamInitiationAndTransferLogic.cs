/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Linq;
using System.IO;

using SocketServer;
using System.Runtime.Serialization;
using System.Xml.Serialization;


namespace System.Net.XMPP
{


    ///<iq id="vsjwT-32" to="test@ninethumbs.com/CALCULON" from="test2@ninethumbs.com/calculon" type="set">
    ///<si xmlns="http://jabber.org/protocol/si" id="jsi_1513143543466953357" mime-type="image/png" profile="http://jabber.org/protocol/si/profile/file-transfer">
    ///<file xmlns="http://jabber.org/protocol/si/profile/file-transfer" name="image_yd.png" size="7057">
    /// <desc>Sending file</desc>
    ///</file>
    ///<feature xmlns="http://jabber.org/protocol/feature-neg">
    ///<x xmlns="jabber:x:data" type="form">
    ///<field var="stream-method" type="list-single">
    ///<option>
    /// <value>http://jabber.org/protocol/bytestreams</value>
    /// </option>
    /// <option>
    /// <value>http://jabber.org/protocol/ibb</value>
    /// </option>
    /// </field>
    /// </x>
    /// </feature></si></iq>

    [Flags]
    public enum StreamOptions
    {
        none = 0,
        bytestreams = 1,
        ibb = 2,
    }

    public enum StreamInitIQType
    {
        Offer,
        Result,
    }

    public class StreamInitIQ : IQ
    {
        public StreamInitIQ(StreamInitIQType type)
            : base()
        {
            StreamInitIQType = type;
        }

        public StreamInitIQ(string strXML)
            : base(strXML)
        {
        }

        public static StreamInitIQ BuildDefaultStreamInitOffer(XMPPClient client, FileTransfer trans)
        {
            StreamInitIQ q = new StreamInitIQ(StreamInitIQType.Offer);
            q.mimetype = trans.MimeType;
            q.filename = trans.FileName;
            q.sid = trans.sid;
            q.filesize = trans.BytesTotal;
            q.FileTransferObject = trans;

            q.StreamOptions = XMPP.StreamOptions.ibb;

            /// Don't offer byte streams if the server doesn't support a SOCKS 5 proxy, since windows phone can't listen for connections
            item filetransfer = client.ServerServiceDiscoveryFeatureList.GetItemByType(ItemType.SOCKS5ByteStream);

            if ((FileTransferManager.SOCKS5Proxy != null) && (FileTransferManager.SOCKS5Proxy.Length > 0))
                filetransfer = new item(); /// User has supplied a socks5 proxy, so we don't need our xmpp server to supply one to use bytestreams

            if ((filetransfer != null) && (FileTransferManager.UseIBBOnly == false))
            {
                q.StreamOptions |= StreamOptions.bytestreams;
            }

            return q;
        }

        public static StreamInitIQ BuildDefaultStreamInitResult(StreamOptions choosenoption)
        {
            StreamInitIQ q = new StreamInitIQ(StreamInitIQType.Result);
            q.sid = null;
            q.mimetype = null;
            q.StreamOptions = choosenoption;
            return q;
        }

        public static int nBaseId = 2334;
        public static object objBaseIdLock = new object();
        public static string GetNextId()
        {
            lock (objBaseIdLock)
            {
                string strRet = nBaseId.ToString();
                nBaseId++;
                return strRet;
            }
        }

        public string mimetype = null;
        public string profile = "http://jabber.org/protocol/si/profile/file-transfer";
        public string sid = null;

        public FileTransfer FileTransferObject = null;
        public string filename = null; // if present, we'll add the <file.. element
        public int filesize = 0;
        public string filehash = null;
        public string filedate = null;
        public string filedesc = "sending file";
        public StreamOptions StreamOptions = StreamOptions.bytestreams | StreamOptions.ibb;
        public StreamInitIQType StreamInitIQType = StreamInitIQType.Offer;
                
        public override void AddInnerXML(System.Xml.Linq.XElement elemMessage)
        {
            XElement elemSI = new XElement("{http://jabber.org/protocol/si}si");
            elemMessage.Add(elemSI);
            if (StreamInitIQType == StreamInitIQType.Offer)
            {
                elemSI.Add(new XAttribute("id", sid),  new XAttribute("profile", profile));
                if (mimetype != null)
                    elemSI.Add(new XAttribute("mime-type", mimetype));

                XElement elemfile = new XElement("{http://jabber.org/protocol/si/profile/file-transfer}file");
                elemSI.Add(elemfile);
                if (filename != null) elemfile.Add(new XAttribute("name", filename));
                if (filesize > 0) elemfile.Add(new XAttribute("size", filesize));
                if (filehash != null) elemfile.Add(new XAttribute("hash", filehash));
                if (filedesc != null) elemfile.Add(new XElement("{http://jabber.org/protocol/si/profile/file-transfer}desc", filedesc));

                XElement elemfeature = new XElement("{http://jabber.org/protocol/feature-neg}feature");
                elemSI.Add(elemfeature);

                XElement x = new XElement("{jabber:x:data}x", new XAttribute("type", "form"));
                elemfeature.Add(x);

                XElement field = new XElement("{jabber:x:data}field", new XAttribute("var", "stream-method"), new XAttribute("type", "list-single"));
                x.Add(field);

                if ((StreamOptions & StreamOptions.bytestreams) == StreamOptions.bytestreams)
                    field.Add(new XElement("{jabber:x:data}option", new XElement("{jabber:x:data}value", "http://jabber.org/protocol/bytestreams")));
                if ((StreamOptions & StreamOptions.ibb) == StreamOptions.ibb)
                    field.Add(new XElement("{jabber:x:data}option", new XElement("{jabber:x:data}value", "http://jabber.org/protocol/ibb")));

            }
            else
            {
                XElement elemfeature = new XElement("{http://jabber.org/protocol/feature-neg}feature");
                elemSI.Add(elemfeature);

                XElement x = new XElement("{jabber:x:data}x", new XAttribute("type", "submit"));
                elemfeature.Add(x);

                XElement field = new XElement("{jabber:x:data}field", new XAttribute("var", "stream-method"));
                x.Add(field);

                // Only add one option.  If user or'd more than one, go with the first
                if ((StreamOptions & StreamOptions.bytestreams) == StreamOptions.bytestreams)
                    field.Add(new XElement("{jabber:x:data}value", "http://jabber.org/protocol/bytestreams"));
                else if ((StreamOptions & StreamOptions.ibb) == StreamOptions.ibb)
                    field.Add(new XElement("{jabber:x:data}value", "http://jabber.org/protocol/ibb"));

            }

            base.AddInnerXML(elemMessage);
        }

        public override void ParseInnerXML(System.Xml.Linq.XElement elem)
        {
            XElement si = elem.FirstNode as XElement;
            if (si == null)
                return;

            if (si.Name != "{http://jabber.org/protocol/si}si")
                return;

            if (si.Attribute("id") != null)
                sid = si.Attribute("id").Value;
            if (si.Attribute("mime-type") != null)
                mimetype = si.Attribute("mime-type").Value;
            if (si.Attribute("profile") != null)
                profile = si.Attribute("profile").Value;

            StreamOptions = StreamOptions.none;
            foreach (XElement nextelem in si.Descendants())
            {
                if (nextelem.Name == "{http://jabber.org/protocol/si/profile/file-transfer}file")
                {
                    if (nextelem.Attribute("name") != null)
                        filename = nextelem.Attribute("name").Value;
                    if (nextelem.Attribute("size") != null)
                        filesize = Convert.ToInt32(nextelem.Attribute("size").Value);
                    if (nextelem.Attribute("hash") != null)
                        filehash = nextelem.Attribute("hash").Value;
                    if (nextelem.Attribute("date") != null)
                        filedate = nextelem.Attribute("date").Value;

                    if (nextelem.Element("{http://jabber.org/protocol/si/profile/file-transfer}desc") != null)
                        filedesc = nextelem.Element("{http://jabber.org/protocol/si/profile/file-transfer}desc").Value;
                }
                else if (nextelem.Name == "{http://jabber.org/protocol/feature-neg}feature")
                {
                    XElement x = nextelem.Element("{jabber:x:data}x");
                    if (x != null)
                    {
                        if (x.Attribute("type") != null)
                        {
                            if (x.Attribute("type").Value == "form")
                                StreamInitIQType = StreamInitIQType.Offer;
                            else if (x.Attribute("type").Value == "submit")
                                StreamInitIQType = StreamInitIQType.Result;
                        }


                        XElement field = x.Element("{jabber:x:data}field");
                        if (field != null)
                        {
                            /// This may work for both form and submits, because the values are there in both cases, just wrapped in an option in form
                            foreach (XElement nextopt in field.Descendants("{jabber:x:data}value"))
                            {
                                if (nextopt.Value == "http://jabber.org/protocol/bytestreams")
                                    StreamOptions |= StreamOptions.bytestreams;
                                else if (nextopt.Value == "http://jabber.org/protocol/ibb")
                                    StreamOptions |= StreamOptions.ibb;
                            }
                        }
                    }
                        

                }
            }
            base.ParseInnerXML(elem);
        }
    }

    public class StreamInitiationAndTransferLogic : Logic
    {
        public StreamInitiationAndTransferLogic(XMPPClient client)
            : base(client)
        {
        }


        public override bool NewIQ(IQ iq)
        {
            if (iq is StreamInitIQ)
            {
                StreamInitIQ siiq = iq as StreamInitIQ;
                if (iq.Type == IQType.result.ToString())
                {
                    /// May be a response to our pending request to send
                    /// 
                    StreamInitIQ initalrequest = null;
                    if (FileSendRequests.ContainsKey(iq.ID) == true)
                    {
                        initalrequest = FileSendRequests[iq.ID];
                        FileSendRequests.Remove(iq.ID);
                    }

                    if (initalrequest != null)
                    {
                        //if (siiq.StreamOptions != StreamOptions.ibb)
                        //{
                        //    /// Tell the host we failed to send the file because we only support ibb
                        //    return true;
                        //}
                        
                        /// Looks like they agree, start an ibb file transfer logic to perform the transfer
                        /// 

                        if ((siiq.StreamOptions & StreamOptions.bytestreams) == StreamOptions.bytestreams)
                            initalrequest.FileTransferObject.FileTransferType = FileTransferType.ByteStreams;
                        else if ((siiq.StreamOptions & StreamOptions.ibb) == StreamOptions.ibb)
                            initalrequest.FileTransferObject.FileTransferType = FileTransferType.IBB;

                        XMPPClient.FileTransferManager.StartNewReceiveStream(initalrequest.FileTransferObject);
                    }

                }
                else if (iq.Type == IQType.error.ToString())
                {
                    /// May be a response to our pending request to send
                    /// 
                    StreamInitIQ initalrequest = null;
                    if (FileSendRequests.ContainsKey(iq.ID) == true)
                    {
                        initalrequest = FileSendRequests[iq.ID];
                        FileSendRequests.Remove(iq.ID);
                    }

                    if (initalrequest != null)
                    {

                        XMPPClient.FileTransferManager.GotStreamErrorResponse(initalrequest.FileTransferObject, iq);
                    }

                }
                //else if (siiq.StreamInitIQType == StreamInitIQType.Offer)
                else if (iq.Type == IQType.set.ToString())
                {
                    /// They want to send a file to us?
                    /// Ask the user if it's OK, and if it is, start an ibb to receive it and send ok
                    /// 

                    if ((siiq.sid == null) || (siiq.sid.Length <= 0) )
                    {
                        IQ iqresponse = new StreamInitIQ(StreamInitIQType.Result);
                        iqresponse.ID = siiq.ID;
                        iqresponse.Type = IQType.error.ToString();
                        iqresponse.To = iq.From;
                        iqresponse.From = XMPPClient.JID;
                        iqresponse.Error = new Error(ErrorType.invalidid);
                        XMPPClient.SendXMPP(iqresponse);
                        return true;
                    }
                    if ((siiq.filename == null) || (siiq.filename.Length <= 0))
                    {
                        IQ iqresponse = new StreamInitIQ(StreamInitIQType.Result);
                        iqresponse.ID = siiq.ID;
                        iqresponse.Type = IQType.error.ToString();
                        iqresponse.To = iq.From;
                        iqresponse.From = XMPPClient.JID;
                        iqresponse.Error = new Error(ErrorType.notacceptable);
                        XMPPClient.SendXMPP(iqresponse);
                        return true;
                    }

                    /// Can only do bytes streams or inband byte streams
                    if ( ((siiq.StreamOptions & StreamOptions.ibb) != StreamOptions.ibb) &&
                         ((siiq.StreamOptions & StreamOptions.bytestreams) != StreamOptions.bytestreams))
                    {
                        IQ iqresponse = new StreamInitIQ(StreamInitIQType.Result);
                        iqresponse.ID = siiq.ID;
                        iqresponse.Type = IQType.error.ToString();
                        iqresponse.To = iq.From;
                        iqresponse.From = XMPPClient.JID;
                        iqresponse.Error = new Error(ErrorType.notacceptable);
                        XMPPClient.SendXMPP(iqresponse);
                        return true;
                    }

                    FileTransfer newreq = new FileTransfer(siiq.filename, siiq.filesize, siiq.From);
                    if ((siiq.StreamOptions & StreamOptions.bytestreams) == StreamOptions.bytestreams)
                        newreq.FileTransferType = FileTransferType.ByteStreams;
                    else if ((siiq.StreamOptions & StreamOptions.ibb) == StreamOptions.ibb)
                        newreq.FileTransferType = FileTransferType.IBB;

                    newreq.sid = siiq.sid;


                    FileDownloadRequests.Add(siiq.sid, siiq);

                    XMPPClient.FileTransferManager.NewIncomingFileRequest(newreq);
                }

                return true;
            }
            return false;
        }


        Dictionary<string, StreamInitIQ> FileSendRequests = new Dictionary<string, StreamInitIQ>();

        Dictionary<string, StreamInitIQ> FileDownloadRequests = new Dictionary<string, StreamInitIQ>();

        /// <summary>
        /// Remove this send request from our list.  
        /// TODO.. send a graceful cancel instead of just ignoring the acceptance
        /// </summary>
        /// <param name="trans"></param>
        internal void RevokeSendRequest(FileTransfer trans)
        {
            string strIdRemove = null;
            foreach (string strId in FileSendRequests.Keys)
            {
                StreamInitIQ iq = FileSendRequests[strId];
                if (iq.sid == trans.sid)
                {
                    strIdRemove = strId;
                    break;
                }
            }
            if (strIdRemove != null)
                FileSendRequests.Remove(strIdRemove);
        }
        

       /// <summary>
       /// Send out a stream initiation request
       /// </summary>
       /// <param name="trans"></param>
        internal void RequestStartFileTransfer(FileTransfer trans)
        {
            if (trans.FileTransferDirection != FileTransferDirection.Send)
                return;
            StreamInitIQ iq = StreamInitIQ.BuildDefaultStreamInitOffer(this.XMPPClient, trans);
            iq.From = XMPPClient.JID;
            iq.To = trans.RemoteJID;
            iq.Type = IQType.set.ToString();
            iq.mimetype = trans.MimeType;
            FileSendRequests.Add(iq.ID, iq);
            XMPPClient.SendXMPP(iq);
        }

        /// <summary>
        /// Tell the remote end we accept their stream request
        /// </summary>
        /// <param name="trans"></param>
        internal void AcceptIncomingFileRequest(FileTransfer trans)
        {
            if (FileDownloadRequests.ContainsKey(trans.sid) == true)
            {
                StreamInitIQ siiq = FileDownloadRequests[trans.sid];
                FileDownloadRequests.Remove(trans.sid);

                StreamOptions options = StreamOptions.bytestreams;
                if ( (siiq.StreamOptions&StreamOptions.bytestreams) == StreamOptions.bytestreams)
                    options = StreamOptions.bytestreams;
                else if ((siiq.StreamOptions & StreamOptions.ibb) == StreamOptions.ibb)
                    options = StreamOptions.ibb;

                StreamInitIQ iqaccept = StreamInitIQ.BuildDefaultStreamInitResult(options);
                iqaccept.ID = siiq.ID;
                iqaccept.From = XMPPClient.JID;
                iqaccept.To = siiq.From;
                iqaccept.Type = IQType.result.ToString();
                XMPPClient.SendXMPP(iqaccept);
            }

        }

        /// <summary>
        /// Tell the remote end we decline their stream request
        /// </summary>
        /// <param name="trans"></param>
        internal void DeclineIncomingFileRequest(FileTransfer trans)
        {
            if (FileDownloadRequests.ContainsKey(trans.sid) == true)
            {
                StreamInitIQ siiq = FileDownloadRequests[trans.sid];
                FileDownloadRequests.Remove(trans.sid);

                StreamInitIQ iqdecline = StreamInitIQ.BuildDefaultStreamInitResult(StreamOptions.ibb);
                iqdecline.ID = siiq.ID; 
                iqdecline.Type = IQType.error.ToString();
                iqdecline.To = siiq.From;
                iqdecline.From = XMPPClient.JID;
                iqdecline.Error = new Error(ErrorType.notacceptable);
                XMPPClient.SendXMPP(iqdecline);
            }
        }
    }

    [DataContract]
    [XmlRoot(ElementName = "streamhost")]
    public class StreamHost
    {
        public StreamHost()
        {
        }

        private string m_strHost = null;
        [XmlAttribute(AttributeName = "host")]
        [DataMember]
        public string Host
        {
            get { return m_strHost; }
            set { m_strHost = value; }
        }

        private string m_strJid = null;
        [XmlAttribute(AttributeName = "jid")]
        [DataMember]
        public string Jid
        {
            get { return m_strJid; }
            set { m_strJid = value; }
        }

        private string m_strPort = null;
        [XmlAttribute(AttributeName = "port")]
        [DataMember]
        public string Port
        {
            get { return m_strPort; }
            set { m_strPort = value; }
        }
    }


    public enum StreamMode
    {
        tcp,
        udp,
    }

    [DataContract]
    [XmlRoot(ElementName = "query", Namespace = "http://jabber.org/protocol/bytestreams")]
    public class ByteStreamQuery
    {
        public ByteStreamQuery()
        {
        }

        private string m_strMode = null;
        [XmlAttribute(AttributeName = "mode")]
        [DataMember]
        public string Mode
        {
            get { return m_strMode; }
            set { m_strMode = value; }
        }

        private string m_strSID = null;
        [XmlAttribute(AttributeName = "sid")]
        [DataMember]
        public string SID
        {
            get { return m_strSID; }
            set { m_strSID = value; }
        }

        private StreamHost[] m_aHosts = null;
        [XmlElement(ElementName = "streamhost")]
        [DataMember]
        public StreamHost[] Hosts
        {
            get { return m_aHosts; }
            set { m_aHosts = value; }
        }

        [XmlElement(ElementName = "streamhost-used")]
        [DataMember]
        public StreamHost StreamHostUsed = null;

        [XmlElement(ElementName = "activate")]
        [DataMember]
        public string Activate = null;

    }
    

    // Build this straight from xml
    [XmlRoot(ElementName = "iq", Namespace = null)]
    public class ByteStreamQueryIQ : IQ
    {

        public ByteStreamQueryIQ()
            : base()
        {
        }

        public ByteStreamQueryIQ(string strXML)
            : base(strXML)
        {
        }

        [XmlAttribute(AttributeName="sid")]
        [DataMember]
        public string SID = null;

        private ByteStreamQuery m_aByteStreamQuery = null;
        [XmlElement(ElementName = "query", Namespace="http://jabber.org/protocol/bytestreams")]
        [DataMember]
        public ByteStreamQuery ByteStreamQuery
        {
            get { return m_aByteStreamQuery; }
            set { m_aByteStreamQuery = value; }
        }

       
    }


    public enum IBBMode
    {
        Send,
        Receive,
    }

    public class ByteStreamStreamLogic : Logic
    {
        public ByteStreamStreamLogic(XMPPClient client, FileTransfer trans)
            : base(client)
        {
            FileTransfer = trans;
        }

        private FileTransfer m_objFileTransfer = null;

        public FileTransfer FileTransfer
        {
            get { return m_objFileTransfer; }
            set { m_objFileTransfer = value; }
        }

        public virtual void Cancel()
        {
            this.IsCompleted = true;
        }

        public virtual void StartAsync()
        {
            System.Threading.ThreadPool.QueueUserWorkItem(new Threading.WaitCallback(DoStart));
        }

        void DoStart(object obj)
        {
            Start();
        }

    }

    /// <summary>
    /// Sends or receives a file using XEP-0047, then notifies the XMPPClient it is finished and to 
    /// be removed from the logic stack
    /// </summary>
    public class InbandByteStreamLogic : ByteStreamStreamLogic
    {
        public InbandByteStreamLogic(XMPPClient client, FileTransfer trans)
            : base(client, trans)
        {
        }


        int nActions = 0;
        IQ InitialIQ = null;
        IQ LastFileDataIQSent = null;
        ByteBuffer FileBuffer = new ByteBuffer();
        const int nBlockSize = 4096*4;
        int nSequence = 0;

        public override void Start()
        {
            if (FileTransfer.FileTransferDirection == FileTransferDirection.Send)
            {
                InitialIQ = new IQ();
                InitialIQ.From = XMPPClient.JID;
                InitialIQ.To = FileTransfer.RemoteJID;
                InitialIQ.Type = IQType.set.ToString();

                InitialIQ.InnerXML = string.Format("<open xmlns='http://jabber.org/protocol/ibb' block-size='{1}' sid='{0}' stanza='iq' />", FileTransfer.sid, nBlockSize);
                XMPPClient.SendXMPP(InitialIQ);
            }
            else
            {
            }

            base.Start();
        }

        public override void Cancel()
        {
            this.IsCompleted = true;
        }

        void SendNextFileIQ()
        {
            /// For debugging, so we can see our progress
            //System.Threading.Thread.Sleep(10);
            if (LastFileDataIQSent == null)
            {
                FileTransfer.BytesRemaining = FileTransfer.BytesTotal;
                FileBuffer.AppendData(FileTransfer.Bytes);
                nSequence = 0;
            }

            if (FileBuffer.Size <= 0)
            {
                FileTransfer.FileTransferState = FileTransferState.Done;
                XMPPClient.FileTransferManager.FinishActiveFileTransfer(FileTransfer);
                this.IsCompleted = true;
                return;
            }

            int nChunkSize = (FileBuffer.Size > nBlockSize) ? nBlockSize : FileBuffer.Size;
            FileTransfer.BytesRemaining -= nChunkSize;
            byte[] bNext = FileBuffer.GetNSamples(nChunkSize);

            LastFileDataIQSent = new IQ();
            LastFileDataIQSent.From = XMPPClient.JID;
            LastFileDataIQSent.To = FileTransfer.RemoteJID;
            LastFileDataIQSent.Type = IQType.set.ToString();

            string strBase64 = Convert.ToBase64String(bNext);
            LastFileDataIQSent.InnerXML = string.Format("<data xmlns='http://jabber.org/protocol/ibb' seq='{0}' sid='{1}' >{2}</data>",
                                                        nSequence++, FileTransfer.sid, strBase64);
            XMPPClient.SendXMPP(LastFileDataIQSent);
            nActions++;

        }


        public override bool NewIQ(IQ iq)
        {
            if (this.IsCompleted == true)  // We've been cancelled
                return false;

            if (FileTransfer.FileTransferDirection == FileTransferDirection.Send)
            {
                if (iq.ID == InitialIQ.ID)
                {
                    if (iq.Type == IQType.error.ToString())
                    {
                        if (iq.Error != null)
                            FileTransfer.Error = iq.Error.ErrorDescription;
                        FileTransfer.FileTransferState = FileTransferState.Error;
                        IsCompleted = true; /// Remove this guy
                        XMPPClient.FileTransferManager.FinishActiveFileTransfer(FileTransfer);
                        return true;
                    }
                    else if (iq.Type == IQType.result.ToString())
                    {
                        // Send the next chunk
                        SendNextFileIQ();
                    }

                    return true;
                }
                else if ((LastFileDataIQSent != null) && (iq.ID == LastFileDataIQSent.ID))
                {
                    if (iq.Type == IQType.error.ToString())
                    {
                        /// TODO.. notify the user there was a failure transferring blocks
                        /// 
                        if (iq.Error != null)
                            FileTransfer.Error = iq.Error.ErrorDescription;
                        FileTransfer.FileTransferState = FileTransferState.Error;
                        XMPPClient.FileTransferManager.FinishActiveFileTransfer(FileTransfer);
                        IsCompleted = true;
                        return true;
                    }
                    else if (iq.Type == IQType.result.ToString())
                    {
                        // Send the next chunk
                        SendNextFileIQ();
                    }
                }
            }
            else
            {
                if (iq.InitalXMLElement != null)
                {
                    XElement elem = iq.InitalXMLElement.FirstNode as XElement;
                    if ((elem != null) && (elem.Name == "{http://jabber.org/protocol/ibb}open"))
                    {
                        string strStreamId = null;
                        if (elem.Attribute("sid") != null)
                            strStreamId = elem.Attribute("sid").Value;
                        if ((strStreamId == null) || (strStreamId != this.FileTransfer.sid))
                            return false;

                        FileBuffer.GetAllSamples();

                        /// SEnd ack to open
                        /// 
                        IQ iqresponse = new IQ();
                        iqresponse.ID = iq.ID;
                        iqresponse.From = XMPPClient.JID;
                        iqresponse.To = FileTransfer.RemoteJID;
                        iqresponse.Type = IQType.result.ToString();
                        XMPPClient.SendXMPP(iqresponse);

                        return true;
                    }
                    if ((elem != null) && (elem.Name == "{http://jabber.org/protocol/ibb}data"))
                    {
                        string strStreamId = null;
                        if (elem.Attribute("sid") != null)
                            strStreamId = elem.Attribute("sid").Value;
                        if ((strStreamId == null) || (strStreamId != FileTransfer.sid))
                            return false;


                        byte[] bData = Convert.FromBase64String(elem.Value);

                        FileTransfer.BytesRemaining -= bData.Length;

                        FileBuffer.AppendData(bData);

                        /// SEnd ack
                        /// 
                        IQ iqresponse = new IQ();
                        iqresponse.ID = iq.ID;
                        iqresponse.From = XMPPClient.JID;
                        iqresponse.To = FileTransfer.RemoteJID;
                        iqresponse.Type = IQType.result.ToString();
                        XMPPClient.SendXMPP(iqresponse);


                        nActions++;

                        if (FileTransfer.BytesRemaining <= 0)
                        {
                            FileTransfer.Bytes = FileBuffer.GetAllSamples();
                            FileTransfer.FileTransferState = FileTransferState.Done;
                            
                            IsCompleted = true;
                            XMPPClient.FileTransferManager.FinishActiveFileTransfer(FileTransfer);
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        public override bool NewMessage(Message iq)
        {
            return base.NewMessage(iq);
        }

    }






    /// <summary>
    /// Sends or receives a file using XEP-0065, then notifies the XMPPClient it is finished and to 
    /// be removed from the logic stack
    /// </summary>
    public class SOCKS5ByteStreamLogic : ByteStreamStreamLogic
    {
        public SOCKS5ByteStreamLogic(XMPPClient client, FileTransfer trans)
            : base(client, trans)
        {
        }


        ByteStreamQueryIQ IQStart = null;
        ByteStreamQueryIQ IQActivate = null;

       
        item FindProxyItem()
        {
            foreach (item nextitem in XMPPClient.ServerServiceDiscoveryFeatureList.Items)
            {
                if (nextitem.ItemType == ItemType.SOCKS5ByteStream)
                    return nextitem;
            }

            return null;
        }

        public override void Start()
        {
            if (FileTransfer.FileTransferDirection == FileTransferDirection.Send)
            {
                /// Tell the other end we want to send a file..  give them a list of SOCKS5 servers they can connect to (hopefully our open fire server supports it)
                /// 

                IQStart = new ByteStreamQueryIQ();
                //IQStart.SID = this.FileTransfer.sid;
                IQStart.ByteStreamQuery = new ByteStreamQuery();
                IQStart.ByteStreamQuery.SID = this.FileTransfer.sid;
                IQStart.ByteStreamQuery.Mode = StreamMode.tcp.ToString();
                IQStart.From = XMPPClient.JID;
                IQStart.To = FileTransfer.RemoteJID;
                IQStart.Type = IQType.set.ToString();

                /// Build our stream objects
                /// 
                /// For windows we can start a local listener and a proxy listener
                /// For windows phone we can only use the proxy (if our jabber server supports it) because we can't listen for connections
                /// 

                string strJID = string.Format("proxy.{0}", XMPPClient.Domain);
                string strHost = XMPPClient.Server;
                string strPort = "7777";

                if ((FileTransferManager.SOCKS5Proxy != null) && (FileTransferManager.SOCKS5Proxy.Length > 0))
                {
                    /// User supplied socks5 proxy...TODO, add port configuration
                    strJID = FileTransferManager.SOCKS5Proxy;
                    strHost = FileTransferManager.SOCKS5Proxy;
                    strPort = "7777";
                }
                else
                {
                    /// Query our xmpp server for a proxy, then for the details
                    /// 

                    item filetransfer = FindProxyItem();
                    if (filetransfer != null)
                    {
                        strJID = filetransfer.JID;
                        /// Query the server for the actual stream host of thi sitem

                        ByteStreamQueryIQ iqqueryproxy = new ByteStreamQueryIQ();
                        iqqueryproxy.From = XMPPClient.JID;
                        iqqueryproxy.To = strJID;
                        iqqueryproxy.Type = IQType.get.ToString();
                        iqqueryproxy.ByteStreamQuery = new ByteStreamQuery();

                        IQ iqret = XMPPClient.SendRecieveIQ(iqqueryproxy, 15000, SerializationMethod.XMLSerializeObject);
                        if ((iqret != null) && (iqret is ByteStreamQueryIQ))
                        {
                            ByteStreamQueryIQ response = iqret as ByteStreamQueryIQ;
                            if ((response.ByteStreamQuery != null) && (response.ByteStreamQuery.Hosts != null) && (response.ByteStreamQuery.Hosts.Length > 0))
                            {
                                strHost = response.ByteStreamQuery.Hosts[0].Host;
                                strPort = response.ByteStreamQuery.Hosts[0].Port;
                                strJID = response.ByteStreamQuery.Hosts[0].Jid;
                            }
                        }

                    }
                }

                StreamHost host = new StreamHost() { Host = strHost, Port = strPort, Jid = strJID };
                //StreamHost host = new StreamHost() { Host = "192.168.1.124", Port = "7777", Jid = string.Format("proxy.{0}", XMPPClient.Domain) };
                IQStart.ByteStreamQuery.Hosts = new StreamHost[] { host };
                IQStart.ByteStreamQuery.Mode = StreamMode.tcp.ToString();


                XMPPClient.SendObject(IQStart);
                
            }
            else
            {
            }

            base.Start();
        }

        public override void Cancel()
        {
            ConnectSuccesful = false;
            this.IsCompleted = true;
            EventConnected.Set();

            EventGotAllData.Set();
            SendCompletedEvent.Set();
        }

    


        public override bool NewIQ(IQ iq)
        {
            if (this.IsCompleted == true)  // We've been cancelled
                return false;

            

            if (FileTransfer.FileTransferDirection == FileTransferDirection.Send)
            {
                if  ( (IQStart != null) && (iq.ID == IQStart.ID) )
                {
                    if (iq.Type == IQType.error.ToString())
                    {
                        IsCompleted = true;
                        if (iq.Error != null)
                            FileTransfer.Error = iq.Error.ErrorDescription;
                        FileTransfer.FileTransferState = FileTransferState.Error;
                        XMPPClient.FileTransferManager.FinishActiveFileTransfer(FileTransfer);
                    }
                    else if (iq is ByteStreamQueryIQ)
                    {
                        ByteStreamQueryIQ bsiq = iq as ByteStreamQueryIQ;
                        if (bsiq.Type == IQType.result.ToString())
                        {
                            System.Threading.ThreadPool.QueueUserWorkItem(new Threading.WaitCallback(SendFileThread), bsiq);
                        }
                        else if (bsiq.Type == IQType.error.ToString())
                        {
                            IsCompleted = true;
                            if (iq.Error != null)
                                FileTransfer.Error = bsiq.Error.ErrorDescription;
                            FileTransfer.FileTransferState = FileTransferState.Error;

                        }
                    }

                    return true;
                }
                if ((IQActivate != null)  && (iq.ID == IQActivate.ID))
                {
                    EventActivate.Set();
                    return true;
                }
                
                
            }
            else
            {
                if (iq is ByteStreamQueryIQ)
                {
                    ByteStreamQueryIQ bsiq = iq as ByteStreamQueryIQ;
 
                    if ( (bsiq.ByteStreamQuery != null) && (bsiq.ByteStreamQuery.Hosts != null) && (bsiq.ByteStreamQuery.Hosts.Length > 0) )
                    {
                        System.Threading.ThreadPool.QueueUserWorkItem(new Threading.WaitCallback(DownloadThread), bsiq);
                    }
                    else
                    {
                        ByteStreamQueryIQ response = new ByteStreamQueryIQ();
                        response.ByteStreamQuery = bsiq.ByteStreamQuery;
                        response.From = XMPPClient.JID;
                        response.To = bsiq.From;
                        response.ID = bsiq.ID;
                        response.Type = IQType.error.ToString();
                        response.Error = new Error(ErrorType.notacceptable);
                        XMPPClient.SendXMPP(response);
                    }
                    /// See qh
                    /// 
                    return true;
                }

            }

            return false;
        }



          public void SendFileThread(object obj)
        {
            ByteStreamQueryIQ bsiq = obj as ByteStreamQueryIQ;

            /// Attempt to open our hosts
            /// 
            StreamHost host = bsiq.ByteStreamQuery.StreamHostUsed;

            SocketServer.SocketClient client = new SocketClient();

            client.SetSOCKSProxy(5, XMPPClient.Server, 7777, "xmppclient");
            client.OnAsyncConnectFinished += new DelegateConnectFinish(client_OnAsyncConnectFinished);
            EventConnected.Reset();
            ConnectSuccesful = false;

            string strHost = string.Format("{0}{1}{2}", this.FileTransfer.sid, XMPPClient.JID, bsiq.From);
            System.Security.Cryptography.SHA1Managed sha = new System.Security.Cryptography.SHA1Managed();
            byte [] bBytes = sha.ComputeHash(System.Text.UTF8Encoding.UTF8.GetBytes(strHost));
            strHost = SocketServer.TLS.ByteHelper.HexStringFromByte(bBytes, false, int.MaxValue).ToLower();

            /// Connect parametrs are the sha1 hash and 0, the socks proxy will connect us to the correct place
            client.ConnectAsync(strHost, 0);

            EventConnected.WaitOne();
            if (ConnectSuccesful == true)
            {

                /// Now we must activate the proxy so we can send
                /// 
                EventActivate.Reset();
                IQActivate = new ByteStreamQueryIQ();
                IQActivate.ByteStreamQuery = new ByteStreamQuery();
                IQActivate.ByteStreamQuery.SID = this.FileTransfer.sid;
                IQActivate.From = XMPPClient.JID;
                IQActivate.To = host.Jid;
                IQActivate.Type = IQType.set.ToString();
                IQActivate.ByteStreamQuery.Activate = FileTransfer.RemoteJID;
                XMPPClient.SendObject(IQActivate);
                EventActivate.WaitOne();

                if (IsCompleted == true)
                {
                    /// Error, exit this thread
                    FileTransfer.FileTransferState = FileTransferState.Error;
                    XMPPClient.FileTransferManager.FinishActiveFileTransfer(FileTransfer);
                    return;
                }


                FileTransfer.BytesRemaining = FileTransfer.Bytes.Length;
                FileTransfer.FileTransferState = FileTransferState.Transferring;

                /// Now send all our data
                /// 
                ByteBuffer buffer = new ByteBuffer();
                buffer.AppendData(FileTransfer.Bytes);


                while (buffer.Size > 0)
                {
                    int nSize = (buffer.Size > 16384) ? 16384 : buffer.Size;
                    Sockets.SocketAsyncEventArgs asyncsend = new Sockets.SocketAsyncEventArgs();
                    asyncsend.SetBuffer(buffer.GetNSamples(nSize), 0, nSize);
                    asyncsend.Completed += new EventHandler<Sockets.SocketAsyncEventArgs>(asyncsend_Completed);
                    
                    
                    SendCompletedEvent.Reset();
                    bSendSuccess = false;
                    bool bSent = false;
                    try
                    {
                        client.socket.SendAsync(asyncsend);
                    }
                    catch (Exception ex)
                    {
                        IsCompleted = true;
                        FileTransfer.Error = ex.Message;
                        FileTransfer.FileTransferState = FileTransferState.Error;
                        return;
                    }
                    SendCompletedEvent.WaitOne();
                    if (IsCompleted == true)
                        break;

                    if ((bSendSuccess == false) && (bSent == false) ) /// was sent async because bSent is false, so we can examine bSendSuccess to make sure we sent the right number of bytes
                    {
                        break;
                    }
                    FileTransfer.BytesRemaining -= nSize;
                }
                

                client.Disconnect();

                FileTransfer.FileTransferState = FileTransferState.Done;
                IsCompleted = true;
                XMPPClient.FileTransferManager.FinishActiveFileTransfer(FileTransfer);
                return;
            }

            FileTransfer.Error = "Failed to send data";
            FileTransfer.FileTransferState = FileTransferState.Error;
            IsCompleted = true;
        }

        System.Threading.ManualResetEvent SendCompletedEvent = new Threading.ManualResetEvent(false);
        System.Threading.ManualResetEvent EventActivate = new Threading.ManualResetEvent(false);
        bool bSendSuccess = false;
        void  asyncsend_Completed(object sender, Sockets.SocketAsyncEventArgs e)
        {
            int nTransferred = e.BytesTransferred;
            if (nTransferred == e.Buffer.Length)
                bSendSuccess = true;
            else
                bSendSuccess = false;
 	        SendCompletedEvent.Set();
        }


        public void DownloadThread(object obj)
        {
            ByteStreamQueryIQ bsiq = obj as ByteStreamQueryIQ;

            /// Attempt to open our hosts
            /// 
            foreach (StreamHost host in bsiq.ByteStreamQuery.Hosts)
            {
                if (IsCompleted == true)
                    break;

                SocketServer.SocketClient client = new SocketClient();

                client.SetSOCKSProxy(5, host.Host, Convert.ToInt32(host.Port), "xmppclient");
                client.OnAsyncConnectFinished += new DelegateConnectFinish(client_OnAsyncConnectFinished);
                EventConnected.Reset();
                ConnectSuccesful = false;

                string strHost = string.Format("{0}{1}{2}", this.FileTransfer.sid, bsiq.From, bsiq.To);
                System.Security.Cryptography.SHA1Managed sha = new System.Security.Cryptography.SHA1Managed();
                byte [] bBytes = sha.ComputeHash(System.Text.UTF8Encoding.UTF8.GetBytes(strHost));
                strHost = SocketServer.TLS.ByteHelper.HexStringFromByte(bBytes, false, int.MaxValue).ToLower();

                /// Connect parametrs are the sha1 hash and 0, the socks proxy will connect us to the correct place
                client.StartReadOnConnect = true;
                client.ConnectAsync(strHost, 0);

                EventConnected.WaitOne();
                
                if (ConnectSuccesful == true)
                {
                    FileTransfer.FileTransferState = FileTransferState.Transferring;
                    client.OnReceiveMessage += new SocketClient.SocketReceiveHandler(client_OnReceiveMessage);
                    DownloadFileBuffer.GetAllSamples();

                    /// connected and negotiated socks5, tell the far end to start sending data
                    /// 
                    ByteStreamQueryIQ iqresponse = new ByteStreamQueryIQ();
                    iqresponse.SID = this.FileTransfer.sid;
                    iqresponse.ByteStreamQuery = new ByteStreamQuery();
                    iqresponse.ByteStreamQuery.StreamHostUsed = new StreamHost();
                    iqresponse.ByteStreamQuery.StreamHostUsed.Jid = host.Jid;
                    iqresponse.From = XMPPClient.JID;
                    iqresponse.ID = bsiq.ID;
                    iqresponse.To = bsiq.From;
                    iqresponse.Type = IQType.result.ToString();
                    XMPPClient.SendObject(iqresponse);

                    /// Now read data until we get our desired amount
                    /// 
                    EventGotAllData.WaitOne();

                    client.Disconnect();
                    return;
                }
            }

            /// Couldn't transfer file, send error
            /// 
            ByteStreamQueryIQ response = new ByteStreamQueryIQ();
            response.ByteStreamQuery = bsiq.ByteStreamQuery;
            response.From = XMPPClient.JID;
            response.To = bsiq.From;
            response.ID = bsiq.ID;
            response.Type = IQType.error.ToString();
            response.Error = new Error(ErrorType.remoteservertimeout);
            XMPPClient.SendXMPP(response);
            FileTransfer.Error = "Could not connect to proxy";
            FileTransfer.FileTransferState = FileTransferState.Error;
            XMPPClient.FileTransferManager.FinishActiveFileTransfer(FileTransfer);
        }

        ByteBuffer DownloadFileBuffer = new ByteBuffer();
        System.Threading.ManualResetEvent EventGotAllData = new Threading.ManualResetEvent(false);
        void client_OnReceiveMessage(SocketClient client, byte[] bData, int nLength)
        {
            FileTransfer.BytesRemaining -= nLength;
            DownloadFileBuffer.AppendData(bData);

            if (DownloadFileBuffer.Size >= FileTransfer.BytesTotal)
            {
                FileTransfer.Bytes = DownloadFileBuffer.GetAllSamples();
                EventGotAllData.Set();
                FileTransfer.FileTransferState = FileTransferState.Done;
                IsCompleted = true;
                XMPPClient.FileTransferManager.FinishActiveFileTransfer(FileTransfer);
            }
        }

        System.Threading.ManualResetEvent EventConnected = new Threading.ManualResetEvent(false);
        bool ConnectSuccesful = false;
        void client_OnAsyncConnectFinished(SocketClient client, bool bSuccess, string strErrors)
        {
            ConnectSuccesful = bSuccess;
            EventConnected.Set();
            
        }

    }
}
