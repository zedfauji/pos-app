using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs.Tables;
using Microsoft.UI.Dispatching;
using System.Collections.Generic;
using MagiDesk.Frontend.Collections;

namespace MagiDesk.Frontend.ViewModels
{
    public sealed class UnsettledBillsViewModel : INotifyPropertyChanged
    {
        private readonly TableRepository _tableRepository;
        private bool _isLoading;
        private bool _hasError;
        private string? _errorMessage;
        // CRITICAL FIX: Use SafeObservableCollection to avoid COM interop issues
        // SafeObservableCollection suppresses COM exceptions that cause Marshal.ThrowExceptionForHR errors
        public SafeObservableCollection<BillResult> UnsettledBills { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public bool HasError
        {
            get => _hasError;
            private set
            {
                _hasError = value;
                OnPropertyChanged();
            }
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        public decimal TotalUnsettledAmount
        {
            get => UnsettledBills.Sum(bill => bill.TotalAmount);
        }

        public int OverdueCount
        {
            get
            {
                var cutoffDate = DateTime.Now.AddDays(-7); // Consider bills overdue after 7 days
                return UnsettledBills.Count(bill => bill.StartTime < cutoffDate);
            }
        }

        public UnsettledBillsViewModel(TableRepository tableRepository)
        {
            _tableRepository = tableRepository;
        }

        public async Task LoadUnsettledBillsAsync()
        {
            try
            {
                IsLoading = true;
                HasError = false;
                ErrorMessage = null;

                System.Diagnostics.Debug.WriteLine("UnsettledBillsViewModel: Loading unsettled bills...");
                var bills = await _tableRepository.GetBillsAsync();
                System.Diagnostics.Debug.WriteLine($"UnsettledBillsViewModel: Received {bills.Count} bills from TableRepository");
                
                // CRITICAL FIX: Use SafeObservableCollection to avoid COM interop issues
                // SafeObservableCollection suppresses COM exceptions that cause Marshal.ThrowExceptionForHR errors
                UnsettledBills.Clear();
                foreach (var bill in bills)
                {
                    UnsettledBills.Add(bill);
                    System.Diagnostics.Debug.WriteLine($"UnsettledBillsViewModel: Added bill {bill.BillId} - ${bill.TotalAmount:F2}");
                }
                
                // Notify that statistics have changed
                OnPropertyChanged(nameof(TotalUnsettledAmount));
                OnPropertyChanged(nameof(OverdueCount));
                
                System.Diagnostics.Debug.WriteLine($"UnsettledBillsViewModel: Load completed - {UnsettledBills.Count} bills loaded");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UnsettledBillsViewModel: Error loading bills - {ex.Message}");
                HasError = true;
                ErrorMessage = $"Failed to load unsettled bills: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}