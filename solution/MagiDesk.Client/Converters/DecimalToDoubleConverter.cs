using Microsoft.UI.Xaml.Data;
using System;

namespace MagiDesk.Client.Converters;

public class DecimalToDoubleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is decimal d)
        {
            return (double)d;
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is double d)
        {
            try 
            {
                return (decimal)d;
            }
            catch
            {
                return 0m;
            }
        }
        return 0m;
    }
}
