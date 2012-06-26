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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Surface;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using Microsoft.Surface.Presentation.Input;

using System.IO.IsolatedStorage;
using System.Runtime.Serialization;
using System.IO;

namespace System.Net.XMPP
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public LoginWindow()
        {
            InitializeComponent();

        }

        /// <summary>
        /// Occurs when the window is about to close. 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }


        /// <summary>
        /// This is called when the application's window is not visible or interactive.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnWindowUnavailable(object sender, EventArgs e)
        {
            //TODO: disable audio, animations here
        }


        private void SurfaceButton_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveAccount != null)
                ActiveAccount.Password = this.TextBoxPassword.Password;

            SaveAccounts();

            this.DialogResult = true;
            this.Close();
        }

        public System.Net.XMPP.XMPPAccount m_objActiveAccount = null;

        public System.Net.XMPP.XMPPAccount ActiveAccount
        {
            get { return m_objActiveAccount; }
            set 
            { 
                m_objActiveAccount = value; 
            }
        }

        public List<System.Net.XMPP.XMPPAccount> AllAccounts = null;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (AllAccounts == null)
            {
                    // Load from storage
                string strPath = Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
                string strFileName = string.Format("{0}\\{1}", strPath, "xmppcred.item");
                FileStream location = null;
                try
                {
                    location = new FileStream(strFileName, System.IO.FileMode.Open);
                    DataContractSerializer ser = new DataContractSerializer(typeof(List<XMPPAccount>));

                    AllAccounts = ser.ReadObject(location) as List<XMPPAccount>;
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

            if (AllAccounts == null)
                AllAccounts = new List<XMPPAccount>();


            if (AllAccounts.Count <= 0)
                this.AllAccounts.Add(ActiveAccount);

            this.ComboBoxAccounts.ItemsSource = AllAccounts;
            bLoading = true;
            if (this.ComboBoxAccounts.Items.Contains(ActiveAccount) == true)
                this.ComboBoxAccounts.SelectedItem = ActiveAccount;
            else
                this.ComboBoxAccounts.SelectedIndex = 0;
            bLoading = false;
        }

        void SaveAccounts()
        {

               string strPath = Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
                string strFileName = string.Format("{0}\\{1}", strPath, "xmppcred.item");
                FileStream location = null;

                try
                {
                    location = new FileStream(strFileName, System.IO.FileMode.Create);
                    DataContractSerializer ser = new DataContractSerializer(typeof(List<XMPPAccount>));
                    ser.WriteObject(location, AllAccounts);
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

        bool bLoading = false;
        bool bIgnoreChanges = false;
        private void ComboBoxAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (bIgnoreChanges == true)
                return;

            if (ActiveAccount == null)
                return;

            ActiveAccount = this.ComboBoxAccounts.SelectedItem as XMPPAccount;
            if (ActiveAccount == null)
                return;
            ActiveAccount.LastPrescence.IsDirty = true;
            this.TextBoxPassword.Password = ActiveAccount.Password;

            bIgnoreChanges = true;
            this.DataContext = ActiveAccount;
            bIgnoreChanges = false;

            if (bLoading == false)
                SaveAccounts();
        }

        private void ButtonAddAccount_Click(object sender, RoutedEventArgs e)
        {
            XMPPAccount newaccount = new XMPPAccount();
            newaccount.AccountName = "New Account";
            this.AllAccounts.Add(newaccount);
            this.DataContext = newaccount;
            this.ComboBoxAccounts.ItemsSource = null;
            this.ComboBoxAccounts.ItemsSource = this.AllAccounts;
            this.ComboBoxAccounts.SelectedItem = newaccount;
            this.TextBoxPassword.Password = newaccount.Password;

        }


        private void TextBoxAccountName_LostFocus(object sender, RoutedEventArgs e)
        {
        }

        private void TextBoxAccountName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ActiveAccount != null)
            {
                this.ActiveAccount.AccountName = this.TextBoxAccountName.Text;
                this.ComboBoxAccounts.SelectedItem = this.ActiveAccount;
                this.ComboBoxAccounts.Text = this.TextBoxAccountName.Text;
            }
        }

        private void TextBoxPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ActiveAccount = this.ComboBoxAccounts.SelectedItem as XMPPAccount;
            if (ActiveAccount == null)
                return;

            ActiveAccount.Password = this.TextBoxPassword.Password;
        }


        
    }
}