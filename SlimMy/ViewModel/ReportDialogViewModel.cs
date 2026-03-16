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
            => IsReportHistoryDetailMode
                ? Visibility.Collapsed
                : (SelectedMessages == null || SelectedMessages.Count == 0)
                    ? Visibility.Visible
                    : Visibility.Collapsed;

        // 신고 미리보기
        public Visibility SelectedListVisibility
            => IsReportHistoryDetailMode
                ? Visibility.Visible
                : (SelectedMessages != null && SelectedMessages.Count > 0)
                    ? Visibility.Visible
                    : Visibility.Collapsed;

        // 기타 사유
        public Visibility IsOtherReasonSelected
            => (SelectedReason?.Code == "OTHER")
                ? Visibility.Visible
                : Visibility.Collapsed;

        private Visibility _submitButtonVisibility;
        public Visibility SubmitButtonVisibility
        {
            get { return _submitButtonVisibility; }
            set { _submitButtonVisibility = value; OnPropertyChanged(nameof(SubmitButtonVisibility)); }
        }

        private Visibility _removeButtonVisibility;
        public Visibility RemoveButtonVisibility
        {
            get { return _removeButtonVisibility; }
            set { _removeButtonVisibility = value; OnPropertyChanged(nameof(RemoveButtonVisibility)); }
        }

        // 신고 미리보기 모드
        private bool _isReportHistoryDetailMode;
        public bool IsReportHistoryDetailMode
        {
            get => _isReportHistoryDetailMode;
            set
            {
                if (_isReportHistoryDetailMode == value) return;
                _isReportHistoryDetailMode = value;
                OnPropertyChanged(nameof(IsReportHistoryDetailMode));
                OnPropertyChanged(nameof(SelectedEmptyVisibility));
                OnPropertyChanged(nameof(SelectedListVisibility));
            }
        }

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

        // 제출 활성/비활성
        private bool _canSubmit;
        public bool CanSubmit
        {
            get { return _canSubmit; }
            set { _canSubmit = value; OnPropertyChanged(nameof(CanSubmit)); }
        }

        // 신고 사유 활성/비활성
        private bool _canReason;
        public bool CanReason
        {
            get { return _canReason; }
            set { _canReason = value; OnPropertyChanged(nameof(CanReason)); }
        }

        // 신고 상세 사유 활성/비활성
        private bool _canDetailText;
        public bool CanDetailText
        {
            get { return _canDetailText; }
            set { _canDetailText = value; OnPropertyChanged(nameof(CanDetailText)); }
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

        public static async Task<ReportDialogViewModel> CreateAsync(ReportTarget target, bool submitCheck)
        {
            var instance = new ReportDialogViewModel();
            await instance.Initialize(target, submitCheck);
            return instance;
        }

        private async Task Initialize(ReportTarget target, bool submitCheck)
        {
            SelectedMessages = new ObservableCollection<ChatMessage>();

            // 컬렉션에 항목이 추가/삭제될 때마다 Visibility 알림
            SelectedMessages.CollectionChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(SelectedEmptyVisibility));
                OnPropertyChanged(nameof(SelectedListVisibility));
            };

            // 신고 대상
            ApplyTarget(target, submitCheck);

            // 신고 상세 데이터 출력
            if (!submitCheck)
            {
                IsReportHistoryDetailMode = true;
                await LoadReportHistoryDetailAsync();
            }
            else
            {
                IsReportHistoryDetailMode = false;
                SelectedReason ??= ReasonOptions.FirstOrDefault();
            }

            // 신고 제출
            SubmitCommand = new AsyncRelayCommand(SubmitReportAsync);
        }

        // 신고 대상
        public void ApplyTarget(ReportTarget target, bool submitCheck)
        {
            TargetTypeLabel = target.TargetType == ReportTargetType.User ? "사용자" : "채팅방";
            ChatRoomTitle = target.ChatRoomTitle ?? "";
            ChatRoomId = target.ChatRoomId == Guid.Empty ? "" : target.ChatRoomId.ToString();
            TargetUserNickName = target.TargetUserNickName;
            SubmitButtonVisibility = submitCheck ? Visibility.Visible : Visibility.Collapsed;
            RemoveButtonVisibility = submitCheck ? Visibility.Visible : Visibility.Collapsed;
            CanReason = submitCheck ? true : false;
            CanDetailText = submitCheck ? true : false;
            ReportTarget = target;

            OnPropertyChanged(nameof(TargetTypeLabel));
            OnPropertyChanged(nameof(ChatRoomTitle));
            OnPropertyChanged(nameof(ChatRoomId));
            OnPropertyChanged(nameof(TargetUserNickName));
            OnPropertyChanged(nameof(SubmitButtonVisibility));
            OnPropertyChanged(nameof(RemoveButtonVisibility));
            OnPropertyChanged(nameof(CanReason));
            OnPropertyChanged(nameof(CanDetailText));
        }

        // 신고 상세 데이터 출력
        private async Task LoadReportHistoryDetailAsync()
        {
            User currentUser = UserSession.Instance.CurrentUser;

            var roomId = ReportTarget?.ChatRoomId ?? Guid.Empty;
            if (roomId == Guid.Empty)
                return;

            var reqRoom = new ChatRooms { ChatRoomId = roomId };

            // 특정 채팅방 데이터 출력
            var selectChatRoomRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendGetChatRoomDetailOnceAsync(reqRoom), getMessage: r => r.Message, userData: currentUser);

            if (selectChatRoomRes?.Ok != true)
                throw new InvalidOperationException($"server not ok: {selectChatRoomRes?.Message}");

            // 채팅방 명
            ChatRoomTitle = selectChatRoomRes.ChatRoomData.ChatRoomName;

            // 신고 메시지
            var reportMessageRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendReportMessageOnceAsync(ReportTarget.ReportID), getMessage: r => r.Message, userData: currentUser);

            if (reportMessageRes?.Ok != true)
                throw new InvalidOperationException($"server not ok: {reportMessageRes?.Message}");

            IsReportHistoryDetailMode = true;

            SelectedMessages.Clear();

            foreach (var reportData in reportMessageRes.ReportMessage)
            {
                SelectedMessages.Add(new ChatMessage
                {
                    MessageID = (Guid)reportData.MessageID,
                    MessageContent = reportData.MessageContent,
                    SentAt = (DateTime)reportData.SentAt
                });
            }

            // 특정 신고 내역 출력
            var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendSelectedReportPrintOnceAsync(ReportTarget.ReportID), getMessage: r => r.Message, userData: currentUser);

            if (res?.Ok != true)
                throw new InvalidOperationException($"server not ok: {res?.Message}");

            // 신고 사유
            SelectedReason = ReasonOptions.FirstOrDefault(x => x.Code == res.ReportData.ReasonCode)
                ?? ReasonOptions.FirstOrDefault(x => x.Code == "OTHER");

            // 상세 내용
            DetailText = res.ReportData.DetailText;
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
                    SentAt = msg.SentAt
                });
            });
        }

        // 신고 저장
        public async Task SubmitReportAsync(object parameter)
        {
            User currentUser = UserSession.Instance.CurrentUser;

            // 신고 정보 저장
            var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendSubmitReportOnceAsync(ReportTarget, SelectedReason.Code, DetailText), getMessage: r => r.Message, userData: currentUser);

            if (res?.Ok != true)
                throw new InvalidOperationException($"server not ok: {res?.Message}");

            // 신고 사유 메시지 저장
            foreach (var messageData in SelectedMessages)
            {
                var messageRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendSubmitReportMessageOnceAsync(messageData, res.ReportID), getMessage: r => r.Message, userData: currentUser);

                if (messageRes?.Ok != true)
                    throw new InvalidOperationException($"server not ok: {messageRes?.Message}");
            }

            MessageBox.Show("신고가 완료되었습니다.");

            // 신고창 닫기
            RequestClose?.Invoke();
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

        // 신고 메시지
        private async Task<ReportMessageRes?> SendReportMessageOnceAsync(Guid reportID)
        {
            var session = UserSession.Instance;

            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.ReportMessageRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { Cmd = "ReportMessage", userID = session.CurrentUser.UserId, reportID = reportID, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.ReportMessage, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<ReportMessageRes>(
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

        // 특정 신고 내역 출력
        private async Task<SelectedReportPrintRes> SendSelectedReportPrintOnceAsync(Guid reportID)
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport
                ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            // 응답 대기 설치
            var waitTask = session.Responses.WaitAsync(MessageType.SelectedReportPrintRes, reqId, TimeSpan.FromSeconds(5));

            // 요청 전송
            var req = new { cmd = "SelectedReportPrint", userID = session.CurrentUser.UserId, reportID = reportID, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.SelectedReportPrint, JsonSerializer.SerializeToUtf8Bytes(req));

            // 수신 루프가 응답을 잡아주면 도착
            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<SelectedReportPrintRes>(
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
