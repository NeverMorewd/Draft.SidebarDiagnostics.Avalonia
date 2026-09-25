using System.Collections;
using System.Globalization;
using Avalonia;

namespace SidebarDiagnostics.App.Localization;

public static class ApplicationLanguage
{
    public static void Apply(string language)
    {
        var application = Application.Current;
        if (application is null)
            return;

        var culture = CultureInfo.GetCultureInfo(LanguageOption.Normalize(language));
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        var resources = UiText.Resources.GetResourceSet(CultureInfo.InvariantCulture, true, true)!;
        foreach (DictionaryEntry entry in resources)
        {
            var key = (string)entry.Key;
            application.Resources[key] = UiText.Get(key, culture);
        }
    }
}
