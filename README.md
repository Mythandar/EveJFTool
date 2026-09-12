# EveJFTool

EveJFTool is a Windows WPF application that calculates straight-line jump-drive route distances, per-leg isotope use, and ISK fuel cost. Routes are supplied by the user and each leg independently supports Jump/Gate mode and an economizer loadout.

Version 1.1 supports all 56 published jump-drive hulls in CCP SDE build 3503375: Jump Freighters, Black Ops, carriers, Command Carriers, Force Auxiliaries, dreadnoughts, Lancer Dreadnoughts, supercarriers, titans, and the Rorqual, including faction and special-edition variants. It supports JDC/JFC/JF skill levels; per-leg Limited, Experimental, or Prototype Economizers (three slots on Jump Freighters, four on the Rorqual); Jita sell, Jita buy, and manual prices; route editing, mirrored return trips, validation, and local saved routes. After two characters, system search presents all matches in a scrollable popup with at most ten visible rows. It intentionally does not perform route finding.

Only the selected ship's own jump-drive fuel is calculated. Jump bridges, conduit passengers, industrial cores, micro jump drives, cyno fuel, fatigue, and operational fitting/cyno checks are not included. Gate entries remain user-supplied connections, not verified stargate links. Capitals are prevented from routing through highsec; Jump Freighters and Black Ops may enter highsec by gate. Disabled Economizer selections are retained for switching back to a compatible hull but do not reduce fuel.

## Download and run

Download the latest `EveJFTool-*-win-x64.zip` from [GitHub Releases](https://github.com/Mythandar/EveJFTool/releases), extract it, and run `EveJFTool.exe`. The release is self-contained and does not require a separate .NET installation.

The application is currently unsigned, so Windows SmartScreen may show an unrecognized-app warning. Verify the ZIP against the published SHA-256 checksum before running it.

## Using the calculator

1. Select the ship (type its name in the dropdown) and character skill levels. The JF skill is disabled for other hull classes because it does not affect their fuel.
2. Choose Jita sell, Jita buy, or manual isotope pricing.
3. Type at least two characters of a solar-system name and select a match, then add it to the route.
4. Set each leg to Jump or Gate and select Economizer types where the hull supports them.
5. Review per-leg fuel and ISK cost plus the route totals. Use **Add Return Trip** to append a mirrored trip with the same leg configurations.

Routes and user preferences can be saved locally. EVE login is not required.

## Requirements and build

- Windows x64
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (current LTS)
- Visual Studio 2022 with .NET desktop development, or the `dotnet` CLI

```powershell
dotnet restore EveJFTool.sln
dotnet build EveJFTool.sln -c Release
dotnet test EveJFTool.sln -c Release
dotnet run --project src/EveJFTool.App/EveJFTool.App.csproj
```

Release output is under `src/EveJFTool.App/bin/Release/net10.0-windows/`.

To create the self-contained release ZIP and checksum:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\publish-release.ps1 -Version 1.1.0
```

## Architecture

- `EveJFTool.App`: WPF views, MVVM view models, commands, formatting, and interaction.
- `EveJFTool.Core`: immutable ship/skill/route models and distance/fuel/route calculation services. It has no WPF or networking dependency.
- `EveJFTool.Data`: bundled/current CCP universe data, local cache, ESI pricing, JSON settings/routes, and file logging.
- `EveJFTool.Tests`: unit and regression tests for formulas, validation, totals, persistence, and real routes.

The provider interfaces in Core keep market and universe sources replaceable. Economizers are modeled as effect data and loadouts rather than special-case booleans, leaving room for later module/effect types and other jump-capable ships.

## Data and formulas

The bundled solar-system database is `mapSolarSystems.jsonl` from CCP SDE build 3494416 (2026-09-04). It is copied to the user cache at first run. Distances use CCP's 3D system coordinates and exact EVE conversion of `9.46e15` meters per light-year.

Fuel is calculated from the hull's SDE consumption attribute, JFC reduction, the JF reduction only on Jump Freighters, and compatible stacking-penalized Economizers; the result is rounded up once to a whole isotope. JDC affects maximum range only. Full evidence and the DOTLAN comparison are in [docs/eve-mechanics.md](docs/eve-mechanics.md).

Jita pricing comes directly from CCP ESI market orders in The Forge, filtered to station 60003760. The app uses lowest sell or highest buy and retains the most recent successful response. Manual pricing works with no network.

## User data

By default, all writable application data stays with the extracted application:

```text
<EveJFTool folder>\Data\
  settings.json
  routes.json
  universe\mapSolarSystems.jsonl
  cache\market-prices.json
  logs\EveJFTool.log
```

On first launch, existing data from `%LOCALAPPDATA%\EveJFTool\` is copied into this folder when a destination file does not already exist. The original files are retained as a backup.

If the application folder is read-only, EveJFTool falls back to `%LOCALAPPDATA%\EveJFTool\` so it can continue to run without administrator rights.

Generated `bin/`, `obj/`, Visual Studio state, test results, and the project-local CLI cache are ignored by Git.

## Disclaimer

EVE Online and all related trademarks are the property of CCP hf. EveJFTool is an independent community tool and is not affiliated with or endorsed by CCP hf.
