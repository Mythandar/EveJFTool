using EveJFTool.Core.Models;

namespace EveJFTool.Core.Services;

public static class ReturnTripBuilder
{
    public static IReadOnlyList<RouteLegRequest> CreateReturnLegs(
        IReadOnlyList<RouteLegRequest> outboundLegs)
    {
        ArgumentNullException.ThrowIfNull(outboundLegs);

        return outboundLegs
            .Reverse()
            .Select(leg => new RouteLegRequest(
                leg.ToSystem,
                leg.FromSystem,
                leg.Kind,
                leg.Economizers))
            .ToArray();
    }
}
