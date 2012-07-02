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
using RTP;
using System.Net.Sockets;
namespace STUNtest
{
    public partial class MainPage : PhoneApplicationPage
    {
        RTPAudioStream stream;
        IPEndPoint localEp,stunEp;
        public void Log(string s)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() => { textBlock1.Text += s + '\n'; });
        }
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            localEp = new IPEndPoint(IPAddress.Parse("172.16.41.174"), 4505);
            stream = new RTPAudioStream(0,null);
            stream.AudioCodec = new G722CodecWrapper();
            stream.UseInternalTimersForPacketPushPull = false;
            stream.Bind(localEp);
            
        }

        public IPEndPoint FindStunAddress()
        {
            JingleMediaSession session = new JingleMediaSession(localEp);
            session.AudioRTPStream = stream;
            IPEndPoint ep = session.PerformSTUNRequest(new DnsEndPoint("stun.ekiga.net", 3478), 4000);
            IPEndPoint ep1 = session.PerformSTUNRequest(new DnsEndPoint("stun.ekiga.net", 3478), 4000);
            Log(ep.ToString());
            Log(ep1.ToString());
            return ep;
        }      
        


        private void button2_Click(object sender, RoutedEventArgs e)
        {
            stunEp = stream.GetSTUNAddress(new DnsEndPoint("stun.ekiga.net",3478),4000);
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            IPEndPoint remote = new IPEndPoint(IPAddress.Parse("172.16.41.174"),4507);
            stream.Testsend(remote,stunEp.ToString());
            Log(stream.TestRecv());

        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            
        }



    }
}