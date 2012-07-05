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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

using System.Net.XMPP;
using System.Runtime.Serialization;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Net.NetworkInformation;

using System.Device.Location;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System.Windows.Resources;
using Microsoft.Phone.BackgroundAudio;

namespace XMPPClient
{
    public class Datastore
    {
        static Dictionary<String, Object> ObjectDictionary = new Dictionary<String, Object>();

         public static bool Add(Object obj, String key)
        {
            if (ObjectDictionary.ContainsKey(key))
                return false;
            ObjectDictionary.Add(key, obj);
            return true;
        }
        public static Object Find(String key)
        {
            if (ObjectDictionary.ContainsKey(key))
                return ObjectDictionary[key];
            return null;
        }
        public static void Remove(String key)
        {
            if (ObjectDictionary.ContainsKey(key))
            {
                ObjectDictionary.Remove(key);
            }
        }
    };
    
       

            


    public partial class App : Application, SocketServer.ILogInterface
    {
        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public PhoneApplicationFrame RootFrame { get; private set; }

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            // Global handler for uncaught exceptions. 
            UnhandledException += Application_UnhandledException;

            // Standard Silverlight initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            ApplicationLifetimeObjects.Add(new XNAAsyncDispatcher(TimeSpan.FromMilliseconds(40)));

            DeviceNetworkInformation.NetworkAvailabilityChanged += new EventHandler<NetworkNotificationEventArgs>(DeviceNetworkInformation_NetworkAvailabilityChanged);
            /// Load our options
            /// 
            LoadOptions();
            //if (Options.LogXML == true)
            //    SocketServer.SocketClient.ShowDebug = true;

            if (Options.RunWithScreenLocked == true)
               PhoneApplicationService.Current.ApplicationIdleDetectionMode = IdleDetectionMode.Disabled;

            CopyToIsolatedStorage();
             XMPPClient.OnXMLReceived += new System.Net.XMPP.XMPPClient.DelegateString(XMPPClient_OnXMLReceived);
             XMPPClient.OnXMLSent += new System.Net.XMPP.XMPPClient.DelegateString(XMPPClient_OnXMLSent);


            // Show graphics profiling information while debugging.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                BackgroundAudioPlayer.Instance.Close();
                // Display the current frame rate counters.
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode, 
                // which shows areas of a page that are handed off to GPU with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Disable the application idle detection by setting the UserIdleDetectionMode property of the
                // application's PhoneApplicationService object to Disabled.
                // Caution:- Use this under debug mode only. Application that disables user idle detection will continue to run
                // and consume battery power when the user is not using the phone.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }

            StartGeoStuff();

            GeoTimer = new System.Windows.Threading.DispatcherTimer();
            GeoTimer.Interval = TimeSpan.FromSeconds(Options.GeoTimeFrequency);
            GeoTimer.Tick += new EventHandler(GeoTimer_Tick);
            GeoTimer.Start();

            SocketServer.SocketClient.ShowDebug = true;

            App.XMPPClient.AutoReconnect = true;
            App.XMPPClient.OnNewConversationItem += new System.Net.XMPP.XMPPClient.DelegateNewConversationItem(XMPPClient_OnNewConversationItem);
            App.XMPPClient.FileTransferManager.OnNewIncomingFileTransferRequest += new FileTransferManager.DelegateIncomingFile(FileTransferManager_OnNewIncomingFileTransferRequest);
            App.XMPPClient.FileTransferManager.OnTransferFinished += new FileTransferManager.DelegateDownloadFinished(FileTransferManager_OnTransferFinished);
            App.XMPPClient.OnServerDisconnect += new EventHandler(XMPPClient_OnServerDisconnect);

        }

