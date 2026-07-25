using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ProGlassAutomation.Views.Optimization;
using ProGlassAutomation.Views.Optimization.Algorithms;

namespace ProGlassAutomation.ViewModels.Optimization.Algorithms
{
    public class OptimizationViewModel : INotifyPropertyChanged
    {
        private OptimizationLevel _selectedLevel = OptimizationLevel.Nirvana;
        private double _lm = 15;
        private double _rm = 15;
        private double _tm = 15;
        private double _bm = 15;
        private double _kerf = 4.0;
        private double _breakout = 15.0; // Trim default as requested: 15 trim and minimum breakout 15

        private double _overallUtilization;
        private double _overallWastage;
        private double _usedSQM;
        private int _totalPartsCut;
        private int _totalPartsUnplaced;

        public event PropertyChangedEventHandler PropertyChanged;

        public OptimizationLevel SelectedLevel
        {
            get => _selectedLevel;
            set
            {
                if (_selectedLevel != value)
                {
                    _selectedLevel = value;
                    OnPropertyChanged();
                }
            }
        }

        public double LM
        {
            get => _lm;
            set { _lm = value; OnPropertyChanged(); }
        }

        public double RM
        {
            get => _rm;
            set { _rm = value; OnPropertyChanged(); }
        }

        public double TM
        {
            get => _tm;
            set { _tm = value; OnPropertyChanged(); }
        }

        public double BM
        {
            get => _bm;
            set { _bm = value; OnPropertyChanged(); }
        }

        public double Kerf
        {
            get => _kerf;
            set { _kerf = value; OnPropertyChanged(); }
        }

        public double Breakout
        {
            get => _breakout;
            set { _breakout = value; OnPropertyChanged(); }
        }

        public double OverallUtilization
        {
            get => _overallUtilization;
            set { _overallUtilization = value; OnPropertyChanged(); }
        }

        public double OverallWastage
        {
            get => _overallWastage;
            set { _overallWastage = value; OnPropertyChanged(); }
        }

        public double UsedSQM
        {
            get => _usedSQM;
            set { _usedSQM = value; OnPropertyChanged(); }
        }

        public int TotalPartsCut
        {
            get => _totalPartsCut;
            set { _totalPartsCut = value; OnPropertyChanged(); }
        }

        public int TotalPartsUnplaced
        {
            get => _totalPartsUnplaced;
            set { _totalPartsUnplaced = value; OnPropertyChanged(); }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
