using System.Text.Json.Serialization;
using System.Globalization;

namespace PhotoOrganizer;

public class GeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly List<LocationCacheItem> _locationCache = new();
    private const double CacheDistanceKm = 10.0;

    public GeocodingService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "PhotoOrganizer/1.0");
        _httpClient.DefaultRequestHeaders.Referrer = new Uri("https://github.com/cassiocmps/Photo-Organizer");
    }

    public async Task<string> GetCityNameAsync(double latitude, double longitude)
    {
        var cachedLocation = FindInCache(latitude, longitude);
        if (cachedLocation != null)
        {
            return cachedLocation.CityName;
        }

        var cityName = await FetchCityFromApiAsync(latitude, longitude);
        _locationCache.Add(new LocationCacheItem(latitude, longitude, cityName));
        
        return cityName;
    }

    private LocationCacheItem? FindInCache(double latitude, double longitude)
    {
        foreach (var item in _locationCache)
        {
            var distance = CalculateHaversineDistance(latitude, longitude, item.Latitude, item.Longitude);
            if (distance < CacheDistanceKm)
            {
                return item;
            }
        }
        return null;
    }

    private async Task<string> FetchCityFromApiAsync(double latitude, double longitude)
    {
        try
        {
            var url = $"https://nominatim.openstreetmap.org/reverse?lat={latitude.ToString("F6", CultureInfo.InvariantCulture)}&lon={longitude.ToString("F6", CultureInfo.InvariantCulture)}&format=json&addressdetails=1";
            
            await Task.Delay(1000);
            
            var response = await _httpClient.GetStringAsync(url);
            var result = System.Text.Json.JsonSerializer.Deserialize<NominatimResponse>(response);

            var city = result?.Address?.City 
                       ?? result?.Address?.Town 
                       ?? result?.Address?.Village 
                       ?? result?.Address?.Municipality
                       ?? result?.Address?.County
                       ?? "Unknown Location";

            return city;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error geocoding ({latitude}, {longitude}): {ex.Message}");
            return "Unknown Location";
        }
    }

    private static double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        
        return R * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

    private class NominatimResponse
    {
        [JsonPropertyName("address")]
        public AddressInfo? Address { get; set; }
    }

    private class AddressInfo
    {
        [JsonPropertyName("city")]
        public string? City { get; set; }
        
        [JsonPropertyName("town")]
        public string? Town { get; set; }
        
        [JsonPropertyName("village")]
        public string? Village { get; set; }
        
        [JsonPropertyName("municipality")]
        public string? Municipality { get; set; }
        
        [JsonPropertyName("county")]
        public string? County { get; set; }
    }
}
