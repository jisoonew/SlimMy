using SlimMy.Model;
using SlimMy.Repository;
using SlimMy.Response;
using SlimMy.Service;
using SlimMy.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SlimMy.ViewModel
{
    public class MyPageViewModel : BaseViewModel
    {
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

            // 사용자 키, 몸무게 출력
            var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendMyDataOnceAsync(userDateBundle), getMessage: r => r.Message, userData: userDateBundle);

            if (res?.Ok != true)
                throw new InvalidOperationException($"server not ok: {res?.Message}");

            UserSession.Instance.CurrentUser.Weight = res.UserData.Weight;
            UserSession.Instance.CurrentUser.Height = res.UserData.Height;
            UserSession.Instance.CurrentUser.BirthDate = res.UserData.BirthDate;
            UserSession.Instance.CurrentUser.Gender = res.UserData.Gender;
            UserSession.Instance.CurrentUser.DietGoal = res.UserData.DietGoal;
            UserSession.Instance.CurrentUser.NickName = res.UserData.NickName;

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

                    User userDateBundle = UserSession.Instance.CurrentUser;

                    // 몸무게 정보 여부
                    var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendTodayWeightCompletedOnceAsync(now), getMessage: r => r.Message, userData: userDateBundle);

                    if (res?.Ok != true)
                        throw new InvalidOperationException($"server not ok: {res?.Message}");

                    // BMI 계산
                    double heightValue = double.Parse(Height) / 100.0;
                    double weightValue = double.Parse(Weight);

                    double bmiValue = weightValue / (heightValue * heightValue);

                    // 몸무게 정보
                    if (res.Count > 0)
                    {
                        // 사용자 정보 수정
                        var userDataRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendUpdateMyPageUserDataOnceAsync(), getMessage: r => r.Message, userData: userDateBundle);

                        if (userDataRes?.Ok != true)
                            throw new InvalidOperationException($"server not ok: {userDataRes?.Message}");

                        // 몸무게 정보 수정
                        var updateMyPageWeightRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendUpdatetMyPageWeightOnceAsync(now, bmiValue), getMessage: r => r.Message, userData: userDateBundle);

                        if (updateMyPageWeightRes?.Ok != true)
                            throw new InvalidOperationException($"server not ok: {res?.Message}");
                    }
                    else
                    {
                        // 사용자 정보 수정
                        var userDataRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendUpdateMyPageUserDataOnceAsync(), getMessage: r => r.Message, userData: userDateBundle);

                        if (userDataRes?.Ok != true)
                            throw new InvalidOperationException($"server not ok: {userDataRes?.Message}");

                        // 몸무게 정보 저장
                        var insertMyPageWeightRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendInsertMyPageWeightOnceAsync(now, bmiValue), getMessage: r => r.Message, userData: userDateBundle);

                        if (insertMyPageWeightRes?.Ok != true)
                            throw new InvalidOperationException($"server not ok: {insertMyPageWeightRes?.Message}");
                    }

                    Password = null;
                    PasswordConfirm = null;
                    CurrentPassword = null;

                    PasswordCheck = false;
                    PasswordNoCheck = false;
                    NewPasswordCheck = false;
                    NewPasswordNoCheck = false;
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
            User userDateBundle = UserSession.Instance.CurrentUser;

            string msg = string.Format("정말로 회원 탈퇴를 진행하시겠습니까?\n탈퇴 시 작성한 게시글, 운동 기록, 채팅 내역 등이 모두 삭제되며 복구할 수 없습니다.");
            MessageBoxResult messageBoxResult = MessageBox.Show(msg, "회원 탈퇴 확인", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (messageBoxResult == MessageBoxResult.No)
            {
                return;
            }
            else
            {
                // 테이블 deleted_at 컬럼에 해당 날짜와 시간 업데이트
                var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendDeleteAccountViewOnceAsync(), getMessage: r => r.Message, userData: userDateBundle);

                if (res?.Ok != true)
                    throw new InvalidOperationException($"server not ok: {res?.Message}");

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

            var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendVerifyPasswordOnceAsync(userDateBundle), getMessage: r => r.Message, userData: userDateBundle);

            // 현재 비밀번호 확인
            PasswordCheck = res.Match;
            PasswordNoCheck = !res.Match;
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

        private static readonly SemaphoreSlim _refreshLock = new(1, 1);

        // 토큰 발급
        private async Task<bool> TryRefreshAsync(User userData)
        {
            await _refreshLock.WaitAsync();

            try
            {
                var session = UserSession.Instance;
                var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

                var authErrorResReqId = Guid.NewGuid();
                var authErrorWaitTask = session.Responses.WaitAsync(MessageType.UserRefreshTokenRes, authErrorResReqId, TimeSpan.FromSeconds(5));

                var authErrorReq = new { cmd = "UserRefreshToken", userID = userData.UserId, accessToken = UserSession.Instance.AccessToken, requestID = authErrorResReqId };
                await transport.SendFrameAsync(MessageType.UserRefreshToken, JsonSerializer.SerializeToUtf8Bytes(authErrorReq));

                var authErrorRespPayload = await authErrorWaitTask;

                var authErrorWeightRes = JsonSerializer.Deserialize<UserRefreshTokenRes>(
                    authErrorRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                Debug.WriteLine($"[확인] Refresh OK, newToken={authErrorWeightRes.NewAccessToken}, " + authErrorWeightRes.Ok);

                if (authErrorWeightRes.Ok == true)
                {
                    UserSession.Instance.AccessToken = authErrorWeightRes.NewAccessToken;

                    Debug.WriteLine($"[CLIENT] Refresh OK, newToken={UserSession.Instance.AccessToken}");

                    return true;
                }
                return false;
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        private async Task<TRes?> SendWithRefreshRetryOnceAsync<TRes>(Func<Task<TRes?>> sendOnceAsync, Func<TRes?, string?> getMessage, User userData)
        {
            var res = await sendOnceAsync();

            // 토큰 만료가 아니라면
            if (!IsAuthExpired(getMessage(res)))
            {
                return res;
            }

            // 토큰 발급
            var refreched = await TryRefreshAsync(userData);

            // 토큰 발급이 정상적으로 진행이 안되었다면
            if (!refreched)
            {
                return res;
            }

            return await sendOnceAsync();
        }

        // 사용자 키, 몸무게 출력
        private async Task<MyDataRes> SendMyDataOnceAsync(User userDateBundle)
        {
            var session = UserSession.Instance;

            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.MyDataRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "MyData", userID = userDateBundle.UserId, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.MyData, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<MyDataRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // 몸무게 정보 여부
        private async Task<TodayWeightCompletedRes> SendTodayWeightCompletedOnceAsync(DateTime now)
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.TodayWeightCompletedRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "TodayWeightCompleted", dateTime = now, userID = UserData.UserId, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.TodayWeightCompleted, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<TodayWeightCompletedRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // 사용자 정보 수정
        private async Task<UpdateMyPageUserDataRes> SendUpdateMyPageUserDataOnceAsync()
        {
            var session = UserSession.Instance;
            var myPageUserDatatransport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var updateMyPageUserDataReqId = Guid.NewGuid();

            var myPageUserDatawaitTask = session.Responses.WaitAsync(MessageType.UpdateMyPageUserDataRes, updateMyPageUserDataReqId, TimeSpan.FromSeconds(5));

            var myPageUserDatareq = new { cmd = "UpdateMyPageUserData", userID = UserData.UserId, height = double.Parse(Height), password = Password, dietGoal = DietGoal, accessToken = UserSession.Instance.AccessToken, requestID = updateMyPageUserDataReqId };
            await myPageUserDatatransport.SendFrameAsync(MessageType.UpdateMyPageUserData, JsonSerializer.SerializeToUtf8Bytes(myPageUserDatareq));

            var userDataRespPayload = await myPageUserDatawaitTask;

            return JsonSerializer.Deserialize<UpdateMyPageUserDataRes>(
                userDataRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // MyPage : 몸무게 정보 수정
        private async Task<UpdatetMyPageWeightRes> SendUpdatetMyPageWeightOnceAsync(DateTime now, double bmiValue)
        {
            var session = UserSession.Instance;
            var updatetMyPageWeighttransport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var updatetMyPageWeightReqId = Guid.NewGuid();

            var updatetMyPageWeightwaitTask = session.Responses.WaitAsync(MessageType.UpdatetMyPageWeightRes, updatetMyPageWeightReqId, TimeSpan.FromSeconds(5));

            var updatetMyPageWeightreq = new { cmd = "UpdatetMyPageWeight", userID = UserData.UserId, dateTime = now, weight = double.Parse(Weight), height = double.Parse(Height), bmi = bmiValue, accessToken = UserSession.Instance.AccessToken, requestID = updatetMyPageWeightReqId };
            await updatetMyPageWeighttransport.SendFrameAsync(MessageType.UpdatetMyPageWeight, JsonSerializer.SerializeToUtf8Bytes(updatetMyPageWeightreq));

            var updatetMyPageWeightRespPayload = await updatetMyPageWeightwaitTask;

            return JsonSerializer.Deserialize<UpdatetMyPageWeightRes>(
                updatetMyPageWeightRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // 몸무게 정보 저장
        private async Task<InsertMyPageWeightRes> SendInsertMyPageWeightOnceAsync(DateTime now, double bmiValue)
        {
            var session = UserSession.Instance;
            var insertMyPageWeightTransport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var insertMyPageWeightReqId = Guid.NewGuid();

            var insertMyPageWeightWaitTask = session.Responses.WaitAsync(MessageType.InsertMyPageWeightRes, insertMyPageWeightReqId, TimeSpan.FromSeconds(5));

            var insertMyPageWeightReq = new { cmd = "InsertMyPageWeight", userID = UserData.UserId, dateTime = now, weight = double.Parse(Weight), height = double.Parse(Height), bmi = bmiValue, accessToken = UserSession.Instance.AccessToken, requestID = insertMyPageWeightReqId };
            await insertMyPageWeightTransport.SendFrameAsync(MessageType.InsertMyPageWeight, JsonSerializer.SerializeToUtf8Bytes(insertMyPageWeightReq));

            var insertMyPageWeightPayload = await insertMyPageWeightWaitTask;

            return JsonSerializer.Deserialize<InsertMyPageWeightRes>(
                insertMyPageWeightPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        //테이블 deleted_at 컬럼에 해당 날짜와 시간 업데이트
        private async Task<DeleteAccountViewRes> SendDeleteAccountViewOnceAsync()
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.DeleteAccountViewRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "DeleteAccountView", userID = UserData.UserId, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.DeleteAccountView, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<DeleteAccountViewRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // 현재 비밀번호 일치 여부
        private async Task<GetUserDataRes> SendVerifyPasswordOnceAsync(User userDateBundle)
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.VerifyPasswordRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "VerifyPassword", userID = userDateBundle.UserId, password = CurrentPassword, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.VerifyPassword, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<GetUserDataRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // 토큰 만료
        private bool IsAuthExpired(string? message) => string.Equals(message, "expired token", StringComparison.OrdinalIgnoreCase) || string.Equals(message, "unauthorized", StringComparison.OrdinalIgnoreCase);

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

        // 닉네임 변경 화면 전환
        public async Task NickNameChangedFunction(object parameter)
        {
            await _navigationService.NavigateToNickName();
        }
    }
}
