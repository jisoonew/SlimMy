using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using MVVM2.ViewModel;
using SlimMy.Model;
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

        public Command InsertCommand { get; set; }
        public Command TestCommand { get; set; }
        public Command LoginCommand { get; set; }
        public Command NickNameCommand { get; set; }

        private Community _communityViewModel; // Community ViewModel 인스턴스 추가

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
            InsertCommand = new Command(InsertUser);
            //TestCommand = new Command(TestUser);

            _repo = new Repo(_connstring);

            _user = new User();

            User.BirthDate = new DateTime(1990, 1, 1);

            //MainServerStart();

            // Community ViewModel 인스턴스 생성
            _communityViewModel = new Community();
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

        public void TestUser(object parameter)
        {
            // WPF 애플리케이션에서 현재 활성화된 메인 윈도우에서 이름이 "passwordBox"인 컨트롤을 찾기 위해 사용되는 메서드
            var passwordBox = Application.Current.MainWindow.FindName("passwordBox") as PasswordBox;

            string password = passwordBox.Password;

            User.Password = password;

            MessageBox.Show("User.Password : " + User.Password);
        }

        // 회원가입
        public void InsertUser(object parameter)
        {
            // WPF 애플리케이션에서 현재 활성화된 메인 윈도우에서 이름이 "passwordBox"인 컨트롤을 찾기 위해 사용되는 메서드
            var passwordBox = Application.Current.MainWindow.FindName("passwordBox") as PasswordBox;
            var passwordChackBox = Application.Current.MainWindow.FindName("passwordChackBox") as PasswordBox;

            string password = passwordBox.Password;
            string passwordChack = passwordChackBox.Password;

            User.Password = password;
            User.PasswordCheck = passwordChack;

            MessageBox.Show("여기는 User.Password : " + User.Password + "\n User.PasswordChack : " + User.PasswordCheck);


            _user.Gender = User.Gender == "남성" ? "남성" : "여성";

            // 유효성 검사
            if (Validator.Validator.ValidateName(User.Name) && Validator.Validator.ValidateNickName(User.NickName)
                && Validator.Validator.ValidatePassword(User.Password, User.PasswordCheck) && Validator.Validator.ValidateBirthDate(User.BirthDate) && Validator.Validator.ValidateHeight(User.Height)
                && Validator.Validator.ValidateWeight(User.Weight) && Validator.Validator.ValidateDietGoal(User.DietGoal) && _repo.BuplicateNickName(User.NickName) && SignUp.count == 1)
            {
                _repo.InsertUser(User.Name, User.Gender, User.NickName, User.Email, User.Password, User.BirthDate, User.Height, User.Weight, User.DietGoal);
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

        // INotifyPropertyChanged 구현
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

