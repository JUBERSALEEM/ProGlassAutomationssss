using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProGlassAutomation.Views.Optimization
{
    public class DemandPart : INotifyPropertyChanged
    {
        private static readonly string[] _palette =
        {
            "#FCD34D", "#FCA5A5", "#FDE68A",
            "#86EFAC", "#A7F3D0", "#BFDBFE",
            "#C4B5FD", "#DDD6FE", "#FBCFE8"
        };

        private static int _nextId = 1;
        private static readonly Random _rand = new Random();

        public DemandPart()
        {
            Id = _nextId++;
            UI_Color = _palette[_rand.Next(_palette.Length)];
        }

        private string _uiColor = "#FCD34D";
        public string UI_Color
        {
            get => _uiColor;
            set { _uiColor = value; OnPropertyChanged(); }
        }

        private int _id;
        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        private string _label = "";
        public string Label
        {
            get => _label;
            set { _label = value; OnPropertyChanged(); }
        }

        private double _l;
        public double L
        {
            get => _l;
            set { _l = value; OnPropertyChanged(); }
        }

        private double _w;
        public double W
        {
            get => _w;
            set { _w = value; OnPropertyChanged(); }
        }

        private int _qty = 1;
        public int Qty
        {
            get => _qty;
            set { _qty = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}