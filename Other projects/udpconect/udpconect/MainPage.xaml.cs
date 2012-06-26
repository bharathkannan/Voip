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

namespace udpconect
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            SocketClient cli = new SocketClient();
            string res = cli.Send("xyzbhatest.com",9050,"bharathwajan");
            op.Text = res;
            string res1 = cli.Receive(9050);
            op.Text += res1;
            op.Text += cli.getres();


        }
    }
}