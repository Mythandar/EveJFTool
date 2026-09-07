# EveJFTool

EveJFTool is a Windows WPF application that calculates straight-line Jump Freighter route distances, per-leg isotope use, and ISK fuel cost. Routes are supplied by the user and each leg independently supports Jump/Gate mode and an economizer loadout.

Version 1 supports Rhea, Anshar, Ark, and Nomad; JDC/JFC/JF skill levels; all mixed combinations of up to three Jump Drive Economizers; Jita sell, Jita buy, and manual prices; route editing; validation; and local saved routes. It intentionally does not perform route finding.

## Requirements and build

- Windows x64
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (current LTS)
- Visual Studio 2022 with .NET desktop development, or the `dotnet` CLI

```powershell
dotnet restore EveJFTool.slnx
dotnet build EveJFTool.slnx -c Release
dotnet test EveJFTool.slnx -c Release
dotnet run --project src/EveJFTool.App/EveJFTool.App.csproj
```

Release output is under `src/EveJFTool.App/bin/Release/net10.0-windows/`.

## Architecture

- `EveJFTool.App`: WPF views, MVVM view models, commands, formatting, and interaction.
- `EveJFTool.Core`: immutable ship/skill/route models and distance/fuel/route calculation services. It has no WPF or networking dependency.
- `EveJFTool.Data`: bundled/current CCP universe data, local cache, ESI pricing, JSON settings/routes, and file logging.
- `EveJFTool.Tests`: unit and regression tests for formulas, validation, totals, persistence, and real routes.

The provider interfaces in Core keep market and universe sources replaceable. Economizers are modeled as effect data and loadouts rather than special-case booleans, leaving room for later module/effect types and other jump-capable ships.

## Data and formulas

The bundled solar-system database is `mapSolarSystems.jsonl` from CCP SDE build 3494416 (2026-09-04). It is copied to the user cache at first run. Distances use CCP's 3D system coordinates and exact EVE conversion of `9.46e15` meters per light-year.

Fuel is calculated from the hull's SDE consumption attribute, JFC and JF multiplicative skill reductions, and stacking-penalized economizer modifiers; the result is rounded up once to a whole isotope. JDC affects maximum range only. Full evidence and the DOTLAN comparison are in [docs/eve-mechanics.md](docs/eve-mechanics.md).

Jita pricing comes directly from CCP ESI market orders in The Forge, filtered to station 60003760. The app uses lowest sell or highest buy and retains the most recent successful response. Manual pricing works with no network.

## User data

No administrator rights are needed. Files are stored below:

```text
%LOCALAPPDATA%\EveJFTool\
  settings.json
  routes.json
  universe\mapSolarSystems.jsonl
  cache\market-prices.json
  logs\EveJFTool.log
```

Generated `bin/`, `obj/`, Visual Studio state, test results, and the project-local CLI cache are ignored by Git.
