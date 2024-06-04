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

        public Command(Action<User> nickNamePrint)
        {
            this.nickNamePrint = nickNamePrint;
        }

        public Command(Action<object> execute_Method, Func<object, bool> canexecute_Method = null)
        {
            this.ExecuteMethod = execute_Method; // 커맨드가 실제로 실행할 함수
            this.CanexecuteMethod = canexecute_Method; // 수행되기 전에 필요한 조건을 검사하는 메소드
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            ExecuteMethod(parameter);
        }
    }
}
