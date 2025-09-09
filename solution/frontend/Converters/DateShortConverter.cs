using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;

namespace MagiDesk.Frontend.Converters;

public class DateShortConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DateTime dt)
        {
            // Short date per current culture if provided, else en-US fallback
            try
            {
                var culture = !string.IsNullOrWhiteSpace(language) ? new CultureInfo(language) : CultureInfo.CurrentCulture;
                return dt.ToString(culture.DateTimeFormat.ShortDatePattern, culture);
            }
            catch
            {
                return dt.ToString("d", CultureInfo.InvariantCulture);
            }
        }
        if (value is DateTimeOffset dto)
        {
            // Short date per current culture if provided, else en-US fallback
            try
            {
                var culture = !string.IsNullOrWhiteSpace(language) ? new CultureInfo(language) : CultureInfo.CurrentCulture;
                return dto.ToString(culture.DateTimeFormat.ShortDatePattern, culture);
            }
            catch
            {
                return dto.ToString("d", CultureInfo.InvariantCulture);
            }
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