        void StartGeoStuff()
        {
            // The watcher variable was previously declared as type GeoCoordinateWatcher. 
            if ((gpswatcher == null) && (App.Options.SendGeoCoordinates == true))
            {
                gpswatcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High); // using high accuracy
                gpswatcher.MovementThreshold = 20; // use MovementThreshold to ignore noise in the signal

                //gpswatcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(gpswatcher_StatusChanged);
                gpswatcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(gpswatcher_PositionChanged);
                gpswatcher.Start();
            }
        }

        

        void XMPPClient_OnServerDisconnect(object sender, EventArgs e)
        {
            this.LogMessage("XMPP", SocketServer.MessageImportance.Medium, "Disconnected from server at {0}", DateTime.Now);
            //Deployment.Current.Dispatcher.BeginInvoke(new EventHandler(DoServerDisconnect), sender, e);
        }

        void DoServerDisconnect(object sender, EventArgs e)
        {
            //MessageBox.Show("You have been disconnected from the server");
        }

        void FileTransferManager_OnTransferFinished(FileTransfer trans)
        {
        }

        void FileTransferManager_OnNewIncomingFileTransferRequest(FileTransfer trans, RosterItem itemfrom)
        {
            Deployment.Current.Dispatcher.BeginInvoke(new FileTransferManager.DelegateIncomingFile(SafeOnNewIncomingFileTransferRequest), trans, itemfrom);
        }

        void SafeOnNewIncomingFileTransferRequest(FileTransfer trans, RosterItem itemfrom)
        {
            this.RootFrame.Navigate(new Uri("/FileTransferPage.xaml", UriKind.Relative));
        }


        object GeoLock = new object();

        void GeoTimer_Tick(object sender, EventArgs e)
        {
            if ((App.Options.SendGeoCoordinates == true) && (CurrentLocation.IsDirty == true))
            {
                if (App.XMPPClient.XMPPState == XMPPState.Ready)
                {
                    if (App.XMPPClient.Connected == false)
                    {
                        LogError("Geo", SocketServer.MessageImportance.Medium, "Can't send geo coordinates.  XPP state is ready but we're not connected");
                        //if (MessageBox.Show("Client was disconnected, would you like to re-connect?", "Connection Lost", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                        //App.XMPPClient.Connect(this);
                        return;
                    }

                    if (System.Threading.Monitor.TryEnter(GeoLock) == true)
                    {
                        try
                        {
                            App.XMPPClient.SetGeoLocation(CurrentLocation.lat, CurrentLocation.lon);
                            CurrentLocation.IsDirty = false;
                        }
                        catch (Exception ex)
                        {
                            /// It appears we don't always get notifed when we loose our connection, we just don't find out until we send
                            /// 
                            LogError("Geo", SocketServer.MessageImportance.Medium, "Exception sending GeoLocation: {0}", ex.Message);
                        }
                        System.Threading.Monitor.Exit(GeoLock);
                    }
                }
            }
        }


        void gpswatcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            // Update our xmpp client's position
            if ((e.Position.Location.Latitude != CurrentLocation.lat) || (e.Position.Location.Longitude != CurrentLocation.lon))
            {
                CurrentLocation.lat = e.Position.Location.Latitude;
                CurrentLocation.lon = e.Position.Location.Longitude;
                CurrentLocation.IsDirty = true;
            }
        }



        void gpswatcher_StatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
            if ((gpswatcher != null) && (App.Options.SendGeoCoordinates == true))
            {
                if (e.Status == GeoPositionStatus.NoData)
                    App.XMPPClient.GeoLocationString = "Unknown Location";
                else if (e.Status == GeoPositionStatus.Initializing)
                    App.XMPPClient.GeoLocationString = "Initializing";
                else if (e.Status == GeoPositionStatus.Disabled)
                    App.XMPPClient.GeoLocationString = "GPS Disabled";
            }
        }


        System.Windows.Threading.DispatcherTimer GeoTimer = null;
        GeoCoordinateWatcher gpswatcher = null;
        geoloc CurrentLocation = new geoloc();


        //void XMPPClient_OnNewConversationItem(RosterItem item, bool bReceived, TextMessage msg)
        //{
          //  Dispatcher.BeginInvoke(new System.Net.XMPP.XMPPClient.DelegateNewConversationItem(DoOnNewConversationItem), item, bReceived, msg);
        //}

        private void CopyToIsolatedStorage()
        {
            using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                string[] files = new string[] { "ding.wav"};

                foreach (var _fileName in files)
                {
                    if (!storage.FileExists(_fileName))
                    {
                        string _filePath = "sounds/" + _fileName;
                        StreamResourceInfo resource = Application.GetResourceStream(new Uri(_filePath, UriKind.Relative));

                        using (IsolatedStorageFileStream file = storage.CreateFile(_fileName))
                        {
                            int chunkSize = 4096;
                            byte[] bytes = new byte[chunkSize];
                            int byteCount;

                            while ((byteCount = resource.Stream.Read(bytes, 0, chunkSize)) > 0)
                            {
                                file.Write(bytes, 0, byteCount);
                            }
                        }
                    }
                }
            }
        }

        AudioTrack newmessagetrack = new AudioTrack(new Uri("ding.wav", UriKind.Relative), "New Message", "xmedianet", "none", null);


        //void DoOnNewConversationItem(RosterItem item, bool bReceived, TextMessage msg)
        void XMPPClient_OnNewConversationItem(RosterItem item, bool bReceived, TextMessage msg)
        {
            /// Save the conversation first
            ChatPage.SaveConversation(item);

            //Microsoft.Phone.PictureDecoder.DecodeJpeg(

            if (bReceived == true)
            {
                if (msg.Message != null)
                {
                    Microsoft.Phone.Shell.ShellToast toast = new Microsoft.Phone.Shell.ShellToast();
                    toast.Title = msg.Message;
                    //toast.NavigationUri = new Uri(string.Format("/ChatPage.xaml?JID={0}", msg.From.BareJID));
                    toast.Show();

                    if (App.Options.PlaySoundOnNewMessage == true)
                    {

                        Microsoft.Phone.BackgroundAudio.BackgroundAudioPlayer.Instance.Track = newmessagetrack;
                        Microsoft.Phone.BackgroundAudio.BackgroundAudioPlayer.Instance.Play();

                        //System.IO.Stream stream = TitleContainer.OpenStream("sounds/ding.wav");
                        //SoundEffect effect = SoundEffect.FromStream(stream);
                        //FrameworkDispatcher.Update();
                        //effect.Play();
                        //stream.Close();
                    }
                    if (App.Options.VibrateOnNewMessage == true)
                        Microsoft.Devices.VibrateController.Default.Start(TimeSpan.FromMilliseconds(200));
                }
            }

        }


        void DeviceNetworkInformation_NetworkAvailabilityChanged(object sender, NetworkNotificationEventArgs e)
        {
            switch (e.NotificationType)
            {
                case NetworkNotificationType.InterfaceConnected:
                    LogMessage("Network", SocketServer.MessageImportance.Medium, "Network was Connected at {0}\r\n", DateTime.Now);

                    if ((XMPPClient != null) && (XMPPClient.XMPPState == XMPPState.Unknown) && (XMPPClient.XMPPAccount != null) && (XMPPClient.XMPPAccount.HaveSuccessfullyConnectedAndAuthenticated==true))
                    {
                        XMPPClient.Connect(this);
                    }
                    break;
                case NetworkNotificationType.InterfaceDisconnected:
                    LogMessage("Network", SocketServer.MessageImportance.Medium, "Network was Disconnected at {0}\r\n", DateTime.Now);
                    if ((XMPPClient != null) && (XMPPClient.XMPPState != XMPPState.Unknown))
                    {
                        XMPPClient.Disconnect();
                        XMPPClient.Connect(this);
                        //MessageBox.Show("Network connection lost");
                    }
                    break;
                case NetworkNotificationType.CharacteristicUpdate:
                    
                    break;
                default:
                    
                    break;
            }

        }

        public static System.Text.StringBuilder XMPPLogBuilder = new System.Text.StringBuilder();
        void XMPPClient_OnXMLSent(System.Net.XMPP.XMPPClient client, string strXML)
        {
            if (Options.LogXML == true)
            {
                XMPPLogBuilder.Append("\r\n-->");
                foreach (char c in strXML)
                {
                    if ( (char.IsControl(c) == true) && (c != 13) && (c != 10) )
                        XMPPLogBuilder.Append(string.Format("0x{0:X2}", (int)c));
                    else
                        XMPPLogBuilder.Append(c);
                }
                //strXML = strXML.Replace("\0", "");
                //XMPPLogBuilder.AppendFormat("--> {0}\r\n", strXML);
            }
        }

        void XMPPClient_OnXMLReceived(System.Net.XMPP.XMPPClient client, string strXML)
        {
            if (Options.LogXML == true)
            {
                XMPPLogBuilder.Append("\r\n<--");
                foreach (char c in strXML)
                {
                    if ((char.IsControl(c) == true) && (c != 13) && (c != 10))
                        XMPPLogBuilder.Append(string.Format("0x{0:X2}", (int)c));
                    else
                        XMPPLogBuilder.Append(c);
                }
                //strXML = strXML.Replace("\0", "");
                //XMPPLogBuilder.AppendFormat("<-- {0}\r\n", strXML);
            }
        }


        public static void LoadOptions()
        {
            string strFilename = "options.xml";
            using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (storage.FileExists("options.xml") == true)
                {
                    // Load from storage
                    IsolatedStorageFileStream location = null;
                    try
                    {
                        location = new IsolatedStorageFileStream(strFilename, System.IO.FileMode.Open, storage);
                        DataContractSerializer ser = new DataContractSerializer(typeof(Options));

                        Options = ser.ReadObject(location) as Options;
                    }
                    catch (Exception)
                    {
                        Options = new Options();
                    }
                    finally
                    {
                        if (location != null)
                            location.Close();
                    }
                }
            }

            Options.UseOnlyIBBFileTransfer = FileTransferManager.UseIBBOnly;
            Options.SOCKS5ByteStreamProxy = FileTransferManager.SOCKS5Proxy;

        }

        public static void SaveOptions()
        {
            string strFilename = "options.xml";
            using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                // Load from storage
                IsolatedStorageFileStream location = null;
                try
                {
                    location = new IsolatedStorageFileStream(strFilename, System.IO.FileMode.Create, storage);
                    DataContractSerializer ser = new DataContractSerializer(typeof(Options));
                    ser.WriteObject(location, Options);
                }
                catch (Exception)
                {
                }
                finally
                {
                    if (location != null)
                        location.Close();
                }

            }


            ((App)Application.Current).GeoTimer.Interval = TimeSpan.FromSeconds(Options.GeoTimeFrequency);
            ((App)Application.Current).StartGeoStuff();

            FileTransferManager.UseIBBOnly = Options.UseOnlyIBBFileTransfer;
            FileTransferManager.SOCKS5Proxy = Options.SOCKS5ByteStreamProxy;
            
        }

        public static void SaveException(string strException)
        {
            string strFilename = "Exception.txt";
            using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                // Load from storage
                IsolatedStorageFileStream location = null;
                try
                {
                    location = new IsolatedStorageFileStream(strFilename, System.IO.FileMode.Create, storage);
                    byte [] bException = System.Text.UTF8Encoding.UTF8.GetBytes(strException);
                    location.Write(bException, 0, bException.Length);
                }
                catch (Exception)
                {
                }
                finally
                {
                    if (location != null)
                        location.Close();
                }

            }
        }

        public static void CheckForExceptionsLastTime()
        {
            string strFilename = "Exception.txt";
            string strExceptionText = null;

            using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
            {

                if (storage.FileExists(strFilename) == true)
                {

                    // Load from storage
                    IsolatedStorageFileStream location = null;
                    try
                    {
                        location = new IsolatedStorageFileStream(strFilename, System.IO.FileMode.Open, storage);
                        byte[] bException = new byte[location.Length];
                        location.Read(bException, 0, bException.Length);

                        strExceptionText = System.Text.UTF8Encoding.UTF8.GetString(bException, 0, bException.Length);
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        if (location != null)
                            location.Close();
                    }

                    storage.DeleteFile(strFilename);
                }
            }

            if ((strExceptionText != null) && (strExceptionText.Length > 0))
            {
                if (MessageBox.Show("An exception occurred the last time this application was run, would you like to forward the details to the developers?", "Application Exception", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    MailException(strExceptionText);
                }
            }
        }
        public static void MailLog()
        {
            MailException(XMPPLogBuilder.ToString());
        }

        static void MailException(string strExceptionText)
        {
            Microsoft.Phone.Tasks.EmailComposeTask task = new Microsoft.Phone.Tasks.EmailComposeTask();
            if (strExceptionText.Length < 32000)
                task.Body = strExceptionText;
            else
                task.Body = strExceptionText.Substring(strExceptionText.Length - 32000);
            task.Subject = "[XMPPClient] Log/Crash Notification Details";
            task.To = "bonnbria@gmail.com";
            task.Show();
        }

        public static System.Net.XMPP.XMPPClient XMPPClient = new System.Net.XMPP.XMPPClient();
        public static Options Options = new Options();

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
          
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            if (WasConnected == true)
            {
                App.XMPPClient.Disconnect();
                System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(DoConnect));
            }
        }



        public static void WaitConnected()
        {
            /// Wait for the client to reconnect... should have already been activated above
            if (App.XMPPClient.Connected == false)
                App.XMPPClient.ConnectHandle.WaitOne(15000);
        }

        public static void WaitReady()
        {
            /// Wait for the client to reconnect... should have already been activated above
            if (App.XMPPClient.Connected == false)
                App.XMPPClient.ConnectHandle.WaitOne(15000);

            int nCount = 0;
            while ( (App.XMPPClient.XMPPState != XMPPState.Ready) && (nCount < 10) )
            {
                System.Threading.Thread.Sleep(0);
                nCount++;
            }
        }


        static void DoConnect(object junk)
        {
            App.XMPPClient.Connect((SocketServer.ILogInterface)Application.Current);
        }

        public static bool WasConnected = false;

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            WasConnected = App.XMPPClient.Connected;
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            GeoTimer.Stop();
            if (gpswatcher != null)
               gpswatcher.Stop();
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                BackgroundAudioPlayer.Instance.Close();
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is QuitException)
                return;
                
            App.SaveException(e.ExceptionObject.ToString() + "\r\n\r\n" + XMPPLogBuilder.ToString());
                

            if (System.Diagnostics.Debugger.IsAttached)
            {
                BackgroundAudioPlayer.Instance.Close();
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            RootFrame.Obscured += new EventHandler<ObscuredEventArgs>(RootFrame_Obscured);
            RootFrame.Unobscured += new EventHandler(RootFrame_Unobscured);

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        void RootFrame_Unobscured(object sender, EventArgs e)
        {
            
        }

        void RootFrame_Obscured(object sender, ObscuredEventArgs e)
        {
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion

        #region ILogInterface Members

        public void LogMessage(string strCateogry, string strGuid, SocketServer.MessageImportance importance, string strMessage, params object[] msgparams)
        {
            if (Options.LogDebug == true)
                XMPPLogBuilder.AppendFormat(strMessage, msgparams);
        }

        public void LogWarning(string strCateogry, string strGuid, SocketServer.MessageImportance importance, string strMessage, params object[] msgparams)
        {
            if (Options.LogDebug == true)
                XMPPLogBuilder.AppendFormat(strMessage, msgparams);
        }

        public void LogError(string strCateogry, string strGuid, SocketServer.MessageImportance importance, string strMessage, params object[] msgparams)
        {
            if (Options.LogDebug == true)
                XMPPLogBuilder.AppendFormat(strMessage, msgparams);
        }

        public void LogMessage(string strGuid, SocketServer.MessageImportance importance, string strMessage, params object[] msgparams)
        {
            if (Options.LogDebug == true)
                XMPPLogBuilder.AppendFormat(strMessage, msgparams);
        }

        public void LogWarning(string strGuid, SocketServer.MessageImportance importance, string strMessage, params object[] msgparams)
        {
            if (Options.LogDebug == true)
                XMPPLogBuilder.AppendFormat(strMessage, msgparams);
        }

        public void LogError(string strGuid, SocketServer.MessageImportance importance, string strMessage, params object[] msgparams)
        {
            if (Options.LogDebug == true)
                XMPPLogBuilder.AppendFormat(strMessage, msgparams);
        }

        public void ClearLog()
        {
        }

        #endregion
    }

    public class XNAAsyncDispatcher : IApplicationService
    {
        private System.Windows.Threading.DispatcherTimer frameworkDispatcherTimer; 
        public XNAAsyncDispatcher(TimeSpan dispatchInterval) 
        {
            this.frameworkDispatcherTimer = new System.Windows.Threading.DispatcherTimer(); 
            this.frameworkDispatcherTimer.Tick += new EventHandler(frameworkDispatcherTimer_Tick); 
            this.frameworkDispatcherTimer.Interval = dispatchInterval; 
        }     
        
        void IApplicationService.StartService(ApplicationServiceContext context) 
        { 
            this.frameworkDispatcherTimer.Start(); 
        }     
        void IApplicationService.StopService() 
        { 
            this.frameworkDispatcherTimer.Stop(); 
        }     
        
        void frameworkDispatcherTimer_Tick(object sender, EventArgs e) 
        { 
            FrameworkDispatcher.Update(); 
        }

    }
}