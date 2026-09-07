namespace EveJFTool.Core.Models;

public enum PriceMode
{
    JitaSell,
    JitaBuy,
    Manual
}

public sealed record MarketPrice(
    int TypeId,
    string TypeName,
    decimal Price,
    PriceMode Mode,
    DateTimeOffset UpdatedAt,
    bool IsCached,
    bool IsStale,
    string Source);
