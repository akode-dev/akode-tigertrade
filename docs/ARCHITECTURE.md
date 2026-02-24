# Architecture Overview

This repository is a TigerTrade custom indicators workspace.
The package includes `AkodeLevelsIndicator` and is intended to include more indicators over time.

## TigerTrade Plugin Model

TigerTrade discovers custom indicators from compiled .NET Framework assemblies copied into:

`%USERPROFILE%\Documents\TigerTrade\Indicators\`

At runtime, TigerTrade loads types decorated with indicator metadata attributes and exposes them in the UI.

## Included Indicator

- `AkodeLevelsIndicator`:
  Pivot-based support/resistance detector with optional broken-level rendering.

## Key Namespaces Used

- `TigerTrade.Chart.Base`
- `TigerTrade.Chart.Indicators.Common`
- `TigerTrade.Chart.Indicators.Drawings`
- `TigerTrade.Core.Utils.Time`
- `TigerTrade.Dx`

## Indicator Lifecycle

1. TigerTrade loads plugin DLLs on startup.
2. Indicator metadata (`[Indicator]`, `[DataContract]`) registers the indicator.
3. User adds **_Akode: Levels** to a chart.
4. On data updates (`Calculation = OnBarClose`), `Execute()` recomputes levels.
5. Indicator emits line series into `Series` for chart rendering.
6. Settings changes trigger recalculation via `OnPropertyChanged()`.

## Data Flow

1. Read raw OHLC arrays from `Helper` (`Date`, `High`, `Low`).
2. Optionally aggregate bars to selected timeframe (`BuildOnTimeframe`).
3. Detect pivot highs/lows using `CandlesBefore`/`CandlesAfter` windows.
4. Mark levels as broken when subsequent prices breach pivot value.
5. Keep visible levels constrained by max-unbroken/max-broken limits.
6. Render line series from pivot start index to latest bar.

## Build-Time Dependency Model

The project references TigerTrade DLLs from `libs/` via `HintPath`.
DLLs are not stored in git and must exist locally to compile.

## Included Sources

Included indicator source:

- `AkodeLevelsIndicator.cs`

Future indicators can be added in `src/Akode.TigerTrade.Indicators/`.
Still out of scope: decompiled/private code and proprietary binaries.
