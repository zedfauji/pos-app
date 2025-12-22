using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace MagiDesk.Client.Converters;

/// <summary>
/// Converts string equality check to boolean for RadioButton IsChecked binding
/// </summary>
public class StringEqualsToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null || parameter == null)
            return false;

        return value.ToString().Equals(parameter.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (parameter == null || !(value is bool boolValue) || !boolValue)
            return DependencyProperty.UnsetValue;

        return parameter.ToString();
    }
}
