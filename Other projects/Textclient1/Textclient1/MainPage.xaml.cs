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

namespace Textclient1
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

        public void Log(string s)
        {
            Deployment.Current.Dispatcher.BeginInvoke( () =>
            {
               textBlock1.Text += s +'\n';
            } );
        }
        private void button2_Click(object sender, RoutedEventArgs e)
        {
            cs.Send("172.16.41.174", 4505, "set "+textBox1.Text);
            Log(cs.Receive(4505));
           

        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            cs.Send("172.16.41.174", 4505, "get " + textBox2.Text);
            string ret = cs.Receive(4505);
            if (ret.Equals("Failed"))
            {
                Log("Callee not available");
                return;
            }
            IPEndPoint remote = null;
            String a, b;
            int pos = ret.IndexOf(':');
            a = ret.Substring(0, pos);
            b = ret.Substring(pos + 1);
            remote = new IPEndPoint(IPAddress.Parse(a), Convert.ToInt32(b));
            cs.Send(remote.Address.ToString(), remote.Port,"call_request:"+textBox1.Text);
            string reply = cs.Receive(remote.Port);
            Log("Remote Says :" + reply);
        }

        }
    }
