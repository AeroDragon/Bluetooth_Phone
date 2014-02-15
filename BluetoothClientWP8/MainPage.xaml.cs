using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using BluetoothClientWP8.Resources;
using Windows.Networking.Sockets;
using Windows.Networking.Proximity;
using System.Diagnostics;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using BluetoothConnectionManager;
using System.Windows.Media;
using Windows.Devices.Geolocation;
using System.IO.IsolatedStorage;
using System.Windows.Threading;

namespace BluetoothClientWP8
{
    public partial class MainPage : PhoneApplicationPage
    {
        private ConnectionManager connectionManager;
        private StateManager stateManager;
        bool tracking = false;
        DispatcherTimer pulse = new DispatcherTimer();
        Location location = new Location();

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            connectionManager = new ConnectionManager();
            connectionManager.MessageReceived += connectionManager_MessageReceived;
            stateManager = new StateManager();
            BuildApplicationBar();
            PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            pulse.Tick += new EventHandler(pulse_Tick);
            pulse.Interval = new TimeSpan(0,0,1);

        }

        private void pulse_Tick(object sender, EventArgs e)
        {
            sendLocation();
        }

        async void connectionManager_MessageReceived(string message)
        {
            Debug.WriteLine("Message received:" + message);
            string[] messageArray = message.Split(':');
            switch (messageArray[0])
            {
                case "LED_RED":
                    stateManager.RedLightOn = messageArray[1] == "ON" ? true : false;
                    Dispatcher.BeginInvoke(delegate()
                    {
                        //RedButton.Background = stateManager.RedLightOn ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Black);
                    });
                    break;
                case "LED_GREEN":
                    stateManager.GreenLightOn = messageArray[1] == "ON" ? true : false;
                    Dispatcher.BeginInvoke(delegate()
                    {
                        //GreenButton.Background = stateManager.GreenLightOn ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Black);
                    });
                    break;
                case "LED_YELLOW":
                    stateManager.YellowLightOn = messageArray[1] == "ON" ? true : false;
                    Dispatcher.BeginInvoke(delegate()
                    {
                        //YellowButton.Background = stateManager.YellowLightOn ? new SolidColorBrush(Colors.Yellow) : new SolidColorBrush(Colors.Black);
                    });
                    break;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            connectionManager.Initialize();
            stateManager.Initialize();
            
            if (IsolatedStorageSettings.ApplicationSettings.Contains("LocationConsent"))
            {
                 //User has already opted in or out of Location
                return;
            }
            else
            {
                MessageBoxResult result =
                    MessageBox.Show("This app accesses your phone's location. Is that ok?",
                    "Location",
                    MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.OK)
                {
                    IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = true;
                }
                else
                {
                    IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = false;
                }

                IsolatedStorageSettings.ApplicationSettings.Save();

                UpdateAppBar();
                
            }
            
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            connectionManager.Terminate();
        }

        private void ConnectAppToDeviceButton_Click_1(object sender, RoutedEventArgs e)
        {
            AppToDevice();
        }

        private async void AppToDevice()
        {
            //ConnectAppToDeviceButton.Content = "Connecting...";
            PeerFinder.AlternateIdentities["Bluetooth:Paired"] = "";
            var pairedDevices = await PeerFinder.FindAllPeersAsync();

            if (pairedDevices.Count == 0)
            {
                Debug.WriteLine("No paired devices were found.");
            }
            else
            {
                foreach (var pairedDevice in pairedDevices)
                {
                    if (pairedDevice.DisplayName == "linvor")//DeviceName.Text)
                    {
                        connectionManager.Connect(pairedDevice.HostName);
                        //ConnectAppToDeviceButton.Content = "Connected";
                        //DeviceName.IsReadOnly = true;
                        //ConnectAppToDeviceButton.IsEnabled = false;
                        continue;
                    }
                }
            }
        }

        #region
        private async void RedButton_Click_1(object sender, RoutedEventArgs e)
        {
            string command = stateManager.RedLightOn ? "TURN_OFF_RED" : "TURN_ON_RED";
            await connectionManager.SendCommand(command);
        }

        private async void GreenButton_Click_1(object sender, RoutedEventArgs e)
        {
            string command = stateManager.GreenLightOn ? "TURN_OFF_GREEN" : "TURN_ON_GREEN";
            await connectionManager.SendCommand(command);
        }

        private async void YellowButton_Click_1(object sender, RoutedEventArgs e)
        {
            string command = stateManager.YellowLightOn ? "TURN_OFF_YELLOW" : "TURN_ON_YELLOW";
            await connectionManager.SendCommand(command);
        }
        #endregion

        ///////////////Navigation////////////////////////////////////////////////////////////
        #region
        
        // Get the current location of the phone. To reduce power consumption, it is recommended that you
        // use one-shot location unless your app requires location tracking.
        private async void OneShotLocation_Click(object sender, RoutedEventArgs e)
        {

            if ((bool)IsolatedStorageSettings.ApplicationSettings["LocationConsent"] != true)
            {
                // The user has opted out of Location. 
                StatusTextBlock.Text = "You have opted out of location. Use the app bar to turn location back on";
                return;
            }

            Geolocator geolocator = new Geolocator();
            geolocator.DesiredAccuracyInMeters = 2;

            try
            {
                // Request the current position
                Geoposition geoposition = await geolocator.GetGeopositionAsync(
                    maximumAge: TimeSpan.FromMinutes(1),
                    timeout: TimeSpan.FromSeconds(10)
                    );
 
                location.latitude = geoposition.Coordinate.Latitude.ToString("0.000000000");
                location.longitude = geoposition.Coordinate.Longitude.ToString("0.000000000");
                LatitudeTextBlock.Text = location.latitude;
                LongitudeTextBlock.Text = location.longitude;
                StatusTextBlock.Text = "location obtained";
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == 0x80004004)
                {
                    // the application does not have the right capability or the location master switch is off
                    StatusTextBlock.Text = "location  is disabled in phone settings.";
                }
                //else
                {
                    // something else happened acquring the location
                }
            }
        }


