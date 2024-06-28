using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.ViewModel
{
    public class AppViewModel : INotifyPropertyChanged
    {
        private string _loggedInUserEmail;

        public string LoggedInUserEmail
        {
            get { return _loggedInUserEmail; }
            set
            {
                _loggedInUserEmail = value;
                OnPropertyChanged(nameof(LoggedInUserEmail));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
