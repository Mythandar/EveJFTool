# Verified EVE mechanics

Verified on 2026-09-06 against Tranquility ESI and CCP Static Data Export (SDE) build **3494416**, released 2026-09-04. The application intentionally treats these values as hull data, not ship-specific code.

## Jump Freighter hull data

The relevant SDE `typeDogma` fields are `jumpDriveConsumptionType` (attribute 866), `jumpDriveRange` (867), and `jumpDriveConsumptionAmount` (868).

| Hull | Type ID | Isotope | Isotope type ID | Base units / LY | Base range |
|---|---:|---|---:|---:|---:|
| Rhea | 28844 | Nitrogen Isotopes | 17888 | 10,000 | 5 LY |
| Anshar | 28848 | Oxygen Isotopes | 17887 | 9,400 | 5 LY |
| Ark | 28850 | Helium Isotopes | 16274 | 8,800 | 5 LY |
| Nomad | 28846 | Hydrogen Isotopes | 17889 | 8,200 | 5 LY |

Primary evidence: [CCP SDE downloads](https://developers.eveonline.com/static-data), [Rhea ESI type](https://esi.evetech.net/universe/types/28844), [Nomad ESI type](https://esi.evetech.net/universe/types/28846), [Anshar ESI type](https://esi.evetech.net/universe/types/28848), and [Ark ESI type](https://esi.evetech.net/universe/types/28850).

## Skill effects

- **Jump Drive Calibration** (type 21611) is **+20% maximum jump range per level**. It does not reduce fuel. Maximum range is `base range × (1 + 0.20 × JDC level)`, so every supported JF has a 10 LY maximum at level V.
- **Jump Fuel Conservation** (type 21610) is **−10% isotope consumption per level**.
- **Jump Freighters** (type 29029) applies the hull's `eliteBonusJumpFreighter2 = −10%` to fuel consumption per level.
- JFC and JF modifiers are multiplicative dogma post-percent operations. At V/V they result in `0.5 × 0.5 = 0.25` of hull base consumption, before economizers.

Primary evidence: [JDC ESI type](https://esi.evetech.net/universe/types/21611), [JFC ESI type](https://esi.evetech.net/universe/types/21610), [Jump Freighters ESI type](https://esi.evetech.net/universe/types/29029), plus SDE dogma effects 1581/1582, 3527/3532, 3593/3595/3596.

Older guides describing +25% JDC and 11.25 LY are obsolete. Current CCP data says +20% and 10 LY.

## Jump Drive Economizers

All three current variants modify `jumpDriveConsumptionAmount` through SDE dogma effect 5911 (`onlineJumpDriveConsumptionAmountBonusPercentage`, a post-percent modifier):

| Variant | Type ID | Fuel reduction |
|---|---:|---:|
| Limited | 34122 | 4% |
| Experimental | 34124 | 7% |
| Prototype | 34126 | 10% |

Their in-game description explicitly states that fitting multiple modules affecting the same attribute is penalized. Apply effects from strongest to weakest with the standard EVE stacking coefficients. For the three possible JF low slots those are `1.0`, `0.869119980021702`, and `0.570583143034018`.

For reductions `r1..rn`, sorted descending, the economizer multiplier is:

```text
Π (1 - reduction[i] × stackingPenalty[i])
```

The V1 core supports every mixed combination of up to three Limited, Experimental, and Prototype modules. A different loadout can be selected for every leg.

Primary evidence: CCP SDE build 3494416 `typeDogma` records 34122/34124/34126 and dogma effect 5911. Readable cross-check: [Prototype Jump Drive Economizer reference](https://everef.net/types/34126). Formula cross-check: [EVE University jump-drive formula](https://wiki.eveuniversity.org/Jump_drives).

## Fuel formula and rounding

For a jump leg:

```text
exact units = distanceLY
              × hullBaseUnitsPerLY
              × (1 - 0.10 × JFC)
              × (1 - 0.10 × JumpFreighters)
              × economizerMultiplier

fuel units = ceiling(exact units)
```

Only the final value is rounded, to the next whole isotope. Gate legs consume zero jump fuel.

Rounding was regression-checked against the current DOTLAN planner using SDE 3494416 coordinates and `Rhea, JDC V, JFC V, JF V`:

| Leg | Exact distance | Raw units | Application | DOTLAN |
|---|---:|---:|---:|---:|
| Ihakana → HKYW-T | 9.93631517031794 LY | 24,840.7879 | 24,841 | 24,841 |
| HKYW-T → O-LJOO | 9.96592627080353 LY | 24,914.8157 | 24,915 | 24,915 |

Planner comparison: [DOTLAN regression route](https://evemaps.dotlan.net/jump/Rhea,555/Ihakana:HKYW-T:O-LJOO).

## Distance

Solar-system `position.x/y/z` values in `mapSolarSystems.jsonl` are meters in the cluster coordinate system. Jump distance is straight-line Euclidean distance—not a stargate path:

```text
meters = sqrt((x2-x1)^2 + (y2-y1)^2 + (z2-z1)^2)
lightYears = meters / 9,460,000,000,000,000.0
```

CCP specifies exactly `9.46 × 10^15` meters per EVE light-year for jump range, slightly different from the scientific constant. Source: [CCP developer map-data guide](https://developers.eveonline.com/docs/guides/map-data/).

## Market pricing

Jita buy/sell uses CCP's public ESI regional market-orders endpoint for The Forge (region 10000002), filtered to Jita IV – Moon 4 – Caldari Navy Assembly Plant (location 60003760). Sell is the lowest station sell order; buy is the highest station buy order. ESI requires no login for this endpoint. Responses are cached locally and identified as cached/stale when live retrieval fails. Manual per-isotope prices remain available offline.

Sources: [CCP ESI overview](https://developers.eveonline.com/docs/services/esi/overview/) and [market-order rate-limit/cache guidance](https://developers.eveonline.com/blog/market-orders-rate-limit-rolls-out-on-february-24-2026).
