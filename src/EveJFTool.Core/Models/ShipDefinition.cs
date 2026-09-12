namespace EveJFTool.Core.Models;

public sealed record ShipDefinition(
    int TypeId,
    string Name,
    int IsotopeTypeId,
    string IsotopeName,
    double BaseFuelPerLightYear,
    double BaseJumpRangeLightYears,
    string ShipClass = "Jump Freighter",
    double JumpFreightersFuelReductionPerLevel = 0,
    int EconomizerSlots = 0)
{
    public string DisplayName => $"{Name} ({ShipClass})";
    public bool UsesJumpFreightersSkill => JumpFreightersFuelReductionPerLevel > 0;
    public bool CanEnterHighSecurity => ShipClass is "Jump Freighter" or "Black Ops";
}
