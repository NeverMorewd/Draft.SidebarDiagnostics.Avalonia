# Security policy

## Reporting a vulnerability

Report suspected vulnerabilities through a private GitHub security advisory for this repository. Do not include machine names, usernames, IP addresses, sensor dumps, access tokens, or other personal system data. Use a public issue only for non-sensitive hardening work.

## Hardware access model

Sidebar Diagnostics does not ship, install, start, or update a kernel driver. Windows hardware data is read through LibreHardwareMonitor 0.9.6, whose published library contains the PawnIO implementation used for privileged low-level access when PawnIO is already installed by the user. If privileged access is unavailable, affected sensors may remain unavailable. The application never asks the user to install or allow WinRing0.

The LibreHardwareMonitor dependency graph contains RAM SPD compatibility code with a `WinRing0` API type name. A type name is not a driver payload. Release auditing therefore rejects driver filenames, service/device identifiers, and OpenLibSys markers while allowing inert compatibility symbols. The audit also requires the expected PawnIO implementation marker in the published hardware library.

Run the same artifact audit used by CI:

```powershell
dotnet publish src/SidebarDiagnostics.App.csproj --configuration Release --runtime win-x64 --self-contained true --output artifacts/security/win-x64
./scripts/Test-ReleaseSecurity.ps1 -PublishDirectory artifacts/security/win-x64
```

## Failure isolation and diagnostics

Hardware polling runs outside the UI thread, allows only one in-flight provider call, and has a two-second deadline. A stalled or failed provider returns the last successful sample and cannot create an unbounded queue of worker tasks. Providers receive cancellation when the deadline or application lifetime expires.

Diagnostics contain only an area, outcome, exception type, and elapsed time. Exception messages, file paths, source URLs, hardware identifiers, machine names, and metric values are not logged. Avalonia and application diagnostics are emitted through the process trace listeners.

## Dependency policy

Locked NuGet restores audit direct and transitive packages. Moderate, high, and critical advisories fail the build. Pull requests also run GitHub dependency review. Release artifacts retain the project license, modification notice, and third-party notices.
