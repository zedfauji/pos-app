using Microsoft.UI.Xaml.Data;
using System;

namespace MagiDesk.Frontend.Converters;

public class CustomizeButtonTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool showCustomization)
        {
            return showCustomization ? "Hide" : "Customize";
        }
        return "Customize";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

