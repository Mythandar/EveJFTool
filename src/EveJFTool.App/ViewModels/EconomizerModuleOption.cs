using EveJFTool.Core.Catalogs;
using EveJFTool.Core.Models;

namespace EveJFTool.App.ViewModels;

public sealed record EconomizerModuleOption(string DisplayName, EconomizerDefinition? Module)
{
    public static IReadOnlyList<EconomizerModuleOption> All { get; } =
    [
        new("None", null),
        new("Limited (4%)", EconomizerCatalog.Limited),
        new("Experimental (7%)", EconomizerCatalog.Experimental),
        new("Prototype (10%)", EconomizerCatalog.Prototype)
    ];

    public static EconomizerModuleOption FromModule(EconomizerDefinition module) =>
        All.First(option => option.Module?.TypeId == module.TypeId);
}
