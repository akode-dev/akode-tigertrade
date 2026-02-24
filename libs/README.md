# TigerTrade DLL Dependencies

This project depends on proprietary TigerTrade assemblies that cannot be redistributed.

## Required Files

Place the following DLL files in this folder:

- `TigerTrade.Chart.dll`
- `TigerTrade.Core.dll`
- `TigerTrade.Dx.dll`
- `TigerTrade.Sockets.dll`
- `TigerTrade.Tc.dll`

## Where to Find Them

Typically inside your TigerTrade installation directory, for example:

- `C:\Program Files (x86)\TigerTrade\`
- `C:\Program Files\TigerTrade\`
- `C:\Users\<you>\AppData\Local\Programs\TigerTrade\`

## Setup Options

### Option A: Script (recommended)

From the repository root:

```powershell
.\scripts\setup-libs.ps1
```

### Option B: Manual copy

Copy the required DLLs listed above from your TigerTrade installation folder into `libs/`.

## Notes

- DLL files in this folder are ignored by git.
- If TigerTrade updates, re-run `setup-libs.ps1` to refresh local dependencies.
