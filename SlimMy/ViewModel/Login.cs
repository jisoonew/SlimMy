using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using MVVM2.ViewModel;
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

    public class Login : ViewModelBase
    {
        private User _user;
        private Repo _repo;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";

        // 여성 혹은 남성중 어떤 선택을 할 것인지
        private bool _isMaleChecked;
        private bool _isFemaleChecked;

        // 이벤트 정의: 로그인 성공 시 발생하는 이벤트
        public event EventHandler<User> DataPassed; // 데이터 전달을 위한 이벤트 정의

        private SignUp _signUp;

        List<User> UserList = new List<User>();

        private ICommand saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                return saveCommand ?? (this.saveCommand = new DelegateCommand(SaveUser));
            }
        }

        public Command InsertCommand { get; set; }
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
            LoginCommand = new Command(LoginSuccess);

            _repo = new Repo(_connstring);

            _user = new User();

            User.BirthDate = new DateTime(1990, 1, 1);

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

        // 회원가입
        public void InsertUser(object parameter)
        {
            _user.Gender = User.Gender == "남성" ? "남성" : "여성";

            // WPF 애플리케이션에서 현재 활성화된 메인 윈도우에서 이름이 "passwordBox"인 컨트롤을 찾기 위해 사용되는 메서드
            var passwordBox = Application.Current.MainWindow.FindName("passwordBox") as PasswordBox;
            var passwordCheckBox = Application.Current.MainWindow.FindName("passwordCheckBox") as PasswordBox;

            string password = passwordBox.Password;
            string passwordCheck = passwordCheckBox.Password;

            User.Password = password;
            User.PasswordCheck = passwordCheck;

            _signUp = new SignUp();

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

        // 로그인
        public void LoginSuccess(object parameter)
        {
            var passwordBox = Application.Current.MainWindow.FindName("passwordBox") as PasswordBox;
            var ipText = Application.Current.MainWindow.FindName("IpTextBox") as TextBox;
            string password = passwordBox.Password;
            User.Password = password;

            bool isSuccess = _repo.LoginSuccess(User.Email, password);

            View.Login login = new View.Login();

            if (isSuccess)
            {
                // 로그인 이후 사용자의 닉네임 가져오기
                string loggedInNickName = _repo.NickName(User.Email);
                User.NickName = loggedInNickName;

                MessageBox.Show("아이피 값 출력 : " + ipText.Text);

                // 싱글톤에 저장
                UserSession.Instance.CurrentUser = new User
                {
                    Email = User.Email,
                    NickName = User.NickName,
                    IpNum = User.IpNum
                };

                // MainPage 실행
                var mainPage = new View.MainPage();
                mainPage.DataContext = this;

                // 새로운 창을 보여줍니다.
                mainPage.Show();

                // 현재 창을 닫습니다.
                Application.Current.MainWindow.Close();  // 로그인 창 닫기

            }
            else
            {
                MessageBox.Show("로그인에 실패했습니다. 이메일과 비밀번호를 확인해 주세요.");
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

