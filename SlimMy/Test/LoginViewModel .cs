using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SlimMy.Test
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly IDataService _dataService;
        private string _userId;
        private readonly IView _view;

        public string UserId
        {
            get => _userId;
            set
            {
                _userId = value;
                OnPropertyChanged(nameof(UserId));
            }
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel(IDataService dataService, IView view)
        {
            _dataService = dataService;
            _view = view;
            LoginCommand = new RelayCommand(Login, CanLogin);
        }

        private void Login(object parameter)
        {
            // 로그인 로직 (예: 사용자 인증)
            // 성공적으로 로그인되었다고 가정
            _dataService.SetUserId(UserId);

            // MainView 열기
            var mainView = new MainView
            {
                DataContext = new MainPageViewModel(_dataService)
            };
            mainView.Show();

            // LoginView 닫기
            _view.Close();
        }

        private bool CanLogin(object parameter)
        {
            // 로그인 가능 여부를 결정하는 로직
            return !string.IsNullOrWhiteSpace(UserId);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
