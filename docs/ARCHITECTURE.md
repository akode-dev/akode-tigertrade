# Architecture Overview

This repository is an open-source package of custom indicators for TigerTrade.
It is designed to host multiple indicators over time under one .NET assembly.

## Goals

- Keep indicator implementations isolated and easy to extend.
- Keep build and deployment simple for local development.
- Avoid shipping proprietary TigerTrade binaries in source control.

## Plugin Architecture (TigerTrade)

TigerTrade loads custom indicator assemblies from:

`%USERPROFILE%\Documents\TigerTrade\Indicators\`

An indicator becomes discoverable when its type is annotated with TigerTrade metadata attributes (for example, `Indicator` + `DataContract`) and compiled into the plugin DLL.

## Repository Structure

- `src/Akode.TigerTrade.Indicators/`
  Core C# project containing indicator implementations.
- `scripts/`
  Local automation for dependency setup, deployment, and release flow.
- `libs/`
  Local-only TigerTrade SDK/runtime dependencies used at compile time.
- `docs/`
  Architecture and project-level documentation.

## Runtime Model (Generic)

Each indicator follows the same high-level execution model:

1. TigerTrade loads plugin DLLs on startup.
2. User adds an indicator to a chart.
3. Indicator reads chart data through TigerTrade APIs.
4. Indicator computes derived series in `Execute()`.
5. Indicator publishes renderable series/objects back to TigerTrade.
6. Property changes trigger recalculation via the platform lifecycle.

## Extension Model

When adding a new indicator:

1. Add a new indicator class in `src/Akode.TigerTrade.Indicators/`.
2. Register metadata attributes required by TigerTrade.
3. Expose user-configurable properties with stable serialization names.
4. Render output through indicator series/drawing primitives.
5. Document usage/settings in `README.md`.

Indicator-specific behavior (logic, settings, formulas) belongs in `README.md`, not in this architecture document.

## Build Dependency Model

The project references TigerTrade DLLs from `libs/` via `HintPath`.
These DLLs are proprietary, local-only, and excluded from git.

## Scope Notes

- In scope: custom indicator source code, scripts, docs, and release assets for this package.
- Out of scope: decompiled/private code and proprietary TigerTrade DLL redistribution.
