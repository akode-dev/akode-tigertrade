# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `AkodeTrendsIndicator` with 5 independent internal levels profiles.
- Shared upper/lower trendline rendering from combined visible support and resistance levels.
- Per-profile candle body mode for pivot detection in the new trends indicator.
- Configurable left/right padding bars for trendline overhang.
- Selectable trendline algorithms with price/timeframe-aware ranking and same-side cleanup.
- Configurable filtering of old support/resistance crossings for trendline pairs.
- Additional trendline modes: outer envelope, consensus, and weighted regression.
- Body-based hiding of already broken trendlines with configurable bar count and tolerance.
- `Break bars` now counts total body-break bars after the first anchor point instead of only consecutive breaks after the second point.

## [1.0.0] - 2026-02-24

### Added

- Initial open-source repository structure for TigerTrade custom indicators.
- `AkodeLevelsIndicator` (pivot-based support/resistance detector).
- `CircularBuffer<T>` helper dependency.
- Timeframe aggregation support (Any, Minute, Hour, Week, Month).
- Configurable pivot parameters and high/low line limits.
- Broken-level rendering support with independent limits.
- Build/deploy scripts and open-source project documentation.
