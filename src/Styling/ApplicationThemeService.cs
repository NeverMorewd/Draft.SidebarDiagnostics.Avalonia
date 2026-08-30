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
            ["ChartPointOutlineBrush"] = "PipboyTextBrush",
            ["RadiusAccentBar"] = "PipboyCornerRadiusNone",
            ["RadiusControl"] = "PipboyCornerRadiusControl",
            ["RadiusSmall"] = "PipboyCornerRadiusControl",
            ["RadiusMedium"] = "PipboyCornerRadiusPanel",
            ["RadiusAction"] = "PipboyCornerRadiusControl",
            ["RadiusCard"] = "PipboyCornerRadiusPanel",
            ["RadiusPanel"] = "PipboyCornerRadiusPanel"
        };

    private ApplicationTheme? _activeTheme;
    private IStyle? _baseTheme;

    public void Apply(ApplicationTheme theme)
    {
        if (_activeTheme == theme)
        {
            return;
        }

        RemoveResourceOverrides();

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

        if (theme == ApplicationTheme.Pipboy)
        {
            ApplyPipboyResourceOverrides();
        }

        _activeTheme = theme;
    }

    private void ApplyPipboyResourceOverrides()
    {
        foreach (var (semanticKey, pipboyKey) in PipboyResourceMap)
        {
            if (!application.TryFindResource(pipboyKey, out var resource) || resource is null)
            {
                throw new InvalidOperationException($"Pip-Boy theme resource '{pipboyKey}' is unavailable.");
            }

            application.Resources[semanticKey] = resource;
        }
    }

    private void RemoveResourceOverrides()
    {
        foreach (var semanticKey in PipboyResourceMap.Keys)
        {
            application.Resources.Remove(semanticKey);
        }
    }
}
