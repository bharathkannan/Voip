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

namespace WPFImageWindows
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class AudioViewerControl : UserControl
    {
        public AudioViewerControl()
        {
            InitializeComponent();
            audiobmp = new WriteableBitmap(320, 200, 96, 96, PixelFormats.Bgr32, null);
            this.audioimage.Source = audiobmp;

            CompositionTarget.Rendering += new EventHandler(CompositionTarget_Rendering);

            AudioDisplayFilter = new AudioDisplayAdapter(this);
            this.Unloaded += new RoutedEventHandler(AudioViewerControl_Unloaded);
            //this.DataContextChanged += new DependencyPropertyChangedEventHandler(AudioViewerControl_DataContextChanged);
        }


        void AudioViewerControl_Unloaded(object sender, RoutedEventArgs e)
        {
           
        }


        public AudioDisplayAdapter AudioDisplayFilter = null;

        public List<AudioSource> Sources = new List<AudioSource>();

        WriteableBitmap audiobmp = new WriteableBitmap(320, 200, 96, 96, PixelFormats.Bgr32, null);


        DateTime dtLastUpdate = DateTime.MinValue;

        void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            TimeSpan tsElapsed = DateTime.Now - dtLastUpdate;
            if (tsElapsed.TotalMilliseconds < 200)
                return;
            dtLastUpdate = DateTime.Now;

           audiobmp.Lock();

           
           // Fade is taking up most of our CPU...
           audiobmp.Clear();
           //audiobmp.Fade(Colors.Black);

           //if (nCount == 10)
           //nCount--;

           //int lastx = 0;
           int lasty = ((int)audiobmp.PixelHeight / 2);
           //int lasty2 = ((int)audiobmp.PixelHeight / 2);

           audiobmp.DrawLine(0, lasty, audiobmp.PixelWidth, lasty, Colors.DarkGoldenrod);

           foreach (AudioSource source in Sources)
           {
              source.Draw(audiobmp, audiobmp.PixelWidth, audiobmp.PixelHeight);
           }


           Int32Rect rect = new Int32Rect(0, 0, audiobmp.PixelWidth, audiobmp.PixelHeight);
           audiobmp.AddDirtyRect(rect);
           audiobmp.Unlock();
        }

        //public void Redraw()
        //{
        //    this.Dispatcher.Invoke(new DelegateDraw(Draw), System.Windows.Threading.DispatcherPriority.Render, null);
        //}

        //delegate void DelegateDraw();

        int nLast = 0;
        int nCount = 10;

        private void audioimage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
       
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double fWidth = this.ActualWidth;
            //double fHeight = this.ActualHeight*.666666f;
            double fHeight = this.ActualHeight;
            if (double.IsNaN(fWidth) == true)
                fWidth = 100;
            if (double.IsNaN(fHeight) == true)
                fHeight = 100;
            audiobmp = new WriteableBitmap((int)fWidth, (int)fHeight, 96, 96, PixelFormats.Bgr32, null);
            this.audioimage.Source = audiobmp;
//            this.audioimage.Height = fHeight;

        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }


    }
}
