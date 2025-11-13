namespace CheapHelpers.Services.Geocoding.Models;

/// <summary>
/// Represents a geographic coordinate with latitude and longitude
/// </summary>
public record GeoCoordinate(double Latitude, double Longitude)
{
    /// <summary>
    /// Validates if the coordinate values are within valid ranges
    /// </summary>
    /// <returns>True if latitude is between -90 and 90, and longitude is between -180 and 180</returns>
    public bool IsValid() =>
        Latitude is >= -90 and <= 90 &&
        Longitude is >= -180 and <= 180;

    /// <summary>
    /// Returns a string representation of the coordinate
    /// </summary>
    public override string ToString() => $"{Latitude},{Longitude}";
}
