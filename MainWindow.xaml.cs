using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

using ProGlassAutomation.Views.SGU;
using ProGlassAutomation.Views.DGU;
using ProGlassAutomation.Views.Lamination;

namespace ProGlassAutomation
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            CurrentDateText = DateTime.Now.ToString("dd MMM yyyy");
            ClockText = DateTime.Now.ToString("HH:mm:ss");

            StartClock();
            LoadWelcome();
        }

        // ================= LIVE CLOCK =================
        private void StartClock()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);

            _timer.Tick += (s, e) =>
            {
                CurrentDateText = DateTime.Now.ToString("dd MMM yyyy");
                ClockText = DateTime.Now.ToString("HH:mm:ss");

                OnPropertyChanged(nameof(CurrentDateText));
                OnPropertyChanged(nameof(ClockText));
            };

            _timer.Start();
        }

        // ================= PROPERTIES =================
        private string _currentDateText;
        public string CurrentDateText
        {
            get => _currentDateText;
            set
            {
                _currentDateText = value;
                OnPropertyChanged(nameof(CurrentDateText));
            }
        }

        private string _clockText;
        public string ClockText
        {
            get => _clockText;
            set
            {
                _clockText = value;
                OnPropertyChanged(nameof(ClockText));
            }
        }

        // ================= WELCOME SCREEN (WITH STATUS CARDS) =================
        private void LoadWelcome()
        {
            MainPanel.Content = new Grid
            {
                Children =
                {
                    new StackPanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,

                        Children =
                        {
                            // TITLE
                            new TextBlock
                            {
                                Text = "🏭 WELCOME TO GLASS ERP SYSTEM",
                                FontSize = 26,
                                FontWeight = FontWeights.Bold,
                                HorizontalAlignment = HorizontalAlignment.Center
                            },

                            new TextBlock
                            {
                                Text = "Select module from sidebar to continue",
                                FontSize = 14,
                                Margin = new Thickness(0,10,0,25),
                                HorizontalAlignment = HorizontalAlignment.Center
                            },

                            // ================= STATUS CARDS =================
                            new StackPanel
                            {
                                Orientation = Orientation.Horizontal,
                                HorizontalAlignment = HorizontalAlignment.Center,

                                Children =
                                {
                                    CreateStatusCard("📊 SGU MODULE", "LIVE READY", Brushes.LimeGreen),
                                    CreateStatusCard("🧮 DGU MODULE", "LIVE READY", Brushes.LimeGreen),
                                    CreateStatusCard("🧪 LAMINATION", "LIVE READY", Brushes.LimeGreen)
                                }
                            },

                            // FOOTER STATUS
                            new TextBlock
                            {
                                Text = "🟢 ALL SYSTEMS ONLINE",
                                Margin = new Thickness(0,25,0,0),
                                Foreground = Brushes.Green,
                                FontWeight = FontWeights.Bold,
                                HorizontalAlignment = HorizontalAlignment.Center
                            }
                        }
                    }
                }
            };
        }

        // ================= STATUS CARD CREATOR =================
        private UIElement CreateStatusCard(string title, string status, Brush color)
        {
            return new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                CornerRadius = new CornerRadius(12),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(15),
                Margin = new Thickness(10),
                Width = 180,
                Height = 90,

                Child = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,

                    Children =
                    {
                        new TextBlock
                        {
                            Text = title,
                            FontWeight = FontWeights.Bold,
                            TextAlignment = TextAlignment.Center
                        },

                        new TextBlock
                        {
                            Text = status,
                            Foreground = color,
                            FontWeight = FontWeights.Bold,
                            FontSize = 14,
                            TextAlignment = TextAlignment.Center
                        }
                    }
                }
            };
        }

        // ================= MODULE LOADER =================
        private void LoadModule(UserControl view)
        {
            try
            {
                if (view != null)
                    MainPanel.Content = view;
                else
                    MessageBox.Show("Module not found.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading module: " + ex.Message);
            }
        }

        // ================= NAVIGATION =================
        private void SGU_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            LoadModule(new SguView());
        }

        private void DGU_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            LoadModule(new DguView());
        }

        private void LAM_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            LoadModule(new LaminationView());
        }

        // ================= PROPERTY CHANGE =================
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}