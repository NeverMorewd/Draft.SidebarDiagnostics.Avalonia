namespace SidebarDiagnostics.App.Localization;

public sealed record LanguageOption(string Code, string DisplayName)
{
    public static IReadOnlyList<LanguageOption> All { get; } =
    [
        new("en", "English"),
        new("zh-Hans", UiText.Get("TextSimplifiedChinese", System.Globalization.CultureInfo.GetCultureInfo("zh-Hans")))
    ];

    public static string Normalize(string? code) => All.Any(option => option.Code == code) ? code! : "en";
}
