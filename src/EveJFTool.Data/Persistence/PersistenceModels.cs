using EveJFTool.Core.Models;

namespace EveJFTool.Data.Persistence;

public sealed class UserSettings
{
    public string SelectedShip { get; set; } = "Rhea";
    public int JumpDriveCalibration { get; set; } = 5;
    public int JumpFuelConservation { get; set; } = 5;
    public int JumpFreighters { get; set; } = 5;
    public PriceMode PriceMode { get; set; } = PriceMode.JitaSell;
    public Dictionary<int, decimal> ManualPrices { get; set; } = [];
    public double WindowWidth { get; set; } = 1400;
    public double WindowHeight { get; set; } = 850;
    public bool WindowMaximized { get; set; }
}

public sealed class SavedRoute
{
    public string Name { get; set; } = string.Empty;
    public List<string> Systems { get; set; } = [];
    public List<SavedLeg> Legs { get; set; } = [];
    public string? ShipName { get; set; }
    public int? JumpDriveCalibration { get; set; }
    public int? JumpFuelConservation { get; set; }
    public int? JumpFreighters { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class SavedLeg
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public LegKind Kind { get; set; }
    public List<int> EconomizerTypeIds { get; set; } = [];
}
