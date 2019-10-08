namespace BetterGeolocator
{
    /// <summary>
    /// A coordinate / location class that can be use in cross platforms.
    /// </summary>
    public class Geocoordinate
    {
        /// <summary>
        /// Location's longitude.
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Location's latitude.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Location's altitude.
        /// </summary>
        public double Altitude { get; set; }

        /// <summary>
        /// Location's accuracy.
        /// </summary>
        public double Accuracy { get; set; }

        /// <summary>
        /// Copy itself to a new Geo coordinate object.
        /// </summary>
        /// <returns>New Geo coordinate object.</returns>
        public Geocoordinate ToNewGeolocation()
        {
            return new Geocoordinate()
            {
                Longitude = Longitude,
                Latitude = Latitude,
                Altitude = Altitude,
                Accuracy = Accuracy
            };
        }
    }
}
