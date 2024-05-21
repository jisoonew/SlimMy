using SlimMy.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SlimMy.ViewModel
{
    public class Login : INotifyPropertyChanged
    {
        private User user;
        private string displayName;

        public User User
        {
            get { return user; }
            set { user = value; OnPropertyChanged(); }
        }

        public string DisplayName
        {
            get { return displayName; }
            set { displayName = value; OnPropertyChanged(); }
        }

        public ICommand ShowNameCommand { get; set; }

        public Login()
        {
            User = new User();
            ShowNameCommand = new RelayCommand(ShowName);
        }

        private void ShowName(object parameter)
        {
            DisplayName = $"Hello, {User.Name}!";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> execute;
        private readonly Func<object, bool> canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return canExecute == null || canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            execute(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
    
    }

