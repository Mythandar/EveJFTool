using EveJFTool.Core.Models;

namespace EveJFTool.Core.Catalogs;

public static class JumpFreighterCatalog
{
    // CCP SDE build 3494416, typeDogma attributes 866, 867 and 868.
    public static IReadOnlyList<ShipDefinition> All { get; } =
    [
        new(28844, "Rhea", 17888, "Nitrogen Isotopes", 10_000, 5),
        new(28848, "Anshar", 17887, "Oxygen Isotopes", 9_400, 5),
        new(28850, "Ark", 16274, "Helium Isotopes", 8_800, 5),
        new(28846, "Nomad", 17889, "Hydrogen Isotopes", 8_200, 5)
    ];

    public static ShipDefinition GetByName(string name) =>
        All.FirstOrDefault(ship => string.Equals(ship.Name, name, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"Unknown Jump Freighter '{name}'.");
}
