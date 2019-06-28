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

namespace BetterGeolocator.Droid.Sample
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        /// <summary>
        /// Pick your lucky number. Any number will do. It is used to identify which permission request returned.
        /// </summary>
        private int PermissionRequestCode = 57;
        private TextView ResultTextView { get; set; }
        private FloatingActionButton GoFloatingActionButton { get; set; }

        private Geolocator Geolocator { get; set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            ResultTextView = FindViewById<TextView>(Resource.Id.result_text_view);

            GoFloatingActionButton = FindViewById<FloatingActionButton>(Resource.Id.go_floating_action_button);
            GoFloatingActionButton.Click += GoFloatingActionButtonOnClick;

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

        private async void GoFloatingActionButtonOnClick(object sender, EventArgs eventArgs)
        {
            // Temporary disable to prevent user from requesting multiple time
            GoFloatingActionButton.Enabled = false;
            ResultTextView.Text = "Loading...";

            // Check if we have the permission to access the location
            if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted)
            {
                // Pop dialog to ask for permission
                var permissions = new string[] { Manifest.Permission.AccessFineLocation };
                Android.Support.V4.App.ActivityCompat.RequestPermissions(this, permissions, PermissionRequestCode);

                GoFloatingActionButton.Enabled = true;
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

            GoFloatingActionButton.Enabled = true;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if (requestCode == PermissionRequestCode &&
                grantResults.Length != 0 &&
                grantResults[0] == Permission.Granted)
            {
                // Permission granted
                Snackbar.Make(ResultTextView, "Permission granted.", Snackbar.LengthLong)
                    .Show();

                // Get location
                GoFloatingActionButtonOnClick(null, null);
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

