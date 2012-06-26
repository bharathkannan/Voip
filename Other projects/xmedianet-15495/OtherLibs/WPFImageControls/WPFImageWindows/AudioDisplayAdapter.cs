using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AudioClasses;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WPFImageWindows
{
    /// <summary>
    /// Receives incoming data from mutliple sources and displays them on an AudioViewerControl
    /// </summary>
    public class AudioDisplayAdapter : IAudioSink
    {
        public AudioDisplayAdapter(AudioViewerControl displaycontrol)
        {
            AudioViewerControl = displaycontrol;
        }

        private AudioViewerControl m_ojbAudioViewerControl = null;

        public AudioViewerControl AudioViewerControl
        {
            get { return m_ojbAudioViewerControl; }
            set { m_ojbAudioViewerControl = value; }
        }

        List<AudioSource> Sources = new List<AudioSource>();

      
        /// <summary>
        /// Add a source to display. 
        /// </summary>
        /// <param name="objSource"></param>
        /// <param name="format"></param>
        /// <param name="tsDuration"></param>
        public AudioSource AddSourceFromObject(object objSource)
        {
            AudioSource newsource = new AudioSource(objSource);
            Sources.Add(newsource);
            return newsource;
        }

        public void AddSource(AudioSource newsource)
        {
            Sources.Add(newsource);
        }

        public TimeSpan DrawDuration = new TimeSpan(0, 0, 0, 0, 20);

        public void Close()
        {

            Sources.Clear();
        }

        #region IAudioSink Members

        public void PushSample(MediaSample sample, object objSource)
        {
            foreach (AudioSource audiosource in Sources)
            {
                if (audiosource.Source == objSource)
                {
                    audiosource.NewData(sample);
                    break;
                }
            }
        }

        public bool IsSinkActive
        {
            get
            {
                return true;
            }
            set
            {
                
            }
        }

        public double SinkAmplitudeMultiplier
        {
            get
            {
                return 1.0f;
            }
            set
            {
                
            }
        }

        #endregion
    }

    public class AudioSource
    {
        public AudioSource(object objSource)
        {
            Source = objSource;
            Random rand = new Random();
            this.ForeColor = ColorArray[rand.Next(ColorArray.Length)];
        }

        public AudioSource(object objSource, AudioFormat formatsource, TimeSpan TimerDuration)
        {
            Source = objSource;
            AudioFormat = formatsource;
            DrawBuffer = new float[AudioFormat.CalculateNumberOfSamplesForDuration(TimerDuration)];
            DrawBufferCopy = new float[AudioFormat.CalculateNumberOfSamplesForDuration(TimerDuration)];
            m_bIsInitialized = true;
        }

        static Color[] ColorArray = new Color[] { System.Windows.Media.Colors.Coral,
                                                System.Windows.Media.Colors.Beige,
                                                System.Windows.Media.Colors.Plum,
                                                System.Windows.Media.Colors.PowderBlue,
                                                System.Windows.Media.Colors.PeachPuff,
                                                System.Windows.Media.Colors.PaleGreen,
                                                System.Windows.Media.Colors.PaleGoldenrod
                                             };


        bool m_bIsInitialized = false;

        private object m_objSource = null;

        public object Source
        {
            get { return m_objSource; }
            set { m_objSource = value; }
        }

        float [] DrawBuffer = null;
        float[] DrawBufferCopy = null;

        private AudioFormat m_objAudioFormat = null;

        internal AudioFormat AudioFormat
        {
            get { return m_objAudioFormat; }
            set { m_objAudioFormat = value; }
        }

        private System.Windows.Media.Color m_cForeColor = Colors.Red;

        internal System.Windows.Media.Color ForeColor
        {
            get { return m_cForeColor; }
            set { m_cForeColor = value; }
        }

        object LockSample = new object();

        /// <summary>
        /// Populate our DrawBuffer with the next x-duration samples from our queue
        /// </summary>
        /// <param name="tsDuration"></param>
        /// <returns></returns>
        float[] PopData()
        {
           lock (LockSample)
           {
              Array.Copy(DrawBuffer, DrawBufferCopy, DrawBufferCopy.Length);
              return DrawBufferCopy;
           }

        }

        // New data was received, update our display buffer
        internal void NewData(MediaSample sample)
        {
            if (m_bIsInitialized == false)
            {
                AudioFormat = sample.AudioFormat;
                DrawBuffer = new float[sample.NumberSamples];
                DrawBufferCopy = new float[sample.NumberSamples];
                m_bIsInitialized = true;
            }


           float[] fData = ImageAquisition.Utils.NormalizeData(sample.Data, (int) sample.AudioFormat.AudioBitsPerSample);
            lock (LockSample)
            {
               
               int nLength = Math.Min(fData.Length, DrawBuffer.Length);
               Array.Copy(fData, DrawBuffer, nLength);
            }
        }

        // Draw this source
        internal void Draw(WriteableBitmap audiobmp, int nWidth, int nHeight)
        {
            if (m_bIsInitialized == false)
                return;

            int lastx = 0;
            int lasty = ((int)audiobmp.PixelHeight / 2);
            float HalfHeight = (float) lasty;

            float [] fData = PopData();

            float fPixelToSampleRatio = ((float)audiobmp.PixelWidth) / ((float)fData.Length);

            for (int x = 0; x < audiobmp.PixelWidth; x++)
            {
                int nAudioIdx = (int) (x / fPixelToSampleRatio);
                if (nAudioIdx < fData.Length)
                {

                    int y = (int)(HalfHeight + (fData[nAudioIdx] * HalfHeight));
                    audiobmp.DrawLineBlend(lastx, lasty, x, y, this.ForeColor);

                    lastx = x;
                    lasty = y;
                }
            }
        }

    }
}

