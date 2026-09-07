using EveJFTool.Core.Models;

namespace EveJFTool.Core.Catalogs;

public static class EconomizerCatalog
{
    public static EconomizerDefinition Limited { get; } = new(34122, "Limited", 0.04);
    public static EconomizerDefinition Experimental { get; } = new(34124, "Experimental", 0.07);
    public static EconomizerDefinition Prototype { get; } = new(34126, "Prototype", 0.10);

    public static IReadOnlyList<EconomizerDefinition> All { get; } = [Limited, Experimental, Prototype];
}
