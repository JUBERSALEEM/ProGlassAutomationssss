using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProGlassAutomation.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // Single property notification (for backward compatibility)
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Multiple properties notification (used by ProformaInvoiceModel)
        protected void Notify(params string[] props)
        {
            foreach (var p in props)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
            }
        }

        // Single property setter (backward compatible)
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // Multiple properties setter (used by ProformaInvoiceModel)
        protected bool Set<T>(ref T field, T value, params string[] props)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            Notify(props);
            return true;
        }
    }
}