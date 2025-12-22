using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace MagiDesk.Client.Converters;

/// <summary>
/// Converts zero/empty to Visibility.Visible, non-zero to Visibility.Collapsed
/// Opposite of ZeroToCollapsedConverter - useful for empty states
/// </summary>
public class ZeroToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int intValue)
        {
            return intValue == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        if (value is decimal decimalValue)
        {
            return decimalValue == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        if (value is double doubleValue)
        {
            return Math.Abs(doubleValue) < 0.01 ? Visibility.Visible : Visibility.Collapsed;
        }
        
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
