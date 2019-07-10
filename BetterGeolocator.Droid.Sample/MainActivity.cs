using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BetterGeolocator.Droid.Sample
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        /// <summary>
        /// Pick your lucky number. Any number will do. It is used to identify which permission request returned.
        /// </summary>
        private const int ForegroundPermissionRequestCode = 57;
        private const int BackgroundPermissionRequestCode = 58;
        private TextView ResultTextView { get; set; }
        private Button ClearCacheLocationButton { get; set; }
        private Button GetLocationForegroundButton { get; set; }
        private Button GetLocationBackgroundButton { get; set; }

        private Geolocator Geolocator { get; set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            ResultTextView = FindViewById<TextView>(Resource.Id.result_text_view);

            ClearCacheLocationButton = FindViewById<Button>(Resource.Id.clear_cache_location_button);
            ClearCacheLocationButton.Click += ClearCacheLocationButtonOnClick;

            GetLocationForegroundButton = FindViewById<Button>(Resource.Id.get_location_foreground_button);
            GetLocationForegroundButton.Click += GetLocationForegroundButtonOnClick;

            GetLocationBackgroundButton = FindViewById<Button>(Resource.Id.get_location_background_button);
            GetLocationBackgroundButton.Click += GetLocationBackgroundButtonOnClick;

            // We can start buffer the location before user click get location.
            // This enabled the location to retrieve faster but it will ignore any error such as lack of permission.
            Geolocator = new Geolocator();
            Geolocator.StartCacheLocation();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void ClearCacheLocationButtonOnClick(object sender, EventArgs eventArgs)
        {
            Geolocator.Clear();
            ResultTextView.Text = "Cleared";
        }

        private async void GetLocationForegroundButtonOnClick(object sender, EventArgs eventArgs)
        {
            // Temporary disable to prevent user from requesting multiple time
            ClearCacheLocationButton.Enabled = false;
            GetLocationForegroundButton.Enabled = false;
            GetLocationBackgroundButton.Enabled = false;
            ResultTextView.Text = "Loading...";

            // Check if we have the permission to access the location
            if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted)
            {
                // Pop dialog to ask for permission
                var permissions = new string[] { Manifest.Permission.AccessFineLocation };
                Android.Support.V4.App.ActivityCompat.RequestPermissions(this, permissions, ForegroundPermissionRequestCode);

                ClearCacheLocationButton.Enabled = true;
                GetLocationForegroundButton.Enabled = true;
                GetLocationBackgroundButton.Enabled = true;
                ResultTextView.Text = "No permission";

                return;
            }

            var sw = Stopwatch.StartNew();

            // Retrieve current location
            var location = await Geolocator.GetLocation(TimeSpan.FromSeconds(30), 200);

            // Output all location information to text view
            ResultTextView.Text = location?.ToString() ?? string.Empty;

            sw.Stop();

            // Show the time taken
            Snackbar.Make(ResultTextView, $"Time: {sw.ElapsedMilliseconds} ms", Snackbar.LengthLong)
                .Show();

            ClearCacheLocationButton.Enabled = true;
            GetLocationForegroundButton.Enabled = true;
            GetLocationBackgroundButton.Enabled = true;
        }

        private async void GetLocationBackgroundButtonOnClick(object sender, EventArgs eventArgs)
        {
            // Temporary disable to prevent user from requesting multiple time
            ClearCacheLocationButton.Enabled = false;
            GetLocationForegroundButton.Enabled = false;
            GetLocationBackgroundButton.Enabled = false;
            ResultTextView.Text = "Loading...";

            // Check if we have the permission to access the location
            if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted)
            {
                // Pop dialog to ask for permission
                var permissions = new string[] { Manifest.Permission.AccessFineLocation };
                Android.Support.V4.App.ActivityCompat.RequestPermissions(this, permissions, BackgroundPermissionRequestCode);

                ClearCacheLocationButton.Enabled = true;
                GetLocationForegroundButton.Enabled = true;
                GetLocationBackgroundButton.Enabled = true;
                ResultTextView.Text = "No permission";

                return;
            }

            await Task.Run(async () =>
            {
                var sw = Stopwatch.StartNew();

                // Retrieve current location
                var location = await Geolocator.GetLocation(TimeSpan.FromSeconds(30), 200);

                RunOnUiThread(() =>
                {
                    // Output all location information to text view
                    ResultTextView.Text = location?.ToString() ?? string.Empty;

                    sw.Stop();

                    // Show the time taken
                    Snackbar.Make(ResultTextView, $"Time: {sw.ElapsedMilliseconds} ms", Snackbar.LengthLong)
                        .Show();

                    ClearCacheLocationButton.Enabled = true;
                    GetLocationForegroundButton.Enabled = true;
                    GetLocationBackgroundButton.Enabled = true;
                });
            });
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if (grantResults.Length != 0 &&
                grantResults[0] == Permission.Granted)
            {
                if (requestCode == ForegroundPermissionRequestCode)
                {
                    // Get location
                    GetLocationForegroundButtonOnClick(null, null);
                }
                else if (requestCode == BackgroundPermissionRequestCode)
                {
                    // Get location
                    GetLocationBackgroundButtonOnClick(null, null);
                }
            }
            else
            {
                // Permission denied
                Snackbar.Make(ResultTextView, "Permission denied.", Snackbar.LengthLong)
                    .Show();
            }
        }
	}
}

