using System;
using System.Threading.Tasks;

namespace BetterGeolocator.Core.Sample
{
    public static class GeolocatorManager
    {
        public static async Task<Geolocation> GetLocation()
        {
            var locator = new Geolocator();
            return await locator.GetLocation(TimeSpan.FromSeconds(10));
        }
    }
}
