namespace EveJFTool.Core.Models;

public sealed record SkillProfile
{
    public SkillProfile(int jumpDriveCalibration, int jumpFuelConservation, int jumpFreighters)
    {
        JumpDriveCalibration = Validate(jumpDriveCalibration, nameof(jumpDriveCalibration));
        JumpFuelConservation = Validate(jumpFuelConservation, nameof(jumpFuelConservation));
        JumpFreighters = Validate(jumpFreighters, nameof(jumpFreighters));
    }

    public int JumpDriveCalibration { get; }
    public int JumpFuelConservation { get; }
    public int JumpFreighters { get; }

    private static int Validate(int value, string name) =>
        value is >= 0 and <= 5
            ? value
            : throw new ArgumentOutOfRangeException(name, value, "Skill level must be from 0 through 5.");
}
