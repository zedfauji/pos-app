using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MagiDesk.Frontend.ViewModels;

namespace MagiDesk.Frontend.Services;

public class NotificationService
{
    private readonly DispatcherQueue _dispatcher;
    private readonly List<NotificationItem> _notifications;
    private readonly List<ReminderItem> _reminders;

    public event EventHandler<ReminderEventArgs>? ReminderTriggered;

    public NotificationService()
    {
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        _notifications = new List<NotificationItem>();
        _reminders = new List<ReminderItem>();
        
        StartReminderMonitoring();
    }

    public void AddNotification(string title, string message, NotificationType type = NotificationType.Info, TimeSpan? autoHideDuration = null)
    {
        _dispatcher.TryEnqueue(() =>
        {
            var notification = new NotificationItem
            {
                Id = Guid.NewGuid(),
                Title = title,
                Message = message,
                Type = type,
                Timestamp = DateTime.Now,
                IsRead = false
            };

            _notifications.Insert(0, notification);
            
            // Keep only last 50 notifications
            if (_notifications.Count > 50)
            {
                _notifications.RemoveRange(50, _notifications.Count - 50);
            }

            NotificationAdded?.Invoke(this, new NotificationEventArgs(notification));
        });
    }

    public void AddReminder(string title, string description, DateTime dueTime, ReminderPriority priority = ReminderPriority.Medium)
    {
        var reminder = new ReminderItem
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            DueTime = dueTime,
            Priority = priority,
            IsCompleted = false
        };

        _reminders.Add(reminder);
        ReminderAdded?.Invoke(this, new ReminderEventArgs(reminder));
    }

    public List<NotificationItem> GetNotifications()
    {
        return _notifications.ToList();
    }

    public List<ReminderItem> GetReminders()
    {
        return _reminders.Where(r => !r.IsCompleted).ToList();
    }

    public int GetUnreadNotificationCount()
    {
        return _notifications.Count(n => !n.IsRead);
    }

    public void MarkNotificationAsRead(Guid notificationId)
    {
        var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
        }
    }

    public void MarkAllNotificationsAsRead()
    {
        foreach (var notification in _notifications)
        {
            notification.IsRead = true;
        }
    }

    public void CompleteReminder(Guid reminderId)
    {
        var reminder = _reminders.FirstOrDefault(r => r.Id == reminderId);
        if (reminder != null)
        {
            reminder.IsCompleted = true;
        }
    }

    public void ShowToastNotification(string title, string message, NotificationType type = NotificationType.Info)
    {
        _dispatcher.TryEnqueue(() =>
        {
            var toast = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK"
            };
            
            toast.ShowAsync();
        });
    }

    public void ShowNotificationFlyout(FrameworkElement targetElement)
    {
        _dispatcher.TryEnqueue(() =>
        {
            var flyout = new Flyout();
            var listView = new ListView
            {
                ItemsSource = _notifications,
                SelectionMode = ListViewSelectionMode.None
            };
            
            flyout.Content = listView;
            flyout.ShowAt(targetElement);
        });
    }

    private void StartReminderMonitoring()
    {
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(1)); // Check every minute
                CheckReminders();
            }
        });
    }

    private void CheckReminders()
    {
        var now = DateTime.Now;
        var upcomingReminders = _reminders
            .Where(r => !r.IsCompleted && r.DueTime <= now.AddMinutes(5) && r.DueTime > now.AddMinutes(-1))
            .ToList();

        foreach (var reminder in upcomingReminders)
        {
            _dispatcher.TryEnqueue(() =>
            {
                ReminderTriggered?.Invoke(this, new ReminderEventArgs(reminder));
                ShowToastNotification($"Reminder: {reminder.Title}", reminder.Description, NotificationType.Info);
            });
        }
    }

    public event EventHandler<NotificationEventArgs>? NotificationAdded;
    public event EventHandler<ReminderEventArgs>? ReminderAdded;
}

public class NotificationEventArgs : EventArgs
{
    public NotificationItem Notification { get; }

    public NotificationEventArgs(NotificationItem notification)
    {
        Notification = notification;
    }
}

public class ReminderEventArgs : EventArgs
{
    public ReminderItem Reminder { get; }

    public ReminderEventArgs(ReminderItem reminder)
    {
        Reminder = reminder;
    }
}
