using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using AudioClasses;
using ImageAquisition;
using System.Net.XMPP;

namespace WPFXMPPClient
{
    /// <summary>
    /// Interaction logic for AudioPlayerUserControl.xaml
    /// </summary>
    public partial class AudioPlayerUserControl : UserControl
    {
        public AudioPlayerUserControl()
        {
            InitializeComponent();
        }

        private AudioFileReader m_objAudioFileReader = new AudioFileReader(AudioFormat.SixteenBySixteenThousandMono);

        public AudioFileReader AudioFileReader
        {
            get { return m_objAudioFileReader; }
            set { m_objAudioFileReader = value; }
        }

        public XMPPClient XMPPClient = null;

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            AudioFileReader.OnPlayFinished += new DelegateSong(AudioFileReader_OnPlayFinished);
            AudioFileReader.OnPlayStarted += new DelegateSong(AudioFileReader_OnPlayStarted);

            this.DataContext = AudioFileReader;
        }

        void AudioFileReader_OnPlayStarted(string strSong, object objSender)
        {
            if (this.XMPPClient.XMPPState == XMPPState.Ready)
            {
                string strName = System.IO.Path.GetFileName(strSong);
                this.XMPPClient.SetTune(strName, "");
            }
        }

        void AudioFileReader_OnPlayFinished(string strSong, object objSender)
        {
            if (this.XMPPClient.XMPPState == XMPPState.Ready)
            {
                this.XMPPClient.SetTune("", "");
            }
        }

        private void ButtonNext_Click(object sender, RoutedEventArgs e)
        {
            AudioFileReader.AbortCurrentSong();
        }

        private void ButtonPause_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonPlay_Click(object sender, RoutedEventArgs e)
        {
            this.AudioFileReader.IsSourceActive = !this.AudioFileReader.IsSourceActive;
        }

        private void ButtonQueueSong_Click(object sender, RoutedEventArgs e)
        {
            /// Let the user choose a song, then enqueue it
            /// 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.InitialDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyMusic);
            dlg.Filter = "Audio Files (*.mp3;*.wma)|*.mp3;*.wma|All Files (*.*)|*.*";
            dlg.Multiselect = true;
            if (dlg.ShowDialog() == true)
            {
                foreach (string strFileName in dlg.FileNames)
                    AudioFileReader.EnqueueFile(strFileName);
            }
        }

        private void ButtonRandom100_Click(object sender, RoutedEventArgs e)
        {
            string[] MusicFiles = new string[] { };
            Random MusicRand = new Random();

            string strDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyMusic);
            MusicFiles = System.IO.Directory.GetFiles(strDir, "*.mp3", System.IO.SearchOption.AllDirectories);

            int nNumbSongs = (MusicFiles.Length > 100) ? 100 : MusicFiles.Length;
            // Queue up 100 random songs
            for (int i = 0; i < 100; i++)
            {
                int nIndex = MusicRand.Next(MusicFiles.Length);
                AudioFileReader.EnqueueFile(MusicFiles[nIndex]);
            }
        }

        private void ButtonDeleteQueue_Click(object sender, RoutedEventArgs e)
        {
            AudioFileReader.ClearPlayQueue();
            AudioFileReader.AbortCurrentSong();
        }

      
    }
}
