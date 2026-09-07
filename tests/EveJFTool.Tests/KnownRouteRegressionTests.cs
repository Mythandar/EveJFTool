using EveJFTool.Core.Catalogs;
using EveJFTool.Core.Models;
using EveJFTool.Core.Services;

namespace EveJFTool.Tests;

public sealed class KnownRouteRegressionTests
{
    [Fact]
    public void Dotlan_IhakanaToOLjoo_Rhea555_MatchesFuelAndDistances()
    {
        // CCP SDE build 3494416 coordinates; DOTLAN URL and results are documented in docs/eve-mechanics.md.
        var universe = new CalculationTests.MemoryUniverse(
            new(30000169, "Ihakana", -7.122261312735623e16, 1.0226415651805973e17, 1.21304533109936e17, 0.380809),
            new(30002485, "HKYW-T", 1.6504967816723208e16, 8.82588066855032e16, 9.059200263915061e16, -0.045379),
            new(30002351, "O-LJOO", 1.0064054039112989e17, 6.624106958811784e16, 5.41955422169908e16, -0.026999));
        var calculator = new RouteCalculator(universe);

        var result = calculator.Calculate(
        [
            new("Ihakana", "HKYW-T", LegKind.Jump, EconomizerLoadout.None),
            new("HKYW-T", "O-LJOO", LegKind.Jump, EconomizerLoadout.None)
        ], JumpFreighterCatalog.GetByName("Rhea"), new SkillProfile(5, 5, 5), null);

        Assert.Equal(9.93631517031794, result.Legs[0].DistanceLightYears, 12);
        Assert.Equal(9.96592627080353, result.Legs[1].DistanceLightYears, 12);
        Assert.Equal(24_841, result.Legs[0].FuelUsed);
        Assert.Equal(24_915, result.Legs[1].FuelUsed);
        Assert.Equal(49_756, result.Totals.FuelByType["Nitrogen Isotopes"]);
    }
}
