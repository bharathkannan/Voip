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
using System.Threading;
using Microsoft.Xna.Framework;
using System.Windows.Threading;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using AudioClasses;
using XMPPClient;

namespace AudioTest
{

    public partial class MainPage : PhoneApplicationPage
    {
        SoundEffectInstance sm;
        byte[] buffer = null;
        byte[] data = null;
        AudioClasses.ByteBuffer MicrophoneQueue = new ByteBuffer();
        Thread MicrophoneThread,SpeakerThread;
        MemoryStream ms = new MemoryStream();
        AudioStreamSource source=new AudioStreamSource();
        // Constructor
        void SafeStartMediaElement(object obj, EventArgs args)
        {
            if (AudioStream.CurrentState != MediaElementState.Playing)
            {
                AudioStream.BufferingTime = new TimeSpan(0, 0, 0);
                AudioStream.SetSource(source);
            }
        }
        void SafeStopMediaElement(object obj, EventArgs args)
        {
            AudioStream.Stop();
        }
        public MainPage()
        {
            InitializeComponent();
            var _Timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            _Timer.Tick += (s, arg) =>
            {
                FrameworkDispatcher.Update();
               
            };
            _Timer.Start();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            MicrophoneThread = new Thread(new ThreadStart(MicrophoneThreadFunction));
            MicrophoneThread.IsBackground = true;
            MicrophoneThread.Name = "Microphone Read Thread";
            MicrophoneThread.Start();
        }
        void StartMic()
        {
            /// wnidows phone can only get mic at 100 ms intervals, not to good for speech
            Microphone mic = Microphone.Default;
            buffer = new byte[mic.GetSampleSizeInBytes(TimeSpan.FromMilliseconds(100))];
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
        public void MicrophoneThreadFunction()
        {
            StartMic();
            while (MicrophoneQueue.Size < 1000000) ;
            data = MicrophoneQueue.GetNSamples(1000000);
            StopMic();
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            SpeakerThread = new Thread(new ThreadStart(SpeakerThreadFunction));
            SpeakerThread.IsBackground = true;
            SpeakerThread.Name = "Speaker Write Thread";
            SpeakerThread.Start();
        }
        public void SpeakerThreadFunction()
        {
            //Deployment.Current.Dispatcher.BeginInvoke(new EventHandler(SafeStartMediaElement), null, null);
            //source.Write(data);

            // Working code


            SoundEffect test = new SoundEffect(data, Microphone.Default.SampleRate, Microsoft.Xna.Framework.Audio.AudioChannels.Mono);
            sm = test.CreateInstance();
            sm.Play();
            test = new SoundEffect(data, Microphone.Default.SampleRate, Microsoft.Xna.Framework.Audio.AudioChannels.Mono);
            sm = test.CreateInstance();
            sm.Play();




        //    Deployment.Current.Dispatcher.BeginInvoke(() => { AudioStream.Play(); });



            //Deployment.Current.Dispatcher.BeginInvoke(() => {
            //textBox1.Text+= "hi";
           // });
          //  Deployment.Current.Dispatcher.BeginInvoke(new EventHandler(SafeStopMediaElement), null, null);


            //new SoundEffect(m_Sound.ToArray(), m_Microphone.SampleRate, AudioChannels.Mono);
        }      


    }
}