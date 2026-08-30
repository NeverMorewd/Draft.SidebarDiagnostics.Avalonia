using System.Text.Json.Serialization;
using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.Services;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AppSettings))]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext;
