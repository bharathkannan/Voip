using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System.Windows.Threading;
using System.Net;
using System.Threading;
using RTP;
using AudioClasses;
namespace PhoneApp1
{
    public partial class MainPage : PhoneApplicationPage
    {
        RTPAudioStream stream = null;
        IPAddress myip;
        IPEndPoint localEp;
        Thread MicrophoneThread;
        AudioClasses.ByteBuffer MicrophoneQueue = new ByteBuffer();
        Boolean IsMicActive;
        List<byte[]> Audio = new List<byte[]>();
        public MainPage()
        {
            InitializeComponent();
            var _Timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            _Timer.Tick += (s, arg) =>
            {
                FrameworkDispatcher.Update();

            };
            _Timer.Start();
            FindMyIP();
            InitializeStream();
          
        }

        void InitializeStream()
        {
            stream = new RTPAudioStream(0,null);
            stream.Bind(localEp);
            stream.AudioCodec = new G722CodecWrapper();
            stream.UseInternalTimersForPacketPushPull = false;
        }

        void Log(string message)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() => { textBlock1.Text += message + '\n'; });
        }
        void FindMyIP()
        {
            MyIPAddress my = new MyIPAddress();
            myip = my.Find();
            ApplicationTitle.Text += myip.ToString();
            localEp = new IPEndPoint(myip, 4001);
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            IsMicActive = true;
            IPEndPoint remote = new IPEndPoint(IPAddress.Parse("172.16.41.174"), Convert.ToInt32(remoteip.Text));
            stream.Start(remote, 50, 50);
            byte[] initdata = new byte[100];
            stream.SendNextSample(initdata);
            Log("Stream Initialised");
        }

        private void textBox1_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
        byte[] Combine(List<byte[]> arrays)
        {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }

        private void button5_Click(object sender, RoutedEventArgs e)
        {
            IsMicActive = false;
            Log("Mic Stopped");
            byte[] data = Combine(Audio);
            SoundEffect test = new SoundEffect(data, Microphone.Default.SampleRate, Microsoft.Xna.Framework.Audio.AudioChannels.Mono);
            SoundEffectInstance sm = test.CreateInstance();
            sm.Play();
            int ct = 20;
            for (int i = 0; i < ct; i++)
                Log(data[i]+ "");
            Log("Playing");
            
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {

            MicrophoneThread = new Thread(new ThreadStart(MicrophoneThreadFunction));
            MicrophoneThread.Name = "Microphone Thread";
            MicrophoneThread.Start();
        }


        public void MicrophoneThreadFunction()
        {

            StartMic();
            Log("Mic started");
            int nSamplesPerPacket = stream.AudioCodec.AudioFormat.CalculateNumberOfSamplesForDuration(TimeSpan.FromMilliseconds(stream.PTimeTransmit));
            int nBytesPerPacket = nSamplesPerPacket * stream.AudioCodec.AudioFormat.BytesPerSample;
            TimeSpan tsPTime = TimeSpan.FromMilliseconds(stream.PTimeTransmit);
            DateTime dtNextPacketExpected = DateTime.Now + tsPTime;
            int nUnavailableAudioPackets = 0;
            while (IsMicActive == true)
            {
                dtNextPacketExpected = DateTime.Now + tsPTime;
                if (MicrophoneQueue.Size >= nBytesPerPacket)
                {
                    byte[] buffer = MicrophoneQueue.GetNSamples(nBytesPerPacket);
                    stream.SendNextSample(buffer);
                    Audio.Add(buffer);
                }
                else
                {
                    nUnavailableAudioPackets++;
                }

                if (MicrophoneQueue.Size > nBytesPerPacket * 6)
                    MicrophoneQueue.GetNSamples(MicrophoneQueue.Size - nBytesPerPacket * 5);

                TimeSpan tsRemaining = dtNextPacketExpected - DateTime.Now;
                int nMsRemaining = (int)tsRemaining.TotalMilliseconds;
                if (nMsRemaining > 0)
                {

                    System.Threading.Thread.Sleep(nMsRemaining);
                }
            }
            Log("Stopping Mic");
            StopMic();
        }

        byte[] buffer = new byte[16 * 40];
        void StartMic()
        {
            Microphone mic = Microphone.Default;
            buffer = new byte[mic.GetSampleSizeInBytes(TimeSpan.FromMilliseconds(100)) * 4];
            mic.BufferDuration = TimeSpan.FromMilliseconds(100);
            mic.BufferReady += new EventHandler<EventArgs>(mic_BufferReady);
            mic.Start();
        }

        void StopMic()
        {
            Microphone mic = Microphone.Default;
            mic.BufferReady -= new EventHandler<EventArgs>(mic_BufferReady);
            mic.Stop();
        }

        void mic_BufferReady(object sender, EventArgs e)
        {
            Microphone mic = Microphone.Default;
            int nSize = mic.GetData(buffer);
            MicrophoneQueue.AppendData(buffer, 0, nSize);
        }

        
    }
}