using LiveCharts;
using LiveCharts.Wpf;
using SlimMy.Model;
using SlimMy.Response;
using SlimMy.Service;
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
using System.Windows.Media;

namespace SlimMy.ViewModel
{
    public class DashBoardViewModel : BaseViewModel
    {
        DateTime now;

        User currentUser = UserSession.Instance.CurrentUser;

        private readonly INavigationService _navigationService;

        private string _todayDisplay;
        public string TodayDisplay
        {
            get { return _todayDisplay; }
            set { _todayDisplay = value; OnPropertyChanged(nameof(TodayDisplay)); }
        }

        private string _todayCalories;
        public string TodayCalories
        {
            get { return _todayCalories; }
            set { _todayCalories = value; OnPropertyChanged(nameof(TodayCalories)); }
        }

        private string _todayDuration;
        public string TodayDuration
        {
            get { return _todayDuration; }
            set { _todayDuration = value; OnPropertyChanged(nameof(TodayDuration)); }
        }

        private string _todayCompleted;
        public string TodayCompleted
        {
            get { return _todayCompleted; }
            set { _todayCompleted = value; OnPropertyChanged(nameof(TodayCompleted)); }
        }

        private string _goalRate;
        public string GoalRate
        {
            get { return _goalRate; }
            set { _goalRate = value; OnPropertyChanged(nameof(GoalRate)); }
        }

        private SeriesCollection _weeklySeries;
        public SeriesCollection WeeklySeries
        {
            get => _weeklySeries;
            set { _weeklySeries = value; OnPropertyChanged(nameof(WeeklySeries)); }
        }

        private List<string> _labels;
        public List<string> Labels
        {
            get => _labels;
            set { _labels = value; OnPropertyChanged(nameof(Labels)); }
        }

        private string _totalSessions;
        public string TotalSessions
        {
            get { return _totalSessions; }
            set { _totalSessions = value; OnPropertyChanged(nameof(TotalSessions)); }
        }

        private string _totalCalories;
        public string TotalCalories
        {
            get { return _totalCalories; }
            set { _totalCalories = value; OnPropertyChanged(nameof(TotalCalories)); }
        }

        private string _totalTime;
        public string TotalTime
        {
            get { return _totalTime; }
            set { _totalTime = value; OnPropertyChanged(nameof(TotalTime)); }
        }

        private string _currentWeight;
        public string CurrentWeight
        {
            get { return _currentWeight; }
            set { _currentWeight = value; OnPropertyChanged(nameof(CurrentWeight)); }
        }

        private ObservableCollection<string> _recentWorkouts;
        public ObservableCollection<string> RecentWorkouts
        {
            get { return _recentWorkouts; }
            set { _recentWorkouts = value; OnPropertyChanged(nameof(RecentWorkouts)); }
        }

        public DashBoardViewModel()
        {
            now = DateTime.Now.Date;
            TodayDisplay = now.ToString("yyyy-MM-dd (dddd)");

            _navigationService = new NavigationService();
        }

        private async Task Initialize()
        {
            RecentWorkouts = new ObservableCollection<string>();

            //// 총 소모 칼로리
            //await TodayCaloriesPrint();

            //// 총 운동 시간
            //await TodayDurationPrint();

            //// 운동 완료 수
            //await TodayCompletedPrint();

            //// 목표 달성률
            //await GoalRatePrint();

            //// 주간 그래프
            //await LoadWeeklyCalorieChart();

            //// 누적 운동 횟수
            //await TotalSessionPrint();

            //// 누적 칼로리
            //await TotalCaloriesPrint();

            //// 누적 운동 시간
            //await TotalTimePrint();

            //await RecentWorkoutsPrint();

            await Step(nameof(TodayCaloriesPrint), TodayCaloriesPrint);
            await Step(nameof(TodayDurationPrint), TodayDurationPrint);
            await Step(nameof(TodayCompletedPrint), TodayCompletedPrint);
            await Step(nameof(GoalRatePrint), GoalRatePrint);
            await Step(nameof(LoadWeeklyCalorieChart), LoadWeeklyCalorieChart);
            await Step(nameof(TotalSessionPrint), TotalSessionPrint);
            await Step(nameof(TotalCaloriesPrint), TotalCaloriesPrint);
            await Step(nameof(TotalTimePrint), TotalTimePrint);
            await Step(nameof(RecentWorkoutsPrint), RecentWorkoutsPrint);
        }

