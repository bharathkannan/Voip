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

namespace WPFXMPPClient
{
    /// <summary>
    /// Interaction logic for AddBuddyWindow.xaml
    /// </summary>
    public partial class AddBuddyWindow : Window
    {
        public AddBuddyWindow()
        {
            InitializeComponent();
        }

        private string m_strJID = "";

        public string JID
        {
            get { return m_strJID; }
            set { m_strJID = value; }
        }
        
        private string m_strNickName = "";

        public string NickName
        {
            get { return m_strNickName; }
            set { m_strNickName = value; }
        }

        private string m_strGroup = "";

        public string Group
        {
            get { return m_strGroup; }
            set { m_strGroup = value; }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            NickName = JID;
            this.DataContext = this;
            this.LabelMessage.Content = string.Format("{0} would like to see your presence, Allow?", JID);
        }

        private void ButtonYes_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            this.Close();
        }

        private void ButtonNo_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;

            this.Close();
        }
    }
}
