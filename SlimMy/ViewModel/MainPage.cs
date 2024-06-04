using SlimMy.Model;
using SlimMy.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SlimMy.ViewModel
{
    class MainPage : INotifyPropertyChanged
    {
        private User _user;
        private Repo _repo;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";

        public MainPage()
        {
            _user = User;
        }

        public User User
        {
            get { return _user; }
            set { _user = value; OnPropertyChanged(nameof(User)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
