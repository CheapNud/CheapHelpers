namespace CheapHelpers.Extensions;

/// <summary>
/// Extension methods for common unit conversions (power, energy, volume).
/// </summary>
public static class UnitConversionExtensions
{
    // Power: Watts ↔ Kilowatts

    public static double WattsToKilowatts(this double watts) => watts / 1000.0;
    public static double KilowattsToWatts(this double kilowatts) => kilowatts * 1000.0;
    public static decimal WattsToKilowatts(this decimal watts) => watts / 1000m;
    public static decimal KilowattsToWatts(this decimal kilowatts) => kilowatts * 1000m;

    // Energy: Wh ↔ kWh

    public static double WhToKwh(this double wattHours) => wattHours / 1000.0;
    public static double KwhToWh(this double kilowattHours) => kilowattHours * 1000.0;
    public static decimal WhToKwh(this decimal wattHours) => wattHours / 1000m;
    public static decimal KwhToWh(this decimal kilowattHours) => kilowattHours * 1000m;

    // Volume: m³ ↔ Liters

    public static double CubicMetersToLiters(this double cubicMeters) => cubicMeters * 1000.0;
    public static double LitersToCubicMeters(this double liters) => liters / 1000.0;
    public static decimal CubicMetersToLiters(this decimal cubicMeters) => cubicMeters * 1000m;
    public static decimal LitersToCubicMeters(this decimal liters) => liters / 1000m;
}
