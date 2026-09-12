using EveJFTool.App.ViewModels;
using EveJFTool.Core.Catalogs;
using EveJFTool.Core.Models;

namespace EveJFTool.Tests;

public class ShipSwitchTests
{
    [Fact]
    public void SwitchingHullsPreservesSelectionsButAppliesOnlyCompatibleSlots()
    {
        var prototype = EconomizerCatalog.Prototype;
        var leg = new RouteLegViewModel("A", "B", LegKind.Jump, new([prototype, prototype, prototype, prototype]));
        leg.ConfigureShip(JumpShipCatalog.GetByName("Rorqual"));
        Assert.True(leg.CanFitFourthEconomizer);
        Assert.Equal(4, leg.ToRequest().Economizers.Modules.Count);
        leg.ConfigureShip(JumpShipCatalog.GetByName("Rhea"));
        Assert.False(leg.CanFitFourthEconomizer);
        Assert.True(leg.CanFitEconomizers);
        Assert.Equal(3, leg.ToRequest().Economizers.Modules.Count);
        leg.ConfigureShip(JumpShipCatalog.GetByName("Widow"));
        Assert.False(leg.CanFitEconomizers);
        Assert.Empty(leg.ToRequest().Economizers.Modules);
        Assert.Equal(4, leg.EconomizerLoadout.Modules.Count);
        leg.ConfigureShip(JumpShipCatalog.GetByName("Rorqual"));
        Assert.Equal(4, leg.ToRequest().Economizers.Modules.Count);
    }

    [Fact]
    public void FourthSlotChangesNotifyAndGateDisablesAllSlots()
    {
        var leg = new RouteLegViewModel("A", "B", LegKind.Jump, EconomizerLoadout.None);
        leg.ConfigureShip(JumpShipCatalog.GetByName("Rorqual"));
        var changes = 0;
        leg.ConfigurationChanged += (_, _) => changes++;
        leg.Economizer4 = EconomizerModuleOption.FromModule(EconomizerCatalog.Limited);
        Assert.Equal(1, changes);
        Assert.Single(leg.ToRequest().Economizers.Modules);
        leg.Kind = LegKind.Gate;
        Assert.False(leg.CanFitEconomizers);
        Assert.False(leg.CanFitFourthEconomizer);
        Assert.Equal(2, changes);
    }
}
