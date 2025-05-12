using LiveCharts;
using LiveCharts.Wpf;
using SlimMy.Model;
using SlimMy.Repository;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SlimMy.ViewModel
{
    public class WeightHistoryViewModel : BaseViewModel
    {
        private WeightHistoryRepository _repo;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";

        // 메모장 아이디
        private Guid memoID;

        private ObservableCollection<WeightRecordItem> _weightRecords;
        public ObservableCollection<WeightRecordItem> WeightRecords
        {
            get { return _weightRecords; }
            set { _weightRecords = value; OnPropertyChanged(nameof(WeightRecords)); }
        }

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

        private SeriesCollection _bmiSeries = new SeriesCollection();
        public SeriesCollection BmiSeries
        {
            get => _bmiSeries;
            set { _bmiSeries = value; OnPropertyChanged(nameof(BmiSeries)); }
        }

        private List<string> _bmiLabels = new List<string>();
        public List<string> BmiLabels
        {
            get => _bmiLabels;
            set { _bmiLabels = value; OnPropertyChanged(nameof(BmiLabels)); }
        }

        public Func<double, string> BmiValueFormatter { get; set; } = value => value.ToString("F2");

        public ICommand SaveCommand { get; set; }

        public ICommand DeleteRecordCommand { get; set; }

        public WeightHistoryViewModel()
        {
        }

        private async Task Initialize()
        {
            _repo = new WeightHistoryRepository(_connstring); // Repo 초기화
            WeightRecords = new ObservableCollection<WeightRecordItem>();

            // 키 수정 불가능
            IsEditingHeight = false;

            // 오늘의 날짜
            InputDate = DateTime.Now.Date;

            await WeightRecordListPrint();

            // BMI 그래프
            UpdateBmiChart();

            // 저장
            SaveCommand = new AsyncRelayCommand(WeightSaveFunction);

            DeleteRecordCommand = new AsyncRelayCommand(DeleteWeightRecord);
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

            var weightDataList = await _repo.GetWeightHistory(userData.UserId);

            foreach (var weightData in weightDataList)
            {
                WeightRecords.Add(weightData);
            }

            InputHeight = WeightRecords.Last().Height.ToString();
            InputWeight = WeightRecords.Last().Weight.ToString();
            TargetWeight = WeightRecords.Last().TargetWeight.ToString();
        }


        // 몸무게 이력 저장
        public async Task WeightSaveFunction(object parameter)
        {
            User userData = UserSession.Instance.CurrentUser;

            double heightValue = double.Parse(InputHeight) / 100.0;
            double weightValue = double.Parse(InputWeight);

            double bmiValue = weightValue / (heightValue * heightValue);

            int weightCompetedCount = await _repo.GetTodayWeightCompleted(InputDate, userData.UserId);
            string msg = string.Format("체중 관리 내역을 저장하시겠습니까?");
            MessageBoxResult messageBoxResult = MessageBox.Show(msg, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (messageBoxResult == MessageBoxResult.No)
            {
                return;
            }
            else
            {
                if (weightCompetedCount < 1)
                {
                    await _repo.InsertWeight(userData.UserId, InputDate, double.Parse(InputWeight), double.Parse(InputHeight), bmiValue, double.Parse(TargetWeight), NoteContent);
                }
                else
                {
                    await _repo.UpdatetWeight(userData.UserId, InputDate, double.Parse(InputWeight), double.Parse(InputHeight), bmiValue, double.Parse(TargetWeight), NoteContent);
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

            var memoContent = await _repo.GetMemoContent(SelectedRecord.Date, userData.UserId);

            if (memoContent != null)
            {
                NoteContent = memoContent.Content;
                memoID = memoContent.MemoID;
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
                    await _repo.DeleteWeight(SelectedRecord.BodyID, memoID);

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

                foreach (var record in WeightRecords)
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
    }
}
