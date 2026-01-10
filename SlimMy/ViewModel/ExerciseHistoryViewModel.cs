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
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using Microsoft.Win32;
using ClosedXML.Excel;
using System.Text.Json;
using SlimMy.Response;
using SlimMy.Service;
using System.Threading;

namespace SlimMy.ViewModel
{
    class ExerciseHistoryViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;

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
            _navigationService = new NavigationService();
        }

        private async Task Initialize()
        {
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

            // 사용자의 운동 계획 출력
            var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendGetExerciseHistoryOnceAsync(currentUser), getMessage: r => r.Message, userData: currentUser);

            if (res?.Ok != true)
                throw new InvalidOperationException($"server not ok: {res?.Message}");

            foreach (var history in res.HistoryItem)
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

            // 사용자의 운동 계획 출력
            var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendGetExerciseHistoryOnceAsync(currentUser), getMessage: r => r.Message, userData: currentUser);

            if (res?.Ok != true)
                throw new InvalidOperationException($"server not ok: {res?.Message}");

            foreach (var history in res.HistoryItem)
            {
                AllExerciseLogs.Add(history);
            }

            var filtered = AllExerciseLogs.Where(x => x.PlannerDate >= StartDate && x.PlannerDate <= EndDate);

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
            if(SelectedExportFormat == "CSV")
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

            else if (SelectedExportFormat == "PDF")
            {
                if (parameter is not ExportChartParameter chartParam)
                {
                    MessageBox.Show("그래프 데이터가 유효하지 않습니다.");
                    return;
                }

                try
                {
                    ExportToPdf(chartParam.FilePath, chartParam.ChartInfos);
                    MessageBox.Show("PDF 내보내기가 완료되었습니다.", "성공", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"내보내기 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            else if (SelectedExportFormat == "Excel")
            {
                var dialog = new SaveFileDialog
                {
                    FileName = "운동기록",
                    DefaultExt = ".xlsx",
                    Filter = "Excel 파일 (*.xlsx)|*.xlsx"
                };

                if (dialog.ShowDialog() == true)
                {
                    ExportToExcel(dialog.FileName);
                    MessageBox.Show("Excel로 내보내기가 완료되었습니다.");
                }
            }
        }

        // 운동 기록 PDF 내보내기
        public void ExportToPdf(string filePath, (FrameworkElement Chart, string Title, IEnumerable<(string Label, string Value)> DataTable)[] chartInfos)
        {
            PdfDocument document = new PdfDocument();
            document.Info.Title = "운동 기록";

            PdfPage page = document.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(page);
            XFont font = new XFont("맑은 고딕", 12, XFontStyle.Regular);
            XFont titleFont = new XFont("맑은 고딕", 14, XFontStyle.Bold);

            double y = 40;

            // 제목
            gfx.DrawString("운동 기록 내역", new XFont("맑은 고딕", 16, XFontStyle.Bold), XBrushes.Black, new XRect(0, y, page.Width, 30), XStringFormats.TopCenter);
            y += 40;

            // 표 헤더
            gfx.DrawString("날짜", font, XBrushes.Black, new XPoint(40, y));
            gfx.DrawString("운동명", font, XBrushes.Black, new XPoint(120, y));
            gfx.DrawString("시간(분)", font, XBrushes.Black, new XPoint(250, y));
            gfx.DrawString("칼로리", font, XBrushes.Black, new XPoint(330, y));
            gfx.DrawString("운동 종류", font, XBrushes.Black, new XPoint(400, y));

            y += 25;

            // 데이터 출력
            foreach (var item in FilteredExerciseLogs)
            {
                gfx.DrawString(item.PlannerDate.ToString("yyyy-MM-dd"), font, XBrushes.Black, new XPoint(40, y));
                gfx.DrawString(item.ExerciseName, font, XBrushes.Black, new XPoint(120, y));
                gfx.DrawString(item.Minutes.ToString(), font, XBrushes.Black, new XPoint(250, y));
                gfx.DrawString(item.Calories.ToString(), font, XBrushes.Black, new XPoint(330, y));
                gfx.DrawString(item.Category, font, XBrushes.Black, new XPoint(400, y));
                y += 20;

                if (y > page.Height - 50)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    y = 40;
                }
            }

            // 차트 + 수치 데이터 출력
            foreach (var (chartElement, chartTitle, chartData) in chartInfos)
            {
                if (y > page.Height - 350)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    y = 40;
                }

                gfx.DrawString(chartTitle, titleFont, XBrushes.Black, new XPoint(40, y));
                y += 25;

                var chartImageSource = RenderChartToBitmap(chartElement, 500, 300);
                var chartImage = ConvertBitmapSourceToXImage(chartImageSource);
                gfx.DrawImage(chartImage, 40, y);
                y += 310;

                if (chartData != null)
                {
                    gfx.DrawString("• 수치 요약", font, XBrushes.Black, new XPoint(40, y));
                    y += 20;

                    foreach (var (label, value) in chartData)
                    {
                        gfx.DrawString(label, font, XBrushes.Black, new XPoint(60, y));
                        gfx.DrawString(value, font, XBrushes.Black, new XPoint(250, y));
                        y += 18;

                        if (y > page.Height - 50)
                        {
                            page = document.AddPage();
                            gfx = XGraphics.FromPdfPage(page);
                            y = 40;
                        }
                    }

                    y += 15;
                }
            }

            document.Save(filePath);
        }

        public static BitmapSource RenderChartToBitmap(FrameworkElement chartElement, int width, int height)
        {
            var drawingVisual = new DrawingVisual();
            using (var context = drawingVisual.RenderOpen())
            {
                var visualBrush = new VisualBrush(chartElement);
                context.DrawRectangle(visualBrush, null, new Rect(0, 0, width, height));
            }

            var renderBitmap = new RenderTargetBitmap(
                width, height, 96d, 96d, PixelFormats.Pbgra32);
            renderBitmap.Render(drawingVisual);
            return renderBitmap;
        }

        private XImage ConvertBitmapSourceToXImage(BitmapSource bitmapSource)
        {
            using (var ms = new MemoryStream())
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(ms);
                ms.Position = 0;
                return XImage.FromStream(ms);
            }
        }

        public void ExportToExcel(string filePath)
        {
            var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("운동 기록");

            // 운동 기록 표
            sheet.Cell(1, 1).Value = "날짜";
            sheet.Cell(1, 2).Value = "운동명";
            sheet.Cell(1, 3).Value = "시간(분)";
            sheet.Cell(1, 4).Value = "칼로리";
            sheet.Cell(1, 5).Value = "운동 종류";

            int row = 2;
            foreach (var item in FilteredExerciseLogs)
            {
                sheet.Cell(row, 1).Value = item.PlannerDate.ToString("yyyy-MM-dd");
                sheet.Cell(row, 2).Value = item.ExerciseName;
                sheet.Cell(row, 3).Value = item.Minutes;
                sheet.Cell(row, 4).Value = item.Calories;
                sheet.Cell(row, 5).Value = item.Category;
                row++;
            }

            sheet.Columns().AdjustToContents();

            // 운동 종류별 비율 (PieChart 데이터)
            var categoryGroup = FilteredExerciseLogs
                .GroupBy(x => x.Category)
                .Select(g => new { Category = g.Key, Count = g.Count(), Percentage = Math.Round((double)g.Count() / FilteredExerciseLogs.Count * 100, 1) })
                .ToList();

            var categorySheet = workbook.Worksheets.Add("운동 종류 비율");
            categorySheet.Cell(1, 1).Value = "운동 종류";
            categorySheet.Cell(1, 2).Value = "횟수";
            categorySheet.Cell(1, 3).Value = "비율 (%)";

            int catRow = 2;
            foreach (var c in categoryGroup)
            {
                categorySheet.Cell(catRow, 1).Value = c.Category;
                categorySheet.Cell(catRow, 2).Value = c.Count;
                categorySheet.Cell(catRow, 3).Value = c.Percentage;
                catRow++;
            }

            categorySheet.Columns().AdjustToContents();

            // 칼로리 소모 추세
            var calorieTrend = FilteredExerciseLogs
                .GroupBy(x => x.PlannerDate.Date)
                .OrderBy(g => g.Key)
                .Select(g => new { Date = g.Key, TotalCalories = g.Sum(x => x.Calories) })
                .ToList();

            var calSheet = workbook.Worksheets.Add("칼로리 추세");
            calSheet.Cell(1, 1).Value = "날짜";
            calSheet.Cell(1, 2).Value = "총 칼로리";

            int calRow = 2;
            foreach (var c in calorieTrend)
            {
                calSheet.Cell(calRow, 1).Value = c.Date.ToString("yyyy-MM-dd");
                calSheet.Cell(calRow, 2).Value = c.TotalCalories;
                calRow++;
            }

            calSheet.Columns().AdjustToContents();

            // 운동 시간 추세
            var timeTrend = FilteredExerciseLogs
                .GroupBy(x => x.PlannerDate.Date)
                .OrderBy(g => g.Key)
                .Select(g => new { Date = g.Key, TotalMinutes = g.Sum(x => x.Minutes) })
                .ToList();

            var timeSheet = workbook.Worksheets.Add("운동 시간 추세");
            timeSheet.Cell(1, 1).Value = "날짜";
            timeSheet.Cell(1, 2).Value = "총 운동 시간(분)";

            int timeRow = 2;
            foreach (var t in timeTrend)
            {
                timeSheet.Cell(timeRow, 1).Value = t.Date.ToString("yyyy-MM-dd");
                timeSheet.Cell(timeRow, 2).Value = t.TotalMinutes;
                timeRow++;
            }

            timeSheet.Columns().AdjustToContents();

            // 저장
            workbook.SaveAs(filePath);
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

        // 사용자의 운동 계획 출력
        private async Task<GetExerciseHistoryRes> SendGetExerciseHistoryOnceAsync(User currentUser)
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.GetExerciseHistoryRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "GetExerciseHistory", userID = currentUser.UserId, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.GetExerciseHistory, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<GetExerciseHistoryRes>(
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
