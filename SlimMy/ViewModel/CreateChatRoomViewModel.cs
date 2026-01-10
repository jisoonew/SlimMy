using SlimMy.Model;
using SlimMy.Response;
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
    public class CreateChatRoomViewModel : BaseViewModel
    {
        private ChatRooms _chat;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";
        public event EventHandler ChatRoomCreated;

        User currentUser = UserSession.Instance.CurrentUser;

        private readonly INavigationService _navigationService;

        public static string myName = null;

        private ObservableCollection<ChatRooms> _chatRooms;

        public ObservableCollection<ChatRooms> ChatRooms
        {
            get { return _chatRooms; }
            set { _chatRooms = value; OnPropertyChanged(nameof(ChatRooms)); }
        }

        public ChatRooms Chat
        {
            get { return _chat; }
            set { _chat = value; OnPropertyChanged(nameof(Chat)); }
        }

        private string _chatName;

        public string ChatName
        {
            get => _chatName;
            set
            {
                if (_chatName != value)
                {
                    _chatName = value;
                    OnPropertyChanged(nameof(ChatName));
                }
            }
        }

        public ICommand OpenCreateChatRoomCommand { get; private set; }

        public CreateChatRoomViewModel()
        {
            _chat = new ChatRooms();
            OpenCreateChatRoomCommand = new AsyncRelayCommand(CreateChat);

            _navigationService = new NavigationService();
        }

        // 채팅방 생성
        private async Task CreateChat(object parameter)
        {
            // 생성 시간
            DateTime now = DateTime.Now;

            var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendInsertChatRoomOnceAsync(now), getMessage: r => r.Message, userData: currentUser);

            if (res?.Ok != true)
                throw new InvalidOperationException($"server not ok: {res?.Message}");

            Guid userId = UserSession.Instance.CurrentUser.UserId;

            var userChatRoomRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendInsertUserChatRoomsOnceAsync(userId, res, now), getMessage: r => r.Message, userData: currentUser);

            if (userChatRoomRes?.Ok != true)
                throw new InvalidOperationException($"server not ok: {userChatRoomRes?.Message}");

            // 이벤트 발생
            ChatRoomCreated?.Invoke(this, EventArgs.Empty);

            CloseWindow();

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

        private async Task<InsertChatRoomRes> SendInsertChatRoomOnceAsync(DateTime now)
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var insertChatRoomReqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.InsertChatRoomRes, insertChatRoomReqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "InsertChatRoom", userID = session.CurrentUser.UserId, chatRoomName = _chat.ChatRoomName, description = _chat.Description, category = _chat.Category, dateTime = now, accessToken = UserSession.Instance.AccessToken, requestID = insertChatRoomReqId };
            await transport.SendFrameAsync(MessageType.InsertChatRoom, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<InsertChatRoomRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task<InsertUserChatRoomsRes> SendInsertUserChatRoomsOnceAsync(Guid userId, InsertChatRoomRes res, DateTime now)
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            // 사용자와 채팅방 간의 관계 생성
            var userChatRoomWaitTask = session.Responses.WaitAsync(MessageType.InsertUserChatRoomsRes, reqId, TimeSpan.FromSeconds(5));

            var userChatRoomReq = new { cmd = "InsertUserChatRooms", userID = userId, chatRoomID = res.ChatRoomID, dateTime = now, isowner = 1, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.InsertUserChatRooms, JsonSerializer.SerializeToUtf8Bytes(userChatRoomReq));

            var userChatRoomRespPayload = await userChatRoomWaitTask;

            return JsonSerializer.Deserialize<InsertUserChatRoomsRes>(
                userChatRoomRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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

        private void CloseWindow()
        {
            // 현재 윈도우를 찾아서 닫기
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    window.Close();
                    break;
                }
            }
        }
    }
}
