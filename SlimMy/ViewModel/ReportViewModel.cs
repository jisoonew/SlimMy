using SlimMy.Model;
using SlimMy.Response;
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
    public class ReportViewModel : BaseViewModel
    {
        public ObservableCollection<ReportItem> ReportItem { get; private set; }

        public ObservableCollection<ReportItem> AllData { get; set; }

        private ObservableCollection<ReportItem> _currentPageData; // 현재 페이지에 표시할 데이터의 컬렉션
        private int _currentPage; // 현재 페이지 번호
        private int _totalPages; // 전체 데이터에서 생성된 총 페이지 수
        private int _pageSize = 1; // 페이지당 항목 수

        public ObservableCollection<ReportItem> CurrentPageData
        {
            get => _currentPageData;
            set
            {
                _currentPageData = value;
                OnPropertyChanged(nameof(CurrentPageData));
            }
        }

        // 현재 페이지 번호를 관리
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage != value)
                {
                    _currentPage = value;
                    UpdateCurrentPageData();
                    OnPropertyChanged(nameof(CurrentPage));
                }
            }
        }

        // 전체 데이터에서 총 몇 개의 페이지가 있는지 계산하여 저장
        public int TotalPages
        {
            get => _totalPages;
            set { _totalPages = value; OnPropertyChanged(nameof(TotalPages)); }
        }

        public ICommand NextPageCommand { get; set; }
        public ICommand PreviousPageCommand { get; set; }

        public ReportViewModel()
        {
            ReportItem = new ObservableCollection<ReportItem>();
        }

        private async Task Initialize()
        {
            await RefreshReport();

            AllData = ReportItem;

            // 총 페이지 수 계산
            TotalPages = (int)Math.Ceiling((double)AllData.Count / _pageSize);

            // 현재 페이지 초기화
            CurrentPage = 1;

            NextPageCommand = new RelayCommand(
execute: _ => NextPage(),
canExecute: _ => CanGoToNextPage());

            PreviousPageCommand = new RelayCommand(
                execute: _ => PreviousPage(),
                canExecute: _ => CanGoToPreviousPage());

            UpdateCurrentPageData();
        }

        public static async Task<ReportViewModel> CreateAsync()
        {
            var instance = new ReportViewModel();
            await instance.Initialize();

            return instance;
        }

        public async Task RefreshReport()
        {
            User userDateBundle = UserSession.Instance.CurrentUser;

            var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendChatRoomPageListOnceAsync(), getMessage: r => r.Message, userData: userDateBundle);

            if (res?.Ok != true)
                throw new InvalidOperationException($"server not ok: {res?.Message}");

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                ReportItem.Clear();
                foreach (var r in res.ReportItem)
                    ReportItem.Add(r);
                OnPropertyChanged(nameof(ReportItem));
            });
        }

        private void UpdateCurrentPageData()
        {
            // Skip: 현재 페이지 이전의 항목을 건너뜀.
            // Tack: 페이지 크기만큼 데이터를 가져옴.
            // PageSize = 10, CurrentPage = 2 → Skip(10).Take(10) → 데이터 11~20번 가져옴.
            if (AllData == null || AllData.Count == 0)
                return;

            int totalDataCount = AllData.Count; // 전체 데이터 수
            int remainingDataCount = totalDataCount - ((CurrentPage - 1) * _pageSize); // 남은 데이터 수

            int dataToTake = Math.Min(_pageSize, remainingDataCount); // 현재 페이지에 가져올 데이터 수

            CurrentPageData = new ObservableCollection<ReportItem>(
                AllData.Skip((CurrentPage - 1) * _pageSize).Take(dataToTake));

            Application.Current.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(CurrentPageData));
            });
        }

        private void NextPage()
        {
            if (CanGoToNextPage())
            {
                CurrentPage++;
            }
        }

        private void PreviousPage()
        {
            if (CurrentPage > 1)
                CurrentPage--;
        }

        private bool CanGoToNextPage() => CurrentPage < TotalPages;
        private bool CanGoToPreviousPage() => CurrentPage > 1;

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

        // 신고 내역 출력
        private async Task<ReportPrintRes> SendChatRoomPageListOnceAsync()
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport
                ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            // 응답 대기 설치
            var waitTask = session.Responses.WaitAsync(MessageType.ReportPrintRes, reqId, TimeSpan.FromSeconds(5));

            // 요청 전송
            var req = new { cmd = "ReportPrint", userID = session.CurrentUser.UserId, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.ReportPrint, JsonSerializer.SerializeToUtf8Bytes(req));

            // 수신 루프가 응답을 잡아주면 도착
            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<ReportPrintRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // 토큰 만료
        private bool IsAuthExpired(string? message) => string.Equals(message, "expired token", StringComparison.OrdinalIgnoreCase) || string.Equals(message, "unauthorized", StringComparison.OrdinalIgnoreCase);
    }
}
