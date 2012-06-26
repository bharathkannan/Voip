/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Net; 

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.ComponentModel;

namespace System.Net.XMPP
{
    public enum FileTransferDirection
    {
        Send,
        Receive,
    }

    public enum FileTransferState
    {
        WaitingOnUserAccept,
        Transferring,
        Done,
        Error
    }

    public enum FileTransferType
    {
        IBB,
        ByteStreams,
    }

    public class FileTransfer : INotifyPropertyChanged
    {
        public FileTransfer(byte [] bData, string strFileName, JID remotejid)
        {
            BytesTotal = bData.Length;
            FileName = strFileName;
            Bytes = bData;
            RemoteJID = remotejid;
            this.FileTransferDirection = System.Net.XMPP.FileTransferDirection.Send;
            this.FileTransferState = System.Net.XMPP.FileTransferState.WaitingOnUserAccept;
        }

        public FileTransfer(string strFileName, int nBytesToBeReceived, JID remotejid)
        {
            BytesTotal = nBytesToBeReceived;
            BytesRemaining = nBytesToBeReceived;
            FileName = strFileName;
            RemoteJID = remotejid;
            this.FileTransferDirection = System.Net.XMPP.FileTransferDirection.Receive;
            this.FileTransferState = System.Net.XMPP.FileTransferState.WaitingOnUserAccept;
        }

        public override string ToString()
        {
            if (this.FileTransferState == XMPP.FileTransferState.Error)
                return string.Format("Error with {0} of {1} to {2}, Error: {3}", this.FileName, FileTransferDirection, RemoteJID.User, Error);

            if (this.FileTransferDirection == System.Net.XMPP.FileTransferDirection.Send)
                return string.Format("Sending file {0}, length {1} to {2}, State: {3}", this.FileName, BytesTotal, RemoteJID.User, this.FileTransferState);
            else
                return string.Format("Receiving file {0}, length {1} from {2}, State: {3}", this.FileName, BytesTotal, RemoteJID.User, this.FileTransferState);
        }

        public object Tag = null;

        public string StringValue
        {
            get
            {
                return ToString();
            }
            set
            {
            }
        }

        private string m_strError = "";

        public string Error
        {
            get { return m_strError; }
            set { m_strError = value; }
        }

        private string m_strsid = Guid.NewGuid().ToString();
        public string sid
        {
            get { return m_strsid; }
            set { m_strsid = value; }
        }

        private int m_nBytesRemaining = 0;
        public int BytesRemaining
        {
            get { return m_nBytesRemaining; }
            set 
            {
                if (m_nBytesRemaining != value)
                {
                    m_nBytesRemaining = value;
                    FirePropertyChanged("BytesRemaining");
                    FirePropertyChanged("PercentTransferred");
                }
            }
        }

        public int PercentTransferred
        {
            get
            {
                if (BytesTotal <= 0)
                    return 0;
                return (int) (100.0f * ( (double)(BytesTotal - BytesRemaining)/ (double)BytesTotal));
            }
            set
            {
            }
        }

        private bool m_bIsSaved = false;
        /// <summary>
        /// Allows the GUI application to specify if this has been saved
        /// </summary>
        public bool IsSaved
        {
            get { return m_bIsSaved; }
            set { m_bIsSaved = value; }
        }
        

        private int m_nBytesTotal = 0;
        public int BytesTotal
        {
            get { return m_nBytesTotal; }
            set
            {
                if (m_nBytesTotal != value)
                {
                    m_nBytesTotal = value;
                    FirePropertyChanged("BytesTotal");
                    FirePropertyChanged("StringValue");
                }
            }
        }

        private string m_strFileName = "";
        public string FileName
        {
            get { return m_strFileName; }
            set { m_strFileName = value; }
        }

        internal ByteStreamStreamLogic ByteStreamLogic = null;

        public void Close()
        {
            ByteStreamLogic = null;
            Bytes = null;
        }

