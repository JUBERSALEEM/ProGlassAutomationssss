using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ProGlassAutomation.Views.Dashboard
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            UpdateSubscriptionStatus();
        }

        void UpdateSubscriptionStatus()
        {
            try
            {
                Services.SubscriptionService.Instance.ReloadConfig();
                bool isActive = Services.SubscriptionService.Instance.IsActive();

                if (isActive)
                {
                    StatusText.Text = "ACTIVE";
                    StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));
                    DaysText.Text = Services.SubscriptionService.Instance.GetDaysRemaining().ToString();
                    DaysText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));
                    ActivateBtn.Visibility = Visibility.Collapsed;
                    SubscriptionStatusCard.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC"));

                    // Update all module status to ACTIVE
                    UpdateModuleStatus(SguStatus, "ACTIVE", "#059669");
                    UpdateModuleStatus(DguStatus, "ACTIVE", "#059669");
                    UpdateModuleStatus(LamStatus, "ACTIVE", "#059669");
                    UpdateModuleStatus(DguLamStatus, "ACTIVE", "#059669");
                    UpdateModuleStatus(OptStatus, "ACTIVE", "#059669");
                }
                else
                {
                    StatusText.Text = "INACTIVE";
                    StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                    DaysText.Text = "0";
                    DaysText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));
                    ActivateBtn.Visibility = Visibility.Visible;
                    SubscriptionStatusCard.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCA5A5"));

                    // Update all module status to LOCKED
                    UpdateModuleStatus(SguStatus, "LOCKED", "#DC2626");
                    UpdateModuleStatus(DguStatus, "LOCKED", "#DC2626");
                    UpdateModuleStatus(LamStatus, "LOCKED", "#DC2626");
                    UpdateModuleStatus(DguLamStatus, "LOCKED", "#DC2626");
                    UpdateModuleStatus(OptStatus, "LOCKED", "#DC2626");
                }
            }
            catch
            {
                StatusText.Text = "INACTIVE";
                StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                DaysText.Text = "0";
                DaysText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));
                ActivateBtn.Visibility = Visibility.Visible;

                UpdateModuleStatus(SguStatus, "LOCKED", "#DC2626");
                UpdateModuleStatus(DguStatus, "LOCKED", "#DC2626");
                UpdateModuleStatus(LamStatus, "LOCKED", "#DC2626");
                UpdateModuleStatus(DguLamStatus, "LOCKED", "#DC2626");
                UpdateModuleStatus(OptStatus, "LOCKED", "#DC2626");
            }
        }

        void UpdateModuleStatus(Border statusBorder, string text, string colorHex)
        {
            if (statusBorder != null)
            {
                statusBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
                if (statusBorder.Child is TextBlock tb)
                {
                    tb.Text = text;
                }
            }
        }

        private void ActivateNow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
                mainWindow.ShowSubscriptionPlan();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
    }
}