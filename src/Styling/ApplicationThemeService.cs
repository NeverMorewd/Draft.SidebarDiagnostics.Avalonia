using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Pipboy.Avalonia;
using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.Styling;

public sealed class ApplicationThemeService(Application application)
{
    private static readonly Uri BaseUri = new("avares://SidebarDiagnostics.App/");
    private ApplicationTheme? _activeTheme;
    private IStyle? _baseTheme;
    private IStyle? _adapter;

    public void Apply(ApplicationTheme theme)
    {
        if (_activeTheme == theme)
        {
            return;
        }

        if (_adapter is not null)
        {
            application.Styles.Remove(_adapter);
        }

        if (_baseTheme is not null)
        {
            application.Styles.Remove(_baseTheme);
        }

        _baseTheme = theme switch
        {
            ApplicationTheme.Pipboy => new PipboyTheme(),
            _ => new FluentTheme { DensityStyle = DensityStyle.Compact }
        };

        application.Styles.Insert(0, _baseTheme);

        _adapter = theme == ApplicationTheme.Pipboy
            ? new StyleInclude(BaseUri)
            {
                Source = new Uri("avares://SidebarDiagnostics.App/Styles/PipboySidebarTheme.axaml")
            }
            : null;

        if (_adapter is not null)
        {
            application.Styles.Add(_adapter);
        }

        _activeTheme = theme;
    }
}
