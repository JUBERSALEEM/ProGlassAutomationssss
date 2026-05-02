// Your Helpers folder RelayCommand likely looks like this:
// Change it to accept Action<object>:

using System.Windows.Input;

namespace ProGlassAutomation.Helpers
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;

        public RelayCommand(Action<object> execute)
        {
            _execute = execute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter) => _execute(parameter);
    }
}