        public static string GetFileNameFromFullString(string strFullFileName)
        {
            string strRet = strFullFileName;
#if WINDOWS_PHONE
            //\Applications\Data\66F652B2-CD0B-48F6-869F-D3B765EFC530\Data\PlatformData\PhotoChooser-907d2cc8-10a0-4745-9d08-ce328088e76e.jpg
            int nIndex = strRet.LastIndexOf("\\");
            if (nIndex > 0)
                strRet = strRet.Substring(nIndex + 1);
#else
            strRet = System.IO.Path.GetFileName(strFullFileName);
#endif
            return strRet; 
        }

        public string MimeType
        {
            get
            {
                string ext = System.IO.Path.GetExtension(FileName);

                if (ext == ".png")
                    return "image/png";
                else if (ext == ".jpg")
                    return "image/jpeg";
                else if (ext == ".bmp")
                    return "image/bmp";
                else
                    return null;
            }
        }


        private byte[] m_bBytes = null;
        public byte[] Bytes
        {
            get { return m_bBytes; }
            set { m_bBytes = value; }
        }

        private JID m_objRemoteJID = "";
        public JID RemoteJID
        {
          get { return m_objRemoteJID; }
          set { m_objRemoteJID = value; }
        }

        private FileTransferDirection m_eFileTransferDirection = FileTransferDirection.Receive;
        public FileTransferDirection FileTransferDirection
        {
            get { return m_eFileTransferDirection; }
            set { m_eFileTransferDirection = value; }
        }

        private FileTransferState m_eFileTransferState = FileTransferState.WaitingOnUserAccept;
        public FileTransferState FileTransferState
        {
            get { return m_eFileTransferState; }
            set
            {
                if (m_eFileTransferState != value)
                {
                    m_eFileTransferState = value;
                    FirePropertyChanged("FileTransferState");
                    FirePropertyChanged("StringValue");
                    FirePropertyChanged("IsVisibleSendCancel");
                    FirePropertyChanged("IsAcceptCancelVisible");
                    FirePropertyChanged("IsSaveVisible");
                }
            }
        }

#if !MONO
        public System.Windows.Visibility IsVisibleSendCancel
        {
            get
            {
                if (
                    (FileTransferDirection == System.Net.XMPP.FileTransferDirection.Send) && 
                    ((FileTransferState != System.Net.XMPP.FileTransferState.Done) && (FileTransferState != System.Net.XMPP.FileTransferState.Error))
                    )
                    return System.Windows.Visibility.Visible;
                return System.Windows.Visibility.Collapsed;
            }
            set
            {
            }
        }

        public System.Windows.Visibility IsAcceptCancelVisible
        {
            get
            {
                    if ((FileTransferDirection == System.Net.XMPP.FileTransferDirection.Receive) &&
                                       (FileTransferState == System.Net.XMPP.FileTransferState.WaitingOnUserAccept) )
                        return System.Windows.Visibility.Visible;
                    return System.Windows.Visibility.Collapsed;
            }
        }

        public System.Windows.Visibility IsSaveVisible
        {
            get
            {
                    if ((FileTransferDirection == System.Net.XMPP.FileTransferDirection.Receive) &&
                                       (FileTransferState == System.Net.XMPP.FileTransferState.Done) )
                        return System.Windows.Visibility.Visible;
                    return System.Windows.Visibility.Collapsed;
            }
        }
#endif
        
        private FileTransferType m_eFileTransferType = FileTransferType.IBB;
        public FileTransferType FileTransferType
        {
            get { return m_eFileTransferType; }
            set { m_eFileTransferType = value; }
        }

        #region INotifyPropertyChanged Members

        void FirePropertyChanged(string strName)
        {
            if (PropertyChanged != null)
            {
#if WINDOWS_PHONE
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(PropertyChanged, this, new System.ComponentModel.PropertyChangedEventArgs(strName));
#elif MONO
                PropertyChanged(this, new PropertyChangedEventArgs(strName));
#else
               System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(PropertyChanged, this, new System.ComponentModel.PropertyChangedEventArgs(strName));
#endif
            }
        }
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged = null;

        #endregion
    }


