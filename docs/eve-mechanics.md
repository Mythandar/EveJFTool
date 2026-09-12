# Verified EVE mechanics

## All jump-drive ships (version 1.1)

Reverified 2026-09-12 using the [official CCP JSONL SDE archive](https://developers.eveonline.com/static-data/eve-online-static-data-latest-jsonl.zip), whose `_sde.jsonl` identifies build **3503375**, released **2026-09-10T11:09:04Z**. This is a moving URL; build identity matters. See [CCP's SDE documentation](https://developers.eveonline.com/docs/services/static-data/).

Selection rule: published `types.jsonl` entries in a `groups.jsonl` group with category 6 (ships), with positive dogma attribute 867. This returns **56 ships**, including Python, Sarathiel, Azariel, and all four Command Carriers. Unpublished/development types are excluded. The complete named catalog and isotope mapping are in `JumpShipCatalog.cs`; the existing four JFs remain in `JumpFreighterCatalog.cs` for compatibility.

| Class | Count | Base fuel / LY | Base LY | JDC V LY |
|---|---:|---:|---:|---:|
| Jump Freighter | 4 | Hull-specific, table below | 5 | 10 |
| Black Ops | 6 | 700 | 4 | 8 |
| Carrier | 4 | 3,000 | 3.5 | 7 |
| Command Carrier | 4 | 3,000 | 3.75 | 7.5 |
| Force Auxiliary | 6 | 3,000 | 3.5 | 7 |
| Dreadnought | 13 | 3,000 | 3.5 | 7 |
| Lancer Dreadnought | 4 | 3,000 | 4 | 8 |
| Supercarrier | 6 | 3,000 | 3 | 6 |
| Titan | 8 | 3,000 | 3 | 6 |
| Capital Industrial Ship (Rorqual) | 1 | 4,000 | 5 | 10 |

All figures are dogma attributes 866/867/868, not inferred from faction names. For example Revenant, Marshal, Python, and Zirnitra use Helium; Rorqual uses Oxygen. Fuel isotope IDs remain 16274 (Helium), 17887 (Oxygen), 17888 (Nitrogen), and 17889 (Hydrogen).

Checked `typeBonus.jsonl` and all `dogmaEffects.jsonl` modifiers targeting attributes 867/868. JDC uses effect 1581; JFC uses 3532; the additional hull fuel skill modifier uses 3593 and belongs to Jump Freighters only. There is no extra Black Ops, carrier, dreadnought, or Capital Industrial Ships skill multiplier on own-ship jump fuel. The Rorqual's 5%/level fuel reduction is explicitly for the **Capital Industrial Core**, not its jump drive.

Economizer descriptions explicitly restrict fitting to **Jump Freighters and Rorquals**. JFs have three low slots; Rorqual has four (attribute 12). The fourth stacking coefficient is `0.282955154023261`. All four strengths are sorted before applying penalties, and fuel is rounded once after multiplication. Other hulls cannot fit these modules, regardless of low-slot count. The calculation API rejects incompatible loadouts; the UI disables incompatible slots and ignores their retained selections, with an explanatory label.

Generalized own-ship fuel formula:

```text
ceil(distanceLY * baseFuel * (1 - 0.1 * JFC)
     * (JF hull ? (1 - 0.1 * JumpFreighters) : 1)
     * compatibleEconomizerMultiplier)
```

JDC V / JFC V / JF V at exactly 1 LY therefore costs 350 isotopes for Black Ops, 1,500 for combat capitals, and 2,000 for Rorqual, without Economizers. Automated tests cover every class, faction exceptions, invalid fitting, fourth-module stacking, highsec gate restrictions, and capital totals. Existing JF real-route regressions remain unchanged. These new hull baselines are SDE-derived checks, not claimed live-client or independent planner observations. Existing rounding evidence below is retained; no correction constants were introduced.

Scope is a ship's own jump drive, not jump portals, conduit passengers, industrial cores, or micro jump drives. Cyno availability, fatigue, fitting/activation prerequisites, and gate adjacency are not validated. Highsec movement is restricted to Black Ops and Jump Freighters, and no hull can jump into highsec.

## Original Jump Freighter verification

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

Jump legs also reject a high-security destination and Pochven. A high-security system can still be an origin for a jump to eligible space, and high-security destinations remain available as Gate legs.

## Market pricing

Jita buy/sell uses CCP's public ESI regional market-orders endpoint for The Forge (region 10000002), filtered to Jita IV – Moon 4 – Caldari Navy Assembly Plant (location 60003760). Sell is the lowest station sell order; buy is the highest station buy order. ESI requires no login for this endpoint. Responses are cached locally and identified as cached/stale when live retrieval fails. Manual per-isotope prices remain available offline.

Sources: [CCP ESI overview](https://developers.eveonline.com/docs/services/esi/overview/) and [market-order rate-limit/cache guidance](https://developers.eveonline.com/blog/market-orders-rate-limit-rolls-out-on-february-24-2026).
