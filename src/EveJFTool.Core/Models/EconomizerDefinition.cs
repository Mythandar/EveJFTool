namespace EveJFTool.Core.Models;

public sealed record EconomizerDefinition(int TypeId, string Name, double FuelReductionFraction);

public sealed record EconomizerLoadout
{
    public static EconomizerLoadout None { get; } = new([]);

    public EconomizerLoadout(IReadOnlyList<EconomizerDefinition> modules)
    {
        ArgumentNullException.ThrowIfNull(modules);
        if (modules.Count > 4)
        {
            throw new ArgumentException("At most four Economizers are supported (Rorqual low slots).", nameof(modules));
        }

        Modules = modules.ToArray();
    }

    public IReadOnlyList<EconomizerDefinition> Modules { get; }
}
