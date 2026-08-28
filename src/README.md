# Sidebar Diagnostics

Sidebar Diagnostics is a cross-platform Avalonia revival of the original Windows sidebar monitor. It is being rebuilt around a portable monitoring core and explicit operating-system adapters for Windows, macOS, and Linux.

## Current milestone

- Avalonia 12 desktop shell for Windows, macOS, and Linux
- System-wide CPU and memory measurements through native platform providers
- Primary-volume utilization
- Aggregate network throughput
- Responsive sidebar layout with a compact dark visual system
- MVVM state separated from metric collection

## Architecture

Views consume immutable snapshots through `ISystemMetricsService`; they do not call operating-system APIs. Platform-specific sensor providers can therefore be added without changing presentation code.

## Build

```shell
dotnet restore
dotnet build
dotnet run --project SidebarDiagnostics.App.csproj
```

## Roadmap

1. Port hardware sensors, alerts, graphs, layout customization, and settings.
2. Add tray integration, launch-at-login adapters, and native packaging.
3. Add automated tests, release workflows, and migration documentation.
