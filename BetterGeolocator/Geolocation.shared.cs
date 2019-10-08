using System;
using System.Text;

namespace BetterGeolocator
{
    /// <summary>
    /// A location class with status and timestamps that can be use in cross platforms.
    /// </summary>
    public class Geolocation
    {
        /// <summary>
        /// Location's coordinate which include accuracy.
        /// </summary>
        public Geocoordinate Coordinate { get; set; }

        /// <summary>
        /// The date time where the location is retrieved.
        /// </summary>
        public DateTime? UpdateDateTime { get; set; }

        /// <summary>
        /// Location's status, or error status.
        /// </summary>
        public GeolocationStatus Status { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Geolocation()
        {
        }

        /// <summary>
        /// Default constructor with status.
        /// </summary>
        public Geolocation(GeolocationStatus status)
        {
            Status = status;
        }

        /// <summary>
        /// Output a simple string representing the state of the location.
        /// </summary>
        public override string ToString()
        {
            var locationStringBuilder = new StringBuilder();
            locationStringBuilder.AppendLine($"Status: {Enum.GetName(typeof(GeolocationStatus), Status)}");
            locationStringBuilder.AppendLine($"Longitude: {Coordinate?.Longitude}");
            locationStringBuilder.AppendLine($"Latitude: {Coordinate?.Latitude}");
            locationStringBuilder.AppendLine($"Altitude: {Coordinate?.Altitude}");
            locationStringBuilder.AppendLine($"Accuracy: {Coordinate?.Accuracy}");
            locationStringBuilder.AppendLine($"UpdateDateTime: {UpdateDateTime}");
            return locationStringBuilder.ToString();
        }

        /// <summary>
        /// Is location's longitude and latitude available?
        /// </summary>
        /// <returns>True if location's longitude and latitude is available, else false.</returns>
        public bool IsLocationAvailable()
        {
            return Coordinate != null;
        }

        /// <summary>
        /// Is location fresh in term of the time retrieved and acceptable?
        /// </summary>
        /// <param name="freshness">Time in milliseconds</param>
        /// <returns>True if the location is fresh, else false.</returns>
        public bool IsFresh(double freshness)
        {
            return UpdateDateTime.HasValue &&
                DateTime.Now.Subtract(UpdateDateTime.Value).TotalMilliseconds <= freshness;
        }

        /// <summary>
        /// Is location accuracy is acceptable.
        /// </summary>
        /// <param name="accuracy">Accuracy in meters</param>
        /// <returns>True if the location is accurate.</returns>
        public bool IsAccurate(double accuracy)
        {
            return Coordinate != null &&
                Coordinate.Accuracy <= accuracy;
        }

        /// <summary>
        /// Copy itself to a new Geo location object.
        /// </summary>
        /// <returns>New Geo location object.</returns>
        public Geolocation ToNewGeolocation(GeolocationStatus? newStatus = null)
        {
            return new Geolocation()
            {
                Coordinate = Coordinate?.ToNewGeolocation(),
                UpdateDateTime = UpdateDateTime,
                Status = newStatus ?? Status
            };
        }
    }
}
