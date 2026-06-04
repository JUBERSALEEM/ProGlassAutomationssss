using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProGlassAutomation.Views.Print
{
    public class PrintSettings : INotifyPropertyChanged
    {
        private string _paperSize = "A4";
        private bool _isLandscape = true;
        private double _topMargin = 0.5;
        private double _bottomMargin = 0.5;
        private double _leftMargin = 0.5;
        private double _rightMargin = 0.5;
        private double _scale = 100;
        private bool _fitToPage = true;
        private bool _printGridLines = false;
        private bool _printHeadings = true;
        private bool _printNotes = true;
        private double _zoom = 100;

        public string PaperSize
        {
            get => _paperSize;
            set { _paperSize = value; OnPropertyChanged(); ApplySettings(); }
        }

        public bool IsLandscape
        {
            get => _isLandscape;
            set { _isLandscape = value; OnPropertyChanged(); OnPropertyChanged(nameof(OrientationDisplay)); ApplySettings(); }
        }

        public double TopMargin
        {
            get => _topMargin;
            set { _topMargin = value; OnPropertyChanged(); ApplySettings(); }
        }

        public double BottomMargin
        {
            get => _bottomMargin;
            set { _bottomMargin = value; OnPropertyChanged(); ApplySettings(); }
        }

        public double LeftMargin
        {
            get => _leftMargin;
            set { _leftMargin = value; OnPropertyChanged(); ApplySettings(); }
        }

        public double RightMargin
        {
            get => _rightMargin;
            set { _rightMargin = value; OnPropertyChanged(); ApplySettings(); }
        }

        public double Scale
        {
            get => _scale;
            set { _scale = value; OnPropertyChanged(); ApplySettings(); }
        }

        public bool FitToPage
        {
            get => _fitToPage;
            set { _fitToPage = value; OnPropertyChanged(); ApplySettings(); }
        }

        public bool PrintGridLines
        {
            get => _printGridLines;
            set { _printGridLines = value; OnPropertyChanged(); }
        }

        public bool PrintHeadings
        {
            get => _printHeadings;
            set { _printHeadings = value; OnPropertyChanged(); }
        }

        public bool PrintNotes
        {
            get => _printNotes;
            set { _printNotes = value; OnPropertyChanged(); }
        }

        public double Zoom
        {
            get => _zoom;
            set { _zoom = value; OnPropertyChanged(); ApplySettings(); }
        }

        public string OrientationDisplay => IsLandscape ? "Landscape" : "Portrait";

        public string[] AvailablePaperSizes => new[] { "A4", "A3", "Letter", "Legal", "Tabloid" };

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler SettingsChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void ApplySettings()
            => SettingsChanged?.Invoke(this, EventArgs.Empty);

        public void ToggleOrientation()
        {
            IsLandscape = !IsLandscape;
        }

        public void SetScaleFromIndex(int index)
        {
            Scale = index switch
            {
                0 => 50,
                1 => 75,
                2 => 100,
                3 => 0, // Fit to page
                _ => 100
            };
            FitToPage = index == 3;
        }

        public void SetPaperSizeFromIndex(int index)
        {
            if (index >= 0 && index < AvailablePaperSizes.Length)
                PaperSize = AvailablePaperSizes[index];
        }

        public void SetMargin(string margin, double value)
        {
            switch (margin.ToUpper())
            {
                case "T": TopMargin = value; break;
                case "B": BottomMargin = value; break;
                case "L": LeftMargin = value; break;
                case "R": RightMargin = value; break;
            }
        }
    }
}