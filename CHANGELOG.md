# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.7.0] - 2026-04-09

### Added

- Confirmation dots for retested levels — draws dots at the original pivot (point 1) and the closest retest (point 2) on horizontal support/resistance levels. Configurable tolerance (%), min bars between touches, dot size, dot color, and dedicated timeframe (minutes).
- Gap-to-next-level indicator on distance labels with solid triangles (▲ for resistance, ▼ for support).
- Configurable font size and bold for both distance and gap labels.
- Configurable decimal precision for distance % labels.

### Fixed

- Confirmation dot algorithm now picks the globally closest retest bar instead of first-in-zone.
- Confirmation dots stay stable when switching chart to lower timeframes via dedicated timeframe setting.

## [1.6.0] - 2026-03-21

### Added

- New indicator _Akode: Funding Timer — draws vertical lines at crypto funding times with configurable hours (default 0,8,16 UTC), time offset, and "show next only" mode.
- Extend horizontal level lines to the right edge with dynamic buffer (afterBars + 5000) in both _Akode: Levels and _Akode: Trends indicators.

### Fixed

- Disabled selection handles (yellow control points) on indicator lines.

## [1.5.0] - 2026-03-21

### Added

- Right padding for level lines in _Akode: Levels — lines now extend 500 bars beyond the last candle to reach the right edge of the chart (configurable via "Right padding bars" setting).

### Fixed

- Disabled selection handles (yellow control points) on all indicator lines — clicking on lines no longer shows draggable squares.

## [1.4.0] - 2026-03-20

### Added

- Round level highlighting for both _Akode: Levels and _Akode: Trends — custom style for horizontal levels at round price numbers, with configurable step and tolerance.
- Distance percent labels for _Akode: Levels (ported from _Akode: Trends).
- Tested level detection (wick pierce without close breakout) with separate styling for both indicators.
- Disclaimer section in README.

### Changed

- IsBroken now checks close instead of wick for more accurate level breakout detection.
- Extracted shared `RoundPriceHelper` to reduce code duplication.

### Removed

- Base line detection from _Akode: Trends.

## [1.3.0] - 2026-03-20

### Added

- GitHub Actions CI workflow for build verification on push and pull requests.
- GitHub Actions Release workflow for automated DLL builds and GitHub Releases on tag push.

### Changed

- Committed TigerTrade reference assemblies to enable CI builds.
- Updated release script to defer to CI by default (added `-SkipCI` flag for manual releases).

## [1.2.0] - 2026-03-15

### Added

- Base line detection — single strongest horizontal level based on pivot density and confirmed price bounces, with configurable lookback window (bars/minutes).
- Live percent-distance labels rendered on the chart for nearest visible support/resistance levels.
- Price scale labels for visible horizontal levels via GetLabels override.

### Changed

- Tuned default values for better out-of-box experience (tolerance, padding, algorithm, merge/level limits).
- Replaced profile color arrays with per-profile display presets (width, dash style, color per profile index).

## [1.1.0] - 2026-03-15

### Added

- `AkodeTrendsIndicator` with 5 independent internal levels profiles.
- Shared upper/lower trendline rendering from combined visible support and resistance levels.
- Per-profile candle body mode for pivot detection.
- Configurable left/right padding bars for trendline overhang.
- Selectable trendline algorithms: Classic Touches, Weighted Regression, RANSAC, Hough Transform.
- Body-based hiding of already broken trendlines with configurable bar count and tolerance.
- Global level filters: max total high/low levels, time filter (minutes), level merge (ticks).
- Per-profile "Include in trend lines" toggle.
- "Apply filters to trend lines" toggle for level filter propagation.
- Trendline memory (bars/minutes) to prevent redrawing.
- Trend merge (ticks) for deduplicating similar diagonal trendlines.

### Changed

- Simplified trendline algorithms to two core types, then expanded with RANSAC and Hough Transform.
- Removed legacy trendline algorithm enum values.

## [1.0.0] - 2026-02-24

### Added

- Initial open-source repository structure for TigerTrade custom indicators.
- `AkodeLevelsIndicator` (pivot-based support/resistance detector).
- `CircularBuffer<T>` helper dependency.
- Timeframe aggregation support (Any, Minute, Hour, Week, Month).
- Configurable pivot parameters and high/low line limits.
- Broken-level rendering support with independent limits.
- Build/deploy scripts and open-source project documentation.
