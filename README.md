# DynamoLauncher

A single-exe console launcher for selecting and launching Dynamo builds. Scans sibling subfolders, presents a numbered list, launches the chosen version.

## Usage

Place `DynamoLauncher.exe` in the same folder as your Dynamo build subfolders:

```
DynamoBuilds\
├── DynamoLauncher.exe          ← launcher lives here
├── DynamoCoreRuntime_4.1.0.4654_20260411T1527\
│   └── DynamoSandbox.exe
├── DynamoCoreRuntime_4.0.2.1234_20251201T0900\
│   └── DynamoSandbox.exe
└── ...
```

Run `DynamoLauncher.exe` (double-click or from terminal). Pick a number, press Enter.

```
  ╔══════════════════════════════╗
  ║     DYNAMO LAUNCHER          ║
  ╚══════════════════════════════╝

  Scanning: C:\DynamoBuilds\

  #   VERSION         BUILD DATE          FOLDER
  ────────────────────────────────────────────────────────────────────────────────
  1   4.1.0.4654      2026-04-11  15:27   DynamoCoreRuntime_4.1.0.4654_20260411T1527
  2   4.0.2.1234      2025-12-01  09:00   DynamoCoreRuntime_4.0.2.1234_20251201T0900

  Number to launch, [R] refresh, [Q] quit:
```

| Key | Action |
|-----|--------|
| `1`–`n` + Enter | Launch that build |
| `R` | Rescan subfolders |
| `Q` | Quit |

## Exe discovery

Searches each subfolder for these executables in order, uses first match:

1. `DynamoSandbox.exe`
2. `DynamoWPFCLI.exe`
3. `DynamoCLI.exe`

## Version / date parsing

Parses folder names matching `_<version>_<YYYYMMDDTHHmm>` (e.g. `DynamoCoreRuntime_4.1.0.4654_20260411T1527`).

Fallbacks when parsing fails:
- **Version** → `FileVersionInfo` from the exe
- **Date** → `File.GetLastWriteTime` of the exe

## Build

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download).

```
dotnet publish -c Release
```

Output: `dist\DynamoLauncher.exe` — self-contained single exe, no runtime required on target machine.

## Requirements

- Windows x64
- No .NET runtime required on target machine (self-contained)
