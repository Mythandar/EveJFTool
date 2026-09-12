using EveJFTool.Core.Models;

namespace EveJFTool.Core.Services;

public static class FuelCalculator
{
    public const double JumpDriveCalibrationRangeBonusPerLevel = 0.20;
    public const double JumpFuelConservationReductionPerLevel = 0.10;
    public const double JumpFreightersReductionPerLevel = 0.10;

    public static IReadOnlyList<double> StackingPenalties { get; } =
    [
        1.0,
        0.869119980021702,
        0.570583143034018,
        0.282955154023261
    ];

    public static double MaximumRange(ShipDefinition ship, SkillProfile skills) =>
        ship.BaseJumpRangeLightYears *
        (1 + (JumpDriveCalibrationRangeBonusPerLevel * skills.JumpDriveCalibration));

    public static double EconomizerMultiplier(EconomizerLoadout loadout)
    {
        ArgumentNullException.ThrowIfNull(loadout);

        var reductions = loadout.Modules
            .Select(module => module.FuelReductionFraction)
            .OrderByDescending(reduction => reduction)
            .ToArray();

        var multiplier = 1d;
        for (var index = 0; index < reductions.Length; index++)
        {
            multiplier *= 1 - (reductions[index] * StackingPenalties[index]);
        }

        return multiplier;
    }

    public static long IsotopesForJump(
        ShipDefinition ship,
        SkillProfile skills,
        double distanceLightYears,
        EconomizerLoadout loadout)
    {
        ArgumentNullException.ThrowIfNull(ship);
        ArgumentNullException.ThrowIfNull(skills);
        ArgumentNullException.ThrowIfNull(loadout);
        if (!double.IsFinite(distanceLightYears) || distanceLightYears < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(distanceLightYears));
        }

        var jfcMultiplier = 1 - (JumpFuelConservationReductionPerLevel * skills.JumpFuelConservation);
        if (loadout.Modules.Count > ship.EconomizerSlots)
            throw new ArgumentException($"{ship.Name} supports at most {ship.EconomizerSlots} Economizers.", nameof(loadout));

        var jfMultiplier = 1 - (ship.JumpFreightersFuelReductionPerLevel * skills.JumpFreighters);
        var exactFuel = distanceLightYears * ship.BaseFuelPerLightYear * jfcMultiplier * jfMultiplier *
                        EconomizerMultiplier(loadout);

        return checked((long)Math.Ceiling(exactFuel));
    }
}
