# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
