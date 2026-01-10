using SlimMy.Model;
using SlimMy.Repository;
using SlimMy.Response;
using SlimMy.Service;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SlimMy.ViewModel
{
    public class NicknameChangeViewModel : BaseViewModel
    {
        // 화면 전환
        private INavigationService _navigationService;

        private Guid _userID;
        public Guid UserID
        {
            get { return _userID; }
            set { _userID = value; OnPropertyChanged(nameof(UserID)); }
        }

        private string _newNickname;
        public string NewNickname
        {
            get => _newNickname;
            set
            {
                _newNickname = value;
                OnPropertyChanged(nameof(NewNickname));
                NickNameCheck = false;
                NickNameNoCheck = false;
            }
        }

        // 닉네임 성공 메시지
        private bool _nickNameCheck;
        public bool NickNameCheck
        {
            get { return _nickNameCheck; }
            set { _nickNameCheck = value; OnPropertyChanged(nameof(NickNameCheck)); }
        }

        // 닉네임 실패 메시지
        private bool _nickNameNoCheck;
        public bool NickNameNoCheck
        {
            get { return _nickNameNoCheck; }
            set { _nickNameNoCheck = value; OnPropertyChanged(nameof(NickNameNoCheck)); }
        }

        // 닉네임 중복 확인
        public ICommand CheckNicknameCommand { get; set; }

        // 닉네임 저장
        public ICommand SaveCommand { get; set; }

        public NicknameChangeViewModel(string initialNickname)
        {
            NewNickname = initialNickname;
        }

        public NicknameChangeViewModel() { }

        private async Task Initialize()
        {
            _navigationService = new NavigationService();

            CheckNicknameCommand = new AsyncRelayCommand(NickNameCheckPrint);

            SaveCommand = new AsyncRelayCommand(NickNameSave);
        }

        public static async Task<NicknameChangeViewModel> CreateAsync()
        {
            try
            {
                var vm = new NicknameChangeViewModel();
                await vm.Initialize();
                return vm;
            }
            catch (Exception ex)
            {
                MessageBox.Show("NicknameChangeViewModel 생성 실패: " + ex.Message);
                return null;
            }
        }

        // 닉네임 여부
        public async Task NickNameCheckPrint(object parameter)
        {
            User currentUser = UserSession.Instance.CurrentUser;

            // 닉네임 출력
            var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendNickNameCheckPrintOnceAsync(), getMessage: r => r.Message, userData: currentUser);

            if (res?.Ok != true)
                throw new InvalidOperationException($"server not ok: {res?.Message}");

            bool isDuplicate = res.NickNames.Any(name => name.Equals(NewNickname));

            // 닉네임 중복
            if (isDuplicate)
            {
                NickNameCheck = false;
                NickNameNoCheck = true;
            }
            else
            {
                if (Validator.Validator.ValidateNickName(NewNickname))
                {
                    NickNameCheck = true;
                    NickNameNoCheck = false;
                }
            }
        }

        // 닉네임 지정
        public async Task NickNameSave(object parameter)
        {
            User userCurrentData = UserSession.Instance.CurrentUser;

            string msg = string.Format("'{0}'로 닉네임을 변경하시겠습니까?", NewNickname);
            MessageBoxResult messageBoxResult = MessageBox.Show(msg, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (messageBoxResult == MessageBoxResult.No)
            {
                return;
            }
            else
            {
                // 닉네임 중복 체크를 안했다면
                if(!NickNameNoCheck && !NickNameCheck)
                {
                    MessageBox.Show("닉네임 중복 여부를 확인해주세요.");
                }

                // 닉네임 중복
                if (NickNameNoCheck)
                {
                    return;
                }

                // 닉네임 지정
                if (NickNameCheck)
                {
                    // 닉네임 수정
                    var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendNickNameSaveOnceAsync(userCurrentData), getMessage: r => r.Message, userData: userCurrentData);

                    if (res?.Ok != true)
                        throw new InvalidOperationException($"server not ok: {res?.Message}");

                    // 닉네임 화면 닫기
                    await _navigationService.NavigateToNickNameClose();
                }
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

        // 닉네임 출력
        private async Task<NickNameCheckPrintRes> SendNickNameCheckPrintOnceAsync()
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.NickNameCheckPrintRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "NickNameCheckPrint", userID = session.CurrentUser.UserId, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.NickNameCheckPrint, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<NickNameCheckPrintRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // 닉네임 수정
        private async Task<NickNameSaveRes> SendNickNameSaveOnceAsync(User userData)
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.NickNameSaveRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "NickNameSave", userID = userData.UserId, userNickName = NewNickname, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.NickNameSave, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<NickNameSaveRes>(
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
    }
}
