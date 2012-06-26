/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

using System.Net.XMPP;
using Microsoft.Xna.Framework.Media;

namespace XMPPClient
{
    public partial class FileTransferPage : PhoneApplicationPage
    {
        public FileTransferPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            App.XMPPClient.FileTransferManager.OnNewIncomingFileTransferRequest += new FileTransferManager.DelegateIncomingFile(FileTransferManager_OnNewIncomingFileTransferRequest);

            this.DataContext = App.XMPPClient;
            this.ListBoxFileTransfers.ItemsSource = App.XMPPClient.FileTransferManager.FileTransfers;
        }

        void FileTransferManager_OnNewIncomingFileTransferRequest(FileTransfer trans, RosterItem itemfrom)
        {
            Dispatcher.BeginInvoke(new FileTransferManager.DelegateIncomingFile(SafeOnNewIncomingFileTransferRequest), trans, itemfrom);
        }

        void SafeOnNewIncomingFileTransferRequest(FileTransfer trans, RosterItem itemfrom)
        {
            this.ListBoxFileTransfers.ItemsSource = null;
            this.ListBoxFileTransfers.ItemsSource = App.XMPPClient.FileTransferManager.FileTransfers;
        }


        private void ButtonCancelSend_Click(object sender, RoutedEventArgs e)
        {
            FileTransfer trans = ((FrameworkElement)sender).DataContext as FileTransfer;
            if (trans != null)
            {
                App.XMPPClient.FileTransferManager.CancelSendFile(trans);
            }
        }

        private void ButtonAcceptTransfer_Click(object sender, RoutedEventArgs e)
        {
            FileTransfer trans = ((FrameworkElement)sender).DataContext as FileTransfer;
            if (trans != null)
            {
                App.XMPPClient.FileTransferManager.AcceptFileDownload(trans);
            }

        }

        private void ButtonDeclineTransfer_Click(object sender, RoutedEventArgs e)
        {
            FileTransfer trans = ((FrameworkElement)sender).DataContext as FileTransfer;
            if (trans != null)
            {
                App.XMPPClient.FileTransferManager.DeclineFileDownload(trans);
            }

        }

        private void ButtonSaveFile_Click(object sender, RoutedEventArgs e)
        {
            FileTransfer trans = ((FrameworkElement)sender).DataContext as FileTransfer;
            if (trans != null)
            {
                var library = new MediaLibrary();
                library.SavePicture(trans.FileName, trans.Bytes);
                MessageBox.Show("File saved to 'Saved Pictures'", "File Saved", MessageBoxButton.OK);
            }
        }

        private void ButtonOpenFile_Click(object sender, RoutedEventArgs e)
        {

        }

    }
}