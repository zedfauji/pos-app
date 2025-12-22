using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Globalization;

namespace MagiDesk.Frontend.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isConnected)
        {
            return isConnected ? new SolidColorBrush(Microsoft.UI.Colors.Green) : new SolidColorBrush(Microsoft.UI.Colors.Red);
        }
        return new SolidColorBrush(Microsoft.UI.Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class BoolToStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isConnected)
        {
            return isConnected ? "Connected" : "Disconnected";
        }
        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool invert = parameter?.ToString()?.Equals("Invert", StringComparison.OrdinalIgnoreCase) == true;
        
        if (value is int count)
        {
            bool shouldShow = count > 0;
            if (invert) shouldShow = !shouldShow;
            return shouldShow ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
        }
        
        if (value is System.Collections.ICollection collection)
        {
            bool shouldShow = collection.Count > 0;
            if (invert) shouldShow = !shouldShow;
            return shouldShow ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
        }
        
        return invert ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class RevenueToHeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is decimal revenue)
        {
            // Convert revenue to height (max 200px, min 20px)
            var maxRevenue = 250m; // Maximum expected revenue
            var height = Math.Max(20, (double)(revenue / maxRevenue * 200));
            return height;
        }
        return 20.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class OccupancyToHeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double occupancy)
        {
            // Convert occupancy percentage to height (max 200px, min 20px)
            var height = Math.Max(20, occupancy / 100.0 * 200);
            return height;
        }
        return 20.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class PercentageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double percentage)
        {
            return $"{percentage:F1}%";
        }
        return "0.0%";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class TimeAgoConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DateTime timestamp)
        {
            var timeSpan = DateTime.Now - timestamp;
            
            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            else if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes}m ago";
            else if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours}h ago";
            else
                return $"{(int)timeSpan.TotalDays}d ago";
        }
        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class PriorityToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is MagiDesk.Frontend.ViewModels.ReminderPriority priority)
        {
            return priority switch
            {
                MagiDesk.Frontend.ViewModels.ReminderPriority.Critical => new SolidColorBrush(Microsoft.UI.Colors.Red),
                MagiDesk.Frontend.ViewModels.ReminderPriority.High => new SolidColorBrush(Microsoft.UI.Colors.Orange),
                MagiDesk.Frontend.ViewModels.ReminderPriority.Medium => new SolidColorBrush(Microsoft.UI.Colors.Yellow),
                MagiDesk.Frontend.ViewModels.ReminderPriority.Low => new SolidColorBrush(Microsoft.UI.Colors.Green),
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

public class NotificationTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is MagiDesk.Frontend.ViewModels.NotificationType type)
        {
            return type switch
            {
                MagiDesk.Frontend.ViewModels.NotificationType.Success => "✅",
                MagiDesk.Frontend.ViewModels.NotificationType.Warning => "⚠️",
                MagiDesk.Frontend.ViewModels.NotificationType.Error => "❌",
                MagiDesk.Frontend.ViewModels.NotificationType.Info => "ℹ️",
                _ => "📢"
            };
        }
        return "📢";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
