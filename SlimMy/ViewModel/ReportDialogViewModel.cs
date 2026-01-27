using GalaSoft.MvvmLight.Command;
using SlimMy.Model;
using SlimMy.Response;
using SlimMy.Service;
using SlimMy.Singleton;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public class ReportDialogViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;

        private ObservableCollection<ChatMessage> _selectedMessages;
        public ObservableCollection<ChatMessage> SelectedMessages
        {
            get { return _selectedMessages; }
            set { _selectedMessages = value; OnPropertyChanged(nameof(SelectedMessages)); }
        }

        public class ReportReasonOption
        {
            public string Code { get; set; } = "";
            public string Label { get; set; } = "";
        }

        public ObservableCollection<ReportReasonOption> ReasonOptions { get; } =
            new ObservableCollection<ReportReasonOption>
            {
        new() { Code = "ABUSE",   Label = "욕설/비하" },
        new() { Code = "SEXUAL",  Label = "성적/음란" },
        new() { Code = "SPAM",    Label = "도배/스팸" },
        new() { Code = "SCAM",    Label = "사기/유도" },
        new() { Code = "ILLEGAL", Label = "불법/위험" },
        new() { Code = "PII",     Label = "개인정보" },
        new() { Code = "OTHER",   Label = "기타(상세 내용에 작성)" },
            };

        private ReportReasonOption? _selectedReason;
        public ReportReasonOption? SelectedReason
        {
            get => _selectedReason;
            set
            {
                _selectedReason = value;
                OnPropertyChanged(nameof(SelectedReason));
                OnPropertyChanged(nameof(IsOtherReasonSelected));
            }
        }

        // 채팅방 명
        private string _chatRoomTitle;
        public string ChatRoomTitle
        {
            get { return _chatRoomTitle; }
            set { _chatRoomTitle = value; OnPropertyChanged(nameof(ChatRoomTitle)); }
        }

        // "선택된 메시지가 없습니다. 채팅 화면에서 메시지를 선택해 주세요."
        public Visibility SelectedEmptyVisibility
            => (SelectedMessages == null || SelectedMessages.Count == 0)
                ? Visibility.Visible
                : Visibility.Collapsed;

        // 메시지 미리보기
        public Visibility SelectedListVisibility
            => (SelectedMessages != null && SelectedMessages.Count > 0)
                ? Visibility.Visible
                : Visibility.Collapsed;

        // 기타 사유
        public Visibility IsOtherReasonSelected
            => (SelectedReason.Code == "OTHER")
                ? Visibility.Visible
                : Visibility.Collapsed;

        // 신고 대상
        private string _targetTypeLabel;
        public string TargetTypeLabel
        {
            get { return _targetTypeLabel; }
            set { _targetTypeLabel = value; OnPropertyChanged(nameof(TargetTypeLabel)); }
        }

        // 채팅방 아이디
        private string _chatRoomId;
        public string ChatRoomId
        {
            get { return _chatRoomId; }
            set { _chatRoomId = value; OnPropertyChanged(nameof(ChatRoomId)); }
        }

        // 신고 닉네임
        private string _targetUserNickName;
        public string TargetUserNickName
        {
            get { return _targetUserNickName; }
            set { _targetUserNickName = value; OnPropertyChanged(nameof(TargetUserNickName)); }
        }

        // 상세 내용
        private string _detailText;
        public string DetailText
        {
            get { return _detailText; }
            set { _detailText = value; OnPropertyChanged(nameof(DetailText)); }
        }

        // 신고
        private ReportTarget _reportTarget;
        public ReportTarget ReportTarget
        {
            get { return _reportTarget; }
            set { _reportTarget = value; OnPropertyChanged(nameof(ReportTarget)); }
        }

        // 메시지 삭제
        public ICommand RemoveSelectedMessageCommand { get; }

        // 신고 제출
        public ICommand SubmitCommand { get; set; }

        public event Action? RequestClose;

        private void CloseMe() => RequestClose?.Invoke();

        public ReportDialogViewModel()
        {
            RemoveSelectedMessageCommand = new RelayCommand<ChatMessage>(RemoveSelectedMessage);
        }

        public static async Task<ReportDialogViewModel> CreateAsync(ReportTarget target)
        {
            var instance = new ReportDialogViewModel();
            await instance.Initialize(target);
            return instance;
        }

        private async Task Initialize(ReportTarget target)
        {
            SelectedMessages = new ObservableCollection<ChatMessage>();

            // 컬렉션에 항목이 추가/삭제될 때마다 Visibility 알림
            SelectedMessages.CollectionChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(SelectedEmptyVisibility));
                OnPropertyChanged(nameof(SelectedListVisibility));
            };

            // 신고 대상
            ApplyTarget(target);

            // 신고 채팅방 데이터 출력
            await ChatRoomData();

            // 신고 제출
            SubmitCommand = new AsyncRelayCommand(SubmitReportAsync);
        }

        // 신고 대상
        public void ApplyTarget(ReportTarget target)
        {
            TargetTypeLabel = target.TargetType == ReportTargetType.User ? "사용자" : "채팅방";
            ChatRoomTitle = target.ChatRoomTitle ?? "";
            ChatRoomId = target.ChatRoomId == Guid.Empty ? "" : target.ChatRoomId.ToString();
            TargetUserNickName = target.TargetUserNickName;
            ReportTarget = target;
        }

        // 신고 채팅방 데이터 출력
        private async Task ChatRoomData()
        {
            ChatRooms currentChattingData = ChattingSession.Instance.CurrentChattingData;
            User currentUser = UserSession.Instance.CurrentUser;

            // 특정 채팅방 데이터 출력
            var selectChatRoomRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendGetChatRoomDetailOnceAsync(currentChattingData), getMessage: r => r.Message, userData: currentUser);

            if (selectChatRoomRes?.Ok != true)
                throw new InvalidOperationException($"server not ok: {selectChatRoomRes?.Message}");

            // 채팅방 명
            ChatRoomTitle = selectChatRoomRes.ChatRoomData.ChatRoomName;
        }

        // 신고 메시지 삭제
        private void RemoveSelectedMessage(ChatMessage? msg)
        {
            if (msg == null) return;

            SelectedMessages.Remove(msg);
        }

        // 신고 메시지
        public async Task ReportMessage(ChatMessage? msg)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SelectedMessages.Add(new ChatMessage
                {
                    MessageID = msg.MessageID,
                    MessageContent = msg.MessageContent,
                    Timestamp = msg.Timestamp
                });
            });
        }

        // 신고 저장
        public async Task SubmitReportAsync(object parameter)
        {
            User currentUser = UserSession.Instance.CurrentUser;

            // 채팅방 신고
            if(ReportTarget.TargetType == 0)
            {
                // 신고 정보 저장
                var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendSubmitReportOnceAsync(ReportTarget, SelectedReason.Code, DetailText), getMessage: r => r.Message, userData: currentUser);

                if (res?.Ok != true)
                    throw new InvalidOperationException($"server not ok: {res?.Message}");

                // 신고 사유 메시지 저장
                foreach(var messageData in SelectedMessages)
                {
                    var messageRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendSubmitReportMessageOnceAsync(messageData, res.ReportID), getMessage: r => r.Message, userData: currentUser);

                    if (messageRes?.Ok != true)
                        throw new InvalidOperationException($"server not ok: {messageRes?.Message}");
                }

                MessageBox.Show("신고가 완료되었습니다.");

                // 신고창 닫기
                RequestClose?.Invoke();
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

        // 특정 채팅방 데이터
        private async Task<GetChatRoomDetailRes?> SendGetChatRoomDetailOnceAsync(ChatRooms currentChattingData)
        {
            var session = UserSession.Instance;

            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.GetChatRoomDetailRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { Cmd = "GetChatRoomDetail", userID = session.CurrentUser.UserId, chatRoomID = currentChattingData.ChatRoomId, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.GetChatRoomDetail, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<GetChatRoomDetailRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // 신고 제출
        private async Task<SubmitReportRes> SendSubmitReportOnceAsync(ReportTarget target, string code, string detailText)
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.SubmitReportRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "SubmitReport", userID = session.CurrentUser.UserId, reportTarget = target, reasonCode = code, detailText = detailText, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.SubmitReport, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<SubmitReportRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // 신고 사유 메시지
        private async Task<SubmitReportMessageRes> SendSubmitReportMessageOnceAsync(ChatMessage selectedMessages, Guid reportID)
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.SubmitReportMessageRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "SubmitReportMessage", userID = session.CurrentUser.UserId, selectedMessage = selectedMessages, reportID = reportID, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.SubmitReportMessage, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<SubmitReportMessageRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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

        // 토큰 만료
        private bool IsAuthExpired(string? message) => string.Equals(message, "expired token", StringComparison.OrdinalIgnoreCase) || string.Equals(message, "unauthorized", StringComparison.OrdinalIgnoreCase);
    }
}
