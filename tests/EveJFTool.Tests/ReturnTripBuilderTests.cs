using EveJFTool.Core.Catalogs;
using EveJFTool.Core.Models;
using EveJFTool.Core.Services;

namespace EveJFTool.Tests;

public sealed class ReturnTripBuilderTests
{
    [Fact]
    public void CreateReturnLegs_ReversesOrderAndDirectionWhilePreservingConfiguration()
    {
        var firstLoadout = new EconomizerLoadout(
            [EconomizerCatalog.Prototype, EconomizerCatalog.Experimental]);
        RouteLegRequest[] outbound =
        [
            new("A", "B", LegKind.Jump, firstLoadout),
            new("B", "C", LegKind.Gate, EconomizerLoadout.None)
        ];

        var result = ReturnTripBuilder.CreateReturnLegs(outbound);

        Assert.Collection(
            result,
            leg =>
            {
                Assert.Equal("C", leg.FromSystem);
                Assert.Equal("B", leg.ToSystem);
                Assert.Equal(LegKind.Gate, leg.Kind);
                Assert.Empty(leg.Economizers.Modules);
            },
            leg =>
            {
                Assert.Equal("B", leg.FromSystem);
                Assert.Equal("A", leg.ToSystem);
                Assert.Equal(LegKind.Jump, leg.Kind);
                Assert.Equal(firstLoadout, leg.Economizers);
            });
    }

    [Fact]
    public void CreateReturnLegs_EmptyRouteProducesNoLegs()
    {
        Assert.Empty(ReturnTripBuilder.CreateReturnLegs([]));
    }
}
