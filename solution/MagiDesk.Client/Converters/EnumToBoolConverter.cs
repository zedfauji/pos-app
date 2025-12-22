using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using MagiDesk.Shared.Enums;
using System;

namespace MagiDesk.Client.Converters;

/// <summary>
/// Converts PaymentMethod enum to bool for radio button binding
/// </summary>
public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null || parameter == null)
            return false;

        var enumValue = value.ToString();
        var targetValue = parameter.ToString();
        
        return enumValue.Equals(targetValue, StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (parameter == null || !(value is bool boolValue) || !boolValue)
            return DependencyProperty.UnsetValue;

        var paramString = parameter.ToString();
        
        if (Enum.TryParse(typeof(PaymentMethod), paramString, true, out var result))
        {
            return result;
        }
        
        return DependencyProperty.UnsetValue;
    }
}
