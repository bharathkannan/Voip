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
using Microsoft.Xna.Framework;
using RTP;
using System.Windows.Threading;
using System.Threading;
using Microsoft.Xna.Framework.Audio;
namespace VoipClient2
{
    public partial class MainPage : PhoneApplicationPage
    {

        RTPAudioStream newstream;
        IPEndPoint newRemoteEp = new IPEndPoint(IPAddress.Parse("172.16.41.174"), 3001);
        

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            var new_Timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            new_Timer.Tick += (s, arg) =>
            {
                FrameworkDispatcher.Update();

            };
            new_Timer.Start();
            newStartStream();
            StartMedia();
        }
        public void newStartStream()
        {
            newstream = new RTPAudioStream(0, null);
            newstream.UseInternalTimersForPacketPushPull = false;
            IPEndPoint newLocalEp = new IPEndPoint(IPAddress.Parse("172.16.41.174"), 4001);
            newstream.Bind(newLocalEp);
            newstream.AudioCodec = new G722CodecWrapper();
            newstream.Start(newRemoteEp, 100, 100);
        }

        public void StartMedia()
        {

            Thread t = new Thread(new ThreadStart(getaudio));
            t.Name = "Checking thread";
            t.Start();


        }
        public void getaudio()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() => { textBlock1.Text += "Entering thread"; });
            newstream.IncomingRTPPacketBuffer.InitialPacketQueueMinimumSize = 4;
            newstream.IncomingRTPPacketBuffer.PacketSizeShiftMax = 10;
            int nMsTook = 0;
            byte[] data = newstream.WaitNextPacketSample(true, 10000, out nMsTook);
            Deployment.Current.Dispatcher.BeginInvoke(() =>
             {
                 textBlock1.Text += nMsTook;
                 foreach (byte b in data)
                     textBlock1.Text += (char)b;
                 SoundEffect test = new SoundEffect(data, Microphone.Default.SampleRate, Microsoft.Xna.Framework.Audio.AudioChannels.Mono);
                 SoundEffectInstance sm = test.CreateInstance();
                 sm.Play();
                 textBlock1.Text += "Exiting";
             });
        }
    }
}