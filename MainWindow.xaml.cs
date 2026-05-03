using System;
using System.Windows;
using System.Windows.Threading;
using ProGlassAutomation.ViewModels;

namespace ProGlassAutomation
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private DispatcherTimer _clockTimer;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            StartClock();
            UpdateClockDisplay();
        }

        public MainViewModel ViewModel => _viewModel;

        private void StartClock()
        {
            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += (s, e) => UpdateClockDisplay();
            _clockTimer.Start();
        }

        private void UpdateClockDisplay()
        {
            Dispatcher.Invoke(() =>
            {
                ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
                CurrentDateText.Text = DateTime.Now.ToString("dd-MMM-yyyy");
            });
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var aboutDialog = new Views.About.AboutDialog();
            aboutDialog.Owner = this;
            aboutDialog.ShowDialog();
        }
    }
}