    public class FileTransferManager
    {
        public FileTransferManager(XMPPClient client)
        {
            XMPPClient = client;
        }

        public static string SOCKS5Proxy = null;
        public static bool UseIBBOnly = false;


        XMPPClient XMPPClient = null;

        private bool m_bAutoDownload = false;
        public bool AutoDownload
        {
            get { return m_bAutoDownload; }
            set { m_bAutoDownload = value; }
        }


        object m_objFileTransferLock = new object();

#if WINDOWS_PHONE
        private ObservableCollection<FileTransfer> m_listFileTransfers = new ObservableCollection<FileTransfer>();
        public ObservableCollection<FileTransfer> FileTransfers
        {
            get { return m_listFileTransfers; }
            set { m_listFileTransfers = value; }
        }
#elif MONO
        private ObservableCollection<FileTransfer> m_listFileTransfers = new ObservableCollection<FileTransfer>();
        public ObservableCollection<FileTransfer> FileTransfers
        {
            get { return m_listFileTransfers; }
            set { m_listFileTransfers = value; }
        }
#else
        private ObservableCollectionEx<FileTransfer> m_listFileTransfers = new ObservableCollectionEx<FileTransfer>();
        public ObservableCollectionEx<FileTransfer> FileTransfers
        {
            get { return m_listFileTransfers; }
            set { m_listFileTransfers = value; }
        }
#endif


        protected FileTransfer FindTransfer(string strSID)
        {
            lock (m_objFileTransferLock)
            {
                foreach (FileTransfer nexttrans in FileTransfers)
                {
                    if (nexttrans.sid == strSID)
                        return nexttrans;
                }
            }

            return null;
        }

        /// <summary>
        /// We've just gotten an ok from the remote end for our request to send them a file.
        /// Add the InbandByteStream service to received the file and set our state accordingly
        /// </summary>
        /// <param name="trans"></param>
        internal void StartNewReceiveStream(FileTransfer trans)
        {
            if (trans.FileTransferType == FileTransferType.IBB)
               trans.ByteStreamLogic = new InbandByteStreamLogic(XMPPClient, trans);
            else
                trans.ByteStreamLogic = new SOCKS5ByteStreamLogic(XMPPClient, trans);
            
            XMPPClient.AddLogic(trans.ByteStreamLogic);
            trans.ByteStreamLogic.StartAsync();
        }

        internal void GotStreamErrorResponse(FileTransfer trans, IQ iq)
        {
            if (iq.Error != null)
            {
                trans.Error = iq.Error.ErrorDescription;
                trans.FileTransferState = FileTransferState.Error;
            }
        }

        public string SendFile(string strFullFileName, JID jidto)
        {
            string strFileName = FileTransfer.GetFileNameFromFullString(strFullFileName);
            System.IO.FileStream stream = new FileStream(strFullFileName, FileMode.Open, FileAccess.Read);
            byte [] bData = new byte[stream.Length];
            stream.Read(bData, 0, bData.Length);
            stream.Close();

            FileTransfer trans = new FileTransfer(bData, strFileName, jidto) { FileTransferDirection = FileTransferDirection.Send };
            lock (m_objFileTransferLock)
            {
                FileTransfers.Add(trans);
            }

            XMPPClient.StreamInitiationAndTransferLogic.RequestStartFileTransfer(trans);
            return trans.sid;
        }

        public string SendFile(string strFullFileName, byte[] bData, JID jidto)
        {
            string strFileName = FileTransfer.GetFileNameFromFullString(strFullFileName);
            FileTransfer trans = new FileTransfer(bData, strFileName, jidto) { FileTransferDirection = FileTransferDirection.Send };
            lock (m_objFileTransferLock)
            {
                FileTransfers.Add(trans);
            }

            XMPPClient.StreamInitiationAndTransferLogic.RequestStartFileTransfer(trans);
            return trans.sid;
        }

