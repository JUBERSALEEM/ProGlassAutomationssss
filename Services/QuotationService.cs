using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProGlassAutomation.Services
{
    public class QuotationService : INotifyPropertyChanged
    {
        private static QuotationService? _instance;
        public static QuotationService Instance => _instance ??= new QuotationService();

        public ObservableCollection<QuotationItem> Items { get; set; } = new();

        public QuotationService()
        {
            _instance = this;
        }

        public void AddItem(string spec, string width, string height, string widthMm, string heightMm, string qty, string price)
        {
            var item = new QuotationItem
            {
                Id = Items.Count + 1,
                Spec = spec,
                Width = width,
                Height = height,
                WidthMm = widthMm,
                HeightMm = heightMm,
                Qty = qty,
                Price = price
            };
            Items.Add(item);
            OnPropertyChanged(nameof(Items));
        }

        public void RemoveItem(QuotationItem item)
        {
            if (item != null)
            {
                Items.Remove(item);
                OnPropertyChanged(nameof(Items));
            }
        }

        public void Clear()
        {
            Items.Clear();
            OnPropertyChanged(nameof(Items));
        }

        public double GetTotal()
        {
            double total = 0;
            foreach (var item in Items)
            {
                if (double.TryParse(item.Price, out var price) && double.TryParse(item.Qty, out var qty))
                {
                    total += price * qty;
                }
            }
            return total;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class QuotationItem : INotifyPropertyChanged
    {
        public int Id { get; set; }

        private string? _spec;
        public string Spec
        {
            get => _spec;
            set { _spec = value; OnPropertyChanged(); }
        }

        private string? _width;
        public string Width
        {
            get => _width;
            set { _width = value; OnPropertyChanged(); }
        }

        private string? _height;
        public string Height
        {
            get => _height;
            set { _height = value; OnPropertyChanged(); }
        }

        private string? _widthMm;
        public string WidthMm
        {
            get => _widthMm;
            set { _widthMm = value; OnPropertyChanged(); }
        }

        private string? _heightMm;
        public string HeightMm
        {
            get => _heightMm;
            set { _heightMm = value; OnPropertyChanged(); }
        }

        private string? _qty;
        public string Qty
        {
            get => _qty;
            set { _qty = value; OnPropertyChanged(); CalculateLineTotal(); }
        }

        private string? _price;
        public string Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(); CalculateLineTotal(); }
        }

        private string? _lineTotal;
        public string LineTotal
        {
            get => _lineTotal;
            set { _lineTotal = value; OnPropertyChanged(); }
        }

        private void CalculateLineTotal()
        {
            if (double.TryParse(Price, out var price) && double.TryParse(Qty, out var qty))
            {
                LineTotal = (price * qty).ToString("0.00");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}