# Sidebar Diagnostics

Sidebar Diagnostics is a modern, cross-platform system monitor for Windows, macOS, and Linux. This independent Avalonia port preserves the original application's glanceable sidebar experience while replacing its Windows-only WPF foundation with a clean, testable architecture.

This repository is a modified work based on [ArcadeRenegade/SidebarDiagnostics](https://github.com/ArcadeRenegade/SidebarDiagnostics), created for the 2026 Avalonia Port Challenge. It is not an official release from the original maintainer.

## Highlights

- One native Avalonia application for Windows, macOS, and Linux
- CPU, memory, primary-volume, and aggregate network monitoring
- Live bounded charts and configurable alert thresholds
- Broad hardware sensor support on Windows and hwmon temperatures on Linux
- Searchable hardware sensor catalog with visibility, pinning, ordering, and custom names
- Vendor-neutral multi-GPU summaries for Intel, AMD, and NVIDIA devices
- Safe external JSON metric cards from local files or explicit HTTP endpoints
- Resilient multi-display placement with mixed-DPI edge docking and primary-display fallback
- High-density CPU, memory, GPU, drive, network, and hardware detail sections
- Configurable clock, machine name, units, sidebar width, and opacity
- Tray controls and native launch-at-login integration on all three platforms
- Atomic JSON settings persistence
- Strict builds, unit tests, coverage artifacts, and locked restores
- Self-contained release archives, macOS app bundles, and SHA-256 checksums

## Platform support

| Capability | Windows | macOS | Linux |
| --- | --- | --- | --- |
| CPU | `GetSystemTimes` | `getloadavg` | `/proc/stat` |
| Memory | `GlobalMemoryStatusEx` | Mach + `sysctl` | `/proc/meminfo` |
| Storage and network | .NET platform APIs | .NET platform APIs | .NET platform APIs |
| Hardware sensors | LibreHardwareMonitor | No stable public system API | hwmon temperatures |
| Tray and launch at login | Supported | Supported | Supported |

The detailed comparison with the WPF application is maintained in [docs/PARITY.md](docs/PARITY.md).

## Build and test

Install the .NET 10 SDK, then run:

```shell
dotnet restore SidebarDiagnostics.slnx --locked-mode
dotnet build SidebarDiagnostics.slnx --configuration Release --no-restore
dotnet test SidebarDiagnostics.slnx --configuration Release --no-build
dotnet run --project src/SidebarDiagnostics.App.csproj
```

## Repository layout

```text
src/                         Application and platform providers
tests/                       Unit tests
docs/                        Architecture and migration records
.github/workflows/           CI and release automation
SidebarDiagnostics.slnx      Repository solution
```

## Releases

Tags matching `v*` run tests on Windows, macOS, and Linux, publish self-contained binaries, create platform-native archives, generate SHA-256 checksums, and attach them to a GitHub Release. macOS artifacts contain a conventional `.app` bundle but are not code-signed or notarized.

## License and attribution

This modified work is distributed under the [GNU General Public License v3.0](LICENSE.md), matching the original project. The complete corresponding source is this repository. See [NOTICE.md](NOTICE.md) for the modification notice and [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for dependency acknowledgements.
