namespace BetterGeolocator
{
    public enum GeolocationStatus
    {
        /// <summary>
        /// Retrieve location data successfully.
        /// </summary>
        Successful,

        /// <summary>
        /// Time out without retrieve any location.
        /// </summary>
        Timeout,

        /// <summary>
        /// Cancel by user / system.
        /// </summary>
        Cancel,

        /// <summary>
        /// Lack of permission to get location data.
        /// </summary>
        NoPermission,

        /// <summary>
        /// GPS feature is not enabled on the device.
        /// </summary>
        FeatureNotEnabled,

        /// <summary>
        /// GPS feature is not found on the devices.
        /// Who the hell still carrying such device?
        /// </summary>
        FeatureNotSupported,

        /// <summary>
        /// Error happen on setup such as no context provided on Android platform.
        /// </summary>
        SetupError,

        /// <summary>
        /// General error happed.
        /// </summary>
        Error
    }
}
