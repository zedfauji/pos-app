using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MagiDesk.Frontend.Views
{
    public sealed partial class SegmentEditDialog : ContentDialog, INotifyPropertyChanged
    {
        private readonly CustomerIntelligenceService _customerService;
        private readonly Services.SegmentDto? _existingSegment;

        // Form fields
        private string _segmentName = string.Empty;
        private string _segmentDescription = string.Empty;
        private bool _isActive = true;
        private double _minOrders = 0;
        private double _maxOrders = 0;
        private double _minTotalSpent = 0;
        private double _maxTotalSpent = 0;
        private double _daysSinceLastOrder = 0;
        private double _minAge = 0;
        private double _maxAge = 0;
        private int _selectedGenderIndex = 0;
        private string _location = string.Empty;
        private int _selectedMembershipLevelIndex = 0;
        private double _minLoyaltyPoints = 0;
        private double _maxLoyaltyPoints = 0;
        private bool _isFormValid = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        public SegmentEditDialog() : this(null) { }

        public SegmentEditDialog(Services.SegmentDto? existingSegment = null)
        {
            this.InitializeComponent();
            _customerService = new CustomerIntelligenceService();
            _existingSegment = existingSegment;

            if (_existingSegment != null)
            {
                Title = "Edit Segment";
                LoadExistingSegment();
            }
            else
            {
                Title = "Create Segment";
            }

            this.PrimaryButtonClick += SegmentEditDialog_PrimaryButtonClick;
        }

        #region Properties

        public string SegmentName
        {
            get => _segmentName;
            set => SetProperty(ref _segmentName, value);
        }

        public string SegmentDescription
        {
            get => _segmentDescription;
            set => SetProperty(ref _segmentDescription, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public double MinOrders
        {
            get => _minOrders;
            set => SetProperty(ref _minOrders, value);
        }

        public double MaxOrders
        {
            get => _maxOrders;
            set => SetProperty(ref _maxOrders, value);
        }

        public double MinTotalSpent
        {
            get => _minTotalSpent;
            set => SetProperty(ref _minTotalSpent, value);
        }

        public double MaxTotalSpent
        {
            get => _maxTotalSpent;
            set => SetProperty(ref _maxTotalSpent, value);
        }

        public double DaysSinceLastOrder
        {
            get => _daysSinceLastOrder;
            set => SetProperty(ref _daysSinceLastOrder, value);
        }

        public double MinAge
        {
            get => _minAge;
            set => SetProperty(ref _minAge, value);
        }

        public double MaxAge
        {
            get => _maxAge;
            set => SetProperty(ref _maxAge, value);
        }

        public int SelectedGenderIndex
        {
            get => _selectedGenderIndex;
            set => SetProperty(ref _selectedGenderIndex, value);
        }

        public string Location
        {
            get => _location;
            set => SetProperty(ref _location, value);
        }

        public int SelectedMembershipLevelIndex
        {
            get => _selectedMembershipLevelIndex;
            set => SetProperty(ref _selectedMembershipLevelIndex, value);
        }

        public double MinLoyaltyPoints
        {
            get => _minLoyaltyPoints;
            set => SetProperty(ref _minLoyaltyPoints, value);
        }

        public double MaxLoyaltyPoints
        {
            get => _maxLoyaltyPoints;
            set => SetProperty(ref _maxLoyaltyPoints, value);
        }

        public bool IsFormValid
        {
            get => _isFormValid;
            set => SetProperty(ref _isFormValid, value);
        }

        #endregion

        private void LoadExistingSegment()
        {
            if (_existingSegment == null) return;

            SegmentName = _existingSegment.Name;
            SegmentDescription = _existingSegment.Description;
            IsActive = _existingSegment.IsActive;

            // Load criteria from existing segment
            // This would need to be implemented based on how criteria are stored
            // For now, setting some default values
        }

        private async void SegmentEditDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true; // Cancel default close behavior

            if (!ValidateForm())
            {
                ValidationInfoBar.IsOpen = true;
                return;
            }

            try
            {
                IsPrimaryButtonEnabled = false;
                PrimaryButtonText = "Saving...";

                var segmentData = CreateSegmentData();

                if (_existingSegment != null)
                {
                    var updateRequest = new Services.UIUpdateSegmentRequest
                    {
                        Name = segmentData.Name,
                        Description = segmentData.Description,
                        IsActive = segmentData.IsActive,
                        Criteria = segmentData.Criteria
                    };
                    await _customerService.UpdateSegmentAsync(Guid.Parse(_existingSegment.Id), updateRequest);
                }
                else
                {
                    var createRequest = new Services.UICreateSegmentRequest
                    {
                        Name = segmentData.Name,
                        Description = segmentData.Description,
                        IsActive = segmentData.IsActive,
                        Criteria = segmentData.Criteria
                    };
                    await _customerService.CreateSegmentAsync(createRequest);
                }

                Hide();
            }
            catch (Exception ex)
            {
                ValidationInfoBar.Message = $"Failed to save segment: {ex.Message}";
                ValidationInfoBar.Severity = InfoBarSeverity.Error;
                ValidationInfoBar.IsOpen = true;
            }
            finally
            {
                IsPrimaryButtonEnabled = true;
                PrimaryButtonText = "Save";
            }
        }

        private Services.UICreateSegmentRequest CreateSegmentData()
        {
            var criteria = new List<Services.UISegmentCriterion>();

            // Add behavior criteria
            if (MinOrders > 0)
            {
                criteria.Add(new Services.UISegmentCriterion
                {
                    Field = "TotalOrders",
                    Operator = "GreaterThanOrEqual",
                    Value = MinOrders.ToString()
                });
            }

            if (MaxOrders > 0)
            {
                criteria.Add(new Services.UISegmentCriterion
                {
                    Field = "TotalOrders",
                    Operator = "LessThanOrEqual",
                    Value = MaxOrders.ToString()
                });
            }

            if (MinTotalSpent > 0)
            {
                criteria.Add(new Services.UISegmentCriterion
                {
                    Field = "TotalSpent",
                    Operator = "GreaterThanOrEqual",
                    Value = MinTotalSpent.ToString()
                });
            }

            if (MaxTotalSpent > 0)
            {
                criteria.Add(new Services.UISegmentCriterion
                {
                    Field = "TotalSpent",
                    Operator = "LessThanOrEqual",
                    Value = MaxTotalSpent.ToString()
                });
            }

            if (DaysSinceLastOrder > 0)
            {
                criteria.Add(new Services.UISegmentCriterion
                {
                    Field = "DaysSinceLastOrder",
                    Operator = "LessThanOrEqual",
                    Value = DaysSinceLastOrder.ToString()
                });
            }

            // Add demographic criteria
            if (MinAge > 0)
            {
                criteria.Add(new Services.UISegmentCriterion
                {
                    Field = "Age",
                    Operator = "GreaterThanOrEqual",
                    Value = MinAge.ToString()
                });
            }

            if (MaxAge > 0)
            {
                criteria.Add(new Services.UISegmentCriterion
                {
                    Field = "Age",
                    Operator = "LessThanOrEqual",
                    Value = MaxAge.ToString()
                });
            }

            if (SelectedGenderIndex > 0)
            {
                var gender = GenderComboBox.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(gender) && gender != "Any")
                {
                    criteria.Add(new Services.UISegmentCriterion
                    {
                        Field = "Gender",
                        Operator = "Equals",
                        Value = gender
                    });
                }
            }

            if (!string.IsNullOrWhiteSpace(Location))
            {
                criteria.Add(new Services.UISegmentCriterion
                {
                    Field = "Location",
                    Operator = "Contains",
                    Value = Location
                });
            }

            // Add loyalty criteria
            if (SelectedMembershipLevelIndex > 0)
            {
                var level = MembershipLevelComboBox.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(level) && level != "Any")
                {
                    criteria.Add(new Services.UISegmentCriterion
                    {
                        Field = "MembershipLevel",
                        Operator = "Equals",
                        Value = level
                    });
                }
            }

            if (MinLoyaltyPoints > 0)
            {
                criteria.Add(new Services.UISegmentCriterion
                {
                    Field = "LoyaltyPoints",
                    Operator = "GreaterThanOrEqual",
                    Value = MinLoyaltyPoints.ToString()
                });
            }

            if (MaxLoyaltyPoints > 0)
            {
                criteria.Add(new Services.UISegmentCriterion
                {
                    Field = "LoyaltyPoints",
                    Operator = "LessThanOrEqual",
                    Value = MaxLoyaltyPoints.ToString()
                });
            }

            return new Services.UICreateSegmentRequest
            {
                Name = SegmentName,
                Description = SegmentDescription,
                IsActive = IsActive,
                Criteria = criteria
            };
        }

        private async void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PreviewButton.IsEnabled = false;
                PreviewButton.Content = "Calculating...";
                PreviewProgressBar.Visibility = Visibility.Visible;

                var segmentData = CreateSegmentData();
                var count = await _customerService.PreviewSegmentSizeAsync(segmentData);

                EstimatedCountText.Text = $"Estimated customers: {count:N0}";
            }
            catch (Exception ex)
            {
                EstimatedCountText.Text = $"Error calculating: {ex.Message}";
            }
            finally
            {
                PreviewButton.IsEnabled = true;
                PreviewButton.Content = "Calculate";
                PreviewProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private void OnFormFieldChanged(object sender, object e)
        {
            ValidateForm();
        }

        private bool ValidateForm()
        {
            var isValid = true;
            var errors = new List<string>();

            // Validate required fields
            if (string.IsNullOrWhiteSpace(SegmentName))
            {
                errors.Add("Segment name is required");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(SegmentDescription))
            {
                errors.Add("Segment description is required");
                isValid = false;
            }

            // Validate range criteria
            if (MinOrders > 0 && MaxOrders > 0 && MinOrders > MaxOrders)
            {
                errors.Add("Minimum orders cannot be greater than maximum orders");
                isValid = false;
            }

            if (MinTotalSpent > 0 && MaxTotalSpent > 0 && MinTotalSpent > MaxTotalSpent)
            {
                errors.Add("Minimum total spent cannot be greater than maximum total spent");
                isValid = false;
            }

            if (MinAge > 0 && MaxAge > 0 && MinAge > MaxAge)
            {
                errors.Add("Minimum age cannot be greater than maximum age");
                isValid = false;
            }

            if (MinLoyaltyPoints > 0 && MaxLoyaltyPoints > 0 && MinLoyaltyPoints > MaxLoyaltyPoints)
            {
                errors.Add("Minimum loyalty points cannot be greater than maximum loyalty points");
                isValid = false;
            }

            IsFormValid = isValid;

            if (!isValid)
            {
                ValidationInfoBar.Message = string.Join(", ", errors);
                ValidationInfoBar.Severity = InfoBarSeverity.Error;
            }
            else
            {
                ValidationInfoBar.IsOpen = false;
            }

            return isValid;
        }

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    // Using DTOs from Services namespace
}
