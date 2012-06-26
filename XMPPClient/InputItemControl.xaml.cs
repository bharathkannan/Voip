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

namespace XMPPClient
{
    public partial class InputItemControl : UserControl
    {
        public InputItemControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = this;
        }

        private string m_strInputLabel = "Enter Value:";

        public string InputLabel
        {
            get { return m_strInputLabel; }
            set { m_strInputLabel = value; }
        }
        private string m_strInputValue = "";

        public string InputValue
        {
            get { return m_strInputValue; }
            set { m_strInputValue = value; }
        }

        public void ShowAndGetItems()
        {
            this.Visibility = System.Windows.Visibility.Visible;
            this.DataContext = null;
            this.DataContext = this;
            this.TextBoxInput.Focus();
            this.TextBoxInput.SelectAll();
        }

        public event EventHandler OnInputSaved = null;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Visibility = System.Windows.Visibility.Collapsed;
            if (OnInputSaved != null)
                OnInputSaved(this, e);
        }
    }
}
