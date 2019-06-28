using System;
using System.Threading;
using System.Threading.Tasks;

namespace BetterGeolocator
{
    public partial class Geolocator
    {
        /// <summary>
        /// Time to consider a location with less accurate threshold as an acceptable location.
        /// This happen if device no longer can retrieve a location with under the accuracy threshold.
        /// This is in milliseconds.
        /// </summary>
        private const double FreshnessThreshold = 10000;

        /// <summary>
        /// The default location freshness or time retrieved that should consider acceptable as current location.
        /// This is in milliseconds.
        /// </summary>
        private const double DefaultTargetFreshness = 60000;

        /// <summary>
        /// The location freshness or time retrieved that should consider acceptable as current location.
        /// This is in milliseconds.
        /// </summary>
        private double TargetFreshness { get; set; }

        /// <summary>
        /// Accuracy delta threshold to be consider.
        /// If the accuracy is larger than this, it will not be considered.
        /// This is in meters.
        /// </summary>
        private const double AccuracyThreshold = 200;

        /// <summary>
        /// The default accuracy that the get location will stop.
        /// This is in meters.
        /// </summary>
        private const double DefaultTargetAccuracy = 100;

        /// <summary>
        /// The accuracy of the location that user can accept.
        /// This is in meters.
        /// </summary>
        private double TargetAccuracy { get; set; }

        /// <summary>
        /// Last known device location.
        /// </summary>
        private Geolocation LastKnownLocation { get; set; }

        /// <summary>
        /// Use this to convert location retrieve into async function.
        /// </summary>
        private TaskCompletionSource<Geolocation> LocationTaskCompletionSource { get; set; }

        /// <summary>
        /// Countdown timer that automatically disable location service when the job is done.
        /// </summary>
        private Timer LocationRetrieveTimer { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Geolocator()
        {
        }

        /// <summary>
        /// Retrieve the best / most accurate location of current device within the given time.
        /// If desired accuracy location is found, the function will return that immediately.
        /// </summary>
        /// <param name="timeout">The maximum time that the retrieve should spend.</param>
        /// <param name="targetAccuracy">The expected accuracy of the location. Distance in meters.</param>
        /// <param name="targetFreshness">The time to consider the location as fresh. Time in milliseconds.</param>
        /// <param name="cancellationToken">Cancellation token, to stop the location request.</param>
        /// <returns>Best / most accurate location of the device in the time given.</returns>
        public Task<Geolocation> GetLocation(TimeSpan timeout, double targetAccuracy = DefaultTargetAccuracy, double targetFreshness = DefaultTargetFreshness, CancellationToken? cancellationToken = null)
        {
#if NETSTANDARD
            return Task.FromResult((Geolocation)null);
#else
            TargetAccuracy = targetAccuracy;
            TargetFreshness = targetFreshness;

            // Check if we already have the location before we start anything
            if (IsLastKnownLocationFreshAndAccurate())
            {
                // Good enough
                return Task.FromResult(LastKnownLocation);
            }
            else
            {
                // If cancellation token is provided, register it
                if (cancellationToken.HasValue)
                {
                    cancellationToken.Value.Register(() => StopLocationUpdate(GeolocationStatus.Cancel));
                }

                // Start a timer that to make sure everything return in the specified time given
                var milliseconds = (int)timeout.TotalMilliseconds;
                LocationRetrieveTimer = new Timer(LocationRetrieveTimerTimeout, null, milliseconds, Timeout.Infinite);

                // Convert this into an async function call
                LocationTaskCompletionSource = new TaskCompletionSource<Geolocation>();

                // Begin to retrieve location data
                StartLocationUpdate();

                return LocationTaskCompletionSource.Task;
            }
#endif
        }

        /// <summary>
        /// Start to retrieve current device location without blocking the thread.
        /// Once it found an acceptable accurate location, it will stop.
        /// This can allow retrieve location much faster later if a location already available.
        /// </summary>
        /// <param name="targetAccuracy">The expected accuracy of the location. Distance in meters.</param>
        /// <param name="cancellationToken">Cancellation token, to stop the location.</param>
        public void StartCacheLocation(double targetAccuracy = DefaultTargetAccuracy, CancellationToken? cancellationToken = null)
        {
#if NETSTANDARD
            // Nothing here
#else
            TargetAccuracy = targetAccuracy;
            TargetFreshness = DefaultTargetFreshness;

            // If cancellation token is provided, register it
            if (cancellationToken.HasValue)
            {
                cancellationToken.Value.Register(() => StopLocationUpdate(GeolocationStatus.Cancel));
            }

            // Begin to retrieve location data
            StartLocationUpdate();
#endif
        }

        /// <summary>
        /// Clear / remove local cached last known location.
        /// </summary>
        public void Clear()
        {
            LastKnownLocation = null;
        }

        /// <summary>
        /// Check if the retrieve last known location is still consider fresh and accurate.
        /// </summary>
        private bool IsLastKnownLocationFreshAndAccurate()
        {
            return LastKnownLocation != null &&
                LastKnownLocation.IsFresh(TargetFreshness) &&
                LastKnownLocation.IsAccurate(TargetAccuracy);
        }

        /// <summary>
        /// Check if the retrieve last known location is still consider fresh and but ignore the accurate.
        /// </summary>
        private bool IsLastKnownLocationFresh()
        {
            return LastKnownLocation != null &&
                LastKnownLocation.IsFresh(TargetFreshness);
        }
    }
}
