using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Pipboy.Avalonia;
using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.Styling;

public sealed class ApplicationThemeService(Application application)
{
    private static readonly IReadOnlyDictionary<string, string> PipboyResourceMap =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["BackgroundWindowBrush"] = "PipboyBackgroundBrush",
            ["BackgroundInputBrush"] = "PipboySurfaceBrush",
            ["BackgroundInputDisabledBrush"] = "PipboyBackgroundBrush",
            ["BackgroundCardBrush"] = "PipboySurfaceBrush",
            ["BackgroundCardAlternateBrush"] = "PipboySurfaceBrush",
            ["BackgroundPanelBrush"] = "PipboySurfaceHighBrush",
            ["BackgroundControlBrush"] = "PipboySurfaceBrush",
            ["BackgroundControlHoverBrush"] = "PipboyHoverBrush",
            ["BackgroundControlDisabledBrush"] = "PipboyBackgroundBrush",
            ["BackgroundControlSecondaryBrush"] = "PipboySurfaceBrush",
            ["BackgroundControlSecondaryHoverBrush"] = "PipboyHoverBrush",
            ["BackgroundAccentSubtleBrush"] = "PipboySelectionBrush",
            ["BackgroundAccentHoverBrush"] = "PipboyHoverBrush",
            ["BackgroundDangerSubtleBrush"] = "PipboySurfaceBrush",
            ["BackgroundDangerHoverBrush"] = "PipboyErrorBrush",
            ["BackgroundSuccessBrush"] = "PipboySurfaceHighBrush",
            ["BackgroundMenuBrush"] = "PipboySurfaceHighBrush",
            ["BorderDefaultBrush"] = "PipboyBorderBrush",
            ["BorderHoverBrush"] = "PipboyPrimaryLightBrush",
            ["BorderFocusBrush"] = "PipboyBorderFocusBrush",
            ["BorderDisabledBrush"] = "PipboyDisabledBrush",
            ["BorderCardBrush"] = "PipboyBorderBrush",
            ["BorderPanelBrush"] = "PipboyBorderBrush",
            ["BorderChromeBrush"] = "PipboyBorderBrush",
            ["BorderAccentBrush"] = "PipboyBorderFocusBrush",
            ["BorderDangerBrush"] = "PipboyErrorBrush",
            ["TextPrimaryBrush"] = "PipboyTextBrush",
            ["TextSecondaryBrush"] = "PipboyTextBrush",
            ["TextBodyBrush"] = "PipboyTextBrush",
            ["TextMutedBrush"] = "PipboyTextDimBrush",
            ["TextSubtleBrush"] = "PipboyTextDimBrush",
            ["TextDimBrush"] = "PipboyTextDimBrush",
            ["TextAccentBrush"] = "PipboyPrimaryBrush",
            ["TextAccentStrongBrush"] = "PipboyPrimaryLightBrush",
            ["TextDangerBrush"] = "PipboyErrorBrush",
            ["TextSuccessBrush"] = "PipboySuccessBrush",
            ["AccentCpuBrush"] = "PipboyPrimaryBrush",
            ["AccentMemoryBrush"] = "PipboyPrimaryBrush",
            ["AccentStorageBrush"] = "PipboyPrimaryBrush",
            ["AccentNetworkBrush"] = "PipboyPrimaryBrush",
            ["AccentGpuBrush"] = "PipboyPrimaryBrush",
            ["AccentHardwareBrush"] = "PipboyPrimaryBrush",
            ["AccentWarningBrush"] = "PipboyWarningBrush",
            ["AccentExternalBrush"] = "PipboyPrimaryBrush",
            ["AccentSuccessBrush"] = "PipboySuccessBrush",
            ["ChartGridBrush"] = "PipboyBorderBrush",
            ["ChartAxisBrush"] = "PipboyPrimaryDarkBrush",
            ["ChartPointOutlineBrush"] = "PipboyTextBrush"
        };

    private ApplicationTheme? _activeTheme;
    private IStyle? _baseTheme;

    public void Apply(ApplicationTheme theme)
    {
        if (_activeTheme == theme)
        {
            return;
        }

        IStyle nextBaseTheme = theme switch
        {
            ApplicationTheme.Pipboy => new PipboyTheme(),
            _ => new FluentTheme { DensityStyle = DensityStyle.Compact }
        };
        var previousBaseTheme = _baseTheme;

        application.Styles.Insert(0, nextBaseTheme);

        try
        {
            var nextOverrides = theme == ApplicationTheme.Pipboy
                ? ResolvePipboyResourceOverrides()
                : null;

            if (previousBaseTheme is not null)
            {
                application.Styles.Remove(previousBaseTheme);
            }

            RemoveResourceOverrides();
            if (nextOverrides is not null)
            {
                foreach (var (key, value) in nextOverrides)
                {
                    application.Resources[key] = value;
                }
            }

            _baseTheme = nextBaseTheme;
            _activeTheme = theme;
        }
        catch
        {
            application.Styles.Remove(nextBaseTheme);
            throw;
        }
    }

    private Dictionary<string, object> ResolvePipboyResourceOverrides()
    {
        var overrides = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var (semanticKey, pipboyKey) in PipboyResourceMap)
        {
            if (!application.TryFindResource(pipboyKey, out var resource) || resource is null)
            {
                throw new InvalidOperationException($"Pip-Boy theme resource '{pipboyKey}' is unavailable.");
            }

            overrides.Add(semanticKey, resource);
        }

        return overrides;
    }

    private void RemoveResourceOverrides()
    {
        foreach (var semanticKey in PipboyResourceMap.Keys)
        {
            application.Resources.Remove(semanticKey);
        }
    }
}
