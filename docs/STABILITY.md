# Stability verification

## Automated coverage

The test suite verifies provider timeouts, caller cancellation, failure recovery, cached-sample fallback, and single-flight polling. CI executes the suite on Windows, macOS, and Linux. macOS additionally exercises its native Mach metrics and per-core sensor calls.

## Soak procedure

Publish the target runtime, close other copies of Sidebar Diagnostics, and run the monitor for at least one hour:

```powershell
dotnet publish src/SidebarDiagnostics.App.csproj --configuration Release --runtime win-x64 --self-contained true --output artifacts/soak/win-x64
./scripts/Measure-Soak.ps1 -Executable artifacts/soak/win-x64/SidebarDiagnostics.App.exe -DurationMinutes 60
```

The default acceptance limits, measured relative to the first sample, are:

- 150 MB maximum working-set growth
- 10 additional threads
- 100 additional handles on Windows
- no unexpected process exit

Run the same procedure after repeatedly opening and saving settings, changing sensor visibility, hiding and showing the sidebar, and attaching or removing displays. For macOS and Linux, pass the corresponding published executable path. Handle growth is Windows-only; working set and thread growth are checked on every platform.

The command prints a JSON summary and exits unsuccessfully if a limit is exceeded. Preserve the command, application version, operating system, duration, and summary when reporting a regression. Do not attach settings files or raw system/sensor dumps.

## Troubleshooting

When hardware polling times out or fails, the sidebar continues with the last successful sample and reports a concise status. Confirm that the application is current, then restart it once. On Windows, install PawnIO independently only if the additional low-level sensors are required and its security model is acceptable for the machine. Never download or allow WinRing0 to restore missing sensors.

If growth exceeds a threshold, reproduce with default settings and then disable external metrics and optional sensors to narrow the provider. A rollback means reinstalling the previous immutable tagged artifact and its recorded checksum; settings remain compatible unless release notes explicitly state otherwise.
