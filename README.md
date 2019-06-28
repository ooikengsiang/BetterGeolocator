# Better Geolocator
Better Geolocator is a cross-platforms location library written in C# that focus on getting a useable location as quickly as possible with the least amount of code. In the event when the latest high accurate location is not available, the next best location will be return instead. Thus allow application to continue to work if needed. For example, when user is inside a building.

In Android implementation, the library uses multiple providers to achieve a better result. The library uses Fused Location Provider from Google Play Service because of the superior performance over Android's Location Manager. If Google Play service is not available, Android's Location Manager will use used automatically instead. For iOS implementation, it shares the same API call just like Android and it retrieve location from both network and GPS to get the best result.

To make retrieve the location at instant speed for user, application can start retrieve the location when the activity / view has loaded using StartCacheLocation then only call GetLocation when user interact with the application such as clicking on button. For example, an application to allow user to check in at a location, the application can start loading the location using StartCacheLocation when the view activity / view is loaded, then only call GetLocation when user confirm they want to check in at the location.

# Installation
Better Geolocator and it's dependecy is required on all projects. This include your core / standard / shared project and also your platform specified project such as Android or iOS.

# Device Permission
This library doesn't handle device permission request. This mean the application must request for the location permission first before calling the library.

## Android Permission
ACCESS_FINE_LOCATION in Android manifest is required. ACCESS_COARSE_LOCATION is not required because ACCESS_FINE_LOCATION will include ACCESS_COARSE_LOCATION automatically.

## iOS Permission
NSLocationWhenInUseUsageDescription key or NSLocationAlwaysAndWhenInUseUsageDescription key must be created in Info.plist together with the description.

# Sample
## Android Sample
```C#
private async void GetDeciceCurrentLocation()
{
    // TODO: Check if we have the permission to access the location

    // Retrieve device current location with 30 seconds timeout and 200 meters accuracy
    var geolocator = new Geolocator(this);
    var location = await geolocator.GetLocation(TimeSpan.FromSeconds(30), 200);
    if (location.Status == GeolocationStatus.Successful)
    {
        // Get device location successful with the accuracy we want
    } 
    else if (location.Status == GeolocationStatus.Timeout)
    {
        // Timeout happen before we can get the location successfully
        // We can still check if any location was return but not as accurate as we requested
        if (location.IsLocationAvailable())
        {
            // Found a less accurate location
        }
    }
}
```

## iOS Sample
```C#
private async void GetDeciceCurrentLocation()
{
    // TODO: Check if we have the permission to access the location

    // Retrieve device current location with 30 seconds timeout and 200 meters accuracy
    var geolocator = new Geolocator();
    var location = await geolocator.GetLocation(TimeSpan.FromSeconds(30), 200);
    if (location.Status == GeolocationStatus.Successful)
    {
        // Get device location successful with the accuracy we want
    } 
    else if (location.Status == GeolocationStatus.Timeout)
    {
        // Timeout happen before we can get the location successfully
        // We can still check if any location was return but not as accurate as we requested
        if (location.IsLocationAvailable())
        {
            // Found a less accurate location
        }
    }
}
```