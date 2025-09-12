using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MagiDesk.Frontend.ViewModels;

public class DashboardViewModel
{
    private readonly ApiService _api;
    public int VendorsCount { get; private set; }
    public int ItemsCount { get; private set; }
    public ObservableCollection<VendorDto> TodayReminders { get; } = new();
    public ObservableCollection<VendorDto> UpcomingReminders { get; } = new();

    public DashboardViewModel(ApiService api)
    {
        _api = api;
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        // Legacy vendor counts removed - using new inventory system
        VendorsCount = 0;
        ItemsCount = 0;
        await LoadRemindersAsync(ct);
    }

    public async Task LoadRemindersAsync(CancellationToken ct = default)
    {
        TodayReminders.Clear();
        UpcomingReminders.Clear();
        // Legacy vendor reminders removed - using new inventory system
    }

    private static bool MatchesDay(string input, DayOfWeek target)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        input = input.Trim();

        // Accept numeric forms: 0-6 (Sunday=0) or 1-7 (Monday=1 or Sunday=7)
        if (int.TryParse(input, out var num))
        {
            if (num >= 0 && num <= 6) return (int)target == num;
            if (num >= 1 && num <= 7)
            {
                // Try Monday=1..Sunday=7 mapping
                var mondayBased = ((int)target - 1 + 7) % 7 + 1; // Monday=1 ... Sunday=7
                return mondayBased == num;
            }
        }

        // Compare against English and Spanish full/abbreviated names
        var cultures = new[] { new CultureInfo("en-US"), new CultureInfo("es-MX"), new CultureInfo("es-ES") };
        foreach (var culture in cultures)
        {
            var fmt = culture.DateTimeFormat;
            var full = fmt.GetDayName(target);
            var abbr = fmt.GetAbbreviatedDayName(target);
            if (string.Equals(input, full, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(input, abbr, StringComparison.OrdinalIgnoreCase)) return true;

            // Fuzzy: compare first 3 letters, diacritics-insensitive
            var nIn = NormalizeDay(input);
            var nFull = NormalizeDay(full);
            var nAbbr = NormalizeDay(abbr);
            if (nIn.Length >= 3 && ((nFull.StartsWith(nIn[..Math.Min(3, nIn.Length)])) || (nAbbr.StartsWith(nIn[..Math.Min(3, nIn.Length)]))))
                return true;
        }
        return false;
    }

    private static string NormalizeDay(string s)
    {
        s = s.Trim().ToLowerInvariant();
        // Remove dots and punctuation
        s = new string(s.Where(ch => char.IsLetter(ch)).ToArray());
        // Remove diacritics
        var norm = s.Normalize(NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var ch in norm)
        {
            var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