        private static async Task Step(string stepName, Func<Task> action)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DashBoard Init] START {stepName}");
                await action();
                System.Diagnostics.Debug.WriteLine($"[DashBoard Init] END   {stepName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashBoard Init] FAIL  {stepName}: {ex}");
                throw new InvalidOperationException($"{stepName} failed: {ex.Message}", ex);
            }
        }

        public static async Task<DashBoardViewModel> CreateAsync()
        {
            try
            {
                var vm = new DashBoardViewModel();
                await vm.Initialize();
                return vm;
            }
            catch (Exception ex)
            {
                MessageBox.Show("DashBoardViewModel 생성 실패: " + ex.Message);
                return null;
            }
        }

        // 오늘의 소모 칼로리의 총량
        public async Task TodayCaloriesPrint()
        {
            var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendGetTodayCaloriesOnceAsync(), getMessage: r => r.Message, userData: currentUser);

            if (res?.Ok != true)
                throw new InvalidOperationException($"server not ok: {res?.Message}");

            TodayCalories = res.TotalCalories + "Kcal";
        }

        // 오늘의 총 운동 시간
        public async Task TodayDurationPrint()
        {
            var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendGetTodayDurationOnceAsync(), getMessage: r => r.Message, userData: currentUser);

            if (res?.Ok != true)
                throw new InvalidOperationException($"server not ok: {res?.Message}");

            int hour = 0, min = 0;

            hour = res.TodayDuration / 60;
            min = res.TodayDuration % 60;

            TodayDuration = hour + "시간 " + min + "분";
        }

        // 오늘의 운동 완료 수
        public async Task TodayCompletedPrint()
        {
            var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendGetTodayCompletedOnceAsync(), getMessage: r => r.Message, userData: currentUser);

            if (res?.Ok != true)
                throw new InvalidOperationException($"server not ok: {res?.Message}");

            TodayCompleted = res.TodayCompleted.ToString();
        }

        // 목표 달성률
        public async Task GoalRatePrint()
        {
            var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendGetTodayCompletedOnceAsync(), getMessage: r => r.Message, userData: currentUser);

            // 세션이 만료되면 로그인 창만 실행
            if (HandleAuthError(res?.Message))
                return;

            if (res?.Ok != true)
                throw new InvalidOperationException($"server not ok: {res?.Message}");

            var totalExerciseRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendGetTotalExerciseOnceAsync(), getMessage: r => r.Message, userData: currentUser);

            // 세션이 만료되면 로그인 창만 실행
            if (HandleAuthError(totalExerciseRes?.Message))
                return;

            if (totalExerciseRes?.Ok != true)
                throw new InvalidOperationException($"server not ok: {res?.Message}");

            double percent = totalExerciseRes.TotalExercise == 0 ? 0 : (double)res.TodayCompleted / totalExerciseRes.TotalExercise * 100;

            string percentStr = Math.Round(percent, 1) + "%";

            GoalRate = percentStr;
        }

        // 주간 그래프
        public async Task LoadWeeklyCalorieChart()
        {
            var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendGetWeeklyCaloriesOnceAsync(), getMessage: r => r.Message, userData: currentUser);

            if (res?.Ok != true)
                throw new InvalidOperationException($"server not ok: {res?.Message}");

            var calorieValues = new ChartValues<int>();
            var labelList = new List<string>();

            foreach (var item in res.List.OrderBy(x => x.Date))
            {
                calorieValues.Add(item.Calories);
                labelList.Add(item.Date.ToString("MM/dd")); // x축 라벨
            }

            WeeklySeries = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "칼로리 소모",
                    Values = calorieValues,
                    Stroke = Brushes.OrangeRed,
                    Fill = Brushes.Transparent,
                    PointGeometrySize = 10
                }
            };

            Labels = labelList;
        }

        // 누적 운동 횟수
        public async Task TotalSessionPrint()
        {
            var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendGetTotalSessionsOnceAsync(), getMessage: r => r.Message, userData: currentUser);

            if (res?.Ok != true)
                throw new InvalidOperationException($"server not ok: {res?.Message}");

            TotalSessions = res.TotalSessionCount.ToString();
        }

        // 누적 총 칼로리
        public async Task TotalCaloriesPrint()
        {
            var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendGetTotalCaloriesOnceAsync(), getMessage: r => r.Message, userData: currentUser);

            if (res?.Ok != true)
                throw new InvalidOperationException($"server not ok: {res?.Message}");

            TotalCalories = res.TotalCaloriesCount.ToString();
        }

        // 누적 운동 시간
        public async Task TotalTimePrint()
        {
            var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendGetTotalTimeOnceAsync(), getMessage: r => r.Message, userData: currentUser);

            if (res?.Ok != true)
                throw new InvalidOperationException($"server not ok: {res?.Message}");

            int hour = 0, min = 0;

            hour = res.TotalTimeCount / 60;
            min = res.TotalTimeCount % 60;

            TotalTime = hour + "시간 " + min + "분";
        }

        // 최근 완료된 운동
        public async Task RecentWorkoutsPrint()
        {
            var currentUser = UserSession.Instance.CurrentUser;

            if (currentUser == null)
            {
                RecentWorkouts.Clear();
                RecentWorkouts.Add("사용자 정보가 없습니다.");
                return;
            }

            var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendGetRecentWorkoutsOnceAsync(), getMessage: r => r.Message, userData: currentUser);

            if (res?.Ok != true)
                throw new InvalidOperationException($"server not ok: {res?.Message}");

            RecentWorkouts.Clear();

            foreach (var item in res.RecentWorkoutList.Take(6))
            {
                RecentWorkouts.Add(item);
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

        private async Task<GetTodayCaloriesRes> SendGetTodayCaloriesOnceAsync()
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.GetTodayCaloriesRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "GetTodayCalories", dateTime = now, userID = currentUser.UserId, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.GetTodayCalories, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<GetTodayCaloriesRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task<GetTodayDurationRes> SendGetTodayDurationOnceAsync()
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.GetTodayDurationRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "GetTodayDuration", dateTime = now, userID = currentUser.UserId, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.GetTodayDuration, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<GetTodayDurationRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task<GetTodayCompletedRes> SendGetTodayCompletedOnceAsync()
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.GetTodayCompletedRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "GetTodayCompleted", dateTime = now, userID = currentUser.UserId, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.GetTodayCompleted, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<GetTodayCompletedRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task<GetTotalExerciseRes> SendGetTotalExerciseOnceAsync()
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var getTotalExerciseReqId = Guid.NewGuid();

            // 전체 운동 리스트
            var totalExerciseWaitTask = session.Responses.WaitAsync(MessageType.GetTotalExerciseRes, getTotalExerciseReqId, TimeSpan.FromSeconds(5));

            var totalExerciseReq = new { cmd = "GetTotalExercise", dateTime = now, userID = currentUser.UserId, accessToken = UserSession.Instance.AccessToken, requestID = getTotalExerciseReqId };
            await transport.SendFrameAsync(MessageType.GetTotalExercise, JsonSerializer.SerializeToUtf8Bytes(totalExerciseReq));

            var totalExerciseRespPayload = await totalExerciseWaitTask;

            return JsonSerializer.Deserialize<GetTotalExerciseRes>(
                totalExerciseRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task<GetWeeklyCaloriesRes> SendGetWeeklyCaloriesOnceAsync()
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.GetWeeklyCaloriesRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "GetWeeklyCalories", dateTime = DateTime.Now, userID = currentUser.UserId, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.GetWeeklyCalories, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<GetWeeklyCaloriesRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task<GetTotalSessionsRes> SendGetTotalSessionsOnceAsync()
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.GetTotalSessionsRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "GetTotalSessions", userID = currentUser.UserId, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.GetTotalSessions, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<GetTotalSessionsRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task<GetTotalCaloriesRes> SendGetTotalCaloriesOnceAsync()
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.GetTotalCaloriesRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "GetTotalCalories", userID = currentUser.UserId, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.GetTotalCalories, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<GetTotalCaloriesRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task<GetTotalTimeRes> SendGetTotalTimeOnceAsync()
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.GetTotalTimeRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "GetTotalTime", userID = currentUser.UserId, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.GetTotalTime, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<GetTotalTimeRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task<GetRecentWorkoutsRes> SendGetRecentWorkoutsOnceAsync()
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.GetRecentWorkoutsRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "GetRecentWorkouts", userID = currentUser.UserId, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.GetRecentWorkouts, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<GetRecentWorkoutsRes>(
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
