using Microsoft.UI.Xaml.Data;
using System;
using System.Collections;

namespace MagiDesk.Frontend.Converters;

public class ItemCountConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int count)
            return $"({count} items total)";
        if (value is ICollection collection)
            return $"({collection.Count} items total)";
        return "(0 items total)";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
