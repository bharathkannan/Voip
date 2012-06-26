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

namespace PhoneApp1
{
    public partial class MainPage : PhoneApplicationPage
    {
        const int ECHO_PORT = 7;  // The Echo protocol uses port 7 in this sample
        const int QOTD_PORT = 17;
        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        private void go_Click(object sender, RoutedEventArgs e)
        {
            sample.Text = "";
            SocketClient cl = new SocketClient();
            string res1 = cl.Connect("xyzbhatest.com", 8002);
            string res2 = cl.getres();
            string res3=  cl.Send("abcdef");
            sample.Text = res1 + res3 ;


        }
    }
}