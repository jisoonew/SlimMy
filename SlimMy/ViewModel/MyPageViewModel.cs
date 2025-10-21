using SlimMy.Model;
using SlimMy.Repository;
using SlimMy.Response;
using SlimMy.Service;
using SlimMy.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SlimMy.ViewModel
{
    public class MyPageViewModel : BaseViewModel
    {
        private UserRepository _repo;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";

        private WeightHistoryRepository _weightRepo;

        private TcpClient client = null;

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

        // 키
        private string _height;
        public string Height
        {
            get { return _height; }
            set { _height = value; OnPropertyChanged(nameof(Height)); }
        }

        // 몸무게
        private string _weight;
        public string Weight
        {
            get { return _weight; }
            set { _weight = value; OnPropertyChanged(nameof(Weight)); }
        }

        // 목표
        private string _dietGoal;
        public string DietGoal
        {
            get { return _dietGoal; }
            set { _dietGoal = value; OnPropertyChanged(nameof(DietGoal)); }
        }

        public ObservableCollection<string> DietGoalList { get; set; }
        public ObservableCollection<string> GenderList { get; set; }

        public ICommand UserDataSaveCommand { get; set; }
        public ICommand NickNameChangedCommand { get; set; }
        public ICommand DeleteAccountViewCommand { get; set; }

        private async Task Initialize()
        {
            _repo = new UserRepository(_connstring); // Repo 초기화

            _weightRepo = new WeightHistoryRepository(_connstring);

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

            // 사용자 정보 저장
            UserDataSaveCommand = new AsyncRelayCommand(UserDataSave);

            // 닉네임 변경
            NickNameChangedCommand = new AsyncRelayCommand(NickNameChangedFunction);

            // 사용자 탈퇴
            DeleteAccountViewCommand = new AsyncRelayCommand(DeleteAccountViewFunction);
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

            var session = UserSession.Instance;

            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var waitTask = session.Responses.WaitMyDataAsync(TimeSpan.FromSeconds(5));

            var req = new { cmd = "MyData", userID = userDateBundle.UserId };
            await transport.SendFrameAsync((byte)MessageType.MyData, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            var res = JsonSerializer.Deserialize<MyDataRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (res?.ok != true)
                throw new InvalidOperationException($"server not ok: {res?.message}");

            UserSession.Instance.CurrentUser.Weight = res.userData.Weight;
            UserSession.Instance.CurrentUser.Height = res.userData.Height;
            UserSession.Instance.CurrentUser.BirthDate = res.userData.BirthDate;
            UserSession.Instance.CurrentUser.Gender = res.userData.Gender;
            UserSession.Instance.CurrentUser.DietGoal = res.userData.DietGoal;
            UserSession.Instance.CurrentUser.NickName = res.userData.NickName;

            UserData = UserSession.Instance.CurrentUser;
            OnPropertyChanged(nameof(UserData));

            NickName = UserData.NickName;
            OnPropertyChanged(nameof(NickName));

            Height = UserData.Height.ToString();
            Weight = UserData.Weight.ToString();
            DietGoal = UserData.DietGoal.ToString();
        }

        // 사용자 정보 수정
        public async Task UserDataSave(object parameter)
        {
            if (PasswordNoCheck)
            {
                MessageBox.Show("현재 비밀번호를 확인해주세요.");
                return;
            }

            if (NewPasswordNoCheck)
            {
                MessageBox.Show("새 비밀번호가 일치하지 않습니다.");
                return;
            }

            if (Validator.Validator.ValidateHeight(double.Parse(Height)) && Validator.Validator.ValidateWeight(double.Parse(Weight)) && PasswordCheck && NewPasswordCheck)
            {
                string msg = string.Format("해당 정보를 저장하시겠습니까?");
                MessageBoxResult messageBoxResult = MessageBox.Show(msg, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (messageBoxResult == MessageBoxResult.No)
                {
                    return;
                }
                else
                {
                    DateTime now = DateTime.Now;
                    var session = UserSession.Instance;
                    var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

                    var waitTask = session.Responses.TodayWeightCompletedAsync(TimeSpan.FromSeconds(5));

                    var req = new { cmd = "TodayWeightCompleted", dateTime = now, userID = UserData.UserId };
                    await transport.SendFrameAsync((byte)MessageType.TodayWeightCompleted, JsonSerializer.SerializeToUtf8Bytes(req));

                    var respPayload = await waitTask;

                    var res = JsonSerializer.Deserialize<TodayWeightCompletedRes>(
                        respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (res?.ok != true)
                        throw new InvalidOperationException($"server not ok: {res?.message}");

                    // BMI 계산
                    double heightValue = double.Parse(Height) / 100.0;
                    double weightValue = double.Parse(Weight);

                    double bmiValue = weightValue / (heightValue * heightValue);

                    // 몸무게 정보
                    if (res.count > 0)
                    {
                        // 사용자 정보
                        var myPageUserDatatransport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

                        var myPageUserDatawaitTask = session.Responses.UpdateMyPageUserDataAsync(TimeSpan.FromSeconds(5));

                        var myPageUserDatareq = new { cmd = "UpdateMyPageUserData", userID = UserData.UserId, height = double.Parse(Height), password = Password, dietGoal = DietGoal };
                        await myPageUserDatatransport.SendFrameAsync((byte)MessageType.UpdateMyPageUserData, JsonSerializer.SerializeToUtf8Bytes(myPageUserDatareq));

                        var userDataRespPayload = await myPageUserDatawaitTask;

                        var userDataRes = JsonSerializer.Deserialize<UpdateMyPageUserDataRes>(
                            userDataRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (userDataRes?.ok != true)
                            throw new InvalidOperationException($"server not ok: {userDataRes?.message}");


                        var updatetMyPageWeighttransport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

                        var updatetMyPageWeightwaitTask = session.Responses.UpdatetMyPageWeightAsync(TimeSpan.FromSeconds(5));

                        var updatetMyPageWeightreq = new { cmd = "UpdatetMyPageWeight", userID = UserData.UserId, dateTime = now, weight = double.Parse(Weight), height = double.Parse(Height), bmi = bmiValue };
                        await updatetMyPageWeighttransport.SendFrameAsync((byte)MessageType.UpdatetMyPageWeight, JsonSerializer.SerializeToUtf8Bytes(updatetMyPageWeightreq));

                        var updatetMyPageWeightRespPayload = await updatetMyPageWeightwaitTask;

                        var updatetMyPageWeightRes = JsonSerializer.Deserialize<UpdatetMyPageWeightRes>(
                            updatetMyPageWeightRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (updatetMyPageWeightRes?.ok != true)
                            throw new InvalidOperationException($"server not ok: {res?.message}");
                    }
                    else
                    {
                        // 사용자 정보
                        var myPageUserDatatransport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

                        var myPageUserDatawaitTask = session.Responses.UpdateMyPageUserDataAsync(TimeSpan.FromSeconds(5));

                        var myPageUserDatareq = new { cmd = "UpdateMyPageUserData", userID = UserData.UserId, height = double.Parse(Height), password = Password, dietGoal = DietGoal };
                        await myPageUserDatatransport.SendFrameAsync((byte)MessageType.UpdateMyPageUserData, JsonSerializer.SerializeToUtf8Bytes(myPageUserDatareq));

                        var userDataRespPayload = await myPageUserDatawaitTask;

                        var userDataRes = JsonSerializer.Deserialize<UpdateMyPageUserDataRes>(
                            userDataRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (userDataRes?.ok != true)
                            throw new InvalidOperationException($"server not ok: {userDataRes?.message}");

                        // 몸무게 정보 저장
                        var insertMyPageWeightTransport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

                        var insertMyPageWeightWaitTask = session.Responses.InsertMyPageWeightAsync(TimeSpan.FromSeconds(5));

                        var insertMyPageWeightReq = new { cmd = "InsertMyPageWeight", userID = UserData.UserId, dateTime = now, weight = double.Parse(Weight), height = double.Parse(Height), bmi = bmiValue };
                        await insertMyPageWeightTransport.SendFrameAsync((byte)MessageType.InsertMyPageWeight, JsonSerializer.SerializeToUtf8Bytes(insertMyPageWeightReq));

                        var insertMyPageWeightPayload = await insertMyPageWeightWaitTask;

                        var insertMyPageWeightRes = JsonSerializer.Deserialize<InsertMyPageWeightRes>(
                            insertMyPageWeightPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (insertMyPageWeightRes?.ok != true)
                            throw new InvalidOperationException($"server not ok: {insertMyPageWeightRes?.message}");
                    }
                }
            }
            else if (PasswordCheck == false || NewPasswordCheck == false)
            {
                MessageBox.Show("비밀번호를 확인해주세요.");
            }
            else
            {
                MessageBox.Show("몸무게와 키를 확인해주세요.");
            }
        }

        // 사용자 탈퇴
        public async Task DeleteAccountViewFunction(object parameter)
        {
            string msg = string.Format("정말로 회원 탈퇴를 진행하시겠습니까?\n탈퇴 시 작성한 게시글, 운동 기록, 채팅 내역 등이 모두 삭제되며 복구할 수 없습니다.");
            MessageBoxResult messageBoxResult = MessageBox.Show(msg, "회원 탈퇴 확인", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (messageBoxResult == MessageBoxResult.No)
            {
                return;
            }
            else
            {
                //테이블 deleted_at 컬럼에 해당 날짜와 시간 업데이트
                var session = UserSession.Instance;
                var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

                var waitTask = session.Responses.DeleteAccountViewAsync(TimeSpan.FromSeconds(5));

                var req = new { cmd = "DeleteAccountView", userID = UserData.UserId };
                await transport.SendFrameAsync((byte)MessageType.DeleteAccountView, JsonSerializer.SerializeToUtf8Bytes(req));

                var respPayload = await waitTask;

                var res = JsonSerializer.Deserialize<DeleteAccountViewRes>(
                    respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (res?.ok != true)
                    throw new InvalidOperationException($"server not ok: {res?.message}");

                //서버 연결 해제
                if (UserSession.Instance.CurrentUser?.Client != null)
                {
                    UserSession.Instance.CurrentUser.Client.Close();
                    UserSession.Instance.CurrentUser.Client = null;
                }

                //세션 제거
                UserSession.Instance.CurrentUser = null;

                await _navigationService.NavigateToCloseAndLoginAsync("MainHome");
            }
        }

        // 현재 비밀번호 일치 여부
        public async Task PasswordCheckPrint()
        {
            User userDateBundle = UserSession.Instance.CurrentUser;

            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var waitTask = session.Responses.GetUserDataAsync(TimeSpan.FromSeconds(5));

            var req = new { cmd = "GetUserData", userID = userDateBundle.UserId, password = CurrentPassword };
            await transport.SendFrameAsync((byte)MessageType.VerifyPassword, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            var res = JsonSerializer.Deserialize<GetUserDataRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // 현재 비밀번호 확인
            PasswordCheck = res.match;
            PasswordNoCheck = !res.match;
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

        // 닉네임 변경 화면 전환
        public async Task NickNameChangedFunction(object parameter)
        {
            await _navigationService.NavigateToNickName();
        }
    }
}
