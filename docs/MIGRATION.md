# Migration notes

## Starting point

The original application is a .NET Framework 4.7.2 WPF program with Windows-specific window management, performance counters, task scheduling, update infrastructure, and a bundled Libre Hardware Monitor source tree. UI, operating-system access, settings, and update behavior are strongly coupled in one project.

## Porting strategy

The port begins with the smallest complete vertical slice: native system metrics, a portable snapshot, a ViewModel, and a working Avalonia sidebar. Features are then restored on top of that boundary instead of translating WPF files one at a time.

This approach avoids carrying Windows assumptions into cross-platform code and gives every milestone a runnable application.

## Key changes

- WPF and .NET Framework were replaced with Avalonia 12 and .NET 10.
- Direct UI access to operating-system APIs was replaced by provider interfaces.
- Mutable global state was replaced by immutable snapshots and injected services.
- Unbounded graph data was replaced by fixed-capacity histories.
- Legacy configuration was replaced by normalized, atomic JSON persistence.
- Windows-only performance counters were replaced by native providers for all three platforms.
- Build quality is enforced with nullable analysis, recommended analyzers, and warnings as errors.

## Honest limitations

All platforms report interval CPU utilization, with per-core load on macOS and Linux. Hardware temperature and device-sensor coverage still differs because macOS does not expose a stable public equivalent to LibreHardwareMonitor or Linux hwmon. Platform providers report only values supported by the operating system rather than presenting placeholders as measurements.

## Remaining migration work

- Additional hardware clocks, fan speeds, and per-device utilization where native APIs permit
- macOS signing and notarization
- Native installers and signed release artifacts
- Before-and-after screenshots and measured resource comparisons
