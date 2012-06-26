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
using AudioClasses;
using SocketServer;
using Microsoft.Xna;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System.Threading;
using System.Windows.Threading;
namespace VoipClient1
{
    public partial class MainPage : PhoneApplicationPage
    {
        byte[] finalval;
        AudioClasses.ByteBuffer MicrophoneQueue = new AudioClasses.ByteBuffer();
        Thread SpeakerThread, MicrophoneThread;
        bool CallActive = false;
        RTPAudioStream stream;
        IPEndPoint RemoteEp = new IPEndPoint(IPAddress.Parse("172.16.41.174"),4001);
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
            StartStream();
            StartMedia();
            //Thread.CurrentThread.Join();
            ReceiveMedia();
        }
        public void ReceiveMedia()
        {
            Thread t = new Thread(new ThreadStart(runrecv));
            t.Name = "receiving";
            t.Start();    
        }
         private byte[] Combine(List<  byte[] > arrays)
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

        public void runrecv()
        {
            int ret; 
            List<byte[]> audio = new List<byte[]>();
            for(int i=0;i<100;i++)
            {
                byte[] temp1=stream.WaitNextPacketSample(true,1000,out ret);
                if (temp1 != null && temp1.Length > 0)
                {
                    audio.Add(temp1);
                }
            }
            finalval = Combine(audio);          
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                textBlock1.Text += "done";
                textBlock1.Text += finalval.Length;
            }
            );
            
            
        }


        public void play()
        {
            SoundEffect s = new SoundEffect(finalval, Microphone.Default.SampleRate, Microsoft.Xna.Framework.Audio.AudioChannels.Stereo);
            SoundEffectInstance sm = s.CreateInstance();
            sm.Play();
            int ct=0;
            foreach (byte b in finalval)
            {
                if(b > 0 && ct < 100)
                {
                    textBlock1.Text += b;
                    textBlock1.Text += ' ';
                }
            }


            textBlock1.Text += "played";

        }
     
        public void StartStream()
        {
            stream = new RTPAudioStream(0, null);
            stream.UseInternalTimersForPacketPushPull = false;
            stream.AudioCodec = new G722CodecWrapper();
            IPEndPoint LocalEndpoint = new IPEndPoint(IPAddress.Parse("172.16.41.174"), 3001);
            stream.Bind(LocalEndpoint);
            stream.Start(RemoteEp, 100, 100);
        }
        public void StartMedia()
        {
            CallActive = true;

            // Don't really need to use the mixer in android since we won't be conferencing people... go directly to the RTP buffer
            //PushPullObject thismember = AudioMixer.AddInputOutputSource(this, this);
            

            /// Start our speaker play thread
         /*   SpeakerThread = new Thread(new ThreadStart(SpeakerThreadFunction));
            SpeakerThread.IsBackground = true;
            SpeakerThread.Name = "Speaker Write Thread";
            SpeakerThread.Start();
            */

            /// Start our microphone read thread
            MicrophoneThread = new Thread(new ThreadStart(MicrophoneThreadFunction));
            MicrophoneThread.IsBackground = true;
            MicrophoneThread.Name = "Microphone Read Thread";
            MicrophoneThread.Start();
        }
        public void MicrophoneThreadFunction()
        {
            StartMic();


            int nSamplesPerPacket = stream.AudioCodec.AudioFormat.CalculateNumberOfSamplesForDuration(TimeSpan.FromMilliseconds(stream.PTimeTransmit));
            int nBytesPerPacket = nSamplesPerPacket * stream.AudioCodec.AudioFormat.BytesPerSample;



            // Min size in ICS was 1280 or 40 ms


            TimeSpan tsPTime = TimeSpan.FromMilliseconds(stream.PTimeTransmit);
            DateTime dtNextPacketExpected = DateTime.Now + tsPTime;

            int nUnavailableAudioPackets = 0;
            for (int i = 1; i * nBytesPerPacket <= 1000000; i++)
            {
                dtNextPacketExpected = DateTime.Now + tsPTime;
                if (MicrophoneQueue.Size >= nBytesPerPacket)
                {
                    byte[] buffer = MicrophoneQueue.GetNSamples(nBytesPerPacket);
                    stream.SendNextSample(buffer);
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {  });

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
                  //  Deployment.Current.Dispatcher.BeginInvoke(() => { textBlock1.Text += "Sleeping\n"; });
                    System.Threading.Thread.Sleep(nMsRemaining);
                }
            }
            StopMic();
    }


     /*          
            
            Deployment.Current.Dispatcher.BeginInvoke( ()=> {textBlock1.Text += "entered the thread"; });
                    while (MicrophoneQueue.Size < 100000);
            //    Deployment.Current.Dispatcher.BeginInvoke( ()=> {textBlock1.Text += "left the thread"; });

                    for (int i = 1; i < 100000*nBytesPerPacket; i += nBytesPerPacket)
                    {
                        byte[] buffer = MicrophoneQueue.GetNSamples(nBytesPerPacket);

                        Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                textBlock1.Text += "\nSize : " + nBytesPerPacket;
                textBlock1.Text += "\n";
                SoundEffect test = new SoundEffect(buffer, Microphone.Default.SampleRate, Microsoft.Xna.Framework.Audio.AudioChannels.Mono);
                SoundEffectInstance sm = test.CreateInstance();
                sm.Play();
            }

            );
                    }
                    //stream.SendNextSample(buffer);
                
              

            StopMic();
        }
    */
        byte[] buffer = new byte[16 * 40];
        void StartMic()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() => { textBlock1.Text += "in start mic"; });
            /// wnidows phone can only get mic at 100 ms intervals, not to good for speech
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

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            play();
        }

        private void textBox1_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

    }
}