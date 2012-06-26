/// Copyright (c) 2011 Brian Bonnett
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

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

using System.Net.XMPP;
using Microsoft.Phone.Shell;

using Microsoft.Phone.Tasks;
using System.Device.Location;



namespace XMPPClient
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            App.XMPPClient.OnRetrievedRoster += new EventHandler(RetrievedRoster);
            App.XMPPClient.OnStateChanged += new EventHandler(StateChanged);
            App.XMPPClient.OnUserSubscriptionRequest += new System.Net.XMPP.XMPPClient.DelegateShouldSubscribeUser(XMPPClient_OnUserSubscriptionRequest);
        }


        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = App.XMPPClient;
            this.ListBoxRoster.DataContext = App.XMPPClient;
            var selected = from c in App.XMPPClient.RosterItems group c by c.Group into n select new GroupingLayer<string, RosterItem>(n);
            this.ListBoxRoster.ItemsSource = selected;
        }


        private void PhoneApplicationPage_Unloaded(object sender, RoutedEventArgs e)
        {
            App.XMPPClient.OnStateChanged -= new EventHandler(StateChanged);
            App.XMPPClient.OnUserSubscriptionRequest -= new System.Net.XMPP.XMPPClient.DelegateShouldSubscribeUser(XMPPClient_OnUserSubscriptionRequest);
        }


     
        void XMPPClient_OnUserSubscriptionRequest(PresenceMessage pres)
        {
            Dispatcher.BeginInvoke(new System.Net.XMPP.XMPPClient.DelegateShouldSubscribeUser(DoOnUserSubscriptionRequest), pres);
        }

        void DoOnUserSubscriptionRequest(PresenceMessage pres)
        {
            MessageBoxResult result = MessageBox.Show(string.Format("User '{0}; wants to see your presence.  Allow?", pres.From), "Allow User To See You?", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                App.XMPPClient.AcceptUserPresence(pres, "", "");
            }
            else
            {
                App.XMPPClient.DeclineUserPresence(pres);
            }
            
        }

   

        public void RetrievedRoster(object obj, EventArgs arg)
        {
            this.Dispatcher.BeginInvoke(SafeSetRoster);   
        }

        void SafeSetRoster()
        {
            this.ListBoxRoster.ItemsSource = null;
            this.ListBoxRoster.DataContext = App.XMPPClient;
            ////this.ListBoxRoster.ItemsSource = App.XMPPClient.RosterItems;

            var selected = from c in App.XMPPClient.RosterItems group c by c.Group into n select new GroupingLayer<string, RosterItem>(n);
            this.ListBoxRoster.ItemsSource = selected;


        }

        public void StateChanged(object obj, EventArgs arg)
        {
            Dispatcher.BeginInvoke(HandleStateChanged);
        }

        void HandleStateChanged()
        {
            if (App.XMPPClient.XMPPState == XMPPState.Ready)
            {
                _performanceProgressBar.IsIndeterminate = false;

                ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IconUri = new Uri("/Images/disconnect.png", UriKind.Relative);
                RectanglePresence.Visibility = System.Windows.Visibility.Visible;
                TextBoxStatus.Visibility = System.Windows.Visibility.Visible;
            }
            else if (App.XMPPClient.XMPPState == XMPPState.Unknown)
            {
                _performanceProgressBar.IsIndeterminate = false;

                ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IconUri = new Uri("/Images/connect.png", UriKind.Relative);
                RectanglePresence.Visibility = System.Windows.Visibility.Collapsed;
                TextBoxStatus.Visibility = System.Windows.Visibility.Collapsed;
            }
            else if (App.XMPPClient.XMPPState == XMPPState.AuthenticationFailed)
            {
                _performanceProgressBar.IsIndeterminate = false;
                MessageBox.Show("Please check your credentials and try again", "Authentication Failed", MessageBoxButton.OK);
                //this.HyperlinkConnect.Content = "Connect";
            }
            else
            {
            }
        }

        private void HyperlinkRosterItem_Click(object sender, RoutedEventArgs e)
        {
            RosterItem item = ((FrameworkElement)sender).DataContext as RosterItem;
            if (item == null)
                return;

            NavigationService.Navigate(new Uri(string.Format("/ChatPage.xaml?JID={0}", item.JID), UriKind.Relative)); 
        }

        private void ButtonExit_Click(object sender, EventArgs e)
        {
            QuitException.Quit();
        }


        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.Back)
            {
                if (MessageBox.Show("Are you sure you want to leave this application?", "Confirm Exit", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                }
                else
                {
                    e.Cancel = true;
                    return;
                }
            }
            base.OnNavigatingFrom(e);
        }


        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.Back)
            {
                QuitException.Quit();

            }

            base.OnNavigatedFrom(e);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (App.XMPPClient.XMPPState == XMPPState.Ready)
            {
                _performanceProgressBar.IsIndeterminate = false;
                ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IconUri = new Uri("/Images/disconnect.png", UriKind.Relative);
                RectanglePresence.Visibility = System.Windows.Visibility.Visible;
                TextBoxStatus.Visibility = System.Windows.Visibility.Visible;
            }
            else if (App.XMPPClient.XMPPState == XMPPState.Unknown)
            {
                _performanceProgressBar.IsIndeterminate = false;
                ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IconUri = new Uri("/Images/connect.png", UriKind.Relative);
                RectanglePresence.Visibility = System.Windows.Visibility.Collapsed;
                TextBoxStatus.Visibility = System.Windows.Visibility.Collapsed;
            }

            base.OnNavigatedTo(e);
        }

        private void ButtonAddAccount_Click(object sender, EventArgs e)
        {

        }

   

        private void ButtonViewMessages_Click(object sender, RoutedEventArgs e)
        {
            RosterItem item = ((FrameworkElement)sender).DataContext as RosterItem;
            if (item == null)
                return;

            NavigationService.Navigate(new Uri(string.Format("/ChatPage.xaml?JID={0}", item.JID), UriKind.Relative)); 

        }

        private void ButtonStartAudioCall_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonSwitch_Click(object sender, RoutedEventArgs e)
        {
            _performanceProgressBar.IsIndeterminate = true;
            //Microsoft.Phone.Shell.SystemTray.ProgressIndicator = _performanceProgressBar;
        }


        private void MenuItemOnline_Click(object sender, RoutedEventArgs e)
        {
            if (App.XMPPClient.XMPPState == XMPPState.Ready)
            {
                App.XMPPClient.PresenceStatus.PresenceShow = PresenceShow.chat;
                App.XMPPClient.PresenceStatus.Status = "Online";
                App.XMPPClient.PresenceStatus.PresenceType = PresenceType.available;
                App.XMPPClient.UpdatePresence();
            }
        }

        private void MenuItemBusy_Click(object sender, RoutedEventArgs e)
        {
            if (App.XMPPClient.XMPPState == XMPPState.Ready)
            {
                App.XMPPClient.PresenceStatus.PresenceShow = PresenceShow.away;
                App.XMPPClient.PresenceStatus.Status = "Busy";
                App.XMPPClient.PresenceStatus.PresenceType = PresenceType.available;
                App.XMPPClient.UpdatePresence();
            }
        }

        private void MenuItemDND_Click(object sender, RoutedEventArgs e)
        {
            if (App.XMPPClient.XMPPState == XMPPState.Ready)
            {
                App.XMPPClient.PresenceStatus.PresenceShow = PresenceShow.dnd;
                App.XMPPClient.PresenceStatus.Status = "DO NOT DISTURB!!";
                App.XMPPClient.PresenceStatus.PresenceType = PresenceType.available;
                App.XMPPClient.UpdatePresence();
            }
        }

        private void MenuItemAway_Click(object sender, RoutedEventArgs e)
        {
            if (App.XMPPClient.XMPPState == XMPPState.Ready)
            {
                App.XMPPClient.PresenceStatus.PresenceShow = PresenceShow.xa;
                App.XMPPClient.PresenceStatus.Status = "away";
                App.XMPPClient.PresenceStatus.PresenceType = PresenceType.available;
                App.XMPPClient.UpdatePresence();
            }
        }

        private void ButtonConnect_Click(object sender, EventArgs e)
        {
            if (App.XMPPClient.XMPPState == XMPPState.Unknown)
            {
                App.XMPPLogBuilder.Clear();

                NavigationService.Navigate(new Uri("/ConnectPage.xaml", UriKind.Relative));
                _performanceProgressBar.IsIndeterminate = true;
            }
            else if (App.XMPPClient.XMPPState > XMPPState.Connected)
            {
                App.XMPPClient.Disconnect();
            }
        }

        private void TextBoxStatus_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBoxStatus.Foreground = Application.Current.Resources["PhoneBackgroundBrush"] as Brush;
        }

        private void TextBoxStatus_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBoxStatus.Foreground = Application.Current.Resources["PhoneContrastBackgroundBrush"] as Brush; 

            if (App.XMPPClient.XMPPState == XMPPState.Ready)
            {
                App.XMPPClient.PresenceStatus.Status = TextBoxStatus.Text; /// Have to do this explicitly because silverlight won't update until after this is called, not as many options as WPF
                App.XMPPClient.UpdatePresence();
            }
        }

        private void ButtonOptions_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri(string.Format("/OptionsPage.xaml"), UriKind.Relative)); 
        }

        private void ButtonFileTransfers_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri(string.Format("/FileTransferPage.xaml"), UriKind.Relative)); 
        }


        private void ButtonMapFriend_Click(object sender, RoutedEventArgs e)
        {
            RosterItem item = ((FrameworkElement)sender).DataContext as RosterItem;
            if (item == null)
                return;
        
            //BingMapsDirectionsTask bingMapsDirectionsTask = new BingMapsDirectionsTask();

            BingMapsTask bingMap = new BingMapsTask();
            bingMap.Center = new GeoCoordinate(item.GeoLoc.lat, item.GeoLoc.lon);

            // GeoCoordinate spaceNeedleLocation = new GeoCoordinate(47.6204,-122.3493);
            // LabeledMapLocation spaceNeedleLML = new LabeledMapLocation("Space Needle", spaceNeedleLocation);

            // If you set the geocoordinate parameter to null, the label parameter is used as a search term.
            //LabeledMapLocation spaceNeedleLML = new LabeledMapLocation("Space Needle", null);

            //bingMapsDirectionsTask.End = spaceNeedleLML;

            // If bingMapsDirectionsTask.Start is not set, the user's current location is used as the start point.

            bingMap.Show();
        }

    }


    public class GroupingLayer<TKey, TElement> : IGrouping<TKey, TElement>
    {

        private readonly IGrouping<TKey, TElement> grouping;

        public GroupingLayer(IGrouping<TKey, TElement> unit)
        {
            grouping = unit;
        }

        public TKey Key
        {
            get { return grouping.Key; }
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            return grouping.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return grouping.GetEnumerator();
        }
    }

    class QuitException : Exception
    {
        public QuitException()
            : base()
        {
        }

        public static void Quit()
        {
            throw new QuitException();
        }
    }
}