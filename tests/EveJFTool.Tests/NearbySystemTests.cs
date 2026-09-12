using EveJFTool.Core.Catalogs;
using EveJFTool.Core.Models;
using EveJFTool.Core.Services;

namespace EveJFTool.Tests;

public class NearbySystemTests
{
    private static SolarSystem System(int id, string name, double ly, double security = 0, int region = 0) =>
        new(id, name, ly * DistanceCalculator.MetersPerLightYear, 0, 0, security, region);

    [Fact]
    public void FindsAllCandidatesIncludingBoundarySortedByDistance()
    {
        var finder = new NearbySystemFinder(new CalculationTests.MemoryUniverse(
            System(1, "Origin", 0), System(2, "Far", 10), System(3, "Near", 1),
            System(4, "Beyond", 10.001), System(5, "Highsec", 2, .45),
            System(6, "Pochven", 3, 0, 10_000_070), System(7, "Bad", double.NaN)));
        var results = finder.Find("Origin", JumpShipCatalog.GetByName("Rhea"), new(5, 5, 5));
        Assert.Equal(new[] { "Near", "Far" }, results.Select(s => s.Name));
        Assert.Equal(10, results[1].DistanceLightYears, 9);
    }

    [Theory]
    [InlineData("Rhea", 0, 1)]
    [InlineData("Rhea", 5, 3)]
    [InlineData("Avatar", 5, 1)]
    [InlineData("Widow", 5, 2)]
    public void UsesActualShipAndSkillRange(string ship, int jdc, int count)
    {
        var finder = new NearbySystemFinder(new CalculationTests.MemoryUniverse(
            System(1, "Origin", 0), System(2, "Five", 5), System(3, "Eight", 8), System(4, "Ten", 10)));
        Assert.Equal(count, finder.Find("Origin", JumpShipCatalog.GetByName(ship), new(jdc, 5, 5)).Count);
    }

    [Fact]
    public void UnknownOrInvalidOriginIsReported()
    {
        var finder = new NearbySystemFinder(new CalculationTests.MemoryUniverse(System(1, "Bad", double.NaN)));
        Assert.Throws<ArgumentException>(() => finder.Find("Missing", JumpShipCatalog.GetByName("Rhea"), new(5, 5, 5)));
        Assert.Throws<ArgumentException>(() => finder.Find("Bad", JumpShipCatalog.GetByName("Rhea"), new(5, 5, 5)));
    }

    [Fact]
    public void HighsecOriginIsAllowedForJumpFreighterButNotCapital()
    {
        var finder = new NearbySystemFinder(new CalculationTests.MemoryUniverse(
            System(1, "Origin", 0, .9), System(2, "Lowsec", 1, .1)));
        Assert.Single(finder.Find("Origin", JumpShipCatalog.GetByName("Rhea"), new(5, 5, 5)));
        Assert.Throws<ArgumentException>(() => finder.Find("Origin", JumpShipCatalog.GetByName("Archon"), new(5, 5, 5)));
    }
}
