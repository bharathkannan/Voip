///// Copyright (c) 2011 Brian Bonnett
///// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
///// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Data;
//using System.Windows.Documents;
//using System.Windows.Input;
//using System.Windows.Media;
//using System.Windows.Media.Imaging;
//using System.Windows.Shapes;

//using System.Net.XMPP;
//using System.IO;

//namespace WPFXMPPClient
//{
//    /// <summary>
//    /// Interaction logic for FileTransferWindow.xaml
//    /// </summary>
//    public partial class MapWindow : Window
//    {
//        public MapWindow()
//        {
//            InitializeComponent();
//        }

//        public XMPPClient XMPPClient = null;

//        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
//        {
//            if ((e.ChangedButton == MouseButton.Left) && (e.ButtonState == MouseButtonState.Pressed))
//                this.DragMove();
//        }

//        private void Window_Loaded(object sender, RoutedEventArgs e)
//        {
//            /// Bind to our XMPP Client filetransfer
//            /// 
//            this.DataContext = XMPPClient;
//            this.ListBoxConversation.ItemsSource = XMPPClient.FileTransferManager.FileTransfers;
//        }

//        private void ButtonCancelSend_Click(object sender, RoutedEventArgs e)
//        {

//        }

//        private void ButtonClose_Click(object sender, RoutedEventArgs e)
//        {
//            this.Close();
//        }

//        private void ButtonAcceptTransfer_Click(object sender, RoutedEventArgs e)
//        {
//            FileTransfer trans = ((FrameworkElement)sender).DataContext as FileTransfer;
//            if (trans != null)
//            {
//                XMPPClient.FileTransferManager.AcceptFileDownload(trans);
//            }
//        }

//        private void ButtonDeclineTransfer_Click(object sender, RoutedEventArgs e)
//        {
//            FileTransfer trans = ((FrameworkElement)sender).DataContext as FileTransfer;
//            if (trans != null)
//            {
//                XMPPClient.FileTransferManager.DeclineFileDownload(trans);
//            }
//        }

//        private void ButtonSaveFile_Click(object sender, RoutedEventArgs e)
//        {
//            FileTransfer trans = ((FrameworkElement)sender).DataContext as FileTransfer;
//            if (trans != null)
//            {
//                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
//                dlg.InitialDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
//                dlg.FileName = trans.FileName;
//                if (dlg.ShowDialog() == true)
//                {
//                    FileStream stream = new FileStream(dlg.FileName, FileMode.Create, FileAccess.Write);
//                    stream.Write(trans.Bytes, 0, trans.Bytes.Length);
//                    stream.Close();
//                }
                
//            }
//        }

//        private void ButtonOpenFile_Click(object sender, RoutedEventArgs e)
//        {
//            FileTransfer trans = ((FrameworkElement)sender).DataContext as FileTransfer;
//            if (trans != null)
//            {
//                if (File.Exists(trans.FileName) == true)
//                {
//                    /// We've set auto save - which changes the full file name to the full directory
//                    System.Diagnostics.Process.Start(trans.FileName);
//                }
//            }
//        }

//        private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
//        {
//            //List<FileTransfer> ClearList = new List<FileTransfer>();
//            //foreach (FileTransfer trans in XMPPClient.FileTransferManager.FileTransfers)
//            //{
//            //    if ((trans.FileTransferState == FileTransferState.Done) || (trans.FileTransferState == FileTransferState.Error))
//            //    {
//            //        ClearList.Add(trans);
//            //    }
//            //}

//            //foreach (FileTransfer trans in ClearList)
//            //{
//            //    XMPPClient.FileTransferManager.FileTransfers.Remove(trans);
//            //    trans.Close();
//            //}

//        }
//    }
//}





/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

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
using System.IO;

namespace WPFXMPPClient
{
    /// <summary>
    /// Interaction logic for MapWindow.xaml
    /// </summary>
    public partial class MapWindow : Window
    {
        public MapWindow()
        {
            InitializeComponent();
        }

