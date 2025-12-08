using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using MVVM2.ViewModel;
using SlimMy.Model;
using SlimMy.Service;
using SlimMy.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SlimMy.ViewModel
{

    public class Login : ViewModelBase
    {
        private User _user;
        private Repo _repo;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";

        // 여성 혹은 남성중 어떤 선택을 할 것인지
        private bool _isMaleChecked;
        private bool _isFemaleChecked;

        public static string myName = null;
        // 이벤트 정의: 로그인 성공 시 발생하는 이벤트
        public event EventHandler<ChatUserList> DataPassed; // 데이터 전달을 위한 이벤트 정의

        private INavigationService _navigationService;

        List<User> UserList = new List<User>();

        private ICommand saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                return saveCommand ?? (this.saveCommand = new DelegateCommand(SaveUser));
            }
        }

        private string _password;
        private string _passwordCheck;

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                RaisePropertyChanged(nameof(Password));
            }
        }

        public string PasswordCheck
        {
            get => _passwordCheck;
            set
            {
                _passwordCheck = value;
                RaisePropertyChanged(nameof(PasswordCheck));
            }
        }

        public AsyncRelayCommand InsertCommand { get; set; }
        public Command TestCommand { get; set; }
        public Command LoginCommand { get; set; }
        public Command NickNameCommand { get; set; }

        private CommunityViewModel _communityViewModel; // Community ViewModel 인스턴스 추가

        private void SaveUser()
        {
            User user = new User
            {
                Email = User.Email
            };

            UserList.Add(user);

            OnPropertyChanged("UserAdded");
        }

        public Login()
        {
            InsertCommand = new AsyncRelayCommand(InsertUser);
            //TestCommand = new Command(TestUser);

            _repo = new Repo(_connstring);

            _user = new User();

            User.BirthDate = new DateTime(1990, 1, 1);

            _navigationService = new NavigationService();

            //MainServerStart();

            // Community ViewModel 인스턴스 생성
            //_communityViewModel = new Community();
        }

        public User User
        {
            get { return _user; }
            set { _user = value; OnPropertyChanged(nameof(User)); }
        }

        private string _textData;

        public string TextData
        {
            get { return _textData; }
            set
            {
                _textData = value;
                OnPropertyChanged(nameof(TextData));
            }
        }

        public bool IsMaleChecked
        {
            get { return _isMaleChecked; }
            set
            {
                _isMaleChecked = value;
                OnPropertyChanged(nameof(IsMaleChecked));
                if (value)
                    User.Gender = "남성"; // 선택된 경우 User의 Gender 값을 업데이트합니다.
            }
        }

        public bool IsFemaleChecked
        {
            get { return _isFemaleChecked; }
            set
            {
                _isFemaleChecked = value;
                OnPropertyChanged(nameof(IsFemaleChecked));
                if (value)
                    User.Gender = "여성"; // 선택된 경우 User의 Gender 값을 업데이트합니다.
            }
        }

        // 회원가입
        public async Task InsertUser(object parameter)
        {
            // WPF 애플리케이션에서 현재 활성화된 메인 윈도우에서 이름이 "passwordBox"인 컨트롤을 찾기 위해 사용되는 메서드
            var passwordBox = Application.Current.MainWindow.FindName("passwordBox") as PasswordBox;
            var passwordChackBox = Application.Current.MainWindow.FindName("passwordChackBox") as PasswordBox;

            string password = passwordBox.Password;
            string passwordChack = passwordChackBox.Password;

            User.Password = password;
            User.PasswordCheck = passwordChack;

            _user.Gender = User.Gender == "남성" ? "남성" : "여성";

            var transport = UserSession.Instance.CurrentUser?.Transport;

            // 유효성 검사
            if (Validator.Validator.ValidateName(User.Name) && Validator.Validator.ValidateNickName(User.NickName)
                && Validator.Validator.ValidatePassword(User.Password, User.PasswordCheck) && Validator.Validator.ValidateBirthDate(User.BirthDate) && Validator.Validator.ValidateHeight(User.Height)
                && Validator.Validator.ValidateWeight(User.Weight) && Validator.Validator.ValidateDietGoal(User.DietGoal) && _repo.BuplicateNickName(User.NickName) && SignUp.count == 1)
            {
                var reqId = Guid.NewGuid();

                var loginReq = new { name = User.Name, gender = User.Gender, nickname = User.NickName, email = User.Email, password = User.Password, birth = User.BirthDate, height = User.Height, weight = User.Weight, diet = User.DietGoal, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
                byte[] payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(loginReq));
                await transport.SendFrameAsync((byte)MessageType.Sign_Up, payload);

                var (respType, respPayload) = await transport.ReadFrameAsync();
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var signUpRes = JsonSerializer.Deserialize<SignUpReply>(respPayload, opts);

                // 세션이 만료되면 로그인 창만 실행
                if (HandleAuthError(signUpRes?.message))
                    return;

                if (respType == MessageType.Sign_UpRes && signUpRes != null && signUpRes.ok)
                {
                    MessageBox.Show("회원가입에 성공하였습니다.");
                }
                else
                {
                    string msg = signUpRes?.message ?? "회원가입에 실패하였습니다.";
                    MessageBox.Show($"회원가입에 실패하였습니다.\n\n사유: {msg}");
                }
            }
            else
            {
                // 유효성 검사에 실패한 경우 처리
                MessageBox.Show("회원가입에 실패하였습니다.");
            }

            if(SignUp.count == 0)
            {
                MessageBox.Show("인증 번호가 일치하지 않습니다.");
            }
        }

        public class LoggedInEventArgs : EventArgs
        {
            public string UserEmail { get; }

            public LoggedInEventArgs(string userEmail)
            {
                UserEmail = userEmail;
            }
        }

        // 세션 만료
        private bool HandleAuthError(string message)
        {
            if (message == "unauthorized" || message == "expired token")
            {
                UserSession.Instance.Clear();

                // 모든 창을 닫고 로그인 창만 생성
                _navigationService.NavigateToLoginOnly();

                return true;
            }
            return false;
        }

        // INotifyPropertyChanged 구현
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

