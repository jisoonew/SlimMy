using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Test
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private readonly IDataService _dataService;
        private string _loggedInUserId;

        public string LoggedInUserId
        {
            get => _loggedInUserId;
            set
            {
                _loggedInUserId = value;
                OnPropertyChanged(nameof(LoggedInUserId));
            }
        }

        public MainPageViewModel(IDataService dataService)
        {
            _dataService = dataService;
            UpdateUserId();
        }

        private void UpdateUserId()
        {
            LoggedInUserId = _dataService.GetUserId();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
