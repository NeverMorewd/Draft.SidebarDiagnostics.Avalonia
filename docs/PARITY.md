# WPF Migration Parity

This audit compares the Avalonia port with the public features and settings of the original WPF application. It distinguishes portable behavior from Windows shell integration.

## Monitoring

| Original capability | Port status | Notes |
| --- | --- | --- |
| CPU utilization | Complete | Native provider on every platform. |
| Memory utilization and used memory | Complete | Native provider on every platform. |
| Logical drive utilization | Redesigned | The compact view presents the primary volume. |
| Network download and upload | Complete | Aggregate interface throughput. |
| Hardware sensors | Platform-limited | Broad LibreHardwareMonitor support on Windows; standard hwmon sensors on Linux; no stable public macOS equivalent. |
| Live graphs | Complete | Every primary metric has bounded history. |
| Dedicated configurable graph window | Redesigned | Integrated charts replace the secondary window. |
| External and local IP display | Complete | Local addresses are shown automatically. External address lookup is opt-in and cached. |

## Presentation and settings

| Original capability | Port status | Notes |
| --- | --- | --- |
| Compact sidebar | Complete | Responsive Avalonia layout. |
| Machine name, clock, date, and 12/24-hour format | Complete | Individually configurable. |
| Alert thresholds | Complete | CPU, memory, storage, and network thresholds. |
| Celsius and Fahrenheit | Complete | Applied to temperature values. |
| Width, opacity, always-on-top, start minimized | Complete | Persisted cross-platform settings. |
| UI scale, fonts, alignment, offsets, and arbitrary colors | Redesigned | The port uses a curated accessible design system. |
| Per-monitor and per-sensor configuration | Complete | Display selection and searchable sensor visibility, pinning, naming, and ordering are persisted. |
| Localization | Not migrated | Current release is English-only. |

## Desktop integration

| Original capability | Port status | Notes |
| --- | --- | --- |
| Tray show, hide, and exit | Complete | Avalonia desktop integration. |
| Launch at login | Complete | Native implementation on all three platforms. |
| Windows AppBar reserved work area | Complete on Windows | Native AppBar integration reserves the selected screen edge. |
| Edge docking and multi-monitor repositioning | Complete | Placement follows the selected display and recovers from topology changes. |
| Click-through and Alt-Tab/tool-window modes | Not migrated | Require platform window-manager adapters. |
| Global hotkeys | Complete | Cross-platform native hooks; macOS and some Linux sessions require operating-system permissions. |
| Automatic application updates | Replaced | GitHub Releases provide immutable checksummed packages. |

## Acceptance baseline

The cross-platform scope covers monitoring, persistence, alerts, charts, tray behavior, and startup integration on each supported operating system. Windows-shell-only behavior is tracked explicitly and is never represented as portable functionality.
