using System;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace BetterGeolocator.Uwp.Sample
{
    public sealed partial class MainPage : Page
    {
        private Geolocator Geolocator { get; set; }

        public MainPage()
        {
            InitializeComponent();

            // We can start buffer the location before user click get location.
            // This enabled the location to retrieve faster but it will ignore any error such as lack of permission.
            Geolocator = new Geolocator();
            Geolocator.StartCacheLocation();
        }

        private void ClearCacheLocationButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear the last known location
            Geolocator.Clear();

            ResultTextBlock.Text = "Cleared";
        }

        private async void GetLocationForegroundButton_Click(object sender, RoutedEventArgs e)
        {
            // Temporary disable to prevent user from requesting multiple time
            ClearCacheLocationButton.IsEnabled = false;
            GetLocationForegroundButton.IsEnabled = false;
            GetLocationBackgroundButton.IsEnabled = false;
            ResultTextBlock.Text = "Loading...";

            // Check if we have the permission to access the location
            var accessStatus = await Windows.Devices.Geolocation.Geolocator.RequestAccessAsync();
            if (accessStatus == GeolocationAccessStatus.Allowed)
            {
                // Show loading
                ResultTextBlock.Text = "Loading...";

                // Retrieve current location
                var location = await Geolocator.GetLocation(TimeSpan.FromSeconds(30), 200);

                // Output all location information to text view
                ResultTextBlock.Text = location?.ToString() ?? string.Empty;
            }

            ClearCacheLocationButton.IsEnabled = true;
            GetLocationForegroundButton.IsEnabled = true;
            GetLocationBackgroundButton.IsEnabled = true;
        }

        private async void GetLocationBackgroundButton_Click(object sender, RoutedEventArgs e)
        {
            // Temporary disable to prevent user from requesting multiple time
            ClearCacheLocationButton.IsEnabled = false;
            GetLocationForegroundButton.IsEnabled = false;
            GetLocationBackgroundButton.IsEnabled = false;
            ResultTextBlock.Text = "Loading...";

            // Check if we have the permission to access the location
            var accessStatus = await Windows.Devices.Geolocation.Geolocator.RequestAccessAsync();
            if (accessStatus == GeolocationAccessStatus.Allowed)
            {
                // Show loading
                ResultTextBlock.Text = "Loading...";

                await Task.Run(async () =>
                {
                    // Retrieve current location
                    var location = await Geolocator.GetLocation(TimeSpan.FromSeconds(30), 200);

                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        // Output all location information to text view
                        ResultTextBlock.Text = location?.ToString() ?? string.Empty;

                        ClearCacheLocationButton.IsEnabled = true;
                        GetLocationForegroundButton.IsEnabled = true;
                        GetLocationBackgroundButton.IsEnabled = true;
                    });
                });
            }
            else
            {
                ClearCacheLocationButton.IsEnabled = true;
                GetLocationForegroundButton.IsEnabled = true;
                GetLocationBackgroundButton.IsEnabled = true;
            }
        }
    }
}
