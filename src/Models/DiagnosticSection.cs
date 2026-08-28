namespace SidebarDiagnostics.App.Models;
public sealed record DiagnosticSection(string Id, string Title, string Subtitle, string AccentColor, IReadOnlyList<DiagnosticMetric> Metrics);
