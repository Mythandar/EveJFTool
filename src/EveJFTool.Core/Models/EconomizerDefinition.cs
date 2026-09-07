namespace EveJFTool.Core.Models;

public sealed record EconomizerDefinition(int TypeId, string Name, double FuelReductionFraction);

public sealed record EconomizerLoadout
{
    public static EconomizerLoadout None { get; } = new([]);

    public EconomizerLoadout(IReadOnlyList<EconomizerDefinition> modules)
    {
        ArgumentNullException.ThrowIfNull(modules);
        if (modules.Count > 3)
        {
            throw new ArgumentException("Jump Freighters have three low slots; at most three economizers are supported.", nameof(modules));
        }

        Modules = modules.ToArray();
    }

    public IReadOnlyList<EconomizerDefinition> Modules { get; }
}
