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

namespace XMPPClient
{
    public partial class DebugPage : PhoneApplicationPage
    {
        public DebugPage()
        {
            InitializeComponent();
        }

        

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            SetLog();
        }
        void SetLog()
        {
            this.StackPanelWorkAroundMicrosoftBugs.Children.Clear();
            string strXML = App.XMPPLogBuilder.ToString();
            int nAt = 0;
            while (true)
            {
                TextBlock block = new TextBlock();
                int nLength = 1024;
                if ( (nAt+nLength) > strXML.Length)
                    nLength = strXML.Length-nAt;
                string strNext = strXML.Substring(nAt, nLength);
                block.Text = strNext;
                block.TextWrapping = TextWrapping.Wrap;
                nAt += nLength;

                this.StackPanelWorkAroundMicrosoftBugs.Children.Add(block);
                if (nAt >= strXML.Length)
                    break;
            }
            
            
        }

        private void ButtonClearLog_Click(object sender, EventArgs e)
        {
            App.XMPPLogBuilder.Clear();
            SetLog();
        }
    }
}