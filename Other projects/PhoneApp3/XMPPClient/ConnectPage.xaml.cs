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

using System.Runtime.Serialization;
using System.IO.IsolatedStorage;
using System.Net.XMPP;

namespace XMPPClient
{

    public partial class ConnectPage : PhoneApplicationPage
    {
        public ConnectPage()
        {
            InitializeComponent();
        }

        List<XMPPAccount> Accounts = new List<XMPPAccount>();

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            /// Check for crashes last time we ran
            App.CheckForExceptionsLastTime();

            using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (storage.FileExists("xmppcred.item") == true)
                {
                    // Load from storage
                    IsolatedStorageFileStream location = null;
                    try
                    {
                        location = new IsolatedStorageFileStream("xmppcred.item", System.IO.FileMode.Open, storage);
                        DataContractSerializer ser = new DataContractSerializer(typeof(List<XMPPAccount>));

                        Accounts = ser.ReadObject(location) as List<XMPPAccount>;
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        if (location != null)
                            location.Close();
                    }
                }
                
            }

            if (Accounts.Count <= 0)
            {
                this.m_bAddingAccount = true;
                AccountNameInputControl.InputValue = "New Account";
                AccountNameInputControl.ShowAndGetItems();
                this.MainScrollViewer.IsEnabled = false;
                this.AccountPicker.IsEnabled = false;
                base.OnNavigatedTo(e);
                return;
            }

            this.AccountPicker.ItemsSource = Accounts;
            this.AccountPicker.SelectedItem = Accounts[0];
            //this.AccountPicker.SelectedItem = App.XMPPClient.XMPPAccount; /// can't do this until we hash/equal by name, 

            base.OnNavigatedTo(e);
        }


        private void AccountPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            if (AccountPicker == null)
                return;

            XMPPAccount acc = AccountPicker.SelectedItem as XMPPAccount;
            if (acc == null)
                return;
            this.TextBoxPassword.Password = acc.Password;
            this.DataContext = acc;
        }


        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {

            using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                // Load from storage
                IsolatedStorageFileStream location = new IsolatedStorageFileStream("xmppcred.item", System.IO.FileMode.Create, storage);
                DataContractSerializer ser = new DataContractSerializer(typeof(List<XMPPAccount>));

                if (App.Options.SavePasswords == false)
                {
                    foreach (XMPPAccount account in Accounts)
                    {
                        account.Password = "";
                    }
                }

                try
                {
                    ser.WriteObject(location, Accounts);
                }
                catch (Exception)
                {
                }
                location.Close();
            }


            base.OnNavigatedFrom(e);
        }

        private void ButtonConnect_Click(object sender, EventArgs e)
        {
            if (m_bAddingAccount == true)
                return;

            App.XMPPClient.XMPPAccount = this.AccountPicker.SelectedItem as XMPPAccount;

            if (App.XMPPClient.XMPPState == System.Net.XMPP.XMPPState.Connected)
                App.XMPPClient.Disconnect();

            /// store the password in memory before serializing in case the user doesn't want them save to disk
            string strPassword = App.XMPPClient.XMPPAccount.Password;

            using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                // Load from storage
                IsolatedStorageFileStream location = new IsolatedStorageFileStream("xmppcred.item", System.IO.FileMode.Create, storage);
                DataContractSerializer ser = new DataContractSerializer(typeof(List<XMPPAccount>));

                if (App.Options.SavePasswords == false)
                {
                    foreach (XMPPAccount account in Accounts)
                    {
                        account.Password = "";
                    }
                }

                try
                {
                    ser.WriteObject(location, Accounts);
                }
                catch (Exception)
                {
                }
                location.Close();
            }


            App.XMPPClient.XMPPAccount.Password = strPassword;
            App.XMPPClient.Connect(App.Current as SocketServer.ILogInterface);
            NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative)); 
        }

        bool m_bAddingAccount = false;
        private void ButtonNewAccount_Click(object sender, EventArgs e)
        {
            if (m_bAddingAccount == true)
                return;

            m_bAddingAccount = true;
            AccountNameInputControl.InputValue = "New Account";
            AccountNameInputControl.ShowAndGetItems();
            this.MainScrollViewer.IsEnabled = false;
            this.AccountPicker.IsEnabled = false;
        }

        private void AccountNameInputControl_OnInputSaved(object sender, EventArgs e)
        {
            m_bAddingAccount = false;
            this.MainScrollViewer.IsEnabled = true;
            this.AccountPicker.IsEnabled = true;

            XMPPAccount XMPPAccount = new XMPPAccount();
            XMPPAccount.JID = "user@gmail.com/phone";
            XMPPAccount.Server = "talk.google.com";
            XMPPAccount.Port = 5223;
            XMPPAccount.UseOldSSLMethod = true;
            XMPPAccount.UseTLSMethod = true;
            XMPPAccount.AccountName = AccountNameInputControl.InputValue;
            Accounts.Add(XMPPAccount);

            List<XMPPAccount> NewAccounts = new List<XMPPAccount>();
            NewAccounts.AddRange(Accounts);

            this.AccountPicker.ItemsSource = NewAccounts;
            Accounts = NewAccounts;
            this.AccountPicker.SelectedItem = XMPPAccount;
            this.DataContext = XMPPAccount;
        }

        private void TextBoxPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            XMPPAccount acc = AccountPicker.SelectedItem as XMPPAccount;
            if (acc == null)
                return;
            acc.Password = this.TextBoxPassword.Password;

        }

        private void CheckBoxUseOldSSLMethod_Checked(object sender, RoutedEventArgs e)
        {
            XMPPAccount acc = AccountPicker.SelectedItem as XMPPAccount;
            if (acc == null)
                return;

            acc.Port = 5223;
        }

        private void CheckBoxUseOldSSLMethod_Unchecked(object sender, RoutedEventArgs e)
        {
            XMPPAccount acc = AccountPicker.SelectedItem as XMPPAccount;
            if (acc == null)
                return;

            acc.Port = 5222;
        }

     
    }
}