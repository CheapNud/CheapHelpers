namespace CheapHelpers.Services.Geocoding.Models;

/// <summary>
/// Represents a rectangular bounding box defined by minimum and maximum coordinates
/// </summary>
public class BoundingBox
{
    /// <summary>
    /// Minimum (southwest) latitude
    /// </summary>
    public double MinLatitude { get; init; }

    /// <summary>
    /// Minimum (southwest) longitude
    /// </summary>
    public double MinLongitude { get; init; }

    /// <summary>
    /// Maximum (northeast) latitude
    /// </summary>
    public double MaxLatitude { get; init; }

    /// <summary>
    /// Maximum (northeast) longitude
    /// </summary>
    public double MaxLongitude { get; init; }

    /// <summary>
    /// Creates a bounding box from two corner coordinates
    /// </summary>
    public static BoundingBox FromCorners(GeoCoordinate southwest, GeoCoordinate northeast) =>
        new()
        {
            MinLatitude = southwest.Latitude,
            MinLongitude = southwest.Longitude,
            MaxLatitude = northeast.Latitude,
            MaxLongitude = northeast.Longitude
        };
}
