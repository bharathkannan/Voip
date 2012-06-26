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

namespace WPFXMPPClient
{
    /// <summary>
    /// Interaction logic for AddNewRosterItemWindow.xaml
    /// </summary>
    public partial class AddNewRosterItemWindow : Window
    {
        public AddNewRosterItemWindow()
        {
            InitializeComponent();
        }

        public XMPPClient client = null;
        private void SurfaceButton_Click(object sender, RoutedEventArgs e)
        {
            client.AddToRoster(this.TextBoxJID.Text, this.TextBoxNickname.Text, this.TextBoxGroup.Text);
            this.DialogResult = true;
            this.Close();
        }
    }
}
