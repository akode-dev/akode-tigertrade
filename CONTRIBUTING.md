# Contributing

Thanks for contributing to this repository.

## Scope

This repository is designed for multiple TigerTrade indicators.

Current implemented scope:

- `AkodeLevelsIndicator`
- `Helpers/CircularBuffer<T>`
- Build/deploy/docs around that scope

New indicator proposals are welcome. Please open an issue first to discuss naming, serialization compatibility, and maintenance expectations.

Still out of scope: Pine Script sources, decompiled/private code, and proprietary binaries.

## Development Setup

1. Install Visual Studio 2022 `17.13+` (or Build Tools with `.slnx` support).
2. Ensure .NET Framework 4.7.2 targeting pack is installed.
3. Clone the repository.
4. Copy TigerTrade DLLs into `libs/`:

```powershell
.\scripts\setup-libs.ps1
```

5. Build:

```powershell
msbuild Akode.TigerTrade.slnx /p:Configuration=Release /p:Platform="Any CPU"
```

## Coding Standards

- Language: C# (.NET Framework 4.7.2).
- Indentation: 4 spaces.
- Braces: Allman style.
- Namespaces/types: `PascalCase`.
- Private fields: `_camelCase`.
- Keep the indicator behavior deterministic and side-effect free.
- Avoid introducing dependencies outside TigerTrade DLLs + BCL.

## Naming Conventions

- Indicator ID format: `X_Akode<Name>Indicator`
- Display name format: `_Akode: <Name>`
- Keep serialization attributes (`[DataContract]`, `[DataMember]`) stable where possible.

## Testing Guidelines

Automated tests are not currently available. Manual verification is required:

1. Build Release successfully.
2. Deploy DLL to `%USERPROFILE%\Documents\TigerTrade\Indicators\`.
3. Open TigerTrade and add **_Akode: Levels**.
4. Validate behavior on multiple symbols/timeframes.
5. Verify settings updates and theme/template behavior.
6. Check broken-level rendering edge cases.

## Pull Request Guidelines

- Keep PRs focused and small.
- Include a clear summary and rationale.
- Include manual testing steps/results.
- Add screenshots/GIFs for visual behavior changes.
- Update `README.md` and `CHANGELOG.md` when behavior changes.

## Commit Message Style

Conventional commits are preferred:

- `feat: ...`
- `fix: ...`
- `refactor: ...`
- `docs: ...`
- `chore: ...`
