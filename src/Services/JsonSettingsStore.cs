using System.Text.Json;
using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.Services;

public sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _filePath;

    public JsonSettingsStore()
        : this(GetDefaultPath())
    {
    }

    public JsonSettingsStore(string filePath)
    {
        _filePath = filePath;
    }

    public async ValueTask<AppSettings> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return AppSettings.Default;
        }

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, SerializerOptions, cancellationToken);
            return (settings ?? AppSettings.Default).Normalize();
        }
        catch (JsonException)
        {
            return AppSettings.Default;
        }
        catch (IOException)
        {
            return AppSettings.Default;
        }

    }

    public async ValueTask SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryPath = $"{_filePath}.tmp";
        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, settings.Normalize(), SerializerOptions, cancellationToken);
        }

        File.Move(temporaryPath, _filePath, true);
    }

    private static string GetDefaultPath()
    {
        var applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(applicationData, "SidebarDiagnostics", "settings.json");
    }
}
