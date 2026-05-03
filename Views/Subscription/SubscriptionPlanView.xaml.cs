using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ProGlassAutomation.ViewModels;

namespace ProGlassAutomation.Views.Subscription
{
    public partial class SubscriptionPlanView : UserControl
    {
        private string _selectedPlanId = "MT3";

        public SubscriptionPlanView()
        {
            InitializeComponent();
            UpdateStatus();
            UpdatePlanSelection();
        }

        private MainViewModel ViewModel => DataContext as MainViewModel;

        private void UpdateStatus()
        {
            var status = Services.SubscriptionService.Instance.GetStatus();

            if (status.IsActive)
            {
                StatusText.Text = "ACTIVE";
                StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));
                DaysText.Text = status.DaysRemaining.ToString();
                DaysText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));
                PlanTypeText.Text = status.PlanName.ToUpper();
                PlanTypeText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));
                PCsText.Text = $"{status.CurrentPCs}/{status.MaxPCs}";
                PCsText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));
            }
            else
            {
                StatusText.Text = "INACTIVE";
                StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                DaysText.Text = "0";
                DaysText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));
                PlanTypeText.Text = "NONE";
                PlanTypeText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));
                PCsText.Text = "0/0";
                PCsText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));
            }
        }

        private void UpdatePlanSelection()
        {
            var defaultBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB"));

            WK1Border.BorderBrush = defaultBrush;
            WK3Border.BorderBrush = defaultBrush;
            WK5Border.BorderBrush = defaultBrush;
            MT1Border.BorderBrush = defaultBrush;
            MT3Border.BorderBrush = defaultBrush;
            MT5Border.BorderBrush = defaultBrush;
            S61Border.BorderBrush = defaultBrush;
            S63Border.BorderBrush = defaultBrush;
            S65Border.BorderBrush = defaultBrush;
            YR1Border.BorderBrush = defaultBrush;
            YR3Border.BorderBrush = defaultBrush;
            YR5Border.BorderBrush = defaultBrush;

            WK1Check.Text = "Select";
            WK3Check.Text = "Select";
            WK5Check.Text = "Select";
            MT1Check.Text = "Select";
            MT3Check.Text = "Select";
            MT5Check.Text = "Select";
            S61Check.Text = "Select";
            S63Check.Text = "Select";
            S65Check.Text = "Select";
            YR1Check.Text = "Select";
            YR3Check.Text = "Select";
            YR5Check.Text = "Select";

            switch (_selectedPlanId)
            {
                case "WK1":
                    WK1Border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                    WK1Check.Text = "Selected";
                    break;
                case "WK3":
                    WK3Border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                    WK3Check.Text = "Selected";
                    break;
                case "WK5":
                    WK5Border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                    WK5Check.Text = "Selected";
                    break;
                case "MT1":
                    MT1Border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A6ED1"));
                    MT1Check.Text = "Selected";
                    break;
                case "MT3":
                    MT3Border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A6ED1"));
                    MT3Check.Text = "Selected";
                    break;
                case "MT5":
                    MT5Border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A6ED1"));
                    MT5Check.Text = "Selected";
                    break;
                case "6M1":
                    S61Border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7C3AED"));
                    S61Check.Text = "Selected";
                    break;
                case "6M3":
                    S63Border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7C3AED"));
                    S63Check.Text = "Selected";
                    break;
                case "6M5":
                    S65Border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7C3AED"));
                    S65Check.Text = "Selected";
                    break;
                case "YR1":
                    YR1Border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));
                    YR1Check.Text = "Selected";
                    break;
                case "YR3":
                    YR3Border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));
                    YR3Check.Text = "Selected";
                    break;
                case "YR5":
                    YR5Border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));
                    YR5Check.Text = "Selected";
                    break;
            }

            var plan = Services.KeyGeneratorService.Instance.GetPlan(_selectedPlanId);
            if (plan != null)
            {
                string pcsText = plan.MaxPCs == 1 ? "1 PC" : $"{plan.MaxPCs} PCs";
                SelectedPlanText.Text = $"Selected: {plan.Name} - AED {plan.Price:N0} ({plan.Days} Days, {pcsText})";
            }
        }

        private void Plan_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag != null)
            {
                _selectedPlanId = border.Tag.ToString() ?? "MT3";
                UpdatePlanSelection();
            }
        }

        private void LicenseKeyInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            ErrorText.Text = "";
        }

        private void ActivateKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string key = LicenseKeyInput.Text.Trim().ToUpper();

                if (string.IsNullOrEmpty(key))
                {
                    ErrorText.Text = "Please enter a license key";
                    return;
                }

                if (Services.SubscriptionService.Instance.ActivateLicense(key))
                {
                    // Use MainViewModel to unlock modules instantly
                    ViewModel?.OnLicenseActivated(key);

                    // Update local status
                    UpdateStatus();

                    // Clear input
                    LicenseKeyInput.Text = "";
                }
                else
                {
                    string error = Services.SubscriptionService.Instance.GetErrorMessage();
                    ErrorText.Text = string.IsNullOrEmpty(error) ? "Activation failed" : error;
                }
            }
            catch (Exception ex)
            {
                ErrorText.Text = "Error: " + ex.Message;
            }
        }

        private void DeactivateKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Are you sure you want to deactivate your license?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    Services.SubscriptionService.Instance.DeactivateLicense();

                    // Use MainViewModel to lock modules instantly
                    ViewModel?.OnLicenseDeactivated();

                    // Update local status
                    UpdateStatus();
                }
            }
            catch (Exception ex)
            {
                ErrorText.Text = "Error: " + ex.Message;
            }
        }

        private void ContactWhatsApp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://wa.me/971559117727",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open WhatsApp: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ContactEmail_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "mailto:Jubersaleem01@gmail.com?subject=ProGlass License Request",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open email: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}