namespace System.Windows.Media.Imaging
{
   /// <summary>
   /// Collection of draw extension methods for the Silverlight WriteableBitmap class.
   /// </summary>
    public static partial class WriteableBitmapExtensions
    {
        /// <summary>
        /// Fills the whole WriteableBitmap with an empty color (0).
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        public static void Clear(this WriteableBitmap bmp)
        {
            Clear(bmp, Colors.Black);
        }
        public static unsafe int* get_Pixels(this WriteableBitmap bmp)
        {
            unsafe
            {
                return (int*)bmp.BackBuffer.ToPointer();
            }
        }

        public static int get_ByteLength(this WriteableBitmap bmp)
        {
            return bmp.BackBufferStride * bmp.PixelHeight;
        }
        public static int get_IntLength(this WriteableBitmap bmp)
        {
            return bmp.BackBufferStride * bmp.PixelHeight / 4;
        }

        [System.Runtime.InteropServices.DllImport("msvcrt.dll", EntryPoint = "memset", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl, SetLastError = false)]
        public static extern IntPtr memset(IntPtr dest, int c, int count);

        /// <summary>
        /// Fills the whole WriteableBitmap with a color.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="color">The color used for filling.</param>
        public static void Clear(this WriteableBitmap bmp, Color color)
        {

            memset(bmp.BackBuffer, 0, bmp.get_ByteLength());
            return;

            int col = (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
            unsafe
            {
                int* pixels = bmp.get_Pixels();
                int pixelCount = bmp.get_IntLength();


                for (int i = 0; i < pixelCount; i++)
                {
                    pixels[i] = col;
                }
            }
        }
        /// <summary>
        /// Draws a colored line by connecting two points using an optimized DDA.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="x1">The x-coordinate of the start point.</param>
        /// <param name="y1">The y-coordinate of the start point.</param>
        /// <param name="x2">The x-coordinate of the end point.</param>
        /// <param name="y2">The y-coordinate of the end point.</param>
        /// <param name="color">The color for the line.</param>
        public static void DrawLine(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, Color color)
        {
            bmp.DrawLine(x1, y1, x2, y2, (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B);
        }

        /// <summary>
        /// Draws a colored line by connecting two points using an optimized DDA.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="x1">The x-coordinate of the start point.</param>
        /// <param name="y1">The y-coordinate of the start point.</param>
        /// <param name="x2">The x-coordinate of the end point.</param>
        /// <param name="y2">The y-coordinate of the end point.</param>
        /// <param name="color">The color for the line.</param>
        public static unsafe void DrawLine(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, int color)
        {
            int pixelWidth = bmp.PixelWidth;
            int pixelHeight = bmp.PixelHeight;
            // Use refs for faster access (really important!) speeds up a lot!
            int w = bmp.PixelWidth;
            int* pixels = (int*)bmp.BackBuffer.ToPointer();
            DrawLine(pixels, pixelWidth, pixelHeight, x1, y1, x2, y2, color);
        }

        public unsafe static void DrawLine(int* pixels, int pixelWidth, int pixelHeight, int x1, int y1, int x2, int y2, int color)
        {
            // Check boundaries
            if (x1 < 0) { x1 = 0; }
            if (y1 < 0) { y1 = 0; }
            if (x2 < 0) { x2 = 0; }
            if (y2 < 0) { y2 = 0; }
            if (x1 >= pixelWidth) { x1 = pixelWidth - 1; }
            if (y1 >= pixelHeight) { y1 = pixelHeight - 1; }
            if (x2 >= pixelWidth) { x2 = pixelWidth - 1; }
            if (y2 >= pixelHeight) { y2 = pixelHeight - 1; }

            // Distance start and end point
            int dx = x2 - x1;
            int dy = y2 - y1;

            const int PRECISION_SHIFT = 8;
            const int PRECISION_VALUE = 1 << PRECISION_SHIFT;

            // Determine slope (absoulte value)
            int lenX, lenY;
            int incy1;
            if (dy >= 0)
            {
                incy1 = PRECISION_VALUE;
                lenY = dy;
            }
            else
            {
                incy1 = -PRECISION_VALUE;
                lenY = -dy;
            }

            int incx1;
            if (dx >= 0)
            {
                incx1 = 1;
                lenX = dx;
            }
            else
            {
                incx1 = -1;
                lenX = -dx;
            }

            if (lenX > lenY)
            { // x increases by +/- 1
                // Init steps and start
                int incy = (dy << PRECISION_SHIFT) / lenX;
                int y = y1 << PRECISION_SHIFT;

                // Walk the line!
                for (int i = 0; i < lenX; i++)
                {
                    pixels[(y >> PRECISION_SHIFT) * pixelWidth + x1] = color;
                    x1 += incx1;
                    y += incy;
                }
            }
            else
            { // since y increases by +/-1, we can safely add (*h) before the for() loop, since there is no fractional value for y
                // Prevent divison by zero
                if (lenY == 0)
                {
                    return;
                }

                // Init steps and start
                int incx = (dx << PRECISION_SHIFT) / lenY;
                int index = (x1 + y1 * pixelWidth) << PRECISION_SHIFT;

                // Walk the line!
                int inc = incy1 * pixelWidth + incx;
                for (int i = 0; i < lenY; i++)
                {
                    pixels[index >> PRECISION_SHIFT] = color;
                    index += inc;
                }
            }
        }

        /// <summary>
        /// Draws a colored line by connecting two points using an optimized DDA.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="x1">The x-coordinate of the start point.</param>
        /// <param name="y1">The y-coordinate of the start point.</param>
        /// <param name="x2">The x-coordinate of the end point.</param>
        /// <param name="y2">The y-coordinate of the end point.</param>
        /// <param name="color">The color for the line.</param>
        public static void DrawLineBlend(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, Color color)
        {
            bmp.DrawLineBlend(x1, y1, x2, y2, (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B);
        }

        /// <summary>
        /// Draws a colored line by connecting two points using an optimized DDA.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="x1">The x-coordinate of the start point.</param>
        /// <param name="y1">The y-coordinate of the start point.</param>
        /// <param name="x2">The x-coordinate of the end point.</param>
        /// <param name="y2">The y-coordinate of the end point.</param>
        /// <param name="color">The color for the line.</param>
        public static unsafe void DrawLineBlend(this WriteableBitmap bmp, int x1, int y1, int x2, int y2, int color)
        {
            int pixelWidth = bmp.PixelWidth;
            int pixelHeight = bmp.PixelHeight;
            // Use refs for faster access (really important!) speeds up a lot!
            int w = bmp.PixelWidth;
            int* pixels = (int*)bmp.BackBuffer.ToPointer();
            DrawLineBlend(pixels, pixelWidth, pixelHeight, x1, y1, x2, y2, color);
        }

        public unsafe static void DrawLineBlend(int* pixels, int pixelWidth, int pixelHeight, int x1, int y1, int x2, int y2, int color)
        {
            // Check boundaries
            if (x1 < 0) { x1 = 0; }
            if (y1 < 0) { y1 = 0; }
            if (x2 < 0) { x2 = 0; }
            if (y2 < 0) { y2 = 0; }
            if (x1 >= pixelWidth) { x1 = pixelWidth - 1; }
            if (y1 >= pixelHeight) { y1 = pixelHeight - 1; }
            if (x2 >= pixelWidth) { x2 = pixelWidth - 1; }
            if (y2 >= pixelHeight) { y2 = pixelHeight - 1; }

            // Distance start and end point
            int dx = x2 - x1;
            int dy = y2 - y1;

            const int PRECISION_SHIFT = 8;
            const int PRECISION_VALUE = 1 << PRECISION_SHIFT;

            // Determine slope (absoulte value)
            int lenX, lenY;
            int incy1;
            if (dy >= 0)
            {
                incy1 = PRECISION_VALUE;
                lenY = dy;
            }
            else
            {
                incy1 = -PRECISION_VALUE;
                lenY = -dy;
            }

            int incx1;
            if (dx >= 0)
            {
                incx1 = 1;
                lenX = dx;
            }
            else
            {
                incx1 = -1;
                lenX = -dx;
            }

            if (lenX > lenY)
            { // x increases by +/- 1
                // Init steps and start
                int incy = (dy << PRECISION_SHIFT) / lenX;
                int y = y1 << PRECISION_SHIFT;

                // Walk the line!
                for (int i = 0; i < lenX; i++)
                {
                    pixels[(y >> PRECISION_SHIFT) * pixelWidth + x1] |= color;
                    x1 += incx1;
                    y += incy;
                }
            }
            else
            { // since y increases by +/-1, we can safely add (*h) before the for() loop, since there is no fractional value for y
                // Prevent divison by zero
                if (lenY == 0)
                {
                    return;
                }

                // Init steps and start
                int incx = (dx << PRECISION_SHIFT) / lenY;
                int index = (x1 + y1 * pixelWidth) << PRECISION_SHIFT;

                // Walk the line!
                int inc = incy1 * pixelWidth + incx;
                for (int i = 0; i < lenY; i++)
                {
                    pixels[index >> PRECISION_SHIFT] |= color;
                    index += inc;
                }
            }
        }

    }

}
