using System.Globalization;
using SidebarDiagnostics.App.Localization;
using SidebarDiagnostics.App.Models;
using Xunit;

namespace SidebarDiagnostics.Tests.Services;

public sealed class LocalizationTests
{
    [Fact]
    public void ChineseSatelliteResourcesAreLoadedAndUnknownCulturesFallBackToEnglish()
    {
        var english = UiText.Get("TextSaveChanges", CultureInfo.GetCultureInfo("en"));
        var chinese = UiText.Get("TextSaveChanges", CultureInfo.GetCultureInfo("zh-Hans"));
        Assert.Equal("Save changes", english);
        Assert.NotEqual(english, chinese);
        Assert.NotEqual("TextSaveChanges", chinese);
        Assert.Equal(english, UiText.Get("TextSaveChanges", CultureInfo.GetCultureInfo("de")));
    }

    [Fact]
    public void SettingsAcceptSupportedLanguagesAndNormalizeUnknownValues()
    {
        Assert.Equal("zh-Hans", (AppSettings.Default with { Language = "zh-Hans" }).Normalize().Language);
        Assert.Equal("en", (AppSettings.Default with { Language = "invalid" }).Normalize().Language);
    }
}
