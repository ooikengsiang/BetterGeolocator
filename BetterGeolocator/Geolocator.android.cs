using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Common;
using Android.Gms.Location;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using System;

namespace BetterGeolocator
{
    public partial class Geolocator : Java.Lang.Object, Android.Locations.ILocationListener
    {
        /// <summary>
        /// Reference to Google Map fused location provider.
        /// </summary>
        private FusedLocationProviderClient FusedLocationClient;

        /// <summary>
        /// Callback class that will pass the location from fused location provided.
        /// </summary>
        private LocationCallback FusedLocationCallback;

        /// <summary>
        /// Reference to Android location manager.
        /// </summary>
        private LocationManager LocationManager { get; set; }

        /// <summary>
        /// Start listen to location update / gather device location.
        /// </summary>
        private async void StartLocationUpdate()
        {
            // Don't start another location manager if there is one already running
            if (FusedLocationClient == null ||
                LocationManager == null)
            {
                var isRequestLocationSuccessful = false;
                var isLastKnownLocationUseable = false;

                // Check permission
                if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(Application.Context, Manifest.Permission.AccessFineLocation) == Permission.Granted)
                {
                    // Check if Google Play service is available, if it is available, then we can only use fused location service
                    if (GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(Application.Context) == ConnectionResult.Success)
                    {
                        try
                        {
                            FusedLocationClient = LocationServices.GetFusedLocationProviderClient(Application.Context);
                            if (FusedLocationClient != null)
                            {
                                // Try get the last known location or current device location
                                if (!UpdateLocation(await FusedLocationClient.GetLastLocationAsync()))
                                {
                                    // Then request the location update
                                    var locationRequest = new LocationRequest()
                                        .SetPriority(LocationRequest.PriorityHighAccuracy)
                                        .SetInterval(5000) // Google document mention 5 seconds
                                        .SetFastestInterval(0);
                                    FusedLocationCallback = new LocationCallback();
                                    FusedLocationCallback.LocationResult += FusedLocationCallback_LocationResult;
                                    await FusedLocationClient.RequestLocationUpdatesAsync(locationRequest, FusedLocationCallback);
                                }
                                else
                                {
                                    // No need to get next provider for location since we already have the location
                                    isLastKnownLocationUseable = true;
                                }

                                isRequestLocationSuccessful = true;
                            }
                        }
                        catch
                        {
                            // Ignore all error including lack of permission or provider is not enabled
                        }
                    }

                    // In some event that Google Play service is not available, fall back to Android location instead
                    if (!isRequestLocationSuccessful)
                    {
                        // Acquire a reference to the system Location Manager
                        LocationManager = (LocationManager)Application.Context.GetSystemService(Context.LocationService);
                        if (LocationManager != null &&
                            LocationManager.AllProviders != null)
                        {
                            // Get location from all provider to increase the chances of getting a location
                            foreach (var provider in LocationManager.AllProviders)
                            {
                                try
                                {
                                    // Try get the last known location or current device location
                                    if (!UpdateLocation(LocationManager.GetLastKnownLocation(provider)))
                                    {
                                        // Then request the location update
                                        LocationManager.RequestLocationUpdates(provider, 0, 0f, this);

                                        isRequestLocationSuccessful = true;
                                    }
                                    else
                                    {
                                        // No need to get next provider for location since we already have the location
                                        isRequestLocationSuccessful = true;
                                        isLastKnownLocationUseable = true;
                                        break;
                                    }
                                }
                                catch
                                {
                                    // Ignore all error including lack of permission or provider is not enabled
                                }
                            }
                        }
                    }
                }

                // Stop location service if all failed
                if (!isRequestLocationSuccessful)
                {
                    StopLocationUpdate(isLastKnownLocationUseable ? 
                        GeolocationStatus.Successful :
                        GeolocationStatus.SetupError);
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

            if (FusedLocationClient != null)
            {
                if (FusedLocationCallback != null)
                {
                    FusedLocationCallback.LocationResult -= FusedLocationCallback_LocationResult;
                    FusedLocationClient.RemoveLocationUpdates(FusedLocationCallback);
                    FusedLocationCallback = null;
                }
                FusedLocationClient = null;
            }

            if (LocationManager != null)
            {
                // Stop listen to location changes
                LocationManager.RemoveUpdates(this);

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
        /// Fused location provider found one / multiple location point.
        /// </summary>
        private void FusedLocationCallback_LocationResult(object sender, LocationCallbackResultEventArgs e)
        {
            if (e.Result.Locations != null)
            {
                foreach (var location in e.Result.Locations)
                {
                    OnLocationChanged(location);
                }
            }
        }

        /// <summary>
        /// Location manager found a location point.
        /// </summary>
        public void OnLocationChanged(Location location)
        {
            UpdateLocation(location);
        }

        /// <summary>
        /// Update last known location and see if it is an acceptable location.
        /// If yes, stop updating the location any further.
        /// </summary>
        private bool UpdateLocation(Location location)
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
                    var locationDateTime = ConvertLocationTimeToDateTime(location.Time);
                    var timeDelta = locationDateTime - LastKnownLocation.UpdateDateTime.Value;
                    var isSignificantlyNewer = timeDelta.TotalMilliseconds > FreshnessThreshold;

                    // Check whether the new location is more or less accurate than saved location
                    // Less is better
                    var accuracyDelta = location.Accuracy - LastKnownLocation.Accuracy;
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
        /// Provided is disabled.
        public void OnProviderDisabled(string provider)
        {
            // Nothing here
        }

        /// <summary>
        /// Provided is enabled.
        /// </summary>
        public void OnProviderEnabled(string provider)
        {
            // Nothing here
        }

        /// <summary>
        /// Status changed.
        /// </summary>
        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {
            // Nothing here
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
        /// Convert Android location given time into .NET date and time.
        /// </summary>
        private DateTime ConvertLocationTimeToDateTime(long time)
        {
            // Add the location time from 1970 January 1st
            var startDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            startDateTime = startDateTime.ToLocalTime().AddMilliseconds(time);
            return startDateTime;
        }

        /// <summary>
        /// Convert Android location to Geo Location that we are using in the library.
        /// </summary>
        private Geolocation ConvertLocationToGeolocation(Location location)
        {
            return new Geolocation()
            {
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                Altitude = location.Altitude,
                Accuracy = location.Accuracy,
                UpdateDateTime = ConvertLocationTimeToDateTime(location.Time)
            };
        }
    }
}
