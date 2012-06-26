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

using System.Net.XMPP;

namespace WPFXMPPClient
{
    /// <summary>
    /// Interaction logic for IncomingCallWindow.xaml
    /// </summary>
    public partial class IncomingCallWindow : Window
    {
        public IncomingCallWindow()
        {
            InitializeComponent();
        }

        public RosterItem IncomingCallFrom = null;
        public bool Accepted = false;
        public delegate void DelegateCallAccept(bool bAccept);
        public event DelegateCallAccept OnAcceptOrDeclineCall = null;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = IncomingCallFrom;
            this.LabelIncomingCall.Content = string.Format("Incoming Call From {0}", IncomingCallFrom.Name);

        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((e.ChangedButton == MouseButton.Left) && (e.ButtonState == MouseButtonState.Pressed))
                this.DragMove();

        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        private void ButtonAcceptCall_Click(object sender, RoutedEventArgs e)
        {
            Accepted = true;
            if (OnAcceptOrDeclineCall != null)
                OnAcceptOrDeclineCall(true);
            this.Close();
        }

        private void ButtonRejectCall_Click(object sender, RoutedEventArgs e)
        {
            Accepted = false;
            if (OnAcceptOrDeclineCall != null)
                OnAcceptOrDeclineCall(false);
            this.Close();
        }
    }
}
