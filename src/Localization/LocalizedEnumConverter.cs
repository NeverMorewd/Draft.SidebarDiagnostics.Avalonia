using System.Globalization;
using Avalonia.Data.Converters;

namespace SidebarDiagnostics.App.Localization;

public sealed class LocalizedEnumConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Enum option ? UiText.Translate(option.ToString()) : value;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
