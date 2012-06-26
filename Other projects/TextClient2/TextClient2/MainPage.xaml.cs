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

namespace TextClient2
{
    public partial class MainPage : PhoneApplicationPage
    {
        SocketClient cs;
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            cs = new SocketClient(ref textBlock1);


        }
        private void button1_Click(object sender, RoutedEventArgs e)
        {

            cs.Send("172.16.41.174",4505,"sender2");
            cs.Receive(4505);
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            cs.Receive(Convert.ToInt32(textBox1.Text));
            cs.Receive(4505);
        }

        



    }
}