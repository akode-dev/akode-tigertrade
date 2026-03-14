# Akode TigerTrade Indicators

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![.NET Framework 4.7.2](https://img.shields.io/badge/.NET%20Framework-4.7.2-blue.svg)](https://dotnet.microsoft.com/)
[![TigerTrade](https://img.shields.io/badge/TigerTrade-6.9%2B-orange.svg)](https://www.tiger.com/terminal)

Open-source TigerTrade custom indicators repository.

This package is designed as a collection of indicators and will expand over time.

## Indicators

### Akode Levels (`AkodeLevelsIndicator`)

Pivot-based support/resistance level detector.

It detects pivot highs/lows and draws horizontal support and resistance levels.
It supports aggregation to higher intervals, tracks broken levels, and limits visible active/broken lines independently for highs and lows.

#### Features

- Pivot-based support/resistance detection from chart highs and lows.
- Optional timeframe aggregation: Any, Minute, Hour, Week, Month.
- Configurable pivot sensitivity (`Candles before` / `Candles after`).
- Independent limits for visible high and low levels.
- Optional rendering of broken levels with dotted style.
- Theme/template integration through TigerTrade indicator APIs.

#### Settings

| Parameter | Default | Description |
| --- | --- | --- |
| Interval | Any Time Frame | Aggregation interval for pivot detection. |
| Value | 1 | Multiplier for selected interval. |
| Candles before | 2 | Bars to the left required for pivot confirmation. |
| Candles after | 2 | Bars to the right required for pivot confirmation. |
| Max High lines to show | 15 | Max active resistance levels displayed. |
| Max Low lines to show | 15 | Max active support levels displayed. |
| Show broken lines | true | Show levels that were breached by price. |
| Max High lines (broken) | 2 | Max broken resistance levels displayed. |
| Max Low lines (broken) | 2 | Max broken support levels displayed. |
| High levels | Green line | Style/color for resistance levels. |
| Low levels | Red line | Style/color for support levels. |

### Akode Trends (`AkodeTrendsIndicator`)

Multi-profile support/resistance and trendline overlay.

It combines up to five independent level-detection profiles inside one indicator,
then builds shared upper/lower trend rays from the combined visible support and resistance levels.

#### Features

- Up to 5 independent internal profiles in one indicator instance.
- Per-profile timeframe, pivot sensitivity, body/wick mode, limits, broken-level handling, and display styles.
- Horizontal levels rendered separately per profile.
- Combined upper and lower trend rays built from all visible levels.
- Multiple selectable trendline ranking modes: classic touches, nearest price, higher timeframe, hybrid clean, outer envelope, consensus, and weighted regression.
- Independent slope filters for upper and lower trendlines.
- Configurable cleanup for old support/resistance crossings.
- Optional hiding of trendlines already broken by candle bodies.
- Theme/template integration through TigerTrade indicator APIs.

#### Settings

| Parameter | Default | Description |
| --- | --- | --- |
| Profile 1 | enabled | First internal levels profile. |
| Profile 2-5 | disabled | Additional internal levels profiles. |
| Show trend lines | true | Toggle shared trendline rendering. |
| Max High trend lines | 3 | Max upper trend rays displayed. |
| Max Low trend lines | 3 | Max lower trend rays displayed. |
| Tolerance in ticks | 2 | Touch tolerance used when scoring trendlines. |
| Min High touches | 2 | Minimum touches required for upper trendlines. |
| Min Low touches | 2 | Minimum touches required for lower trendlines. |
| Left padding bars | 3 | Extend trendlines a few bars to the left of the first touch. |
| Right padding bars | 20 | Extend trendlines into the empty chart area to the right. |
| High: only down slope | true | Restrict upper trendlines to descending slope. |
| Low: only up slope | true | Restrict lower trendlines to ascending slope. |
| Trendline algorithm | Classic Touches | Select the ranking/cleanup mode for trendline candidates. |
| Allowed past crossing bars | 5 | Allow upper/lower intersections only within the last N bars to the left of the current bar. |
| Hide broken trend lines | true | Hide trendlines already broken by candle bodies. |
| Break bars | 2 | Total number of body-break bars after the first anchor point required to hide a trendline. |
| Break tolerance in ticks | 2 | Extra tolerance beyond the trendline before a body-break counts. |

More indicators can be added to this package over time.

## Screenshots

| Indicator | Preview |
| --- | --- |
| Akode Levels | ![Akode Levels](docs/images/levels-indicator.png) |
| Akode Trends | Screenshot TBD |

## Requirements

- Windows with TigerTrade installed (tested against `6.9+`).
- .NET Framework 4.7.2 targeting pack.
- Visual Studio 2022 `17.13+` or .NET SDK `9.0.200+` (required for `.slnx` support).
- Local TigerTrade DLLs placed in `libs/` (see [libs/README.md](libs/README.md)).

## Quick Start

### For Developers (build from source)

1. Copy TigerTrade DLL dependencies into `libs/`:

```powershell
.\scripts\setup-libs.ps1
```

2. Build:

```powershell
msbuild Akode.TigerTrade.slnx /p:Configuration=Release /p:Platform="Any CPU"
```

If `msbuild` is not available in `PATH`, use:

```powershell
dotnet msbuild Akode.TigerTrade.slnx /p:Configuration=Release /p:Platform="Any CPU"
```

3. Deploy to TigerTrade:

```powershell
.\scripts\deploy.ps1
```

4. Restart TigerTrade and add **_Akode: Levels** or **_Akode: Trends** to chart.

### Manual Install (pre-built DLL)

1. Download `Akode.TigerTrade.Indicators.dll` from [Releases](../../releases).
2. Copy the DLL to:

```text
%USERPROFILE%\Documents\TigerTrade\Indicators\
```

3. Restart TigerTrade and add **_Akode: Levels** or **_Akode: Trends** to chart.

## Releasing on GitHub

`libs/*.dll` are proprietary TigerTrade dependencies and are ignored by git.
They stay local and are used only to compile.

Recommended release flow:

1. Prepare local dependencies:

```powershell
.\scripts\setup-libs.ps1
```

2. Build Release:

```powershell
msbuild Akode.TigerTrade.slnx /p:Configuration=Release /p:Platform="Any CPU"
```

Or:

```powershell
dotnet msbuild Akode.TigerTrade.slnx /p:Configuration=Release /p:Platform="Any CPU"
```

3. Publish only the built plugin DLL from:

`src/Akode.TigerTrade.Indicators/bin/Release/Akode.TigerTrade.Indicators.dll`

4. Create Git tag and GitHub Release, then upload this DLL as a release asset.

If you later automate releases via CI, use a self-hosted runner with local TigerTrade DLLs.
Do not upload proprietary TigerTrade DLLs to the repository or release assets.

## Project Structure

```text
.
|- .github/
|- docs/
|  |- images/
|  \- ARCHITECTURE.md
|- libs/
|- scripts/
|- src/
|  \- Akode.TigerTrade.Indicators/
|     |- AkodeTrendsIndicator.cs
|     |- AkodeTrendsProfileSettings.cs
|     |- AkodeLevelsIndicator.cs
|     |- Helpers/
|     |- Properties/
|     \- Akode.TigerTrade.Indicators.csproj
|- Akode.TigerTrade.slnx
|- CHANGELOG.md
|- CONTRIBUTING.md
|- CODE_OF_CONDUCT.md
\- LICENSE
```

## Build Output

Release assembly path:

`src/Akode.TigerTrade.Indicators/bin/Release/Akode.TigerTrade.Indicators.dll`

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for development workflow, coding standards, and PR expectations.

## License

MIT License. See [LICENSE](LICENSE).

Tiger Trade is a product of Tiger Trade Capital AG. This repository is independent and not affiliated with Tiger Trade Capital AG.
