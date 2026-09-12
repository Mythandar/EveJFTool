using EveJFTool.Core.Catalogs;
using EveJFTool.Core.Models;
using EveJFTool.Core.Services;

namespace EveJFTool.Tests;

public class JumpShipTests
{
    [Fact]
    public void CatalogContainsAll56PublishedJumpShipsFromSde3503375()
    {
        Assert.Equal(56, JumpShipCatalog.All.Count);
        Assert.Equal(56, JumpShipCatalog.All.Select(s => s.TypeId).Distinct().Count());
        Assert.Equal(10, JumpShipCatalog.All.Select(s => s.ShipClass).Distinct().Count());
        foreach (var ship in JumpFreighterCatalog.All)
            Assert.Equal(ship, JumpShipCatalog.GetByName(ship.Name));
        Assert.All(JumpShipCatalog.All, ship =>
        {
            Assert.True(ship.BaseFuelPerLightYear > 0);
            Assert.Contains(ship.IsotopeTypeId, new[] { 16274, 17887, 17888, 17889 });
        });
    }

    [Theory]
    [InlineData("Rhea", 10, 2500)]
    [InlineData("Widow", 8, 350)]
    [InlineData("Python", 8, 350)]
    [InlineData("Rorqual", 10, 2000)]
    [InlineData("Archon", 7, 1500)]
    [InlineData("Apostle", 7, 1500)]
    [InlineData("Phoenix Navy Issue", 7, 1500)]
    [InlineData("Sarathiel", 7, 1500)]
    [InlineData("Hubris", 8, 1500)]
    [InlineData("Revenant", 6, 1500)]
    [InlineData("Azariel", 6, 1500)]
    [InlineData("Salvation", 7.5, 1500)]
    public void HullRangeAndOneLightYearFuel(string name, double range, long fuel)
    {
        var ship = JumpShipCatalog.GetByName(name);
        var skills = new SkillProfile(5, 5, 5);
        Assert.Equal(range, FuelCalculator.MaximumRange(ship, skills));
        Assert.Equal(fuel, FuelCalculator.IsotopesForJump(ship, skills, 1, EconomizerLoadout.None));
    }

    [Fact]
    public void JumpFreightersSkillNeverDiscountsOtherHullClasses()
    {
        foreach (var ship in JumpShipCatalog.All.Where(s => s.ShipClass != "Jump Freighter"))
            Assert.Equal(
                FuelCalculator.IsotopesForJump(ship, new(5, 5, 0), 2.123, EconomizerLoadout.None),
                FuelCalculator.IsotopesForJump(ship, new(5, 5, 5), 2.123, EconomizerLoadout.None));
    }

    [Fact]
    public void FourEconomizersApplyFourthStackingPenaltyOnlyOnRorqual()
    {
        var module = EconomizerCatalog.All.Single(m => m.TypeId == 34126);
        var loadout = new EconomizerLoadout([module, module, module, module]);
        var expected = (long)Math.Ceiling(4000 * .5 * .9 * (1 - .1 * .869119980021702)
            * (1 - .1 * .570583143034018) * (1 - .1 * .282955154023261));
        Assert.Equal(expected, FuelCalculator.IsotopesForJump(JumpShipCatalog.GetByName("Rorqual"), new(5, 5, 5), 1, loadout));
        Assert.Throws<ArgumentException>(() => FuelCalculator.IsotopesForJump(
            JumpShipCatalog.GetByName("Rhea"), new(5, 5, 5), 1, loadout));
        Assert.Throws<ArgumentException>(() => FuelCalculator.IsotopesForJump(
            JumpShipCatalog.GetByName("Widow"), new(5, 5, 5), 1, new([module])));
    }

    [Theory]
    [InlineData("Rhea", true)]
    [InlineData("Widow", true)]
    [InlineData("Rorqual", false)]
    [InlineData("Avatar", false)]
    [InlineData("Gaia", false)]
    public void HighsecGateAccessMatchesHull(string name, bool allowed)
    {
        var calculator = Calculator(.9);
        var leg = Assert.Single(calculator.Calculate([new("A", "B", LegKind.Gate, EconomizerLoadout.None)],
            JumpShipCatalog.GetByName(name), new(5, 5, 5), 1000).Legs);
        Assert.Equal(allowed, leg.IsValid);
        Assert.Equal(0, leg.FuelUsed);
    }

    [Fact]
    public void IncompatibleModuleIsFlaggedRatherThanThrowingDuringRouteCalculation()
    {
        var module = EconomizerCatalog.All[0];
        var leg = Assert.Single(Calculator().Calculate([new("A", "B", LegKind.Jump, new([module]))],
            JumpShipCatalog.GetByName("Nyx"), new(5, 5, 5), 1000).Legs);
        Assert.False(leg.IsValid);
        Assert.Contains("Economizers", leg.Status);
        Assert.Equal(0, leg.FuelUsed);
    }

    [Fact]
    public void CapitalRouteTotalsUseCapitalFuelAndPrice()
    {
        var result = Calculator().Calculate([
            new("A", "B", LegKind.Jump, EconomizerLoadout.None),
            new("B", "A", LegKind.Jump, EconomizerLoadout.None)],
            JumpShipCatalog.GetByName("Phoenix"), new(5, 5, 5), 1200);
        Assert.All(result.Legs, leg => Assert.True(leg.IsValid));
        Assert.Equal(3000, result.Totals.FuelByType["Nitrogen Isotopes"]);
        Assert.Equal(3_600_000m, result.Totals.TotalIskCost);
    }

    private static RouteCalculator Calculator(double destinationSecurity = 0) => new(
        new CalculationTests.MemoryUniverse(new(1, "A", 0, 0, 0, 0),
            new(2, "B", DistanceCalculator.MetersPerLightYear, 0, 0, destinationSecurity)));
}
