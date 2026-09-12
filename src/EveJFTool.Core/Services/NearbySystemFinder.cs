using EveJFTool.Core.Interfaces;
using EveJFTool.Core.Models;

namespace EveJFTool.Core.Services;

public sealed record NearbySystem(string Name, double DistanceLightYears, double SecurityStatus);

/// <summary>Geometric jump candidates, not a cyno or route availability guarantee.</summary>
public sealed class NearbySystemFinder(IUniverseRepository universe)
{
    public IReadOnlyList<NearbySystem> Find(string originName, ShipDefinition ship, SkillProfile skills)
    {
        if (!universe.TryGetSystem(originName, out var origin) || origin is null)
            throw new ArgumentException($"Unknown solar system: {originName}");
        if (!HasCoordinates(origin))
            throw new ArgumentException($"Missing coordinates for {originName}");
        if (!ship.CanEnterHighSecurity && origin.SecurityStatus >= .45)
            throw new ArgumentException($"{ship.Name} cannot travel through highsec.");

        var range = FuelCalculator.MaximumRange(ship, skills);
        var matches = new List<NearbySystem>();
        foreach (var name in universe.SystemNames)
        {
            if (!universe.TryGetSystem(name, out var target) || target is null
                || target.Id == origin.Id || target.SecurityStatus >= .45
                || target.RegionId == 10_000_070 || !HasCoordinates(target)) continue;
            var distance = DistanceCalculator.InLightYears(origin, target);
            if (distance > 0 && distance <= range + 1e-9)
                matches.Add(new(target.Name, distance, target.SecurityStatus));
        }
        return matches.OrderBy(system => system.DistanceLightYears)
            .ThenBy(system => system.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static bool HasCoordinates(SolarSystem system) =>
        double.IsFinite(system.X) && double.IsFinite(system.Y) && double.IsFinite(system.Z);
}
