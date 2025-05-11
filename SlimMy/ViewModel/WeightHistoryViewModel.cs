using SlimMy.Model;
using SlimMy.Repository;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        private ObservableCollection<WeightRecordItem> _weightRecords;
        public ObservableCollection<WeightRecordItem> WeightRecords
        {
            get { return _weightRecords; }
            set { _weightRecords = value; OnPropertyChanged(nameof(WeightRecords)); }
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
            set { _inputHeight = value; OnPropertyChanged(nameof(InputHeight)); }
        }

        // 몸무게 텍스트
        private string _inputWeight;
        public string InputWeight
        {
            get { return _inputWeight; }
            set { _inputWeight = value; OnPropertyChanged(nameof(InputWeight)); }
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

        public ICommand SaveCommand { get; set; }

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

            // 저장
            SaveCommand = new AsyncRelayCommand(WeightSaveFunction);


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
                MessageBox.Show("DashBoardViewModel 생성 실패: " + ex.Message);
                return null;
            }
        }

        // 몸무게 기록 출력
        public async Task WeightRecordListPrint()
        {
            User userData = UserSession.Instance.CurrentUser;

            var weightDataList = await _repo.GetWeightHistory(userData.UserId);

            foreach(var weightData in weightDataList)
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

            await _repo.InsertWeight(userData.UserId, InputDate, double.Parse(InputWeight), double.Parse(InputHeight), bmiValue, double.Parse(TargetWeight), NoteContent);
        }
    }
}
