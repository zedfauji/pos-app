using Microsoft.UI.Xaml.Data;

namespace MagiDesk.Frontend.Converters;

public class TableCountConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int count)
        {
            return $"{count} table{(count == 1 ? "" : "s")}";
        }
        return "0 tables";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

