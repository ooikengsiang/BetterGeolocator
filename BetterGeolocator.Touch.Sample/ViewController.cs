using CoreLocation;
using System;
using UIKit;

namespace BetterGeolocator.Touch.Sample
{
    public partial class ViewController : UIViewController
    {
        private UILabel ResultLabel { get; set; }
        private UIButton GoButton { get; set; }

        private Geolocator Geolocator { get; set; }

        public ViewController (IntPtr handle) 
            : base (handle)
        {
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            ResultLabel = new UILabel()
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Lines = 0
            };
            View.AddSubview(ResultLabel);
            View.AddConstraint(NSLayoutConstraint.Create(ResultLabel, NSLayoutAttribute.Left, NSLayoutRelation.Equal, View, NSLayoutAttribute.Left, 1f, 0f));
            View.AddConstraint(NSLayoutConstraint.Create(ResultLabel, NSLayoutAttribute.Right, NSLayoutRelation.Equal, View, NSLayoutAttribute.Right, 1f, 0f));
            View.AddConstraint(NSLayoutConstraint.Create(ResultLabel, NSLayoutAttribute.Top, NSLayoutRelation.Equal, View, NSLayoutAttribute.TopMargin, 1f, 0f));
            View.AddConstraint(NSLayoutConstraint.Create(ResultLabel, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, View, NSLayoutAttribute.BottomMargin, 1f, 0f));

            GoButton = new UIButton()
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                BackgroundColor = UIColor.LightGray
            };
            GoButton.SetTitle("Go", UIControlState.Normal);
            View.AddSubview(GoButton);
            View.AddConstraint(NSLayoutConstraint.Create(GoButton, NSLayoutAttribute.Left, NSLayoutRelation.Equal, View, NSLayoutAttribute.Left, 1f, 0f));
            View.AddConstraint(NSLayoutConstraint.Create(GoButton, NSLayoutAttribute.Right, NSLayoutRelation.Equal, View, NSLayoutAttribute.Right, 1f, 0f));
            View.AddConstraint(NSLayoutConstraint.Create(GoButton, NSLayoutAttribute.Top, NSLayoutRelation.GreaterThanOrEqual, View, NSLayoutAttribute.TopMargin, 1f, 0f));
            View.AddConstraint(NSLayoutConstraint.Create(GoButton, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, View, NSLayoutAttribute.BottomMargin, 1f, 0f));
            GoButton.TouchUpInside += GoButton_TouchUpInside;

            // We can start buffer the location before user click get location.
            // This enabled the location to retrieve faster but it will ignore any error such as lack of permission.
            Geolocator = new Geolocator();
            Geolocator.StartCacheLocation();
        }

        private async void GoButton_TouchUpInside(object sender, EventArgs e)
        {
            if (CLLocationManager.Status == CLAuthorizationStatus.AuthorizedAlways ||
                CLLocationManager.Status == CLAuthorizationStatus.AuthorizedWhenInUse)
            {
                // Retrieve current location
                var location = await Geolocator.GetLocation(TimeSpan.FromSeconds(30), 200);

                // Output all location information to text view
                ResultLabel.Text = location?.ToString() ?? string.Empty;
            }
            else
            {
                // Request for permission
                var locationManager = new CLLocationManager();
                locationManager.RequestWhenInUseAuthorization();
            }
        }
    }
}