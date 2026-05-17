using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ProGlassAutomation.ViewModels;

namespace ProGlassAutomation
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private DispatcherTimer _clockTimer;

        // SINGLE SOURCE OF TRUTH
        private Popup _activeDropdown;
        private string _activeDropdownName;

        // Independent layer
        private Popup _screenshotPopup;

        // Throttle validation
        private DateTime _lastValidationTime;
        private const int VALIDATION_THROTTLE_MS = 16;

        // Thread safety
        private readonly object _dropdownLock = new object();

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            StartClock();
            UpdateLicenseStatus();
            CreateScreenshotPopup();

            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.IsLicensed))
                    UpdateLicenseStatus();
            };
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            var now = DateTime.Now;
            if ((now - _lastValidationTime).TotalMilliseconds < VALIDATION_THROTTLE_MS)
                return;
            _lastValidationTime = now;

            ValidateDropdownState();
        }

        private void ValidateDropdownState()
        {
            if (_screenshotPopup != null && _screenshotPopup.IsOpen)
                return;

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(ValidateDropdownState);
                return;
            }

            if (_activeDropdown == null || !_activeDropdown.IsOpen)
            {
                _activeDropdown = null;
                _activeDropdownName = null;
                return;
            }

            Point mousePos = Mouse.GetPosition(this);
            Button activeButton = GetButtonByDropdownName(_activeDropdownName);

            if (activeButton != null)
            {
                bool mouseOverCard = IsMouseOverElement(activeButton, true);
                bool mouseOverDropdown = IsMouseOverElement(_activeDropdown.Child as FrameworkElement, false);

                if (!mouseOverCard && !mouseOverDropdown)
                {
                    CloseDropdown();
                }
            }
        }

        private bool IsMouseOverElement(FrameworkElement element, bool useParentBorder)
        {
            if (element == null)
                return false;

            try
            {
                Point mousePos = Mouse.GetPosition(this);

                FrameworkElement checkElement = element;
                if (useParentBorder)
                {
                    var border = GetParentBorder(element);
                    if (border != null)
                        checkElement = border;
                }

                GeneralTransform transform = checkElement.TransformToAncestor(this);
                Point elementPos = transform.Transform(new Point(0, 0));

                double width = checkElement.ActualWidth;
                double height = checkElement.ActualHeight;

                return mousePos.X >= elementPos.X &&
                       mousePos.X <= elementPos.X + width &&
                       mousePos.Y >= elementPos.Y &&
                       mousePos.Y <= elementPos.Y + height;
            }
            catch
            {
                return false;
            }
        }

        private Border GetParentBorder(DependencyObject element)
        {
            DependencyObject parent = element;
            while (parent != null)
            {
                if (parent is Border border)
                    return border;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        private Button GetButtonByDropdownName(string dropdownName)
        {
            if (string.IsNullOrEmpty(dropdownName))
                return null;

            return dropdownName switch
            {
                "DashboardDropdown" => BtnDashboard,
                "LicensingDropdown" => BtnSubscription,
                "CalculatorsDropdown" => BtnCalculators,
                "OperationsDropdown" => BtnOperations,
                "ReportsDropdown" => BtnReports,
                "SettingsDropdown" => BtnSettings,
                _ => null
            };
        }

        private void CloseDropdown()
        {
            lock (_dropdownLock)
            {
                if (_activeDropdown != null)
                {
                    try { _activeDropdown.IsOpen = false; }
                    catch { }
                }
                _activeDropdown = null;
                _activeDropdownName = null;
            }
        }

        private void OpenDropdown(string dropdownName)
        {
            lock (_dropdownLock)
            {
                var dropdown = this.FindName(dropdownName) as Popup;
                if (dropdown == null)
                    return;

                if (_activeDropdown != null && _activeDropdown != dropdown)
                {
                    try { _activeDropdown.IsOpen = false; }
                    catch { }
                }

                _activeDropdown = dropdown;
                _activeDropdownName = dropdownName;

                try { dropdown.IsOpen = true; }
                catch { }
            }
        }

        // CLICK handler - open dropdown
        private void DropdownButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string dropdownName = button.Tag as string;
                if (!string.IsNullOrEmpty(dropdownName))
                {
                    OpenDropdown(dropdownName);
                }
            }
        }

        // MOUSE ENTER handler - auto-open dropdown
        private void DropdownButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Button button)
            {
                string dropdownName = button.Tag as string;
                if (!string.IsNullOrEmpty(dropdownName))
                {
                    OpenDropdown(dropdownName);
                }
            }
        }

        // ==================== MENU HANDLERS ====================

        private void MenuItem_Overview_Click(object sender, RoutedEventArgs e)
        {
            CloseDropdown();
            _viewModel.ShowDashboard();
        }

        private void MenuItem_Statistics_Click(object sender, RoutedEventArgs e)
        {
            CloseDropdown();
            _viewModel.ShowDashboard();
        }

        private void MenuItem_RecentActivity_Click(object sender, RoutedEventArgs e)
        {
            CloseDropdown();
            _viewModel.ShowDashboard();
        }

        private void MenuItem_ActivateLicense_Click(object sender, RoutedEventArgs e)
        {
            CloseDropdown();
            _viewModel.ShowSubscriptionPlan();
        }

        private void MenuItem_LicenseInfo_Click(object sender, RoutedEventArgs e)
        {
            CloseDropdown();
            _viewModel.ShowSubscriptionPlan();
        }

        private void MenuItem_Subscription_Click(object sender, RoutedEventArgs e)
        {
            CloseDropdown();
            _viewModel.ShowSubscriptionPlan();
        }

        private void MenuItem_SGU_Click(object sender, RoutedEventArgs e)
        {
            if (CheckLicense())
            {
                CloseDropdown();
                _viewModel.ShowSGUCalculator();
            }
        }

        private void MenuItem_DGU_Click(object sender, RoutedEventArgs e)
        {
            if (CheckLicense())
            {
                CloseDropdown();
                _viewModel.ShowDGUCalculator();
            }
        }

        private void MenuItem_Lamination_Click(object sender, RoutedEventArgs e)
        {
            if (CheckLicense())
            {
                CloseDropdown();
                _viewModel.ShowLaminationCalculator();
            }
        }

        private void MenuItem_DguLam_Click(object sender, RoutedEventArgs e)
        {
            if (CheckLicense())
            {
                CloseDropdown();
                _viewModel.ShowDGULaminationCalculator();
            }
        }

        private void MenuItem_Optimizer_Click(object sender, RoutedEventArgs e)
        {
            if (CheckLicense())
            {
                CloseDropdown();
                _viewModel.ShowGlassOptimization();
            }
        }

        private void MenuItem_SheetStore_Click(object sender, RoutedEventArgs e)
        {
            CloseDropdown();
            _viewModel.ShowSheetStore();
        }

        private void MenuItem_DailyWorks_Click(object sender, RoutedEventArgs e)
        {
            CloseDropdown();
            _viewModel.ShowDailyWorks();
        }

        private void MenuItem_Deliveries_Click(object sender, RoutedEventArgs e)
        {
            CloseDropdown();
            _viewModel.ShowDeliveries();
        }

        private void MenuItem_DailyWorksReport_Click(object sender, RoutedEventArgs e)
        {
            CloseDropdown();
            _viewModel.ShowDailyWorks();
        }

        private void MenuItem_DeliveryReport_Click(object sender, RoutedEventArgs e)
        {
            CloseDropdown();
            _viewModel.ShowDeliveries();
        }

        private void MenuItem_InventoryReport_Click(object sender, RoutedEventArgs e)
        {
            CloseDropdown();
            _viewModel.ShowSheetStore();
        }

        private void MenuItem_Profile_Click(object sender, RoutedEventArgs e)
        {
            CloseDropdown();
            _viewModel.ShowProfile();
        }

        private void MenuItem_Users_Click(object sender, RoutedEventArgs e)
        {
            CloseDropdown();
            _viewModel.ShowUsers();
        }

        private void BtnHome_Click(object sender, RoutedEventArgs e)
        {
            CloseDropdown();
            _viewModel.ShowDashboard();
        }

        // ==================== SCREENSHOT ====================

        private void CreateScreenshotPopup()
        {
            _screenshotPopup = new Popup
            {
                AllowsTransparency = true,
                PopupAnimation = PopupAnimation.Fade,
                StaysOpen = false,
                PlacementTarget = ScreenshotBtn,
                Placement = PlacementMode.Top
            };

            var border = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(0),
                Margin = new Thickness(0, 8, 0, 0),
                MinWidth = 280,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 15,
                    ShadowDepth = 3,
                    Opacity = 0.2,
                    Color = Colors.Black
                }
            };

            var stack = new StackPanel { Margin = new Thickness(0) };

            var header = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB")),
                CornerRadius = new CornerRadius(12, 12, 0, 0),
                Padding = new Thickness(16, 12, 16, 12)
            };

            var headerStack = new StackPanel { Orientation = Orientation.Horizontal };
            headerStack.Children.Add(new TextBlock { Text = "📷", FontSize = 16, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center });
            headerStack.Children.Add(new TextBlock { Text = "Screenshot Quality", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center });
            header.Child = headerStack;
            stack.Children.Add(header);

            var btnStack = new StackPanel { Margin = new Thickness(8, 12, 8, 12) };
            btnStack.Children.Add(CreateQualityButton("📱", "HD 720p", "2560 × 1440", "#64748B", "#F1F5F9", "720p", ScreenshotQuality.HD));
            btnStack.Children.Add(CreateQualityButton("🖥️", "Full HD 1080p", "3840 × 2160", "#2563EB", "#DBEAFE", "1080p", ScreenshotQuality.FullHD));
            btnStack.Children.Add(CreateQualityButton("🎬", "2K QHD", "5120 × 2880", "#D97706", "#FEF3C7", "2K", ScreenshotQuality.QHD));
            btnStack.Children.Add(CreateQualityButton("📺", "4K UHD", "7680 × 4320", "#059669", "#D1FAE5", "4K", ScreenshotQuality.UltraHD));
            btnStack.Children.Add(CreateQualityButton("🏆", "8K UHD", "10240 × 5760", "#DC2626", "#FEE2E2", "8K", ScreenshotQuality.UHD8K));
            stack.Children.Add(btnStack);

            var cancelBorder = new Border
            {
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(8, 8, 8, 12)
            };

            var cancelBtn = new Button
            {
                Content = "Cancel",
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(16, 8, 16, 8),
                Cursor = Cursors.Hand,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            cancelBtn.Click += (s, args) => CloseScreenshotPopup();
            cancelBorder.Child = cancelBtn;
            stack.Children.Add(cancelBorder);

            border.Child = stack;
            _screenshotPopup.Child = border;
        }

        private Button CreateQualityButton(string icon, string title, string resolution, string textColor, string badgeColor, string badgeText, ScreenshotQuality quality)
        {
            var button = new Button
            {
                Tag = quality,
                Margin = new Thickness(0, 0, 0, 6),
                Padding = new Thickness(12, 10, 12, 10),
                Cursor = Cursors.Hand,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Background = Brushes.White,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"))
            };

            button.MouseEnter += (s, args) =>
            {
                button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFF6FF"));
                button.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
            };

            button.MouseLeave += (s, args) =>
            {
                button.Background = Brushes.White;
                button.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));
            };

            button.Click += (s, args) =>
            {
                CloseScreenshotPopup();
                CaptureScreenshot(quality);
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var iconText = new TextBlock { Text = icon, FontSize = 20, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(iconText, 0);
            grid.Children.Add(iconText);

            var textStack = new StackPanel { Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            textStack.Children.Add(new TextBlock { Text = title, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")) });
            textStack.Children.Add(new TextBlock { Text = resolution, FontSize = 10, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")) });
            Grid.SetColumn(textStack, 1);
            grid.Children.Add(textStack);

            var badgeBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(badgeColor)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 3, 8, 3),
                VerticalAlignment = VerticalAlignment.Center
            };
            badgeBorder.Child = new TextBlock { Text = badgeText, FontSize = 9, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(textColor)) };
            Grid.SetColumn(badgeBorder, 2);
            grid.Children.Add(badgeBorder);

            button.Content = grid;
            return button;
        }

        private void CloseScreenshotPopup()
        {
            if (_screenshotPopup != null)
                _screenshotPopup.IsOpen = false;
        }

        private void ScreenshotBtn_Click(object sender, RoutedEventArgs e)
        {
            CloseDropdown();
            if (_screenshotPopup != null)
                _screenshotPopup.IsOpen = !_screenshotPopup.IsOpen;
        }

        public enum ScreenshotQuality { HD, FullHD, QHD, UltraHD, UHD8K }

        private async void CaptureScreenshot(ScreenshotQuality quality)
        {
            try
            {
                // Target resolutions - maximum quality presets
                int targetWidth = 3840, targetHeight = 2160; // 4K base
                switch (quality)
                {
                    case ScreenshotQuality.HD: targetWidth = 2560; targetHeight = 1440; break;
                    case ScreenshotQuality.QHD: targetWidth = 5120; targetHeight = 2880; break;
                    case ScreenshotQuality.UltraHD: targetWidth = 7680; targetHeight = 4320; break;
                    case ScreenshotQuality.UHD8K: targetWidth = 10240; targetHeight = 5760; break;
                }

                // Force layout update on UI thread
                UpdateLayout();
                Dispatcher.Invoke(() => { }, DispatcherPriority.Loaded);

                // Calculate scale to reach target resolution
                double scaleX = targetWidth / ActualWidth;
                double scaleY = targetHeight / ActualHeight;
                double scale = Math.Max(scaleX, scaleY);

                // Minimum 4x scale for ultra quality
                if (scale < 4.0) scale = 4.0;

                int width = (int)(ActualWidth * scale);
                int height = (int)(ActualHeight * scale);
                int dpi = (int)(96 * scale);

                // Create high-quality bitmap on UI thread
                RenderTargetBitmap rtb = new RenderTargetBitmap(
                    width, height, dpi, dpi, PixelFormats.Pbgra32);
                rtb.Render(this);
                rtb.Freeze();

                // Show save dialog
                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PNG Image|*.png|BMP Image|*.bmp|JPEG Image|*.jpg",
                    FileName = $"ProGlass_{quality}_{DateTime.Now:yyyyMMdd_HHmmss}.png",
                    Title = "Save Screenshot"
                };

                if (sfd.ShowDialog() == true)
                {
                    string filePath = sfd.FileName;
                    string extension = Path.GetExtension(filePath).ToLower();

                    // Ensure directory exists
                    var directory = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(directory) && !string.IsNullOrEmpty(directory))
                        Directory.CreateDirectory(directory);

                    // Save file on background thread - ENCODE HERE, NOT ON UI THREAD
                    await Task.Run(() =>
                    {
                        // Create encoder on background thread
                        BitmapEncoder encoder = extension switch
                        {
                            ".png" => new PngBitmapEncoder { Interlace = PngInterlaceOption.On },
                            ".bmp" => new BmpBitmapEncoder(),
                            _ => new JpegBitmapEncoder { QualityLevel = 100 }
                        };

                        encoder.Frames.Add(BitmapFrame.Create(rtb));

                        using var fs = File.Create(filePath);
                        encoder.Save(fs);
                    });

                    // Get file size for display
                    var fileInfo = new FileInfo(filePath);
                    string sizeDisplay = fileInfo.Length >= 1024 * 1024
                        ? $"{fileInfo.Length / (1024.0 * 1024.0):F2} MB"
                        : $"{fileInfo.Length / 1024.0:F1} KB";

                    // Show success message on UI thread
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"✅ Screenshot saved!\n\n📁 {filePath}\n📐 {width}x{height}\n💾 {sizeDisplay}", "Screenshot", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Error: {ex.Message}", "Screenshot Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void UpdateLicenseStatus()
        {
            if (_viewModel.IsLicensed)
            {
                LicenseWarningBorder.Visibility = Visibility.Collapsed;
                LicenseActiveBorder.Visibility = Visibility.Visible;
            }
            else
            {
                LicenseWarningBorder.Visibility = Visibility.Visible;
                LicenseActiveBorder.Visibility = Visibility.Collapsed;
            }
        }

        private void LicenseWarningBorder_Click(object sender, MouseButtonEventArgs e)
        {
            _viewModel.ShowSubscriptionPlan();
        }

        private void StartClock()
        {
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => {
                ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
                CurrentDateText.Text = DateTime.Now.ToString("dd-MMM-yyyy");
            };
            _clockTimer.Start();
        }

        private bool CheckLicense()
        {
            if (!_viewModel.IsLicensed)
            {
                MessageBox.Show("Enterprise License Required.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                _viewModel.ShowSubscriptionPlan();
                UpdateLicenseStatus();
                return false;
            }
            return true;
        }
    }
}