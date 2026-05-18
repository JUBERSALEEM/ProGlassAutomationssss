using System;
using System.Windows.Input;

namespace ProGlassAutomation.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _exec;
        private readonly Func<object, bool> _canExec;

        public RelayCommand(Action<object> exec, Func<object, bool> canExec = null)
        {
            _exec = exec ?? throw new ArgumentNullException(nameof(exec));
            _canExec = canExec;
        }

        public RelayCommand(Action exec, Func<bool> canExec = null)
            : this(_ => exec(), canExec != null ? _ => canExec() : null) { }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object p) => _canExec?.Invoke(p) ?? true;
        public void Execute(object p) => _exec(p);
    }
}