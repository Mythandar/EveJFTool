using EveJFTool.Core.Models;

namespace EveJFTool.Core.Interfaces;

public interface IUniverseRepository
{
    IReadOnlyList<string> SystemNames { get; }
    Task InitializeAsync(CancellationToken cancellationToken = default);
    bool TryGetSystem(string name, out SolarSystem? system);
}

public interface IMarketPriceProvider
{
    Task<IReadOnlyDictionary<int, MarketPrice>> GetPricesAsync(
        IReadOnlyCollection<ShipDefinition> ships,
        PriceMode mode,
        CancellationToken cancellationToken = default);
}

public interface IAppLogger
{
    void Info(string message);
    void Error(string message, Exception? exception = null);
}
