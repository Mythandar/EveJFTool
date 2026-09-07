namespace EveJFTool.Core.Models;

public sealed record ShipDefinition(
    int TypeId,
    string Name,
    int IsotopeTypeId,
    string IsotopeName,
    double BaseFuelPerLightYear,
    double BaseJumpRangeLightYears);
