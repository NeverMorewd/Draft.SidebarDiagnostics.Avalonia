# Contributing

## Requirements

- .NET 10 SDK
- Windows, macOS, or Linux

## Workflow

1. Create a focused branch.
2. Keep platform-specific APIs inside `src/Services/Platform`.
3. Add tests for behavior that does not require a physical sensor.
4. Run `dotnet test SidebarDiagnostics.slnx` before opening a pull request.
5. Keep source code and documentation in English. Put user-facing text in `src/Localization/Strings.resx` with English fallback and translations in culture-specific resource files. Never use translated text or custom display names as metric identifiers.

Warnings are treated as errors. Avoid broad exception handling unless the failure represents a documented platform capability boundary.
