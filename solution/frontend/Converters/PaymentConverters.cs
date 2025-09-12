using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Globalization;

namespace MagiDesk.Frontend.Converters
{
    /// <summary>
    /// Converts payment method strings to appropriate background colors
    /// </summary>
    public class PaymentMethodToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string paymentMethod)
            {
                return paymentMethod.ToLowerInvariant() switch
                {
                    "cash" => new SolidColorBrush(Microsoft.UI.Colors.Green),
                    "card" => new SolidColorBrush(Microsoft.UI.Colors.Blue),
                    "digital" => new SolidColorBrush(Microsoft.UI.Colors.Purple),
                    "upi" => new SolidColorBrush(Microsoft.UI.Colors.Orange),
                    _ => new SolidColorBrush(Microsoft.UI.Colors.Gray)
                };
            }
            return new SolidColorBrush(Microsoft.UI.Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts overdue status strings to appropriate background colors
    /// </summary>
    public class OverdueToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string status)
            {
                return status.ToLowerInvariant() switch
                {
                    "overdue" => new SolidColorBrush(Microsoft.UI.Colors.Red),
                    "pending" => new SolidColorBrush(Microsoft.UI.Colors.Orange),
                    "recent" => new SolidColorBrush(Microsoft.UI.Colors.Green),
                    _ => new SolidColorBrush(Microsoft.UI.Colors.Gray)
                };
            }
            return new SolidColorBrush(Microsoft.UI.Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts DateTime to formatted string
    /// </summary>
    public class DateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTime dateTime)
            {
                return dateTime.ToString("MMM dd, yyyy HH:mm");
            }
            if (value is DateTimeOffset dateTimeOffset)
            {
                return dateTimeOffset.ToString("MMM dd, yyyy HH:mm");
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
