using LiveCharts;
using LiveCharts.Wpf;
using SlimMy.Model;
using SlimMy.Response;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace SlimMy.ViewModel
{
    public class DashBoardViewModel : BaseViewModel
    {
        DateTime now;

        User currentUser = UserSession.Instance.CurrentUser;

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
        }

        private async Task Initialize()
        {
            RecentWorkouts = new ObservableCollection<string>();

            // 총 소모 칼로리
            await TodayCaloriesPrint();

            // 총 운동 시간
            await TodayDurationPrint();

            // 운동 완료 수
            await TodayCompletedPrint();

            // 목표 달성률
            await GoalRatePrint();

            // 주간 그래프
            await LoadWeeklyCalorieChart();

            // 누적 운동 횟수
            await TotalSessionPrint();

            // 누적 칼로리
            await TotalCaloriesPrint();

            // 누적 운동 시간
            await TotalTimePrint();

            await RecentWorkoutsPrint();

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
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.GetTodayCaloriesRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "GetTodayCalories", dateTime = now, userID = currentUser.UserId, requestID = reqId };
            await transport.SendFrameAsync((byte)MessageType.GetTodayCalories, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            var res = JsonSerializer.Deserialize<GetTodayCaloriesRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (res?.ok != true)
                throw new InvalidOperationException($"server not ok: {res?.message}");

            TodayCalories = res.totalCalories + "Kcal";
        }

        // 오늘의 총 운동 시간
        public async Task TodayDurationPrint()
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.GetTodayDurationRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "GetTodayDuration", dateTime = now, userID = currentUser.UserId, requestID = reqId };
            await transport.SendFrameAsync((byte)MessageType.GetTodayDuration, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            var res = JsonSerializer.Deserialize<GetTodayDurationRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (res?.ok != true)
                throw new InvalidOperationException($"server not ok: {res?.message}");

            int hour = 0, min = 0;

            hour = res.todayDuration / 60;
            min = res.todayDuration % 60;

            TodayDuration = hour + "시간 " + min + "분";
        }

        // 오늘의 운동 완료 수
        public async Task TodayCompletedPrint()
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.GetTodayCompletedRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "GetTodayCompleted", dateTime = now, userID = currentUser.UserId, requestID = reqId };
            await transport.SendFrameAsync((byte)MessageType.GetTodayCompleted, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            var res = JsonSerializer.Deserialize<GetTodayCompletedRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (res?.ok != true)
                throw new InvalidOperationException($"server not ok: {res?.message}");

            TodayCompleted = res.todayCompleted.ToString();
        }

        // 목표 달성률
        public async Task GoalRatePrint()
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            // 완료한 운동 리스트
            var waitTask = session.Responses.WaitAsync(MessageType.GetTodayCompletedRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "GetTodayCompleted", dateTime = now, userID = currentUser.UserId, requestID = reqId };
            await transport.SendFrameAsync((byte)MessageType.GetTodayCompleted, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            var res = JsonSerializer.Deserialize<GetTodayCompletedRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (res?.ok != true)
                throw new InvalidOperationException($"server not ok: {res?.message}");

            var getTotalExerciseReqId = Guid.NewGuid();

            // 전체 운동 리스트
            var totalExerciseWaitTask = session.Responses.WaitAsync(MessageType.GetTotalExerciseRes, getTotalExerciseReqId, TimeSpan.FromSeconds(5));

            var totalExerciseReq = new { cmd = "GetTotalExercise", dateTime = now, userID = currentUser.UserId, requestID = getTotalExerciseReqId };
            await transport.SendFrameAsync((byte)MessageType.GetTotalExercise, JsonSerializer.SerializeToUtf8Bytes(totalExerciseReq));

            var totalExerciseRespPayload = await totalExerciseWaitTask;

            var totalExerciseRes = JsonSerializer.Deserialize<GetTotalExerciseRes>(
                totalExerciseRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (totalExerciseRes?.ok != true)
                throw new InvalidOperationException($"server not ok: {res?.message}");

            double percent = totalExerciseRes.totalExercise == 0 ? 0 : (double)res.todayCompleted / totalExerciseRes.totalExercise * 100;

            string percentStr = Math.Round(percent, 1) + "%";

            GoalRate = percentStr;
        }

        // 주간 그래프
        public async Task LoadWeeklyCalorieChart()
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.GetWeeklyCaloriesRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "GetWeeklyCalories", dateTime = DateTime.Now, userID = currentUser.UserId, requestID = reqId };
            await transport.SendFrameAsync((byte)MessageType.GetWeeklyCalories, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            var res = JsonSerializer.Deserialize<GetWeeklyCaloriesRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (res?.ok != true)
                throw new InvalidOperationException($"server not ok: {res?.message}");

            var calorieValues = new ChartValues<int>();
            var labelList = new List<string>();

            foreach (var item in res.list.OrderBy(x => x.Date))
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
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.GetTotalSessionsRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "GetTotalSessions", userID = currentUser.UserId, requestID = reqId };
            await transport.SendFrameAsync((byte)MessageType.GetTotalSessions, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            var res = JsonSerializer.Deserialize<GetTotalSessionsRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (res?.ok != true)
                throw new InvalidOperationException($"server not ok: {res?.message}");

            TotalSessions = res.totalSessionCount.ToString();
        }

        // 누적 총 칼로리
        public async Task TotalCaloriesPrint()
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.GetTotalCaloriesRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "GetTotalCalories", userID = currentUser.UserId, requestID = reqId };
            await transport.SendFrameAsync((byte)MessageType.GetTotalCalories, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            var res = JsonSerializer.Deserialize<GetTotalCaloriesRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (res?.ok != true)
                throw new InvalidOperationException($"server not ok: {res?.message}");

            TotalCalories = res.totalCaloriesCount.ToString();
        }

        // 누적 운동 시간
        public async Task TotalTimePrint()
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.GetTotalTimeRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "GetTotalTime", userID = currentUser.UserId, requestID = reqId };
            await transport.SendFrameAsync((byte)MessageType.GetTotalTime, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            var res = JsonSerializer.Deserialize<GetTotalTimeRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (res?.ok != true)
                throw new InvalidOperationException($"server not ok: {res?.message}");

            int hour = 0, min = 0;

            hour = res.totalTimeCount / 60;
            min = res.totalTimeCount % 60;

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

            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.GetRecentWorkoutsRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "GetRecentWorkouts", userID = currentUser.UserId, requestID = reqId };
            await transport.SendFrameAsync((byte)MessageType.GetRecentWorkouts, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            var res = JsonSerializer.Deserialize<GetRecentWorkoutsRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (res?.ok != true)
                throw new InvalidOperationException($"server not ok: {res?.message}");

            RecentWorkouts.Clear();

            foreach (var item in res.recentWorkoutList.Take(6))
            {
                RecentWorkouts.Add(item);
            }
        }
    }
}
