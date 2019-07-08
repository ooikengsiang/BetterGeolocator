using CoreLocation;
using Foundation;
using System;

namespace BetterGeolocator
{
    public partial class Geolocator
    {
        /// <summary>
        /// Reference to iOS location manager.
        /// </summary>
        private CLLocationManager LocationManager { get; set; }

        /// <summary>
        /// Check if location permission is already granted.
        /// </summary>
        /// <returns>Return true if location permission is granted, else false.</returns>
        private bool IsPermissionGrantedImpl()
        {
            return CLLocationManager.Status == CLAuthorizationStatus.AuthorizedAlways ||
                CLLocationManager.Status == CLAuthorizationStatus.AuthorizedWhenInUse;
        }

        /// <summary>
        /// Start listen to location update / gather device location.
        /// </summary>
        private void StartLocationUpdate()
        {
            // Don't start another location manager if there is one already running
            if (LocationManager == null)
            {
                var isRequestLocationSuccessful = false;
                var isLastKnownLocationUseable = false;

                // Check permission
                if (IsPermissionGrantedImpl())
                {
                    // Acquire a reference to the system Location Manager
                    LocationManager = new CLLocationManager();
                    if (LocationManager != null)
                    {
                        try
                        {
                            // Try get the last known location or current device location
                            if (!UpdateLocation(LocationManager.Location))
                            {
                                // Request location
                                LocationManager.LocationsUpdated += LocationManager_LocationsUpdated;
                                LocationManager.StartUpdatingLocation();
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
                    }
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
            if (LocationRetrieveTimer != null)
            {
                // Destroy the timer
                LocationRetrieveTimer.Dispose();
                LocationRetrieveTimer = null;
            }

            if (LocationManager != null)
            {
                // Stop listen to location changes
                LocationManager.StopUpdatingLocation();
                LocationManager.LocationsUpdated -= LocationManager_LocationsUpdated;

                // Remove the reference
                LocationManager = null;
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
        /// Location manager found location points, might be multiple.
        /// </summary>
        private void LocationManager_LocationsUpdated(object sender, CLLocationsUpdatedEventArgs e)
        {
            if (e != null &&
                e.Locations != null)
            {
                foreach (var location in e.Locations)
                {
                    if (UpdateLocation(location))
                    {
                        // No need to get next location since we already have the location
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Update last known location and see if it is an acceptable location.
        /// If yes, stop updating the location any further.
        /// </summary>
        public bool UpdateLocation(CLLocation location)
        {
            bool isLocationAcceptable = false;

            if (location != null)
            {
                if (LastKnownLocation == null ||
                    !LastKnownLocation.UpdateDateTime.HasValue)
                {
                    // No location saved so far, accept any new location
                    LastKnownLocation = ConvertLocationToGeolocation(location);
                }
                else
                {
                    // Check whether the new location is newer or older
                    var locationDateTime = ConvertLocationTimeToDateTime(location.Timestamp);
                    var timeDelta = locationDateTime - LastKnownLocation.UpdateDateTime.Value;
                    var isSignificantlyNewer = timeDelta.TotalMilliseconds > FreshnessThreshold;

                    // Check whether the new location is more or less accurate than saved location
                    // Less is better
                    var accuracyDelta = ((location.HorizontalAccuracy + location.VerticalAccuracy) / 2) - LastKnownLocation.Accuracy;
                    var isMoreOrSameAccurate = accuracyDelta <= 0;
                    var isSignificantlyLessAccurate = accuracyDelta > AccuracyThreshold;

                    // Determine location quality using a combination of timeliness and accuracy
                    if (isMoreOrSameAccurate ||
                        (isSignificantlyNewer && !isSignificantlyLessAccurate))
                    {
                        // Accept this newer / more accurate location
                        LastKnownLocation = ConvertLocationToGeolocation(location);
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
        /// Convert iOS location given time into .NET date and time.
        /// </summary>
        private DateTime ConvertLocationTimeToDateTime(NSDate date)
        {
            // Add the location time from 2001 January 1st
            var startDateTime = new DateTime(2001, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            startDateTime = startDateTime.AddSeconds(date.SecondsSinceReferenceDate).ToLocalTime();
            return startDateTime;
        }

        /// <summary>
        /// Convert iOS location to Geo Location that we are using in the library.
        /// </summary>
        private Geolocation ConvertLocationToGeolocation(CLLocation location)
        {
            return new Geolocation()
            {
                Latitude = location.Coordinate.Longitude,
                Longitude = location.Coordinate.Latitude,
                Altitude = location.Altitude,
                Accuracy = (location.HorizontalAccuracy + location.VerticalAccuracy) / 2,
                UpdateDateTime = ConvertLocationTimeToDateTime(location.Timestamp)
            };
        }
    }
}
