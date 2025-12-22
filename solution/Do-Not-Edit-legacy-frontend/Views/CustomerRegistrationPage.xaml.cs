using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using MagiDesk.Frontend.Services;
using MembershipLevelDto = MagiDesk.Frontend.Services.MembershipLevelDto;
using CreateCustomerRequest = MagiDesk.Frontend.Services.CreateCustomerRequest;

namespace MagiDesk.Frontend.Views
{
    public sealed partial class CustomerRegistrationPage : Page
    {
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly ObservableCollection<MembershipLevelDto> _membershipLevels;
        private DateTime? _selectedDateOfBirth;
        private DateTime? _selectedMembershipExpiry;

        public CustomerRegistrationPage()
        {
            this.InitializeComponent();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _membershipLevels = new ObservableCollection<MembershipLevelDto>();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await InitializePageAsync();
        }

        private async Task InitializePageAsync()
        {
            try
            {
                LoadingRing.IsActive = true;
                await LoadMembershipLevelsAsync();
                SetDefaultValues();
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Failed to initialize registration form", ex.Message);
            }
            finally
            {
                LoadingRing.IsActive = false;
            }
        }

        private async Task LoadMembershipLevelsAsync()
        {
            try
            {
                if (App.CustomerApi == null) return;

                var levels = await App.CustomerApi.GetAllMembershipLevelsAsync();
                
                _dispatcherQueue.TryEnqueue(() =>
                {
                    _membershipLevels.Clear();
                    MembershipLevelComboBox.Items.Clear();
                    
                    if (levels != null)
                    {
                        foreach (var level in levels)
                        {
                            _membershipLevels.Add(level);
                            MembershipLevelComboBox.Items.Add(new ComboBoxItem 
                            { 
                                Content = level.Name, 
                                Tag = level.MembershipLevelId.ToString() 
                            });
                        }
                        
                        // Select default membership level
                        var defaultLevel = levels.FirstOrDefault(l => l.IsDefault);
                        if (defaultLevel != null)
                        {
                            var defaultItem = MembershipLevelComboBox.Items
                                .Cast<ComboBoxItem>()
                                .FirstOrDefault(item => item.Tag?.ToString() == defaultLevel.MembershipLevelId.ToString());
                            if (defaultItem != null)
                            {
                                MembershipLevelComboBox.SelectedItem = defaultItem;
                            }
                        }
                        else if (MembershipLevelComboBox.Items.Count > 0)
                        {
                            MembershipLevelComboBox.SelectedIndex = 0;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Failed to load membership levels", ex.Message);
            }
        }

        private void SetDefaultValues()
        {
            // Set default membership expiry to 1 year from now
            var defaultExpiry = DateTime.Now.AddYears(1);
            MembershipExpiryPicker.Date = new DateTimeOffset(defaultExpiry);
            _selectedMembershipExpiry = defaultExpiry;
        }

        private bool ValidateForm()
        {
            var errors = new List<string>();

            // Required fields validation
            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text))
                errors.Add("First name is required");

            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text))
                errors.Add("Last name is required");

            // Email validation
            if (!string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                if (!IsValidEmail(EmailTextBox.Text))
                    errors.Add("Please enter a valid email address");
            }

            // Phone validation
            if (!string.IsNullOrWhiteSpace(PhoneTextBox.Text))
            {
                if (!IsValidPhone(PhoneTextBox.Text))
                    errors.Add("Please enter a valid phone number");
            }

            // At least one contact method required
            if (string.IsNullOrWhiteSpace(EmailTextBox.Text) && string.IsNullOrWhiteSpace(PhoneTextBox.Text))
            {
                errors.Add("Either email or phone number is required");
            }

            // Date validation
            if (DateOfBirthPicker.Date.Date > DateTime.Now.Date)
            {
                errors.Add("Date of birth cannot be in the future");
            }

            if (MembershipExpiryPicker.Date.Date <= DateTime.Now.Date)
            {
                errors.Add("Membership expiry date must be in the future");
            }

            if (errors.Any())
            {
                ShowValidationErrors(errors);
                return false;
            }

            HideValidationErrors();
            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidPhone(string phone)
        {
            // Simple phone validation - remove common formatting and check if it's all digits
            var cleanPhone = phone.Replace("-", "").Replace("(", "").Replace(")", "").Replace(" ", "").Replace("+", "");
            return cleanPhone.Length >= 10 && cleanPhone.All(char.IsDigit);
        }

        private void ShowValidationErrors(List<string> errors)
        {
            ValidationMessageText.Text = string.Join("\n", errors);
            ValidationMessageText.Visibility = Visibility.Visible;
        }

        private void HideValidationErrors()
        {
            ValidationMessageText.Visibility = Visibility.Collapsed;
        }

        private async Task ShowErrorAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            
            await dialog.ShowAsync();
        }

        private async Task ShowSuccessAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            
            await dialog.ShowAsync();
        }

        // Event Handlers
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
                return;

            try
            {
                LoadingRing.IsActive = true;
                SaveButton.IsEnabled = false;

                if (App.CustomerApi == null)
                {
                    await ShowErrorAsync("Error", "Customer API is not available");
                    return;
                }

                // Get selected membership level
                var membershipLevelId = Guid.Empty; // Default to empty
                if (MembershipLevelComboBox.SelectedItem is MembershipLevelDto selectedLevel)
                {
                    membershipLevelId = selectedLevel.MembershipLevelId;
                }
                else if (_membershipLevels?.Any() == true)
                {
                    membershipLevelId = _membershipLevels.First().MembershipLevelId;
                }

                var request = new CreateCustomerRequest
                {
                    FirstName = FirstNameTextBox.Text.Trim(),
                    LastName = LastNameTextBox.Text.Trim(),
                    Email = string.IsNullOrWhiteSpace(EmailTextBox.Text) ? null : EmailTextBox.Text.Trim(),
                    Phone = string.IsNullOrWhiteSpace(PhoneTextBox.Text) ? null : PhoneTextBox.Text.Trim(),
                    DateOfBirth = DateOfBirthPicker.Date.DateTime,
                    MembershipLevelId = membershipLevelId,
                    MembershipExpiryDate = MembershipExpiryPicker.Date.DateTime,
                    Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim()
                };

                var customer = await App.CustomerApi.CreateCustomerAsync(request);

                if (customer != null)
                {
                    await ShowSuccessAsync("Success", $"Customer '{customer.FullName}' has been created successfully!");
                    
                    // Navigate back to dashboard
                    if (Frame.CanGoBack)
                    {
                        Frame.GoBack();
                    }
                }
                else
                {
                    await ShowErrorAsync("Error", "Failed to create customer. Please try again.");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error", $"Failed to create customer: {ex.Message}");
            }
            finally
            {
                LoadingRing.IsActive = false;
                SaveButton.IsEnabled = true;
            }
        }

        private void DateOfBirthPicker_DateChanged(object sender, DatePickerValueChangedEventArgs e)
        {
            _selectedDateOfBirth = e.NewDate.DateTime;
        }

        private void MembershipExpiryPicker_DateChanged(object sender, DatePickerValueChangedEventArgs e)
        {
            _selectedMembershipExpiry = e.NewDate.DateTime;
        }
    }
}
