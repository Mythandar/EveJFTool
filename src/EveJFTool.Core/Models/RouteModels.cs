namespace EveJFTool.Core.Models;

public enum LegKind
{
    Jump,
    Gate
}

public sealed record RouteLegRequest(
    string FromSystem,
    string ToSystem,
    LegKind Kind,
    EconomizerLoadout Economizers);

public sealed record RouteLegResult(
    string FromSystem,
    string ToSystem,
    LegKind Kind,
    double DistanceLightYears,
    long FuelUsed,
    string FuelType,
    decimal? FuelPrice,
    decimal? IskCost,
    bool IsValid,
    string Status);

public sealed record RouteTotals(
    int JumpLegs,
    int GateLegs,
    double TotalJumpDistanceLightYears,
    IReadOnlyDictionary<string, long> FuelByType,
    decimal TotalIskCost,
    bool HasCompletePricing);

public sealed record RouteCalculationResult(IReadOnlyList<RouteLegResult> Legs, RouteTotals Totals);
