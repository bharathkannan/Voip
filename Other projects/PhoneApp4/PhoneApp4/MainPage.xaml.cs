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
using FindMyIP;
namespace PhoneApp4
{
    public partial class MainPage : PhoneApplicationPage
    {
        IPAddress MyIp;
        SocketClient cs = new SocketClient();
        int RemotePort;
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            MyIPAddress finder = new MyIPAddress();
            finder.Find((address) =>
            {
                MyIp = address;
            });
           
        }
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            IPAddress RemoteIP = IPAddress.Parse(textBox1.Text);
            string res = cs.Connect(textBox1.Text, 8237);
            string res1= cs.Send("8001");
            textBox1.Text += res + res1;
            textBox1.Text += cs.Receive();
            textBox1.Text += cs.Receive();

        }
    }
}