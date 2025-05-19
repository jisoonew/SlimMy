using SlimMy.Model;
using SlimMy.Repository;
using SlimMy.Service;
using SlimMy.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SlimMy.ViewModel
{
    public class MyPageViewModel : BaseViewModel
    {
        private UserRepository _repo;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";

        // 화면 전환
        private INavigationService _navigationService;

        // 사용자 정보
        private User _userData;
        public User UserData
        {
            get { return _userData; }
            set { _userData = value; OnPropertyChanged(nameof(UserData)); }
        }

        // 새 비밀번호
        private string _password;
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
            }
        }

        // 새 비밀번호 확인
        private string _passwordConfirm;
        public string PasswordConfirm
        {
            get => _passwordConfirm;
            set
            {
                _passwordConfirm = value;
                OnPropertyChanged(nameof(PasswordConfirm));

                if (!string.IsNullOrWhiteSpace(Password) && !string.IsNullOrWhiteSpace(PasswordConfirm))
                {
                    NewPasswordCheckPrint();
                }
            }
        }

        // 현재 비밀번호
        private string _currentPassword;
        public string CurrentPassword
        {
            get => _currentPassword;
            set
            {
                _currentPassword = value; OnPropertyChanged(nameof(CurrentPassword));
                if (!string.IsNullOrWhiteSpace(CurrentPassword))
                {
                    _ = PasswordCheckPrint();
                }
            }
        }

        // 현재 비밀번호 성공 메시지
        private bool _passwordCheck;
        public bool PasswordCheck
        {
            get { return _passwordCheck; }
            set { _passwordCheck = value; OnPropertyChanged(nameof(PasswordCheck)); }
        }

        // 현재 비밀번호 실패 메시지
        private bool _passwordNoCheck;
        public bool PasswordNoCheck
        {
            get { return _passwordNoCheck; }
            set { _passwordNoCheck = value; OnPropertyChanged(nameof(PasswordNoCheck)); }
        }

        // 새 비밀번호 성공 메시지
        private bool _newPasswordCheck;
        public bool NewPasswordCheck
        {
            get { return _newPasswordCheck; }
            set { _newPasswordCheck = value; OnPropertyChanged(nameof(NewPasswordCheck)); }
        }

        // 새 비밀번호 실패 메시지
        private bool _newPasswordNoCheck;
        public bool NewPasswordNoCheck
        {
            get { return _newPasswordNoCheck; }
            set { _newPasswordNoCheck = value; OnPropertyChanged(nameof(NewPasswordNoCheck)); }
        }


        // 닉네임
        private string _nickName;
        public string NickName
        {
            get { return _nickName; }
            set { _nickName = value; OnPropertyChanged(nameof(NickName)); }
        }

        public ObservableCollection<string> DietGoalList { get; set; }
        public ObservableCollection<string> GenderList { get; set; }

        public ICommand UserDataSaveCommand { get; set; }
        public ICommand NickNameChangedCommand { get; set; }

        public MyPageViewModel()
        {
        }

        private async Task Initialize()
        {
            _repo = new UserRepository(_connstring); // Repo 초기화

            _navigationService = new NavigationService();

            DietGoalList = new ObservableCollection<string>
        {
            "체중 감량",
            "체중 유지",
            "체중 증가"
        };

            GenderList = new ObservableCollection<string>
        {
            "남성",
            "여성"
        };

            // 사용자 데이터 출력
            await UserDataPrint();

            UserDataSaveCommand = new AsyncRelayCommand(UserDataSave);

            NickNameChangedCommand = new AsyncRelayCommand(NickNameChangedFunction);
        }

        public static async Task<MyPageViewModel> CreateAsync()
        {
            try
            {
                var vm = new MyPageViewModel();
                await vm.Initialize();
                return vm;
            }
            catch (Exception ex)
            {
                MessageBox.Show("MyPageViewModel 생성 실패: " + ex.Message);
                return null;
            }
        }

        // 사용자 데이터 출력
        public async Task UserDataPrint()
        {
            User userDateBundle = UserSession.Instance.CurrentUser;

            User userData = await _repo.GetUserData(userDateBundle.UserId);

            UserSession.Instance.CurrentUser.Weight = userData.Weight;
            UserSession.Instance.CurrentUser.Height = userData.Height;
            UserSession.Instance.CurrentUser.BirthDate = userData.BirthDate;
            UserSession.Instance.CurrentUser.Gender = userData.Gender;
            UserSession.Instance.CurrentUser.DietGoal = userData.DietGoal;

            UserData = UserSession.Instance.CurrentUser;

            NickName = UserData.NickName;
        }

        // 사용자 정보 수정
        public async Task UserDataSave(object parameter)
        {
            User userDateBundle = UserSession.Instance.CurrentUser;
            User userData = await _repo.GetUserData(userDateBundle.UserId);

            // 현재 비밀번호 확인
            if (userData.Password.Equals(CurrentPassword))
            {
                PasswordCheck = true;
                PasswordNoCheck = false;
            }
            else
            {
                PasswordCheck = false;
                PasswordNoCheck = true;
            }
        }

        // 현재 비밀번호 일치 여부
        public async Task PasswordCheckPrint()
        {
            User userDateBundle = UserSession.Instance.CurrentUser;
            User userData = await _repo.GetUserData(userDateBundle.UserId);

            // 현재 비밀번호 확인
            if (userData.Password.Equals(CurrentPassword))
            {
                PasswordCheck = true;
                PasswordNoCheck = false;
            }
            else
            {
                PasswordCheck = false;
                PasswordNoCheck = true;
            }
        }

        // 새 비밀번호 일치 여부
        public void NewPasswordCheckPrint()
        {
            // 새 비밀번호 확인
            if (Password.Equals(PasswordConfirm))
            {
                NewPasswordCheck = true;
                NewPasswordNoCheck = false;
            }
            else
            {
                NewPasswordCheck = false;
                NewPasswordNoCheck = true;
            }
        }

        public async Task NickNameChangedFunction(object parameter)
        {
            _navigationService.NavigateToNickName();

            //NicknameChangeViewModel nicknameChangeViewModel = new NicknameChangeViewModel();
            //await nicknameChangeViewModel.NickNameCheckPrint(NickName);
        }
    }
}
