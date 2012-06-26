/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

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
using System.Windows.Shapes;

namespace WPFImageWindows
{
    /// <summary>
    /// Interaction logic for OverlayWindow.xaml
    /// </summary>
    public partial class OverlayWindow : Window
    {
        public OverlayWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            rectangle.Width = CaptureRectangle.Width;
            rectangle.Height = CaptureRectangle.Height;
            Canvas.SetLeft(rectangle, CaptureRectangle.X);
            Canvas.SetTop(rectangle, CaptureRectangle.Y);
        }

        public Rect CaptureRectangle = new Rect(0, 0, 100, 100);
        Point StartingPoint;
        private bool m_bDrawingCaptureArea = true;

        protected bool DrawingCaptureArea
        {
            get { return m_bDrawingCaptureArea; }
        }

        void SaveScreenAndClose()
        {
            /// Hide our rectangles/etc before closing
            /// 

            CaptureRectangle.X = StartingPoint.X;
            CaptureRectangle.Y = StartingPoint.Y;
            CaptureRectangle.Width = rectangle.Width;
            CaptureRectangle.Height = rectangle.Height;

            this.Close();
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.DialogResult = true;
                SaveScreenAndClose();
                return;
            }
            else if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
                this.Close();
                return;
            }
            base.OnPreviewKeyDown(e);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (DrawingCaptureArea == true)
            {
                StartingPoint = e.GetPosition(this as IInputElement);
                this.CaptureMouse();

                rectangle.Width = 0;
                rectangle.Height = 0;
                Canvas.SetLeft(rectangle, StartingPoint.X);
                Canvas.SetTop(rectangle, StartingPoint.Y);
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if ((DrawingCaptureArea == true) && (e.MouseDevice.Captured == this))
            {
                Point CurrentPoint = e.GetPosition(this as IInputElement);

                double fX = Math.Min(CurrentPoint.X, StartingPoint.X);
                double fY = Math.Min(CurrentPoint.Y, StartingPoint.Y);
                Canvas.SetLeft(rectangle, fX);
                Canvas.SetTop(rectangle, fY);

                rectangle.Width = Math.Abs(CurrentPoint.X - StartingPoint.X);
                rectangle.Height = Math.Abs(CurrentPoint.Y - StartingPoint.Y);

            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if ((DrawingCaptureArea == true) && (e.MouseDevice.Captured == this))
            {
                Point CurrentPoint = e.GetPosition(this as IInputElement);
                double fX = Math.Min(CurrentPoint.X, StartingPoint.X);
                double fY = Math.Min(CurrentPoint.Y, StartingPoint.Y);
                Canvas.SetLeft(rectangle, fX);
                Canvas.SetTop(rectangle, fY);

                rectangle.Width = Math.Abs(CurrentPoint.X - StartingPoint.X);
                rectangle.Height = Math.Abs(CurrentPoint.Y - StartingPoint.Y);

                ReleaseMouseCapture();
            }
            base.OnMouseUp(e);
        }

        public void ShowArea()
        {

            //this.Left = rect.left;
            //this.Top = rect.top;
            //this.Width = rect.right - rect.left;
            //this.Height = rect.bottom - rect.top;
            rectangle.Stroke = Brushes.Red;
            System.Windows.Media.Animation.Storyboard board = Resources["FadeBorder"] as System.Windows.Media.Animation.Storyboard;
            BeginStoryboard(board);
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

        }
    }
}