        private void TrackLocation_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)IsolatedStorageSettings.ApplicationSettings["LocationConsent"] != true)
            {
                // The user has opted out of Location.
                StatusTextBlock.Text = "You have opted out of location. Use the app bar to turn location back on";
                return;
            }


            if (!tracking)
            {
                // If not currently tacking, create a new Geolocator and set options.
                // Assigning the PositionChanged event handler begins location acquisition.

                if (App.Geolocator == null)
                {
                    // Use the app's global Geolocator variable
                    App.Geolocator = new Geolocator();
                }

                App.Geolocator.DesiredAccuracy = PositionAccuracy.High;
                App.Geolocator.MovementThreshold = 5; // The units are meters.
                App.Geolocator.ReportInterval = 3000;

                App.Geolocator.StatusChanged += geolocator_StatusChanged;
                App.Geolocator.PositionChanged += geolocator_PositionChanged;

                tracking = true;
                TrackLocationButton.Content = "stop tracking";
            }
            else
            {
                // To stop location acquisition, remove the position changed and status changed event handlers.
                App.Geolocator.PositionChanged -= geolocator_PositionChanged;
                App.Geolocator.StatusChanged -= geolocator_StatusChanged;
                App.Geolocator = null;

                tracking = false;
                TrackLocationButton.Content = "track location";
                StatusTextBlock.Text = "stopped";
            }
        }

        // The PositionChanged event is raised when new position data is available
        void geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            if (!App.RunningInBackground)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    location.latitude = args.Position.Coordinate.Latitude.ToString("0.000000000");
                    location.longitude=args.Position.Coordinate.Longitude.ToString("0.000000000");
                    LatitudeTextBlock.Text = location.latitude;
                    LongitudeTextBlock.Text = location.longitude;
                });
            }
            else
            {
                Microsoft.Phone.Shell.ShellToast toast = new Microsoft.Phone.Shell.ShellToast();
                toast.Content = args.Position.Coordinate.Latitude.ToString("0.000000000");
                toast.Title = "Location: ";
                toast.NavigationUri = new Uri("/MainPage.xaml", UriKind.Relative);
                toast.Show();

            }
        }

        // The StatusChanged event is raised when the status of the location service changes.
        void geolocator_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {

            string status = "";

            switch (args.Status)
            {
                case PositionStatus.Disabled:
                    // the application does not have the right capability or the location master switch is off
                    status = "location is disabled in phone settings";
                    break;
                case PositionStatus.Initializing:
                    // the geolocator started the tracking operation
                    status = "initializing";
                    break;
                case PositionStatus.NoData:
                    // the location service was not able to acquire the location
                    status = "no data";
                    break;
                case PositionStatus.Ready:
                    // the location service is generating geopositions as specified by the tracking parameters
                    status = "ready";
                    break;
                case PositionStatus.NotAvailable:
                    status = "not available";
                    // not used in WindowsPhone, Windows desktop uses this value to signal that there is no hardware capable to acquire location information
                    break;
                case PositionStatus.NotInitialized:
                    // the initial state of the geolocator, once the tracking operation is stopped by the user the geolocator moves back to this state

                    break;
            }


            if (!App.RunningInBackground)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    StatusTextBlock.Text = status;
                });
            }
        }

        // When the page is removed from the backstack, remove the event handlers to stop location acquisition
        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            App.Geolocator.PositionChanged -= geolocator_PositionChanged;
            App.Geolocator.StatusChanged -= geolocator_StatusChanged;
            App.Geolocator = null;
        }

        // Allow the user to toggle opting in and out of location with an ApplicationBar menu item.
        private void BuildApplicationBar()
        {
            // Set the page's ApplicationBar to a new instance of ApplicationBar.
            ApplicationBar = new ApplicationBar();

            ApplicationBarMenuItem menuItem = new ApplicationBarMenuItem();
            menuItem.Text = "loading";

            menuItem.Click += menuItem_Click;
            ApplicationBar.MenuItems.Add(menuItem);
            ApplicationBar.IsMenuEnabled = true;
        }

        void menuItem_Click(object sender, EventArgs e)
        {
            if ((bool)IsolatedStorageSettings.ApplicationSettings["LocationConsent"] == true)
            {
                IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = false;
            }
            else
            {
                IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = false;
            }
            UpdateAppBar();

        }


        void UpdateAppBar()
        {

            ApplicationBarMenuItem menuItem = (ApplicationBarMenuItem)ApplicationBar.MenuItems[0];

            if ((bool)IsolatedStorageSettings.ApplicationSettings["LocationConsent"] == false)
            {
                menuItem.Text = "opt in to location";
            }
            else
            {
                menuItem.Text = "opt out of location";
            }
        }

        #endregion

        private async void btnSend_CoOrd_Click(object sender, RoutedEventArgs e)
        {
            pulse.Start();            
        }

        private async void sendLocation()
        {
            await connectionManager.SendCommand(location.ToString());
        }
    }
}

