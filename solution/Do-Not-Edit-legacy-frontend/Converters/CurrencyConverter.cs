using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;

namespace MagiDesk.Frontend.Converters;

public class CurrencyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        try
        {
            var culture = new CultureInfo(string.IsNullOrWhiteSpace(language) ? "en-US" : language);
            if (value is decimal dec)
                return dec.ToString("C", culture);
            if (value is double dd)
                return dd.ToString("C", culture);
            if (value is float ff)
                return ff.ToString("C", culture);
            if (value is int ii)
                return ii.ToString("C", culture);
        }
        catch { }
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
