using EveJFTool.Core.Catalogs;
using EveJFTool.Core.Interfaces;
using EveJFTool.Core.Models;
using EveJFTool.Core.Services;

namespace EveJFTool.Tests;

public sealed class CalculationTests
{
    [Theory]
    [InlineData("Rhea", 17888, "Nitrogen Isotopes", 10000)]
    [InlineData("Anshar", 17887, "Oxygen Isotopes", 9400)]
    [InlineData("Ark", 16274, "Helium Isotopes", 8800)]
    [InlineData("Nomad", 17889, "Hydrogen Isotopes", 8200)]
    public void CurrentHullCatalog_MatchesCcpSde(string name, int isotopeTypeId, string isotopeName, double baseFuel)
    {
        var ship = JumpFreighterCatalog.GetByName(name);

        Assert.Equal(isotopeTypeId, ship.IsotopeTypeId);
        Assert.Equal(isotopeName, ship.IsotopeName);
        Assert.Equal(baseFuel, ship.BaseFuelPerLightYear);
        Assert.Equal(5, ship.BaseJumpRangeLightYears);
    }

    [Fact]
    public void LightYearConversion_UsesCcpGameConstant()
    {
        var origin = System("Origin", 0, 0, 0);
        var oneLightYearAway = System("Destination", DistanceCalculator.MetersPerLightYear, 0, 0);

        Assert.Equal(1d, DistanceCalculator.InLightYears(origin, oneLightYearAway), 12);
        Assert.Equal(9_460_000_000_000_000d, DistanceCalculator.MetersPerLightYear);
    }

    [Fact]
    public void CoordinateDistance_UsesAllThreeAxes()
    {
        var unit = DistanceCalculator.MetersPerLightYear;
        var origin = System("Origin", 0, 0, 0);
        var destination = System("Destination", 3 * unit, 4 * unit, 12 * unit);

        Assert.Equal(13d, DistanceCalculator.InLightYears(origin, destination), 12);
    }

    [Fact]
    public void JumpDriveCalibration_ChangesRangeButNotFuel()
    {
        var ship = JumpFreighterCatalog.GetByName("Rhea");
        var zero = new SkillProfile(0, 0, 0);
        var calibrated = new SkillProfile(5, 0, 0);

        Assert.Equal(5d, FuelCalculator.MaximumRange(ship, zero));
        Assert.Equal(10d, FuelCalculator.MaximumRange(ship, calibrated));
        Assert.Equal(
            FuelCalculator.IsotopesForJump(ship, zero, 2, EconomizerLoadout.None),
            FuelCalculator.IsotopesForJump(ship, calibrated, 2, EconomizerLoadout.None));
    }

    [Fact]
    public void FuelSkills_AreMultiplicativePostPercentModifiers()
    {
        var ship = JumpFreighterCatalog.GetByName("Rhea");

        Assert.Equal(20_000, FuelCalculator.IsotopesForJump(ship, new SkillProfile(0, 0, 0), 2, EconomizerLoadout.None));
        Assert.Equal(5_000, FuelCalculator.IsotopesForJump(ship, new SkillProfile(0, 5, 5), 2, EconomizerLoadout.None));
    }

    [Fact]
    public void Economizers_ApplyStrongestFirstWithStackingPenalty()
    {
        var loadout = new EconomizerLoadout(
        [
            EconomizerCatalog.Limited,
            EconomizerCatalog.Prototype,
            EconomizerCatalog.Experimental
        ]);
        var expected = (1 - 0.10) *
                       (1 - (0.07 * FuelCalculator.StackingPenalties[1])) *
                       (1 - (0.04 * FuelCalculator.StackingPenalties[2]));

        Assert.Equal(expected, FuelCalculator.EconomizerMultiplier(loadout), 12);
    }

    [Fact]
    public void FuelConsumption_RoundsUpOnlyAtFinalWholeUnit()
    {
        var ship = JumpFreighterCatalog.GetByName("Rhea");
        var skills = new SkillProfile(5, 5, 5);

        Assert.Equal(24_841, FuelCalculator.IsotopesForJump(ship, skills, 9.93631517031794, EconomizerLoadout.None));
    }

    [Fact]
    public void GateLeg_HasZeroDistanceFuelAndCost()
    {
        var universe = new MemoryUniverse(
            System("A", 0, 0, 0),
            System("B", DistanceCalculator.MetersPerLightYear, 0, 0));
        var calculator = new RouteCalculator(universe);

        var result = calculator.Calculate(
            [new RouteLegRequest("A", "B", LegKind.Gate, new EconomizerLoadout([EconomizerCatalog.Prototype]))],
            JumpFreighterCatalog.GetByName("Rhea"),
            new SkillProfile(5, 5, 5),
            1_000m);

        var leg = Assert.Single(result.Legs);
        Assert.Equal(0, leg.DistanceLightYears);
        Assert.Equal(0, leg.FuelUsed);
        Assert.Equal(0m, leg.IskCost);
        Assert.Equal(1, result.Totals.GateLegs);
    }

