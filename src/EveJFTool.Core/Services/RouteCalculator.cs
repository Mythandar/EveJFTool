using EveJFTool.Core.Interfaces;
using EveJFTool.Core.Models;

namespace EveJFTool.Core.Services;

public sealed class RouteCalculator(IUniverseRepository universe)
{
    public RouteCalculationResult Calculate(
        IReadOnlyList<RouteLegRequest> requests,
        ShipDefinition ship,
        SkillProfile skills,
        decimal? isotopePrice)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(ship);
        ArgumentNullException.ThrowIfNull(skills);

        var results = requests.Select(request => CalculateLeg(request, ship, skills, isotopePrice)).ToArray();
        var fuelByType = results
            .Where(leg => leg.FuelUsed > 0)
            .GroupBy(leg => leg.FuelType)
            .ToDictionary(group => group.Key, group => group.Sum(leg => leg.FuelUsed));

        var totals = new RouteTotals(
            results.Count(leg => leg.Kind == LegKind.Jump),
            results.Count(leg => leg.Kind == LegKind.Gate),
            results.Where(leg => leg.Kind == LegKind.Jump).Sum(leg => leg.DistanceLightYears),
            fuelByType,
            results.Sum(leg => leg.IskCost ?? 0),
            results.Where(leg => leg.Kind == LegKind.Jump && leg.FuelUsed > 0)
                .All(leg => leg.FuelPrice.HasValue));

        return new RouteCalculationResult(results, totals);
    }

    private RouteLegResult CalculateLeg(
        RouteLegRequest request,
        ShipDefinition ship,
        SkillProfile skills,
        decimal? isotopePrice)
    {
        if (!universe.TryGetSystem(request.FromSystem, out var from))
        {
            return Invalid(request, $"Unknown system: {request.FromSystem}", ship.IsotopeName);
        }

        if (!universe.TryGetSystem(request.ToSystem, out var to))
        {
            return Invalid(request, $"Unknown system: {request.ToSystem}", ship.IsotopeName);
        }

        if (from!.Id == to!.Id)
        {
            return Invalid(request, "Consecutive systems must be different", ship.IsotopeName);
        }

        if (request.Kind == LegKind.Gate)
        {
            return new(request.FromSystem, request.ToSystem, request.Kind, 0, 0, ship.IsotopeName, isotopePrice, 0, true, "Gate");
        }

        var distance = DistanceCalculator.InLightYears(from, to);
        var maximumRange = FuelCalculator.MaximumRange(ship, skills);
        if (distance > maximumRange + 1e-9)
        {
            return new(
                request.FromSystem,
                request.ToSystem,
                request.Kind,
                distance,
                0,
                ship.IsotopeName,
                isotopePrice,
                0,
                false,
                $"OUT OF RANGE ({distance:N2} > {maximumRange:N2} LY)");
        }

        var fuel = FuelCalculator.IsotopesForJump(ship, skills, distance, request.Economizers);
        decimal? cost = isotopePrice.HasValue ? fuel * isotopePrice.Value : null;
        var status = isotopePrice.HasValue ? "OK" : "Price unavailable";
        return new(
            request.FromSystem,
            request.ToSystem,
            request.Kind,
            distance,
            fuel,
            ship.IsotopeName,
            isotopePrice,
            cost,
            true,
            status);
    }

    private static RouteLegResult Invalid(RouteLegRequest request, string status, string fuelType) =>
        new(request.FromSystem, request.ToSystem, request.Kind, 0, 0, fuelType, null, null, false, status);
}
