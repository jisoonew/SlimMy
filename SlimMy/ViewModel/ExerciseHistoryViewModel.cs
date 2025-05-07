using LiveCharts;
using LiveCharts.Wpf;
using SlimMy.Model;
using SlimMy.Repository;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace SlimMy.ViewModel
{
    class ExerciseHistoryViewModel : BaseViewModel
    {
        private ExerciseHistoryRepository _repo;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";

        private ObservableCollection<WorkoutHistoryItem> _filteredExerciseLogs;
        public ObservableCollection<WorkoutHistoryItem> FilteredExerciseLogs
        {
            get { return _filteredExerciseLogs; }
            set { _filteredExerciseLogs = value; OnPropertyChanged(nameof(FilteredExerciseLogs)); }
        }

        private DateTime _startDate;
        public DateTime StartDate
        {
            get { return _startDate; }
            set { _startDate = value; OnPropertyChanged(nameof(StartDate)); }
        }

        private DateTime _endDate;
        public DateTime EndDate
        {
            get { return _endDate; }
            set { _endDate = value; OnPropertyChanged(nameof(EndDate)); }
        }

        private SeriesCollection _calorieTrendSeries;
        public SeriesCollection CalorieTrendSeries
        {
            get { return _calorieTrendSeries; }
            set { _calorieTrendSeries = value; OnPropertyChanged(nameof(CalorieTrendSeries)); }
        }

        private SeriesCollection _categoryDistributionSeries;
        public SeriesCollection CategoryDistributionSeries
        {
            get { return _categoryDistributionSeries; }
            set { _categoryDistributionSeries = value; OnPropertyChanged(nameof(CategoryDistributionSeries)); }
        }

        private SeriesCollection _durationTrendSeries;
        public SeriesCollection DurationTrendSeries
        {
            get { return _durationTrendSeries; }
            set { _durationTrendSeries = value; OnPropertyChanged(nameof(DurationTrendSeries)); }
        }

        private ObservableCollection<WorkoutHistoryItem> _allExerciseLogs;
        public ObservableCollection<WorkoutHistoryItem> AllExerciseLogs
        {
            get { return _allExerciseLogs; }
            set { _allExerciseLogs = value; OnPropertyChanged(nameof(AllExerciseLogs)); }
        }

        private string _searchKeyword;
        public string SearchKeyword
        {
            get { return _searchKeyword; }
            set { _searchKeyword = value; OnPropertyChanged(nameof(SearchKeyword)); }
        }

        public ObservableCollection<string> ExportFormatOptions { get; set; } = new ObservableCollection<string>
{
    "CSV", "Excel", "PDF"
};

        public string SelectedExportFormat { get; set; }

        private string _averageCalories;
        public string AverageCalories
        {
            get { return _averageCalories; }
            set { _averageCalories = value; OnPropertyChanged(nameof(AverageCalories)); }
        }

        private string _averageDuration;
        public string AverageDuration
        {
            get { return _averageDuration; }
            set { _averageDuration = value; OnPropertyChanged(nameof(AverageDuration)); }
        }

        private string[] _labels;
        public string[] Labels
        {
            get { return _labels; }
            set { _labels = value; OnPropertyChanged(nameof(Labels)); }
        }

        public AsyncRelayCommand SearchCommand { get; set; }

        public ICommand ExportCommand { get; set; }

        public ExerciseHistoryViewModel()
        {
        }

        private async Task Initialize()
        {
            _repo = new ExerciseHistoryRepository(_connstring); // Repo 초기화

            FilteredExerciseLogs = new ObservableCollection<WorkoutHistoryItem>();

            AllExerciseLogs = new ObservableCollection<WorkoutHistoryItem>();

            // 사용자가 완료한 운동 내역
            await ExerciseHistoryPrint();

            // 칼로리 소모 그래프
            ExerciseCaloriesPrint();

            // 운동 카테고리 비율 그래프
            CategoryDistribution();

            // 운동 시간 그래프
            DurationTrendSeriesPrint();

            // 평균 칼로리 계산
            AvgCalories();

            // 평균 운동 시간 계산
            AvgDuration();

            SearchCommand = new AsyncRelayCommand(SearchExerciseHistory);

            ExportCommand = new RelayCommand(ExecuteExport);
        }

        public static async Task<ExerciseHistoryViewModel> CreateAsync()
        {
            try
            {
                var vm = new ExerciseHistoryViewModel();
                await vm.Initialize();
                return vm;
            }
            catch (Exception ex)
            {
                MessageBox.Show("DashBoardViewModel 생성 실패: " + ex.Message);
                return null;
            }
        }

        // 사용자가 완료한 운동 내역
        public async Task ExerciseHistoryPrint()
        {
            User currentUser = UserSession.Instance.CurrentUser;

            var historyList = await _repo.GetExerciseHistory(currentUser.UserId);

            foreach (var history in historyList)
            {
                FilteredExerciseLogs.Add(history);
            }

            EndDate = FilteredExerciseLogs.FirstOrDefault().PlannerDate;

            StartDate = FilteredExerciseLogs.LastOrDefault().PlannerDate;
        }

        // 칼로리 소모 그래프
        public void ExerciseCaloriesPrint()
        {
            var calorieValues = new ChartValues<int>();
            var labelList = new List<string>();

            // 날짜별로 그룹화하여 합산
            var groupedByDate = FilteredExerciseLogs
                .GroupBy(x => x.PlannerDate.Date)
                .OrderBy(g => g.Key);

            foreach (var item in groupedByDate)
            {
                int totalCalories = item.Sum(x => x.Calories);
                calorieValues.Add(totalCalories);
                labelList.Add(item.Key.ToString("MM/dd")); // x축 라벨
            }

            CalorieTrendSeries = new SeriesCollection
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

            Labels = labelList.ToArray();
            OnPropertyChanged(nameof(Labels));
        }

        // 운동 카테고리 비율 그래프
        public void CategoryDistribution()
        {
            var values = new SeriesCollection();

            // 날짜별 그룹화 후 운동 시간 합산
            var grouped = FilteredExerciseLogs
                .GroupBy(x => x.Category)
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                values.Add(new PieSeries
                {
                    Title = "",
                    Values = new ChartValues<int> { group.Count() },
                    DataLabels = true,
                    LabelPoint = chartPoint => $"{group.Key} {chartPoint.Participation:P1}" // 카테고리 + 퍼센트
                });
            }

            CategoryDistributionSeries = values;
        }

        // 운동 시간 추세 그래프
        public void DurationTrendSeriesPrint()
        {
            var calorieValues = new ChartValues<int>();
            var labelList = new List<string>();

            // 날짜별로 그룹화하여 합산
            var groupedByDate = FilteredExerciseLogs
                .GroupBy(x => x.PlannerDate.Date)
                .OrderBy(g => g.Key);

            foreach (var item in groupedByDate)
            {
                int totalCalories = item.Sum(x => x.Minutes);
                calorieValues.Add(totalCalories);
                labelList.Add(item.Key.ToString("MM/dd")); // x축 라벨
            }

            DurationTrendSeries = new SeriesCollection
    {
        new LineSeries
        {
            Title = "운동 시간",
            Values = calorieValues,
            Stroke = Brushes.OrangeRed,
            Fill = Brushes.Transparent,
            PointGeometrySize = 10
        }
    };

            Labels = labelList.ToArray();
            OnPropertyChanged(nameof(Labels));
        }

        // 평균 칼로리 계산
        public void AvgCalories()
        {
            int CalorieSum = 0;

            foreach (var calorie in FilteredExerciseLogs)
            {
                CalorieSum += calorie.Calories;
            }

            int avgCalories = CalorieSum / FilteredExerciseLogs.Count();

            AverageCalories = avgCalories.ToString() + "Kcal";
        }

        // 평균 운동 시간 계산
        public void AvgDuration()
        {
            int durationSum = 0;

            foreach (var calorie in FilteredExerciseLogs)
            {
                durationSum += calorie.Minutes;
            }

            int avgDuration = durationSum / FilteredExerciseLogs.Count();

            AverageDuration = avgDuration.ToString() + "분";
        }

        // 검색 기능
        public async Task SearchExerciseHistory(object parameter)
        {
            AllExerciseLogs.Clear();

            User currentUser = UserSession.Instance.CurrentUser;

            var historyList = await _repo.GetExerciseHistory(currentUser.UserId);

            foreach (var history in historyList)
            {
                AllExerciseLogs.Add(history);
            }

            var filtered = AllExerciseLogs
    .Where(x => x.PlannerDate >= StartDate && x.PlannerDate <= EndDate);

            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                filtered = filtered.Where(x => x.ExerciseName.Contains(SearchKeyword));
            }

            if (!filtered.Any())
            {
                MessageBox.Show("해당 기간에 완료된 운동 기록이 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            FilteredExerciseLogs.Clear();
            FilteredExerciseLogs = new ObservableCollection<WorkoutHistoryItem>(filtered);

            // 칼로리 소모 그래프
            ExerciseCaloriesPrint();

            // 운동 카테고리 비율 그래프
            CategoryDistribution();

            // 운동 시간 그래프
            DurationTrendSeriesPrint();
        }

        // 운동 기록 CSV 내보내기
        public void ExportToCsv(string filePath)
        {
            var sb = new StringBuilder();

            // 헤더
            sb.AppendLine("날짜,운동명,시간(분),칼로리,운동 종류");

            // 데이터
            foreach (var item in FilteredExerciseLogs)
            {
                sb.AppendLine($"{item.PlannerDate:yyyy-MM-dd},{item.ExerciseName},{item.Minutes},{item.Calories},{item.Category}");
            }

            sb.AppendLine("[칼로리 소모 추세]");
            sb.AppendLine("날짜,총 칼로리");

            // 칼로리 소모 그래프
            var groupedByDate = FilteredExerciseLogs
                .GroupBy(x => x.PlannerDate.Date)
                .OrderBy(g => g.Key);

            foreach (var groupedByDateItem in groupedByDate)
            {
                int totalCalories = groupedByDateItem.Sum(x => x.Calories);
                sb.AppendLine($"{groupedByDateItem.Key:yyyy-MM-dd},{totalCalories}");
            }

            sb.AppendLine("[운동 종류별 비율]");
            sb.AppendLine("운동 종류,비율(%)");

            // 운동 카테고리 비율 그래프
            var grouped = FilteredExerciseLogs
                .GroupBy(x => x.Category)
                .OrderBy(g => g.Key);

            int totalCount = FilteredExerciseLogs.Count;

            foreach (var groupedItem in grouped)
            {
                double percent = (double)groupedItem.Count() / totalCount * 100;
                sb.AppendLine($"{groupedItem.Key},{percent:F1}%");
            }

            // 운동 시간 추세
            sb.AppendLine("[운동 시간 추세]");
            sb.AppendLine("날짜,총 시간(분)");

            var durationGrouped = FilteredExerciseLogs
                .GroupBy(x => x.PlannerDate)
                .OrderBy(g => g.Key);

            foreach (var durationGroupedItem in durationGrouped)
            {
                int totalMinutes = durationGroupedItem.Sum(x => x.Minutes);
                sb.AppendLine($"{durationGroupedItem.Key:yyyy-MM-dd},{totalMinutes}");
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        // 내보내기
        private void ExecuteExport(object parameter)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = "운동기록",
                DefaultExt = ".csv",
                Filter = "CSV 파일 (*.csv)|*.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    ExportToCsv(dialog.FileName);
                    MessageBox.Show("CSV 내보내기가 완료되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"내보내기 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