    [Fact]
    public void RouteTotals_SumJumpLegsAndIgnoreGateFuel()
    {
        var unit = DistanceCalculator.MetersPerLightYear;
        var universe = new MemoryUniverse(
            System("A", 0, 0, 0),
            System("B", unit, 0, 0),
            System("C", 2 * unit, 0, 0),
            System("D", 3 * unit, 0, 0));
        var calculator = new RouteCalculator(universe);
        var ship = JumpFreighterCatalog.GetByName("Rhea");

        var result = calculator.Calculate(
        [
            new("A", "B", LegKind.Jump, EconomizerLoadout.None),
            new("B", "C", LegKind.Jump, new EconomizerLoadout([EconomizerCatalog.Prototype])),
            new("C", "D", LegKind.Gate, EconomizerLoadout.None)
        ], ship, new SkillProfile(5, 5, 5), 1_000m);

        Assert.Equal(2, result.Totals.JumpLegs);
        Assert.Equal(1, result.Totals.GateLegs);
        Assert.Equal(2d, result.Totals.TotalJumpDistanceLightYears, 12);
        Assert.Equal(4_750, result.Totals.FuelByType["Nitrogen Isotopes"]);
        Assert.Equal(4_750_000m, result.Totals.TotalIskCost);
    }

    [Fact]
    public void JumpBeyondMaximumRange_IsInvalidAndNotCosted()
    {
        var unit = DistanceCalculator.MetersPerLightYear;
        var universe = new MemoryUniverse(System("A", 0, 0, 0), System("B", 6 * unit, 0, 0));
        var calculator = new RouteCalculator(universe);

        var result = calculator.Calculate(
            [new("A", "B", LegKind.Jump, EconomizerLoadout.None)],
            JumpFreighterCatalog.GetByName("Rhea"),
            new SkillProfile(0, 5, 5),
            1_000m);

        var leg = Assert.Single(result.Legs);
        Assert.False(leg.IsValid);
        Assert.Contains("OUT OF RANGE", leg.Status);
        Assert.Equal(0, leg.FuelUsed);
        Assert.Equal(0m, leg.IskCost);
    }

    [Fact]
    public void ConsecutiveDuplicateSystem_IsInvalid()
    {
        var universe = new MemoryUniverse(System("A", 0, 0, 0));
        var calculator = new RouteCalculator(universe);

        var result = calculator.Calculate(
            [new("A", "A", LegKind.Jump, EconomizerLoadout.None)],
            JumpFreighterCatalog.GetByName("Rhea"),
            new SkillProfile(5, 5, 5),
            null);

        Assert.False(Assert.Single(result.Legs).IsValid);
    }

    [Fact]
    public void JumpIntoHighSecurity_IsInvalidButGateIsAllowed()
    {
        var unit = DistanceCalculator.MetersPerLightYear;
        var universe = new MemoryUniverse(
            new SolarSystem(1, "Low", 0, 0, 0, 0.1),
            new SolarSystem(2, "High", unit, 0, 0, 0.9));
        var calculator = new RouteCalculator(universe);
        var ship = JumpFreighterCatalog.GetByName("Rhea");
        var skills = new SkillProfile(5, 5, 5);

        var jump = calculator.Calculate([new("Low", "High", LegKind.Jump, EconomizerLoadout.None)], ship, skills, null);
        var gate = calculator.Calculate([new("Low", "High", LegKind.Gate, EconomizerLoadout.None)], ship, skills, null);

        Assert.False(Assert.Single(jump.Legs).IsValid);
        Assert.Contains("high-security", jump.Legs[0].Status);
        Assert.True(Assert.Single(gate.Legs).IsValid);
    }

    private static SolarSystem System(string name, double x, double y, double z) =>
        new(name.GetHashCode(), name, x, y, z, 0);

    internal sealed class MemoryUniverse(params SolarSystem[] systems) : IUniverseRepository
    {
        private readonly Dictionary<string, SolarSystem> _systems = systems.ToDictionary(system => system.Name, StringComparer.OrdinalIgnoreCase);
        public IReadOnlyList<string> SystemNames => _systems.Keys.ToArray();
        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public bool TryGetSystem(string name, out SolarSystem? system) => _systems.TryGetValue(name, out system);
    }
}
