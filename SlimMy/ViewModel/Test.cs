using SlimMy.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace SlimMy.ViewModel
{
    class Test : INotifyPropertyChanged
    {
        private User _user;
        private Repo _repo;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";

        public Command InsertCommand { get; set; }

        public Test()
        {
            InsertCommand = new Command(InsertPerson);
            _user = new User();
            _repo = new Repo(_connstring);
        }

        public User User
        {
            get { return _user; }
            set { _user = value; OnPropertyChanged(nameof(User)); }
        }

        private void InsertPerson(object parameter)
        {
            _repo.InsertPerson(User.Dv_1);
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