        public void CancelSendFile(FileTransfer trans)
        {
            XMPPClient.StreamInitiationAndTransferLogic.RevokeSendRequest(trans);

            if (trans.ByteStreamLogic != null)
            {
                trans.ByteStreamLogic.Cancel();
            }

            lock (m_objFileTransferLock)
            {
                if (FileTransfers.Contains(trans) == true)
                {
                   FileTransfers.Remove(trans);
                }
            }

            trans.Error = "Cancelled by User";
            trans.FileTransferState = FileTransferState.Error;
            trans.Close();

        }

        public void AcceptFileDownload(FileTransfer trans)
        {
            if ( (trans != null) && (trans.ByteStreamLogic == null) )
            {
                trans.FileTransferState = FileTransferState.Transferring;
                if (trans.FileTransferType == FileTransferType.IBB)
                    trans.ByteStreamLogic = new InbandByteStreamLogic(XMPPClient, trans);
                else
                    trans.ByteStreamLogic = new SOCKS5ByteStreamLogic(XMPPClient, trans);

                XMPPClient.AddLogic(trans.ByteStreamLogic);
                trans.ByteStreamLogic.Start();

                XMPPClient.StreamInitiationAndTransferLogic.AcceptIncomingFileRequest(trans);
            }
        }

        public void DeclineFileDownload(FileTransfer trans)
        {
            if (trans != null)
            {
                lock (m_objFileTransferLock)
                {
                    FileTransfers.Remove(trans);
                }

                trans.Error = "Declined by User";
                trans.FileTransferState = FileTransferState.Error;
                XMPPClient.StreamInitiationAndTransferLogic.DeclineIncomingFileRequest(trans);
                trans.Close();
            }
            //  Microsoft.Xna.Framework.Media.SavePicture
        }


        public delegate void DelegateIncomingFile(FileTransfer trans, RosterItem itemfrom);
        public event DelegateIncomingFile OnNewIncomingFileTransferRequest = null;

        public delegate void DelegateDownloadFinished(FileTransfer trans);
        public event DelegateDownloadFinished OnTransferFinished = null;

        /// <summary>
        /// A remote client has asked us if we want to receive a file
        /// </summary>
        /// <param name="trans"></param>
        internal void NewIncomingFileRequest(FileTransfer trans)
        {
            if (AutoDownload == true)
            {
#if WINDOWS_PHONE
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(new DelegateAddTrans(DoAddTrans), trans);
#else
                lock (m_objFileTransferLock)
                {
                    FileTransfers.Add(trans);
                }
#endif
                System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(DoAskUserIfTheyWantToReceiveFile), trans);

                this.AcceptFileDownload(trans);
                return;
            }



            if (OnNewIncomingFileTransferRequest != null)
            {

#if WINDOWS_PHONE
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(new DelegateAddTrans(DoAddTrans), trans);
#else
               lock (m_objFileTransferLock)
               {
                   FileTransfers.Add(trans);
               }
#endif

                System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(DoAskUserIfTheyWantToReceiveFile), trans);
            }
            else 
            {
                XMPPClient.StreamInitiationAndTransferLogic.DeclineIncomingFileRequest(trans);
            }
        }

        delegate void DelegateAddTrans(FileTransfer trans);
        void DoAddTrans(FileTransfer trans)
        {
            lock (m_objFileTransferLock)
            {
                FileTransfers.Add(trans);
            }
        }

        void DoAskUserIfTheyWantToReceiveFile(object objAskInfo)
        {
            FileTransfer trans = objAskInfo as FileTransfer;
            try
            {
                RosterItem item = XMPPClient.FindRosterItem(trans.RemoteJID);

                OnNewIncomingFileTransferRequest(trans, item);
            }
            catch (Exception)
            { }
        }

        internal void FinishActiveFileTransfer(FileTransfer trans)
        {
            // Tell our client that this file transfer is finished
            try
            {
                if (OnTransferFinished != null)
                {
                    RosterItem item = XMPPClient.FindRosterItem(trans.RemoteJID);
                    if (item != null)
                        OnTransferFinished(trans);
                }

            }
            catch (Exception)
            {
            }
        }

       

    }
}
