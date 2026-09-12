using EveJFTool.Core.Models;

namespace EveJFTool.Core.Catalogs;

/// <summary>Published jump-drive ships from CCP SDE 3503375 (2026-09-10).</summary>
public static class JumpShipCatalog
{
    public static IReadOnlyList<ShipDefinition> All { get; } = Build();

    public static ShipDefinition GetByName(string name) => All.FirstOrDefault(
        ship => string.Equals(ship.Name, name, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"Unknown jump ship '{name}'.");

    private static IReadOnlyList<ShipDefinition> Build()
    {
        var ships = new List<ShipDefinition>(JumpFreighterCatalog.All);
        Add("Black Ops", 700, 4,
            (22428, "Redeemer", 16274), (22430, "Sin", 17887),
            (22436, "Widow", 17888), (22440, "Panther", 17889),
            (44996, "Marshal", 16274), (85236, "Python", 16274));
        Add("Carrier", 3000, 3.5,
            (23757, "Archon", 16274), (23915, "Chimera", 17888),
            (23911, "Thanatos", 17887), (24483, "Nidhoggur", 17889));
        Add("Force Auxiliary", 3000, 3.5,
            (37604, "Apostle", 16274), (37605, "Minokawa", 17888),
            (37606, "Lif", 17889), (37607, "Ninazu", 17887),
            (42242, "Dagon", 16274), (45645, "Loggerhead", 17888));
        Add("Dreadnought", 3000, 3.5,
            (19720, "Revelation", 16274), (19722, "Naglfar", 17889),
            (19724, "Moros", 17887), (19726, "Phoenix", 17888),
            (42124, "Vehement", 17887), (42243, "Chemosh", 16274),
            (45647, "Caiman", 17888), (52907, "Zirnitra", 16274),
            (73787, "Naglfar Fleet Issue", 17889), (73790, "Revelation Navy Issue", 16274),
            (73792, "Moros Navy Issue", 17887), (73793, "Phoenix Navy Issue", 17888),
            (87381, "Sarathiel", 17889));
        Add("Lancer Dreadnought", 3000, 4,
            (77281, "Hubris", 17887), (77283, "Bane", 16274),
            (77284, "Karura", 17888), (77288, "Valravn", 17889));
        Add("Supercarrier", 3000, 3,
            (3514, "Revenant", 16274), (22852, "Hel", 17889),
            (23913, "Nyx", 17887), (23917, "Wyvern", 17888),
            (23919, "Aeon", 16274), (42125, "Vendetta", 17887));
        Add("Titan", 3000, 3,
            (671, "Erebus", 17887), (3764, "Leviathan", 17888),
            (11567, "Avatar", 16274), (23773, "Ragnarok", 17889),
            (42126, "Vanquisher", 17887), (42241, "Molok", 16274),
            (45649, "Komodo", 17888), (78576, "Azariel", 17889));
        Add("Command Carrier", 3000, 3.75,
            (92822, "Salvation", 16274), (92823, "Simurgh", 17888),
            (92824, "Gaia", 17887), (92825, "Ymir", 17889));
        ships.Add(new(28352, "Rorqual", 17887, "Oxygen Isotopes", 4000, 5,
            "Capital Industrial Ship", 0, 4));
        return ships.OrderBy(ship => ship.ShipClass).ThenBy(ship => ship.Name).ToArray();

        void Add(string shipClass, double fuel, double range, params (int Id, string Name, int FuelId)[] hulls)
        {
            foreach (var hull in hulls)
            {
                var isotope = hull.FuelId switch
                {
                    16274 => "Helium Isotopes", 17887 => "Oxygen Isotopes",
                    17888 => "Nitrogen Isotopes", 17889 => "Hydrogen Isotopes",
                    _ => throw new InvalidOperationException("Unknown isotope")
                };
                ships.Add(new(hull.Id, hull.Name, hull.FuelId, isotope, fuel, range, shipClass));
            }
        }
    }
}
