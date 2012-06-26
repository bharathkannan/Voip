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
    /// Interaction logic for FileTransferWindow.xaml
    /// </summary>
    public partial class FileTransferWindow : Window
    {
        public FileTransferWindow()
        {
            InitializeComponent();
        }

        public XMPPClient XMPPClient = null;

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
            this.ListBoxConversation.ItemsSource = XMPPClient.FileTransferManager.FileTransfers;
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
                if (File.Exists(trans.FileName) == true)
                {
                    /// We've set auto save - which changes the full file name to the full directory
                    System.Diagnostics.Process.Start(trans.FileName);
                }
            }
        }

        private void ButtonClearTransfers_Click(object sender, RoutedEventArgs e)
        {
            List<FileTransfer> ClearList = new List<FileTransfer>();
            foreach (FileTransfer trans in XMPPClient.FileTransferManager.FileTransfers)
            {
                if ((trans.FileTransferState == FileTransferState.Done) || (trans.FileTransferState == FileTransferState.Error))
                {
                    ClearList.Add(trans);
                }
            }

            foreach (FileTransfer trans in ClearList)
            {
                XMPPClient.FileTransferManager.FileTransfers.Remove(trans);
                trans.Close();
            }

        }
    }
}
