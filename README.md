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
- Native Windows AppBar integration that reserves the docked desktop work area
- Configurable global shortcuts for show/focus, hide, and visibility toggle
- High-density CPU, memory, GPU, drive, network, and hardware detail sections
- Configurable clock, machine name, units, sidebar width, and opacity
- Live switching between the default Fluent-based appearance and the monochromatic Pip-Boy theme
- Tray controls and native launch-at-login integration on all three platforms
- Atomic JSON settings persistence
- Strict builds, unit tests, coverage artifacts, and locked restores
- Bounded single-flight hardware polling with cross-platform failure isolation
- Automated dependency, legacy-driver artifact, and long-running stability audits
- Self-contained release archives, macOS app bundles, and SHA-256 checksums

## Platform support

| Capability | Windows | macOS | Linux |
| --- | --- | --- | --- |
| CPU | `GetSystemTimes` | Mach CPU ticks, including per-core load | `/proc/stat`, including per-core load |
| Memory | `GlobalMemoryStatusEx` | Mach + `sysctl` | `/proc/meminfo` |
| Storage and network | .NET platform APIs | .NET platform APIs | .NET platform APIs |
| Hardware sensors | LibreHardwareMonitor | No stable public system API | hwmon temperatures |
| Tray and launch at login | Supported | Supported | Supported |

The detailed comparison with the WPF application is maintained in [docs/PARITY.md](docs/PARITY.md).

### Global shortcut support

Global shortcuts use a platform-neutral `Ctrl+Alt+Key` style syntax. Windows works without additional permissions. macOS requires Accessibility permission. X11 is supported through the native input hook. Wayland support depends on compositor and input-device permissions; the settings screen reports when the session denies global input access. Duplicate shortcuts are rejected without affecting the running monitor, and clearing a shortcut disables that action.

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

Tags matching `v*` run tests on Windows, macOS, and Linux, publish trimmed Native AOT binaries for Windows x64, Linux x64/ARM64, and macOS x64/ARM64 on matching-architecture runners, create platform-native archives, generate SHA-256 checksums, and attach them to a GitHub Release. macOS artifacts contain a conventional `.app` bundle but are not code-signed or notarized.

Each release also contains APT-installable Debian packages plus generated WinGet and Homebrew metadata tied to the immutable artifacts. Installation, upgrade, uninstall, rollback, community-index status, and Flatpak tradeoffs are documented in [docs/PACKAGE_MANAGERS.md](docs/PACKAGE_MANAGERS.md).

Package-manager installation from a tagged GitHub Release:

```powershell
winget install --manifest .\winget\<version>
```

```shell
brew install --cask ./homebrew/Casks/sidebar-diagnostics-avalonia.rb
sudo apt install ./SidebarDiagnostics-linux-x64.deb
```

The WinGet manifest and Homebrew Cask are inside the release metadata bundle. Public-index commands become available after their external repository reviews; the Debian package works directly with APT.

## Security and stability

The hardware access trust model and vulnerability reporting process are documented in [SECURITY.md](SECURITY.md). Repeatable timeout, artifact, dependency, and soak-test procedures are documented in [docs/STABILITY.md](docs/STABILITY.md).

## Themes

The default Sidebar theme uses Fluent control templates with a dedicated semantic palette. The optional Pip-Boy appearance uses [Pipboy.Avalonia](https://github.com/NeverMorewd/Pipboy.Avalonia) and maps the same application-level design tokens onto its generated monochromatic palette. Theme changes preview immediately in Settings, persist only when saved, and revert when the dialog is cancelled or closed.

## License and attribution

This modified work is distributed under the [GNU General Public License v3.0](LICENSE.md), matching the original project. The complete corresponding source is this repository. See [NOTICE.md](NOTICE.md) for the modification notice and [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for dependency acknowledgements.
