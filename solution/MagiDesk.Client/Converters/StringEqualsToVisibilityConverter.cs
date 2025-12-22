using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace MagiDesk.Client.Converters;

/// <summary>
/// Converts string equality check to Visibility
/// Usage: Visibility="{Binding Value, Converter={StaticResource StringEqualsToVisibilityConverter}, ConverterParameter=ExpectedValue}"
/// </summary>
public class StringEqualsToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null || parameter == null)
            return Visibility.Collapsed;

        var valueString = value.ToString();
        var parameterString = parameter.ToString();

        return valueString.Equals(parameterString, StringComparison.OrdinalIgnoreCase) 
            ? Visibility.Visible 
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
