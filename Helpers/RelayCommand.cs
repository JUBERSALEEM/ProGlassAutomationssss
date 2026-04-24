using System;
using System.Windows.Input;

namespace ProGlassAutomation.Helpers
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        public RelayCommand(Action execute) => _execute = execute;
        public event EventHandler CanExecuteChanged;
        public bool CanExecute(object p) => true;
        public void Execute(object p) => _execute();
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        public RelayCommand(Action<T> execute) => _execute = execute;
        public event EventHandler CanExecuteChanged;
        public bool CanExecute(object p) => true;
        public void Execute(object p) => _execute(p is T ? (T)p : default);
    }
}