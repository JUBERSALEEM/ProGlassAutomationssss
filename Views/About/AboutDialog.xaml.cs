using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ProGlassAutomation.Views.About
{
    public partial class AboutDialog : Window
    {
        private const double AnimationDuration = 0.3;
        private const double FadeInOpacity = 1.0;
        private const double FadeOutOpacity = 0.0;

        public AboutDialog()
        {
            InitializeComponent();
            this.Loaded += AboutDialog_Loaded;
        }

        /// <summary>
        /// Initialize animations and UI on window load
        /// </summary>
        private void AboutDialog_Loaded(object sender, RoutedEventArgs e)
        {
            AnimateWindowEntry();
        }

        /// <summary>
        /// Smooth fade-in animation on window entry
        /// </summary>
        private void AnimateWindowEntry()
        {
            var fadeInAnimation = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromSeconds(AnimationDuration),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            this.BeginAnimation(OpacityProperty, fadeInAnimation);
        }

        /// <summary>
        /// Handle window drag from title bar
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ClickCount == 2)
                {
                    // Double-click to maximize/restore
                    this.WindowState = this.WindowState == WindowState.Maximized
                        ? WindowState.Normal
                        : WindowState.Maximized;
                }
                else
                {
                    // Allow window dragging
                    this.DragMove();
                }
            }
            catch (InvalidOperationException)
            {
                // Drag operation failed (user moved mouse too fast)
            }
        }

        /// <summary>
        /// Smooth close animation and window shutdown
        /// </summary>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            AnimateWindowExit();
        }

        /// <summary>
        /// Fade-out animation before closing
        /// </summary>
        private void AnimateWindowExit()
        {
            var fadeOutAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromSeconds(AnimationDuration),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            fadeOutAnimation.Completed += (s, e) => this.Close();
            this.BeginAnimation(OpacityProperty, fadeOutAnimation);
        }

        /// <summary>
        /// Open email client with developer contact
        /// </summary>
        private void Email_Click(object sender, RoutedEventArgs e)
        {
            const string developerEmail = "Jubersaleem01@gmail.com";
            const string subject = "ProGlass ERP - Support Request";

            try
            {
                var mailtoUri = new Uri($"mailto:{developerEmail}?subject={Uri.EscapeDataString(subject)}");
                Process.Start(new ProcessStartInfo(mailtoUri.AbsoluteUri)
                {
                    UseShellExecute = true
                });

                ShowNotification("Email client opened successfully");
            }
            catch (Exception ex)
            {
                ShowErrorNotification($"Failed to open email client: {ex.Message}");
            }
        }

        /// <summary>
        /// Open WhatsApp conversation with developer
        /// </summary>
        private void WhatsApp_Click(object sender, RoutedEventArgs e)
        {
            const string phoneNumber = "+971559117727";
            const string message = "Hi, I need support with ProGlass ERP";

            try
            {
                // WhatsApp Web URL scheme
                var whatsappUri = new Uri($"https://wa.me/{phoneNumber.Replace("+", "").Replace("-", "")}?text={Uri.EscapeDataString(message)}");
                Process.Start(new ProcessStartInfo(whatsappUri.AbsoluteUri)
                {
                    UseShellExecute = true
                });

                ShowNotification("Opening WhatsApp...");
            }
            catch (Exception ex)
            {
                ShowErrorNotification($"Failed to open WhatsApp: {ex.Message}");
            }
        }

        /// <summary>
        /// Display success notification with smooth animation
        /// </summary>
        private void ShowNotification(string message)
        {
            // Optional: Implement a toast notification system
            // For now, just log to debug
            Debug.WriteLine($"[Notification] {message}");
        }

        /// <summary>
        /// Display error notification with smooth animation
        /// </summary>
        private void ShowErrorNotification(string message)
        {
            // Optional: Implement a toast notification system
            Debug.WriteLine($"[Error] {message}");

            MessageBox.Show(
                message,
                "ProGlass ERP - Notification",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        /// <summary>
        /// Handle escape key to close window
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                AnimateWindowExit();
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

        /// <summary>
        /// Smooth scroll behavior for scrollviewer
        /// </summary>
        private void OptimizeScrolling()
        {
            // ScrollViewer can be enhanced with custom behavior
            // This is a placeholder for future scroll optimization
        }
    }
}