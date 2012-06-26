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

namespace chat_client
{
    public partial class MainPage : PhoneApplicationPage
    {
        SocketClient cs;
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            cs = new SocketClient(ref Chat);
                
            
        }

        public void writetobox(string text)
        {
            Chat.Text += "Server:\n";
            Chat.Text += text;
        }

        private void send_Click(object sender, RoutedEventArgs e)
        {
            if (!cs.check())
            {
                string tosend = input.Text;
                cs.Send("client.openvpn.net", 9050, tosend);
                Chat.Text += "\n";
                Chat.Text += "Client:";
                Chat.Text += tosend;
                cs.Receive(9050);
            }
            else
            {
                Chat.Text += "\nConnection closed";
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            Chat.Text += "Closing connection";
            cs.Close();
        }

        private void input_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
        }
    }
