using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace MagiDesk.Client.Converters;

/// <summary>
/// Converts decimal to Visibility.Collapsed if zero, otherwise Visible
/// </summary>
public class ZeroToCollapsedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is decimal decimalValue)
        {
            return decimalValue == 0 ? Visibility.Collapsed : Visibility.Visible;
        }
        if (value is double doubleValue)
        {
            return Math.Abs(doubleValue) < 0.01 ? Visibility.Collapsed : Visibility.Visible;
        }
        if (value is int intValue)
        {
            return intValue == 0 ? Visibility.Collapsed : Visibility.Visible;
        }
        
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
