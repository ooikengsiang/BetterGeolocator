using System;

namespace BetterGeolocator
{
    public partial class Geolocator
    {
        /// <summary>
        /// Reference to Windows geolocator.
        /// </summary>
        private Windows.Devices.Geolocation.Geolocator WindowsGeolocator { get; set; }

        /// <summary>
        /// Check if location permission is already granted.
        /// </summary>
        /// <returns>Return true if location permission is granted, else false.</returns>
        private bool IsPermissionGrantedImpl()
        {
            // Re-use the geolocator if found
            var winGeolocator = WindowsGeolocator;
            if (winGeolocator == null)
            {
                winGeolocator = new Windows.Devices.Geolocation.Geolocator();
            }
            return winGeolocator.LocationStatus != Windows.Devices.Geolocation.PositionStatus.Disabled;
        }

        /// <summary>
        /// Start listen to location update / gather device location.
        /// </summary>
        private async void StartLocationUpdate()
        {
            // Don't start another Windows geolocator if there is one already running
            if (WindowsGeolocator == null)
            {
                var isRequestLocationSuccessful = false;
                var isLastKnownLocationUseable = false;

                try
                {
                    // Acquire a reference to the system Windows geolocator 
                    WindowsGeolocator = new Windows.Devices.Geolocation.Geolocator()
                    {
                        DesiredAccuracyInMeters = (int)DefaultTargetAccuracy,
                        ReportInterval = 0
                    };

                    // Try get the last known location or current device location
                    if (!UpdateLocation(await WindowsGeolocator.GetGeopositionAsync()))
                    {
                        // Then request the location update
                        WindowsGeolocator.PositionChanged += WindowsGeolocator_PositionChanged;
                    }
                    else
                    {
                        // No need to get new location since we already have the location
                        isLastKnownLocationUseable = true;
                    }

                    isRequestLocationSuccessful = true;
                }
                catch
                {
                    // Ignore all error including lack of permission or provider is not enabled
                }

                // Stop location service if initialized failed or we already have the location
                if (!isRequestLocationSuccessful)
                {
                    StopLocationUpdate(GeolocationStatus.SetupError);
                }
                else if (isLastKnownLocationUseable)
                {
                    StopLocationUpdate(GeolocationStatus.Successful);
                }
            }
        }

        /// <summary>
        /// Stop listen to location update / gatherer device location.
        /// </summary>
        private void StopLocationUpdate(GeolocationStatus status)
        {
            if (WindowsGeolocator != null)
            {
                WindowsGeolocator.PositionChanged -= WindowsGeolocator_PositionChanged;
                WindowsGeolocator = null;
            }

            // Make sure we only update the result if it is not completed yet
            if (LocationTaskCompletionSource != null &&
                !LocationTaskCompletionSource.Task.IsCompleted)
            {
                // Craft the location with all information we have whatever the location is acceptable or not
                var location = IsLastKnownLocationFresh() ?
                    LastKnownLocation.ToNewGeolocation(status) :
                    new Geolocation(status);

                // return the location
                LocationTaskCompletionSource.SetResult(location);
            }
        }

        /// <summary>
        /// Windows geolocator found a location point.
        /// </summary>
        private void WindowsGeolocator_PositionChanged(Windows.Devices.Geolocation.Geolocator sender, Windows.Devices.Geolocation.PositionChangedEventArgs args)
        {
            UpdateLocation(args.Position);
        }

        /// <summary>
        /// Update last known location and see if it is an acceptable location.
        /// If yes, stop updating the location any further.
        /// </summary>
        private bool UpdateLocation(Windows.Devices.Geolocation.Geoposition position)
        {
            bool isLocationAcceptable = false;

            if (position != null)
            {
                if (LastKnownLocation == null ||
                    !LastKnownLocation.UpdateDateTime.HasValue)
                {
                    // No location saved so far, accept any new location
                    LastKnownLocation = ConvertPositionoGeolocation(position);
                }
                else
                {
                    // Check whether the new location is newer or older
                    var locationDateTime = position.Coordinate.Timestamp.Date;
                    var timeDelta = locationDateTime - LastKnownLocation.UpdateDateTime.Value;
                    var isSignificantlyNewer = timeDelta.TotalMilliseconds > FreshnessThreshold;

                    // Check whether the new location is more or less accurate than saved location
                    // Less is better
                    var accuracyDelta = position.Coordinate.Accuracy - LastKnownLocation.Accuracy;
                    var isMoreOrSameAccurate = accuracyDelta <= 0;
                    var isSignificantlyLessAccurate = accuracyDelta > AccuracyThreshold;

                    // Determine location quality using a combination of timeliness and accuracy
                    if (isMoreOrSameAccurate ||
                        (isSignificantlyNewer && !isSignificantlyLessAccurate))
                    {
                        // Accept this newer / more accurate location
                        LastKnownLocation = ConvertPositionoGeolocation(position);
                    }
                }

                // Turn off the location if it is good enough
                if (IsLastKnownLocationFreshAndAccurate())
                {
                    isLocationAcceptable = true;

                    // Acceptable location, stop running location
                    StopLocationUpdate(GeolocationStatus.Successful);
                }
            }

            return isLocationAcceptable;
        }

        /// <summary>
        /// Trigger when the timeout reach and we have to pack up and return the location.
        /// </summary>
        private void LocationRetrieveTimerTimeout(object state)
        {
            if (LocationRetrieveTimer != null)
            {
                StopLocationUpdate(GeolocationStatus.Timeout);
            }
        }

        /// <summary>
        /// Convert Windows Geo position to Geo Location that we are using in the library.
        /// </summary>
        private Geolocation ConvertPositionoGeolocation(Windows.Devices.Geolocation.Geoposition position)
        {
            return new Geolocation()
            {
                Latitude = position.Coordinate.Point.Position.Latitude,
                Longitude = position.Coordinate.Point.Position.Longitude,
                Altitude = position.Coordinate.Point.Position.Altitude,
                Accuracy = position.Coordinate.Accuracy,
                UpdateDateTime = position.Coordinate.Timestamp.DateTime
            };
        }
    }
}
