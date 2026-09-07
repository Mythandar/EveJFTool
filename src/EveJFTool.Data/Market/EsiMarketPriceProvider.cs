using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EveJFTool.Core.Interfaces;
using EveJFTool.Core.Models;

namespace EveJFTool.Data.Market;

public sealed class EsiMarketPriceProvider(
    HttpClient httpClient,
    string cachePath,
    IAppLogger logger) : IMarketPriceProvider
{
    private const int TheForgeRegionId = 10_000_002;
    private const long Jita44StationId = 60_003_760;
    private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(20);

    public async Task<IReadOnlyDictionary<int, MarketPrice>> GetPricesAsync(
        IReadOnlyCollection<ShipDefinition> ships,
        PriceMode mode,
        CancellationToken cancellationToken = default)
    {
        if (mode == PriceMode.Manual)
        {
            return new Dictionary<int, MarketPrice>();
        }

        var cached = await LoadCacheAsync(cancellationToken);
        try
        {
            var result = new Dictionary<int, MarketPrice>();
            foreach (var ship in ships.DistinctBy(ship => ship.IsotopeTypeId))
            {
                var price = await GetJitaPriceAsync(ship.IsotopeTypeId, mode, cancellationToken);
                if (price is null)
                {
                    throw new InvalidOperationException($"No Jita orders were returned for {ship.IsotopeName}.");
                }

                result[ship.IsotopeTypeId] = new MarketPrice(
                    ship.IsotopeTypeId,
                    ship.IsotopeName,
                    price.Value,
                    mode,
                    DateTimeOffset.UtcNow,
                    false,
                    false,
                    "CCP ESI – Jita 4-4");
            }

            await SaveCacheAsync(result.Values, cancellationToken);
            logger.Info($"Updated {result.Count} {mode} isotope prices from CCP ESI.");
            return result;
        }
        catch (Exception exception)
        {
            logger.Error("CCP ESI market price update failed; attempting cached prices.", exception);
            var now = DateTimeOffset.UtcNow;
            return cached.Values
                .Where(price => price.Mode == mode)
                .ToDictionary(
                    price => price.TypeId,
                    price => price with
                    {
                        IsCached = true,
                        IsStale = now - price.UpdatedAt > StaleAfter,
                        Source = "Local cache (CCP ESI)"
                    });
        }
    }

    private async Task<decimal?> GetJitaPriceAsync(int typeId, PriceMode mode, CancellationToken cancellationToken)
    {
        var path = $"https://esi.evetech.net/markets/{TheForgeRegionId}/orders?order_type=all&type_id={typeId}";
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation("X-Compatibility-Date", "2026-09-05");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var orders = await response.Content.ReadFromJsonAsync<List<EsiOrder>>(cancellationToken: cancellationToken) ?? [];
        var stationOrders = orders.Where(order => order.LocationId == Jita44StationId);

        return mode switch
        {
            PriceMode.JitaSell => stationOrders.Where(order => !order.IsBuyOrder).Select(order => (decimal?)order.Price).Min(),
            PriceMode.JitaBuy => stationOrders.Where(order => order.IsBuyOrder).Select(order => (decimal?)order.Price).Max(),
            _ => null
        };
    }

    private async Task<Dictionary<int, MarketPrice>> LoadCacheAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(cachePath))
        {
            return [];
        }

        try
        {
            await using var stream = File.OpenRead(cachePath);
            var prices = await JsonSerializer.DeserializeAsync<List<MarketPrice>>(stream, cancellationToken: cancellationToken) ?? [];
            return prices.ToDictionary(price => price.TypeId);
        }
        catch (Exception exception)
        {
            logger.Error("Market cache could not be read.", exception);
            return [];
        }
    }

    private async Task SaveCacheAsync(IEnumerable<MarketPrice> prices, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
        await using var stream = File.Create(cachePath);
        await JsonSerializer.SerializeAsync(stream, prices.ToArray(), cancellationToken: cancellationToken);
    }

    private sealed class EsiOrder
    {
        [JsonPropertyName("is_buy_order")]
        public bool IsBuyOrder { get; set; }

        [JsonPropertyName("location_id")]
        public long LocationId { get; set; }

        [JsonPropertyName("price")]
        public double Price { get; set; }
    }
}
