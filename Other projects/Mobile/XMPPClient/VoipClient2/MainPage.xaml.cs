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
using System.Windows.Threading;
using System.Threading;
using AudioClasses;
using SocketServer;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework;
using RTP;
namespace VoipClient2
{
    public partial class MainPage : PhoneApplicationPage
    {
        RTPAudioStream newstream;
        IPEndPoint newRemoteEp = new IPEndPoint(IPAddress.Parse("172.16.41.174"), 6001);
        IPEndPoint newLocalEp = new IPEndPoint(IPAddress.Parse("172.16.41.174"), 5001);

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            var _Timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            _Timer.Tick += (s, arg) =>
            {
                FrameworkDispatcher.Update();

            };
            _Timer.Start();
            newStartStream();
            //StartMedia();
        }
        public void newStartStream()
        {
            textBlock1.Text += "hi";
            newstream = new RTPAudioStream(0, null);
            newstream.Bind(newLocalEp);
            newstream.AudioCodec = new G722CodecWrapper();
            newstream.Start(newRemoteEp, 100, 100);
        }

        public void StartMedia()
        {
            newstream.IncomingRTPPacketBuffer.InitialPacketQueueMinimumSize = 4;
            newstream.IncomingRTPPacketBuffer.PacketSizeShiftMax = 10;
            int nMsTook = 0;
            byte[] data = newstream.WaitNextPacketSample(true, 1000000, out nMsTook);
            SoundEffect test = new SoundEffect(data, Microphone.Default.SampleRate, Microsoft.Xna.Framework.Audio.AudioChannels.Mono);
            SoundEffectInstance sm = test.CreateInstance();
            sm.Play();
            foreach (byte b in data)
                textBlock1.Text += (char)b;
            
        }
    }
}