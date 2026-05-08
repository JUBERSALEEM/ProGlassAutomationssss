using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ProGlassAutomation.ViewModels;

namespace ProGlassAutomation.Views.Dashboard
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            UpdateSubscriptionStatus();
        }

        private MainViewModel ViewModel => DataContext as MainViewModel;

        void UpdateSubscriptionStatus()
        {
            try
            {
                Services.SubscriptionService.Instance.ReloadConfig();
                bool isActive = Services.SubscriptionService.Instance.IsActive();

                if (isActive)
                {
                    // ACTIVE STATE
                    StatusText.Text = "ACTIVE";
                    StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));

                    DaysText.Text = Services.SubscriptionService.Instance.GetDaysRemaining().ToString();
                    DaysText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));

                    ActivateBtn.Visibility = Visibility.Collapsed;

                    StatusIndicator.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));

                    DaysCounterBg.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1FAE5"));

                    SubscriptionCard.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));

                    SubscriptionStatusText.Text = "Active";
                    SubscriptionStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));

                    ModuleCountText.Text = "5/5 Active";

                    // Update all module status to ACTIVE
                    UpdateModuleStatus(SguStatus, "ACTIVE", "#10B981", "#D1FAE5");
                    UpdateModuleStatus(DguStatus, "ACTIVE", "#10B981", "#D1FAE5");
                    UpdateModuleStatus(LamStatus, "ACTIVE", "#10B981", "#D1FAE5");
                    UpdateModuleStatus(DguLamStatus, "ACTIVE", "#10B981", "#D1FAE5");
                    UpdateModuleStatus(OptStatus, "ACTIVE", "#10B981", "#D1FAE5");
                }
                else
                {
                    // INACTIVE STATE
                    StatusText.Text = "INACTIVE";
                    StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));

                    DaysText.Text = "0";
                    DaysText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));

                    ActivateBtn.Visibility = Visibility.Visible;

                    StatusIndicator.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));

                    DaysCounterBg.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF2F2"));

                    SubscriptionCard.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));

                    SubscriptionStatusText.Text = "Inactive";
                    SubscriptionStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));

                    ModuleCountText.Text = "0/5 Active";

                    // Update all module status to LOCKED
                    UpdateModuleStatus(SguStatus, "LOCKED", "#EF4444", "#FEE2E2");
                    UpdateModuleStatus(DguStatus, "LOCKED", "#EF4444", "#FEE2E2");
                    UpdateModuleStatus(LamStatus, "LOCKED", "#EF4444", "#FEE2E2");
                    UpdateModuleStatus(DguLamStatus, "LOCKED", "#EF4444", "#FEE2E2");
                    UpdateModuleStatus(OptStatus, "LOCKED", "#EF4444", "#FEE2E2");
                }
            }
            catch
            {
                // ERROR STATE
                StatusText.Text = "INACTIVE";
                StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                DaysText.Text = "0";
                DaysText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                ActivateBtn.Visibility = Visibility.Visible;
                StatusIndicator.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                DaysCounterBg.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF2F2"));
                SubscriptionStatusText.Text = "Inactive";
                SubscriptionStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                ModuleCountText.Text = "0/5 Active";

                UpdateModuleStatus(SguStatus, "LOCKED", "#EF4444", "#FEE2E2");
                UpdateModuleStatus(DguStatus, "LOCKED", "#EF4444", "#FEE2E2");
                UpdateModuleStatus(LamStatus, "LOCKED", "#EF4444", "#FEE2E2");
                UpdateModuleStatus(DguLamStatus, "LOCKED", "#EF4444", "#FEE2E2");
                UpdateModuleStatus(OptStatus, "LOCKED", "#EF4444", "#FEE2E2");
            }
        }

        void UpdateModuleStatus(Border statusBorder, string text, string textColor, string bgColor)
        {
            if (statusBorder != null)
            {
                statusBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgColor));

                var stackPanel = statusBorder.Child as StackPanel;
                if (stackPanel != null)
                {
                    foreach (var child in stackPanel.Children)
                    {
                        if (child is TextBlock tb)
                        {
                            tb.Text = text;
                            tb.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(textColor));
                        }
                        else if (child is Path path)
                        {
                            path.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(textColor));
                        }
                    }
                }
            }
        }

        private void ActivateNow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ViewModel?.ShowSubscriptionPlan();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
    }
}