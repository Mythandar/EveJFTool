using EveJFTool.Core.Catalogs;
using EveJFTool.Core.Models;

namespace EveJFTool.App.ViewModels;

public sealed record EconomizerOption(string Key, string DisplayName, EconomizerLoadout Loadout)
{
    public static IReadOnlyList<EconomizerOption> CreateAll()
    {
        var options = new List<EconomizerOption>
        {
            new("none", "None", EconomizerLoadout.None)
        };

        for (var prototype = 0; prototype <= 3; prototype++)
        {
            for (var experimental = 0; experimental <= 3; experimental++)
            {
                for (var limited = 0; limited <= 3; limited++)
                {
                    var count = prototype + experimental + limited;
                    if (count is 0 or > 3)
                    {
                        continue;
                    }

                    var modules = Enumerable.Repeat(EconomizerCatalog.Prototype, prototype)
                        .Concat(Enumerable.Repeat(EconomizerCatalog.Experimental, experimental))
                        .Concat(Enumerable.Repeat(EconomizerCatalog.Limited, limited))
                        .ToArray();
                    var labels = new List<string>();
                    if (prototype > 0) labels.Add($"{prototype}× Prototype");
                    if (experimental > 0) labels.Add($"{experimental}× Experimental");
                    if (limited > 0) labels.Add($"{limited}× Limited");
                    var key = string.Join('-', modules.Select(module => module.TypeId).Order());
                    options.Add(new EconomizerOption(key, string.Join(" + ", labels), new EconomizerLoadout(modules)));
                }
            }
        }

        return options
            .OrderBy(option => option.Loadout.Modules.Count)
            .ThenByDescending(option => option.Loadout.Modules.Sum(module => module.FuelReductionFraction))
            .ThenBy(option => option.DisplayName)
            .ToArray();
    }

    public static string KeyFor(IEnumerable<int> typeIds) => string.Join('-', typeIds.Order());
}
