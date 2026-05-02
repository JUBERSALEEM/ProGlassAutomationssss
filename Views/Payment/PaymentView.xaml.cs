using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ProGlassAutomation.Views.Payment
{
    public partial class PaymentView : UserControl
    {
        private string _currentPlanTag = "";
        private string _currentPlanName = "";
        private decimal _currentAmount = 0;
        private int _currentDays = 0;

        public PaymentView()
        {
            InitializeComponent();
        }

        public PaymentView(string planTag, string planName, decimal amount, int days)
        {
            InitializeComponent();

            _currentPlanTag = planTag;
            _currentPlanName = planName;
            _currentAmount = amount;
            _currentDays = days;

            PlanNameText.Text = planName;
            PlanDaysText.Text = $"{days} Days Access";
            PlanAmountText.Text = $"AED {amount}";
        }

        private void PayPal_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string paymentUrl = Services.PaymentService.Instance.CreatePaypalPayment(_currentAmount, _currentPlanTag).GetAwaiter().GetResult();

                if (!string.IsNullOrEmpty(paymentUrl))
                {
                    Process.Start(paymentUrl);
                    MessageBox.Show(
                        $"Please complete your PayPal payment.\n\nAfter payment, contact us on WhatsApp with payment confirmation.\n\nWe will provide your activation key after payment verification.",
                        "PayPal Payment",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                else
                {
                    MessageBox.Show(
                        "Unable to create PayPal payment. Please contact us directly.",
                        "Payment Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Stripe_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string checkoutUrl = Services.PaymentService.Instance.CreateStripeCheckoutSession(_currentAmount, _currentPlanTag);

                if (!string.IsNullOrEmpty(checkoutUrl))
                {
                    Process.Start(checkoutUrl);
                    MessageBox.Show(
                        $"Please complete your Stripe payment.\n\nAfter payment, contact us on WhatsApp with payment confirmation.\n\nWe will provide your activation key after payment verification.",
                        "Stripe Payment",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                else
                {
                    MessageBox.Show(
                        "Unable to create Stripe payment. Please contact us directly.",
                        "Payment Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Razorpay_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string razorpayUrl = Services.PaymentService.Instance.CreateRazorpayOrder(_currentAmount, _currentPlanTag);

                if (!string.IsNullOrEmpty(razorpayUrl))
                {
                    Process.Start(razorpayUrl);
                    MessageBox.Show(
                        $"Please complete your Razorpay payment.\n\nAfter payment, contact us on WhatsApp with payment confirmation.\n\nWe will provide your activation key after payment verification.",
                        "Razorpay Payment",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                else
                {
                    MessageBox.Show(
                        "Unable to create Razorpay payment. Please contact us directly.",
                        "Payment Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BankTransfer_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string bankDetails = @"
BANK TRANSFER DETAILS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Bank Name: First Abu Dhabi Bank (FAB)
Account Name: ProGlass LLC
Account Number: 1234567890
IBAN: AE12 3456 7890 1234 5678 90
Swift Code: FABAEADXXX

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Please include your name and phone number in the transfer reference.

After transfer, send payment proof to:
WhatsApp: +971-559117727
Email: Jubersaleem01@gmail.com

We will provide your activation key after payment verification.
";

                MessageBox.Show(bankDetails, "Bank Transfer Details", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActivateKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string key = ActivationKeyInput.Text.Trim().ToUpper();
                ErrorText.Text = "";

                if (string.IsNullOrEmpty(key))
                {
                    ErrorText.Text = "Please enter a valid activation key.";
                    ErrorText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                    return;
                }

                // Validate key format
                if (!Services.PaymentService.Instance.ValidateKey(key))
                {
                    ErrorText.Text = "Invalid key format. Please check and try again.";
                    ErrorText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                    return;
                }

                // Activate the license
                bool success = Services.SubscriptionService.Instance.ActivateLicense(key);

                if (success)
                {
                    ActivationKeyInput.Text = "";

                    MessageBox.Show(
                        $"Congratulations! Your subscription is now active.\n\nPlan: {_currentPlanName}\nDays: {_currentDays}\n\nYou now have full access to all features.",
                        "Activation Successful",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                    // Navigate to dashboard
                    GoToDashboard();
                }
                else
                {
                    ErrorText.Text = "Activation failed. Please contact support.";
                    ErrorText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                }
            }
            catch (Exception ex)
            {
                ErrorText.Text = $"Error: {ex.Message}";
                ErrorText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
            }
        }

        private void ContactWhatsApp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("https://wa.me/971559117727");
            }
            catch
            {
                MessageBox.Show("WhatsApp: +971-559117727", "Contact", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ContactEmail_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("mailto:Jubersaleem01@gmail.com?subject=ProGlass ERP Payment");
            }
            catch
            {
                MessageBox.Show("Email: Jubersaleem01@gmail.com", "Contact", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BackToPlans_Click(object sender, RoutedEventArgs e)
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

        private void GoToDashboard()
        {
            try
            {
                MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
                mainWindow.ShowDashboard();
            }
            catch { }
        }
    }
}