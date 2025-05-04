using LiveCharts;
using LiveCharts.Wpf;
using SlimMy.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace SlimMy.ViewModel
{
    public class DashBoardViewModel : BaseViewModel
    {
        private Repo _repo;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";
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
            _repo = new Repo(_connstring); // Repo 초기화

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
            int totalCalories = await _repo.GetTodayCalories(now, currentUser.UserId);

            TodayCalories = totalCalories + "Kcal";
        }

        // 오늘의 총 운동 시간
        public async Task TodayDurationPrint()
        {
            int todayDuration = await _repo.GetTodayDuration(now, currentUser.UserId);

            int hour = 0, min = 0;

            hour = todayDuration / 60;
            min = todayDuration % 60;

            TodayDuration = hour + "시간 " + min + "분";
        }

        // 오늘의 운동 완료 수
        public async Task TodayCompletedPrint()
        {
            int todayCompleted = await _repo.GetTodayCompleted(now, currentUser.UserId);

            TodayCompleted = todayCompleted.ToString();
        }

        // 목표 달성률
        public async Task GoalRatePrint()
        {
            int todayCompleted = await _repo.GetTodayCompleted(now, currentUser.UserId);

            int totalExercise = await _repo.GetTotalExercise(now, currentUser.UserId);

            double percent = totalExercise == 0 ? 0 : (double)todayCompleted / totalExercise * 100;

            string percentStr = Math.Round(percent, 1) + "%";

            GoalRate = percentStr;
        }

        // 주간 그래프
        public async Task LoadWeeklyCalorieChart()
        {
            var list = await _repo.GetWeeklyCalories(DateTime.Now, currentUser.UserId);

            // 예: 7일간 DailyCalorie 객체 리스트
            var calorieValues = new ChartValues<int>();
            var labelList = new List<string>();

            foreach (var item in list.OrderBy(x => x.Date))
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
            int totalSessionCount = await _repo.GetTotalSessions(currentUser.UserId);

            TotalSessions = totalSessionCount.ToString();
        }

        // 누적 총 칼로리
        public async Task TotalCaloriesPrint()
        {
            int totalCaloriesCount = await _repo.GetTotalCalories(currentUser.UserId);

            TotalCalories = totalCaloriesCount.ToString();
        }

        // 누적 운동 시간
        public async Task TotalTimePrint()
        {
            int totalTimeCount = await _repo.GetTotalTime(currentUser.UserId);

            int hour = 0, min = 0;

            hour = totalTimeCount / 60;
            min = totalTimeCount % 60;

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

            List<string> recentWorkoutList = await _repo.GetRecentWorkouts(currentUser.UserId);

            RecentWorkouts.Clear();
            foreach (var item in recentWorkoutList.Take(6))
            {
                RecentWorkouts.Add(item);
            }
        }
    }
}
