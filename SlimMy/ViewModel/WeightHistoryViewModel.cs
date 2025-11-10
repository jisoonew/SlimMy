using ClosedXML.Excel;
using LiveCharts;
using LiveCharts.Wpf;
using SlimMy.Model;
using SlimMy.Repository;
using SlimMy.Response;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SlimMy.ViewModel
{
    public class WeightHistoryViewModel : BaseViewModel
    {
        // 메모장 아이디
        private Guid memoID;

        private ObservableCollection<WeightRecordItem> _weightRecords;
        public ObservableCollection<WeightRecordItem> WeightRecords
        {
            get { return _weightRecords; }
            set { _weightRecords = value; OnPropertyChanged(nameof(WeightRecords)); }
        }

        // 몸무게 내역 선택
        private WeightRecordItem _selectedRecord;
        public WeightRecordItem SelectedRecord
        {
            get { return _selectedRecord; }
            set
            {
                _selectedRecord = value; OnPropertyChanged(nameof(SelectedRecord));
                if (SelectedRecord != null)
                {
                    _ = LoadMemoAsync(); // fire and forget 방식
                }
            }
        }

        // 날짜
        private DateTime _inputDate;
        public DateTime InputDate
        {
            get { return _inputDate; }
            set { _inputDate = value; OnPropertyChanged(nameof(InputDate)); }
        }

        // 키 수정(체크 박스)
        private bool _isEditingHeight;
        public bool IsEditingHeight
        {
            get { return _isEditingHeight; }
            set { _isEditingHeight = value; OnPropertyChanged(nameof(IsEditingHeight)); }
        }

        // 키 텍스트
        private string _inputHeight;
        public string InputHeight
        {
            get { return _inputHeight; }
            set { _inputHeight = value; OnPropertyChanged(nameof(InputHeight)); if (InputHeight != null) { UpdateBmiChart(); } }
        }

        // 몸무게 텍스트
        private string _inputWeight;
        public string InputWeight
        {
            get { return _inputWeight; }
            set { _inputWeight = value; OnPropertyChanged(nameof(InputWeight)); if (InputWeight != null) { UpdateBmiChart(); } }
        }

        // 메모장
        private string _noteContent;
        public string NoteContent
        {
            get { return _noteContent; }
            set { _noteContent = value; OnPropertyChanged(nameof(NoteContent)); }
        }

        // 목표 몸무게
        private string _targetWeight;
        public string TargetWeight
        {
            get { return _targetWeight; }
            set { _targetWeight = value; OnPropertyChanged(nameof(TargetWeight)); }
        }

        // BMI 그래프
        private SeriesCollection _bmiSeries = new SeriesCollection();
        public SeriesCollection BmiSeries
        {
            get => _bmiSeries;
            set { _bmiSeries = value; OnPropertyChanged(nameof(BmiSeries)); }
        }

        // BMI 그래프 날짜
        private List<string> _bmiLabels = new List<string>();
        public List<string> BmiLabels
        {
            get => _bmiLabels;
            set { _bmiLabels = value; OnPropertyChanged(nameof(BmiLabels)); }
        }

        // 몸무게 그래프
        private SeriesCollection _weightTrendSeries = new SeriesCollection();
        public SeriesCollection WeightTrendSeries
        {
            get => _weightTrendSeries;
            set { _weightTrendSeries = value; OnPropertyChanged(nameof(WeightTrendSeries)); }
        }

        // 몸무게 그래프 날짜
        private List<string> _weightTrendLabels = new List<string>();
        public List<string> WeightTrendLabels
        {
            get => _weightTrendLabels;
            set { _weightTrendLabels = value; OnPropertyChanged(nameof(WeightTrendLabels)); }
        }

        // 검색어 카테고리
        private string _selectedSearchValue;
        public string SelectedSearchValue
        {
            get { return _selectedSearchValue; }
            set { _selectedSearchValue = value; OnPropertyChanged(nameof(SelectedSearchValue)); }
        }

        // 검색어
        private string _searchKeyword;
        public string SearchKeyword
        {
            get { return _searchKeyword; }
            set { _searchKeyword = value; OnPropertyChanged(nameof(SearchKeyword)); }
        }

        private double _weightDiffFromPrevious;
        public double WeightDiffFromPrevious
        {
            get => _weightDiffFromPrevious;
            set { _weightDiffFromPrevious = value; OnPropertyChanged(nameof(WeightDiffFromPrevious)); }
        }

        public Func<double, string> BmiValueFormatter { get; set; } = value => value.ToString("F2");

        public ICommand SaveCommand { get; set; }

        public ICommand DeleteRecordCommand { get; set; }

        public ICommand SearchCommand { get; set; }

        public ICommand ExportCsvCommand { get; set; }

        public event Action ExportPdfRequested;
        public event Action ExportExcelRequested;

        public ICommand ExportPdfCommand { get; set; }

        public ICommand ExportExcelCommand { get; set; }

        public WeightHistoryViewModel()
        {
        }

        private async Task Initialize()
        {
            WeightRecords = new ObservableCollection<WeightRecordItem>();

            // 키 수정 불가능
            IsEditingHeight = false;

            // 오늘의 날짜
            InputDate = DateTime.Now.Date;

            await WeightRecordListPrint();

            // BMI 그래프
            UpdateBmiChart();

            // 몸무게 내역 그래프
            UpdateWeightChart();

            // 몸무게 변화량
            CalculateWeightDiff();

            // 저장
            SaveCommand = new AsyncRelayCommand(WeightSaveFunction);

            DeleteRecordCommand = new AsyncRelayCommand(DeleteWeightRecord);

            SearchCommand = new AsyncRelayCommand(SelectedSearchCategoryPrint);

            // CSV
            ExportCsvCommand = new RelayCommand(_ => ExportToCsv());

            // PDF
            ExportPdfCommand = new RelayCommand(_ => ExportPdfRequested?.Invoke());

            // Excel
            ExportExcelCommand = new RelayCommand(_ => ExportExcelRequested?.Invoke());
        }

        public static async Task<WeightHistoryViewModel> CreateAsync()
        {
            try
            {
                var vm = new WeightHistoryViewModel();
                await vm.Initialize();
                return vm;
            }
            catch (Exception ex)
            {
                MessageBox.Show("WeightHistoryViewModel 생성 실패: " + ex.Message);
                return null;
            }
        }

        // 몸무게 기록 출력
        public async Task WeightRecordListPrint()
        {
            User userData = UserSession.Instance.CurrentUser;

            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.GetWeightHistoryRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "GetWeightHistory", userID = userData.UserId };
            await transport.SendFrameAsync((byte)MessageType.GetWeightHistory, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            var res = JsonSerializer.Deserialize<GetWeightHistoryRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (res?.ok != true)
                throw new InvalidOperationException($"server not ok: {res?.message}");

            foreach (var weightData in res.weightRecordItem)
            {
                WeightRecords.Add(weightData);
            }

            InputHeight = WeightRecords.Last().Height.ToString();
            InputWeight = WeightRecords.Last().Weight.ToString();
            TargetWeight = WeightRecords.Last().TargetWeight.ToString();
        }

        // 몸무게 변화량
        private void CalculateWeightDiff()
        {
            var sorted = WeightRecords.OrderBy(r => r.Date).ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                if (i == 0)
                {
                    sorted[i].WeightDiffFromPrevious = "0";
                }
                else
                {
                    double diff = Math.Round(sorted[i].Weight - sorted[i - 1].Weight, 1);
                    sorted[i].WeightDiffFromPrevious = diff.ToString();
                }
            }
        }


        // 몸무게 이력 저장
        public async Task WeightSaveFunction(object parameter)
        {
            User userData = UserSession.Instance.CurrentUser;

            double heightValue = double.Parse(InputHeight) / 100.0;
            double weightValue = double.Parse(InputWeight);

            double bmiValue = weightValue / (heightValue * heightValue);

            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.GetTodayWeightCompletedRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "GetTodayWeightCompleted", dateTime = InputDate, userID = userData.UserId };
            await transport.SendFrameAsync((byte)MessageType.GetTodayWeightCompleted, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            var res = JsonSerializer.Deserialize<GetTodayWeightCompletedRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (res?.ok != true)
                throw new InvalidOperationException($"server not ok: {res?.message}");

            string msg = string.Format("체중 관리 내역을 저장하시겠습니까?");
            MessageBoxResult messageBoxResult = MessageBox.Show(msg, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (messageBoxResult == MessageBoxResult.No)
            {
                return;
            }
            else
            {
                if (res.weightCount < 1)
                {
                    var insertWeightResReqId = Guid.NewGuid();

                    var insertWeightWaitTask = session.Responses.WaitAsync(MessageType.InsertWeightRes, insertWeightResReqId, TimeSpan.FromSeconds(5));

                    var insertWeightReq = new { cmd = "InsertWeight", requestId = reqId, userID = userData.UserId, dateTime = InputDate, inputWeight = double.Parse(InputWeight), inputHeight = double.Parse(InputHeight), bmiValue = bmiValue, targetWeight = double.Parse(TargetWeight), noteContent = NoteContent };
                    await transport.SendFrameAsync((byte)MessageType.InsertWeight, JsonSerializer.SerializeToUtf8Bytes(insertWeightReq));

                    var insertWeightRespPayload = await insertWeightWaitTask;

                    var insertWeightRes = JsonSerializer.Deserialize<InsertWeightRes>(
                        insertWeightRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (insertWeightRes?.ok != true)
                        throw new InvalidOperationException($"server not ok: {insertWeightRes?.message}");
                }
                else
                {
                    var updateWeightReqId = Guid.NewGuid();

                    var updatetWeightWaitTask = session.Responses.WaitAsync(MessageType.UpdateWeightRes, updateWeightReqId, TimeSpan.FromSeconds(5));

                    var updateWeightReq = new { cmd = "UpdateWeight", userID = userData.UserId, dateTime = InputDate, inputWeight = double.Parse(InputWeight), inputHeight = double.Parse(InputHeight), bmiValue = bmiValue, targetWeight = double.Parse(TargetWeight), noteContent = NoteContent };
                    await transport.SendFrameAsync((byte)MessageType.UpdateWeight, JsonSerializer.SerializeToUtf8Bytes(updateWeightReq));

                    var updateWeightRespPayload = await updatetWeightWaitTask;

                    var updateWeightRes = JsonSerializer.Deserialize<UpdateWeightRes>(
                        updateWeightRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (updateWeightRes?.ok != true)
                        throw new InvalidOperationException($"server not ok: {updateWeightRes?.message}");
                }
            }
        }

        // 몸무게 이력을 선택 후 메모장 출력
        public async Task MemoPrint()
        {
            User userData = UserSession.Instance.CurrentUser;

            if (SelectedRecord == null)
            {
                NoteContent = string.Empty;
                return;
            }

            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.GetMemoContentRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "GetMemoContent", dateTime = SelectedRecord.Date, userID = userData.UserId };
            await transport.SendFrameAsync((byte)MessageType.GetMemoContent, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            var res = JsonSerializer.Deserialize<GetMemoContentRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (res?.ok != true)
                throw new InvalidOperationException($"server not ok: {res?.message}");

            if (res.memoContent != null)
            {
                NoteContent = res.memoContent.Content;
                memoID = res.memoContent.MemoID;
            }
            else
            {
                NoteContent = string.Empty;
            }
        }

        // 안전한 fire and forget 방식을 위한 비동기 메서드
        // await를 통해 UI 스레드가 멈추지 않도록 비동기 흐름을 유지하기 위함
        // 이렇게 하지 않으면 UI가 멈추거나, 아니면 메모의 내용이 모두 불러와지지 않는 현상이 발생할 수 있음
        private async Task LoadMemoAsync()
        {
            try
            {
                await MemoPrint();
            }
            catch (Exception ex)
            {
                // 로그 남기기
                Debug.WriteLine("메모 불러오기 중 오류 발생: " + ex.Message);
            }
        }

        // 몸무게 이력 삭제
        public async Task DeleteWeightRecord(object parameter)
        {
            DateTime selectedWeightDate = SelectedRecord.Date;

            string msg = string.Format("해당 {0} 내역을 삭제 하시겠습니까?", selectedWeightDate.ToString("yyyy-MM-dd"));
            MessageBoxResult messageBoxResult = MessageBox.Show(msg, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (messageBoxResult == MessageBoxResult.No)
            {
                return;
            }
            else
            {
                if (SelectedRecord != null)
                {
                    var session = UserSession.Instance;
                    var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

                    var reqId = Guid.NewGuid();

                    var waitTask = session.Responses.WaitAsync(MessageType.DeleteWeightRes, reqId, TimeSpan.FromSeconds(5));

                    var req = new { cmd = "DeleteWeight", bodyID = SelectedRecord.BodyID, memoID = memoID };
                    await transport.SendFrameAsync((byte)MessageType.DeleteWeight, JsonSerializer.SerializeToUtf8Bytes(req));

                    var respPayload = await waitTask;

                    var res = JsonSerializer.Deserialize<DeleteWeightRes>(
                        respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (res?.ok != true)
                        throw new InvalidOperationException($"server not ok: {res?.message}");

                    NoteContent = string.Empty;
                }
            }
        }

        // BMI 그래프
        private void UpdateBmiChart()
        {
            if (InputHeight != null && InputWeight != null)
            {
                var bmiValues = new ChartValues<double>();
                var labels = new List<string>();

                // 그래프에 10개의 데이터만 출력
                int maxBMICount = 10;

                // 날짜 오름차순으로 정렬된 복사본 사용
                var sortedRecords = WeightRecords.OrderBy(r => r.Date).TakeLast(maxBMICount).ToList();

                foreach (var record in sortedRecords)
                {
                    bmiValues.Add(Math.Round(record.BMI, 2));
                    labels.Add(record.Date.ToString("yyyy-MM-dd"));
                }

                BmiSeries = new SeriesCollection
    {
        new LineSeries
        {
            Title = "BMI ",
            Values = bmiValues,
            DataLabels = true,
            LabelPoint = point =>
            {
                double bmi = point.Y;
                string category = GetBmiCategory(bmi);
                return $"{bmi:F2} ({category})";
            }
        }
    };

                BmiLabels = labels;
                OnPropertyChanged(nameof(BmiSeries));
                OnPropertyChanged(nameof(BmiLabels));
            }
        }

        // BMI 구간
        private string GetBmiCategory(double bmi)
        {
            if (bmi < 18.5) return "저체중";
            else if (bmi < 23.0) return "정상";
            else if (bmi < 25.0) return "과체중";
            else if (bmi < 30.0) return "비만";
            else return "고도비만";
        }

        // 몸무게 그래프
        private void UpdateWeightChart()
        {
            if (InputHeight != null && InputWeight != null)
            {
                var weightValues = new ChartValues<double>();
                var weightlabels = new List<string>();

                // 그래프에 30개의 데이터만 출력
                int maxWeightCount = 30;

                // 날짜 오름차순으로 정렬된 복사본 사용
                var sortedRecords = WeightRecords.OrderBy(r => r.Date).TakeLast(maxWeightCount).ToList();

                foreach (var record in sortedRecords)
                {
                    weightValues.Add(Math.Round(record.Weight, 2));
                    weightlabels.Add(record.Date.ToString("yyyy-MM-dd"));
                }

                WeightTrendSeries = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "몸무게 ",
                        Values = weightValues,
                        DataLabels = true
                    }
                };

                WeightTrendLabels = weightlabels;
                OnPropertyChanged(nameof(WeightTrendSeries));
                OnPropertyChanged(nameof(WeightTrendLabels));
            }
        }

        // 검색
        public async Task SelectedSearchCategoryPrint(object parameter)
        {
            User userData = UserSession.Instance.CurrentUser;

            if (SelectedSearchValue == "메모")
            {
                try
                {
                    var session = UserSession.Instance;
                    var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

                    var reqId = Guid.NewGuid();

                    var waitTask = session.Responses.WaitAsync(MessageType.GetSearchedMemoContentRes, reqId, TimeSpan.FromSeconds(5));

                    var req = new { cmd = "GetSearchedMemoContent", userID = userData.UserId, searchKeyword = SearchKeyword };
                    await transport.SendFrameAsync((byte)MessageType.GetSearchedMemoContent, JsonSerializer.SerializeToUtf8Bytes(req));

                    var respPayload = await waitTask;

                    var res = JsonSerializer.Deserialize<GetSearchedMemoContentRes>(
                        respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (res?.ok != true)
                        throw new InvalidOperationException($"server not ok: {res?.message}");

                    WeightRecords.Clear();
                    WeightRecords.Add(res.weightMemoRecord.Record);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("해당 내용은 존재하지 않습니다.");
                }
            }

            if (SelectedSearchValue == "날짜")
            {
                try
                {
                    if (!DateTime.TryParseExact(SearchKeyword, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                    {
                        MessageBox.Show("날짜 형식은 yyyy-MM-dd 형식으로 입력하세요. 예: 2025-05-13");
                        return;
                    }

                    var session = UserSession.Instance;
                    var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

                    var reqId = Guid.NewGuid();

                    var waitTask = session.Responses.WaitAsync(MessageType.GetSearchedDateRes, reqId, TimeSpan.FromSeconds(5));

                    var req = new { cmd = "GetSearchedDate", userID = userData.UserId, parsedDate = parsedDate };
                    await transport.SendFrameAsync((byte)MessageType.GetSearchedDate, JsonSerializer.SerializeToUtf8Bytes(req));

                    var respPayload = await waitTask;

                    var res = JsonSerializer.Deserialize<GetSearchedDateRes>(
                        respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (res?.ok != true)
                        throw new InvalidOperationException($"server not ok: {res?.message}");

                    WeightRecords.Clear();
                    WeightRecords.Add(res.weightMemoRecord.Record);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("해당 날짜는 존재하지 않습니다.");
                }

            }

            if (SelectedSearchValue == "몸무게")
            {
                try
                {
                    var session = UserSession.Instance;
                    var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

                    var reqId = Guid.NewGuid();

                    var waitTask = session.Responses.WaitAsync(MessageType.GetSearchedWeightRes, reqId, TimeSpan.FromSeconds(5));

                    var req = new { cmd = "GetSearchedWeight", userID = userData.UserId, searchKeyword = double.Parse(SearchKeyword) };
                    await transport.SendFrameAsync((byte)MessageType.GetSearchedWeight, JsonSerializer.SerializeToUtf8Bytes(req));

                    var respPayload = await waitTask;

                    var res = JsonSerializer.Deserialize<GetSearchedWeightRes>(
                        respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (res?.ok != true)
                        throw new InvalidOperationException($"server not ok: {res?.message}");

                    WeightRecords.Clear();
                    WeightRecords.Add(res.weightMemoRecord.Record);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("해당 몸무게 내역은 존재하지 않습니다.");
                }
            }
        }

        // CSV
        private void ExportToCsv()
        {
            if (WeightRecords == null || WeightRecords.Count == 0)
            {
                MessageBox.Show("저장할 데이터가 없습니다.");
                return;
            }

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV 파일 (*.csv)|*.csv",
                FileName = "WeightHistory.csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var csvLines = new List<string>
            {
                "==== [몸무게 & BMI 내역] ====",
                "날짜,몸무게(kg),목표 몸무게(kg),변화량,BMI"
            };

                    foreach (var record in WeightRecords.OrderBy(r => r.Date))
                    {
                        string line = $"{record.Date:yyyy-MM-dd},{record.Weight},{record.TargetWeight},{record.WeightDiffFromPrevious:+0.0;-0.0;0},{record.BMI:F2}";
                        csvLines.Add(line);
                    }

                    File.WriteAllLines(saveDialog.FileName, csvLines, Encoding.UTF8);
                    MessageBox.Show("CSV 저장이 완료되었습니다.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("CSV 저장 중 오류 발생: " + ex.Message);
                }
            }
        }
    }
}
