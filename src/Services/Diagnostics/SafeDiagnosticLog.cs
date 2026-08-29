using System.Diagnostics;

namespace SidebarDiagnostics.App.Services.Diagnostics;

internal static class SafeDiagnosticLog
{
    public static void Write(string area, string outcome, Exception? exception = null, long? elapsedMilliseconds = null)
    {
        var exceptionType = exception?.GetType().Name ?? "None";
        var elapsed = elapsedMilliseconds?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "Unknown";
        Trace.WriteLine($"Area={area} Outcome={outcome} Exception={exceptionType} ElapsedMs={elapsed}");
    }
}
