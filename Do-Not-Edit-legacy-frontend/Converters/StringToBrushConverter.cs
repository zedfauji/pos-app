using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace MagiDesk.Frontend.Converters
{
    public class StringToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string colorString)
            {
                try
                {
                    // Parse hex color string (e.g., "#FF8C00")
                    if (colorString.StartsWith("#"))
                    {
                        var hex = colorString.Substring(1);
                        var r = System.Convert.ToByte(hex.Substring(0, 2), 16);
                        var g = System.Convert.ToByte(hex.Substring(2, 2), 16);
                        var b = System.Convert.ToByte(hex.Substring(4, 2), 16);
                        return new SolidColorBrush(Windows.UI.Color.FromArgb(255, r, g, b));
                    }
                }
                catch
                {
                    // Fallback to default color
                }
            }
            
            // Default fallback color
            return new SolidColorBrush(Microsoft.UI.Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
