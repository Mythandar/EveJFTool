using EveJFTool.Core.Models;

namespace EveJFTool.Core.Services;

public static class DistanceCalculator
{
    // CCP deliberately uses this game constant rather than the scientific SI light-year.
    public const double MetersPerLightYear = 9_460_000_000_000_000d;

    public static double InLightYears(SolarSystem from, SolarSystem to)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);

        var dx = from.X - to.X;
        var dy = from.Y - to.Y;
        var dz = from.Z - to.Z;
        return Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz)) / MetersPerLightYear;
    }
}
