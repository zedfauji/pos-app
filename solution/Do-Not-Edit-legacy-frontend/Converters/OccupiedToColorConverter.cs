using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace MagiDesk.Frontend.Converters;

public class OccupiedToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool occupied)
        {
            return occupied ? new SolidColorBrush(Microsoft.UI.Colors.Red) : new SolidColorBrush(Microsoft.UI.Colors.Green);
        }
        return new SolidColorBrush(Microsoft.UI.Colors.Green); // Default to available color
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