        public XMPPClient XMPPClient = null;
        private RosterItem m_OurRosterItem = null;

        public RosterItem OurRosterItem
        {
            get { return m_OurRosterItem; }
            set { m_OurRosterItem = value;
            MapUserControl1.OurRosterItem = value;
            }
        }

        private bool m_SingleRosterItemMap = true;

        public bool SingleRosterItemMap
        {
            get { return m_SingleRosterItemMap; }
            set
            {
                MapUserControl1.SingleRosterItemMap = m_SingleRosterItemMap;
                m_SingleRosterItemMap = value;
            }
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((e.ChangedButton == MouseButton.Left) && (e.ButtonState == MouseButtonState.Pressed))
                this.DragMove();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            /// Bind to our XMPP Client filetransfer
            /// 
            this.DataContext = XMPPClient;
            MapUserControl1.XMPPClient = XMPPClient;
            MapUserControl1.OurRosterItem = this.OurRosterItem;
         //   this.ListBoxConversation.ItemsSource = XMPPClient.FileTransferManager.FileTransfers;

            


        }

        private void ButtonCancelSend_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ButtonAcceptTransfer_Click(object sender, RoutedEventArgs e)
        {
            FileTransfer trans = ((FrameworkElement)sender).DataContext as FileTransfer;
            if (trans != null)
            {
                XMPPClient.FileTransferManager.AcceptFileDownload(trans);
            }
        }

        private void ButtonDeclineTransfer_Click(object sender, RoutedEventArgs e)
        {
            FileTransfer trans = ((FrameworkElement)sender).DataContext as FileTransfer;
            if (trans != null)
            {
                XMPPClient.FileTransferManager.DeclineFileDownload(trans);
            }
        }

        private void ButtonSaveFile_Click(object sender, RoutedEventArgs e)
        {
            FileTransfer trans = ((FrameworkElement)sender).DataContext as FileTransfer;
            if (trans != null)
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.InitialDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
                dlg.FileName = trans.FileName;
                if (dlg.ShowDialog() == true)
                {
                    FileStream stream = new FileStream(dlg.FileName, FileMode.Create, FileAccess.Write);
                    stream.Write(trans.Bytes, 0, trans.Bytes.Length);
                    stream.Close();
                }

            }
        }

        private void ButtonOpenFile_Click(object sender, RoutedEventArgs e)
        {
            FileTransfer trans = ((FrameworkElement)sender).DataContext as FileTransfer;
            if (trans != null)
            {
                string strFullFileName = string.Format("{0}\\{1}", System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), trans.FileName);
                if (File.Exists(strFullFileName) == false)
                {
                    FileStream stream = new FileStream(strFullFileName, FileMode.Create, FileAccess.Write);
                    stream.Write(trans.Bytes, 0, trans.Bytes.Length);
                    stream.Close();
                }
                System.Diagnostics.Process.Start(strFullFileName);
            }
        }

        private void MapUserControl1_Loaded(object sender, RoutedEventArgs e)
        {
            MapUserControl1.Refresh();
        }


        private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            MapUserControl1.Refresh();
            //ButtonLoadURL_Click(null, e);
        }

        private void ButtonZoomIn_Click(object sender, RoutedEventArgs e)
        {
            MapUserControl1.ZoomIn(1);
        }

        private void ButtonZoomOut_Click(object sender, RoutedEventArgs e)
        {
            MapUserControl1.ZoomOut(1);
        }


        //private void LoadMap()
        //{
        //    WebBrowserMap.Navigate("http://www.yahoo.com");
        //}

        //private void ButtonLoadURL_Click(object sender, RoutedEventArgs e)
        //{
        //    string strURL = TextBoxURL.Text;

        //    WebBrowserMap.Navigate(strURL);
        //}
    }
}
