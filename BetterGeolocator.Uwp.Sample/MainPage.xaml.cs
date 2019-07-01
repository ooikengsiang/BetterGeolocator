using System;
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

        private async void GetLocationButton_Click(object sender, RoutedEventArgs e)
        {
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
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear the last known location
            Geolocator.Clear();

            ResultTextBlock.Text = "Cleared";
        }
    }
}
