using System;
using System.Windows.Input;

namespace SlimMy
{
    public class CommandBase : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public CommandBase(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();

        public void Execute(object parameter) => _execute();

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}