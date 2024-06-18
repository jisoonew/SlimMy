using SlimMy.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SlimMy
{
    class Command : ICommand
    {
        Action<object> ExecuteMethod;
        Func<object, bool> CanexecuteMethod;
        private Action<User> nickNamePrint;
        private Action<Chat> chatData;
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;
        private readonly Action<object> execute;
        private readonly Func<object, bool> canExecute;

        public Command(Action<object> execute)
            : this(execute, null)
        {
        }

        public Command(Action<User> nickNamePrint)
        {
            this.nickNamePrint = nickNamePrint;
        }

        public Command(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return this.canExecute == null || this.canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            this.execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
