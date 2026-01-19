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
using System.Windows.Input;

namespace SlimMy.ViewModel
{
    public class PlannerViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;

        public ICommand ExerciseCommand { get; set; }

        public ObservableCollection<PlanItem> Items { get; set; }

        public ICommand SelectedPlnnaerCommand { get; set; }

        public ICommand DeletePlannerGroupCommand { get; set; }

        public int UpdateIndex { get; set; }

        public ICommand DeleteCommand { get; set; }

        public ICommand SaveCommand { get; set; }

        public ICommand DietGoalCommand { get; set; }

        private PlanItem? _editingItem;

        // 새로 만들기
        public ICommand NewPlannerCommand { get; set; }

        private PlanItem _selectedPlnnerData;
        public PlanItem SelectedPlannerData
        {
            get { return _selectedPlnnerData; }
            set
            {
                if (_selectedPlnnerData != value)
                {
                    _selectedPlnnerData = value;
                    OnPropertyChanged(nameof(SelectedPlannerData));
                }
                else
                {
                    // 동일한 항목을 선택해도 동작하도록 처리
                    OnPropertyChanged(nameof(SelectedPlannerData));
                }
            }
        }

        private string _plannerTitle;
        public string PlannerTitle
        {
            get { return _plannerTitle; }
            set { _plannerTitle = value; OnPropertyChanged(nameof(PlannerTitle)); }
        }

        private DateTime _selectedDate;
        public DateTime SelectedDate
        {
            get { return _selectedDate; }
            set
            {
                if (_selectedDate != value)
                {
                    _selectedDate = value;
                    OnPropertyChanged(nameof(SelectedDate));

                    // 특정 날짜에 해당하는 플래너 내역 출력
                    SelectedDatePlanner();
                }
            }
        }

        private string _totalCalories;
        public string TotalCalories
        {
            get { return _totalCalories; }
            set { _totalCalories = value; OnPropertyChanged(nameof(TotalCalories)); }
        }

        // 해당 운동 리스트 위로
        public ICommand MoveUpCommand => new RelayCommand(_ =>
        {
            int index = Items.IndexOf(SelectedPlannerData);
            if (index > 0)
                Items.Move(index, index - 1);
        }, _ => SelectedPlannerData != null && Items.IndexOf(SelectedPlannerData) > 0);

        // 해당 운동 리스트 아래로
        public ICommand MoveDownCommand => new RelayCommand(_ =>
        {
            int index = Items.IndexOf(SelectedPlannerData);
            if (index < Items.Count - 1)
                Items.Move(index, index + 1);
        }, _ => SelectedPlannerData != null && Items.IndexOf(SelectedPlannerData) < Items.Count - 1);

        public ICommand UpdateCommand { set; get; }

        private static PlannerViewModel _instance;
        public static PlannerViewModel Instance => _instance ?? (_instance = new PlannerViewModel());

        private ObservableCollection<PlannerWithGroup> _plannerGroups;
        public ObservableCollection<PlannerWithGroup> PlannerGroups
        {
            get => _plannerGroups;
            set { _plannerGroups = value; OnPropertyChanged(nameof(PlannerGroups)); }
        }

        private PlannerWithGroup _selectedPlannerGroup;
        public PlannerWithGroup SelectedPlannerGroup
        {
            get => _selectedPlannerGroup;
            set
            {
                if (_selectedPlannerGroup != value)
                {
                    _selectedPlannerGroup = value;
                    OnPropertyChanged(nameof(SelectedPlannerGroup));

                    // 운동 리스트 세팅
                    if (_selectedPlannerGroup != null)
                    {
                        Items = new ObservableCollection<PlanItem>(
                            _selectedPlannerGroup.Exercises.Select(ex => new PlanItem
                            {
                                ExerciseID = ex.Exercise_Info_ID,
                                PlannerID = ex.PlannerID,
                                IsCompleted = ex.IsCompleted,
                                Name = ex.ExerciseName,
                                Minutes = ex.Minutes,
                                Calories = ex.Calories
                            }));

                        PlannerTitle = _selectedPlannerGroup.PlannerTitle;

                        OnPropertyChanged(nameof(PlannerTitle));
                        OnPropertyChanged(nameof(Items));

                        // 선택한 플래너의 총 칼로리
                        SelectedTotalCalories();
                    }
                    else
                    {
                        Items.Clear();
                        OnPropertyChanged(nameof(Items));
                    }
                }
            }
        }

        public IEnumerable<DateTime> HighlightDates { get; set; }

        public PlannerViewModel(IDataService dataService)
        {
            Initialize();
        }

        public PlannerViewModel()
        {
            _navigationService = new NavigationService();

            Items = new ObservableCollection<PlanItem>();

            SelectedPlnnaerCommand = new RelayCommand(PrintPlannerData);

            DeleteCommand = new RelayCommand(DeletePlannerPrint);

            NewPlannerCommand = new RelayCommand(CleanPlanner);

            SelectedDate = DateTime.Now;

            HighlightDates = new List<DateTime>
        {
            new DateTime(2025, 4, 30),
            new DateTime(2025, 5, 1)
        };

            Items.Clear();
        }

        private async Task Initialize()
        {
            SaveCommand = new AsyncRelayCommand(InsertPlannerPrint);

            DeletePlannerGroupCommand = new AsyncRelayCommand(AllDeletePlanner);

            UpdateCommand = new AsyncRelayCommand(UpdatePlannerData);

            ExerciseCommand = new AsyncRelayCommand(AddExerciseNavigation);

            DietGoalCommand = new AsyncRelayCommand(DietGoalNavigation);
        }

        public static async Task<PlannerViewModel> CreateAsync()
        {
            var instance = new PlannerViewModel();
            await instance.Initialize();
            return instance;
        }

        // 운동 추가 뷰
        public async Task AddExerciseNavigation(object parameter)
        {
            await _navigationService.NavigateToAddExerciseViewAsync(this);
        }

        // 목표 설정 뷰
        public async Task DietGoalNavigation(object parameter)
        {
            await _navigationService.NavigateToDietGoalViewAsync();
        }

        // 리스트 선택한 운동 데이터 출력
        public void PrintPlannerData(object parameter)
        {
            if (parameter is PlanItem planItem)
            {
                SelectedPlannerData = planItem;
            }
        }

        // 리스트 선택한 운동 데이터 수정
        public async Task UpdatePlannerData(object parameter)
        {
            // 운동 추가 뷰모델에게 데이터 수정을 알림
            ExerciseViewModel.IsEditMode = true;

            _editingItem = SelectedPlannerData;

            // 운동 추가 뷰 생성
            await _navigationService.NavigateToAddExerciseViewAsync(this);
        }

        // 플래너 수정
        public void UpdatePlannerPrint(Exercise exerciseData, string calories, int minutes)
        {
            if (_editingItem == null) return;

            _editingItem.ExerciseID = exerciseData.ExerciseID;
            _editingItem.Name = exerciseData.ExerciseName;
            _editingItem.Minutes = minutes;
            _editingItem.Calories = int.Parse(calories);

            TotalCaloriesCalculate();
        }

        // 플래너 목록(리스트) 삭제
        public void DeletePlannerPrint(object parameter)
        {
            if (SelectedPlannerData != null)
            {
                string msg = string.Format("{0}를 삭제하시겠습니까?", SelectedPlannerData.Name);
                MessageBoxResult messageBoxResult = MessageBox.Show(msg, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (messageBoxResult == MessageBoxResult.No)
                {
                    return;
                }
                else
                {
                    Items.Remove(SelectedPlannerData);
                    SelectedPlannerData = null;

                    TotalCaloriesCalculate();
                }
            }
            else
            {
                MessageBox.Show("운동 항목을 선택해주세요.");
            }
        }

        // 플래너 저장
        public async Task InsertPlannerPrint(object parameter)
        {
            string msg = string.Format("저장하시겠습니까?");
            MessageBoxResult messageBoxResult = MessageBox.Show(msg, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (messageBoxResult == MessageBoxResult.No)
            {
                return;
            }
            else
            {
                User currentUser = UserSession.Instance.CurrentUser;

                // 선택된 플래너가 있다면
                if (SelectedPlannerGroup != null)
                {
                    // 해당 플래너가 존재하는가?
                    var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendInsertPlannerPrintOnceAsync(), getMessage: r => r.Message, currentUser);

                    if (res?.Ok != true)
                        throw new InvalidOperationException($"server not ok: {res?.Message}");

                    // 플래너 출력
                    var plannerPrintRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendPlannerPrintOnceAsync(currentUser), getMessage: r => r.Message, userData: currentUser);

                    if (plannerPrintRes?.Ok != true)
                        throw new InvalidOperationException($"server not ok: {plannerPrintRes?.Message}");

                    var dbItems = plannerPrintRes.PlannerPrint.SelectMany(g => g.Exercises);

                    // DB snapshot: PlannerID -> DbItem
                    var dbMap = dbItems.ToDictionary(x => x.PlannerID);

                    // UI snapshot: PlannerID -> (UiItem, OrderNo)
                    var uiMap = Items.Select((item, idx) => new { item, OrderNo = idx })
                                     .ToDictionary(x => x.item.PlannerID, x => (x.item, x.OrderNo));

                    // 현재 플래너 목록의 플래너 아이디에 DB에 존재하는 데이터와 일치하지 않는 아이디
                    // 즉, DB에는 있고, 현재 플래너 목록에는 없는 플래너 아이디를 추출
                    var deletedItems = dbMap.Keys.Except(uiMap.Keys).ToList();

                    // 선택된 플래너에서 일부를 삭제하고 저장하려고 한다면
                    foreach (var deletedItemBundle in deletedItems)
                    {
                        // 현재 플래너 목록에 없는 플래너 아이디는 모두 삭제
                        var deleteRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendDeletePlannerListOnceAsync(deletedItemBundle), getMessage: r => r.Message, userData: currentUser);

                        if (deleteRes?.Ok != true)
                            throw new InvalidOperationException($"server not ok: {deleteRes?.Message}");
                    }

                    // 공통 ID
                    var commonIds = uiMap.Keys.Intersect(dbMap.Keys).ToList();

                    var notSavedIds = uiMap.Keys.Except(dbMap.Keys).ToList();

                    var notSavedExercises = notSavedIds
                        .Select(id =>
                        {
                            var (ui, orderNo) = uiMap[id];

                            return new PlannerExercise
                            {
                                PlannerID = ui.PlannerID,
                                Exercise_Info_ID = ui.ExerciseID,
                                ExerciseName = ui.Name,
                                Minutes = ui.Minutes,
                                Calories = ui.Calories,
                                IsCompleted = ui.IsCompleted,
                                Indexnum = orderNo
                            };
                        })
                        .Where(x => x != null).ToList();

                    // 플래너 일부 데이터 추가
                    foreach (var notSavedItem in notSavedExercises)
                    {
                        var deleteRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendNotSavedPlannerOnceAsync((PlannerExercise)notSavedItem), getMessage: r => r.Message, userData: currentUser);

                        if (deleteRes?.Ok != true)
                            throw new InvalidOperationException($"server not ok: {deleteRes?.Message}");
                    }

                    var changedExercises = commonIds
                        .Select(id =>
                        {
                            var db = dbMap[id];
                            var (ui, orderNo) = uiMap[id];

                            var changed = 
                            db.Exercise_Info_ID != ui.ExerciseID ||
                            db.Minutes != ui.Minutes ||
                            db.Calories != ui.Calories ||
                            db.IsCompleted != ui.IsCompleted ||
                            db.Indexnum != orderNo;

                            if (!changed) return null;

                            return new PlannerExercise
                            {
                                PlannerID = ui.PlannerID,
                                Exercise_Info_ID = ui.ExerciseID,
                                ExerciseName = ui.Name,
                                Minutes = ui.Minutes,
                                Calories = ui.Calories,
                                IsCompleted = ui.IsCompleted,
                                Indexnum = orderNo
                            };
                        })
                        .Where(x => x != null).ToList();

                    // 플래너 일부 데이터 수정
                    foreach (var updateItem in changedExercises)
                    {
                        var deleteRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendSavePlannerChangesOnceAsync((PlannerExercise)updateItem), getMessage: r => r.Message, userData: currentUser);

                        if (deleteRes?.Ok != true)
                            throw new InvalidOperationException($"server not ok: {deleteRes?.Message}");
                    }
                }

                // 해당 사용자의 플래너 여부
                var exerciseRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendExerciseCheckOnceAsync(currentUser), getMessage: r => r.Message, userData: currentUser);

                if (exerciseRes?.Ok != true)
                    throw new InvalidOperationException($"server not ok: {exerciseRes?.Message}");

                // 해당 사용자가 작성한 플래너가 있다면
                if (!exerciseRes.ExerciseCheck)
                {
                    // 플래너 리스트 전체 수정
                    //var updatePlannerRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendUpdatePlannerOnceAsync(), getMessage: r => r.Message, userData: currentUser);

                    //if (updatePlannerRes?.Ok != true)
                    //    throw new InvalidOperationException($"server not ok: {updatePlannerRes?.Message}");

                    // 플래너 전체 저장
                    var insertPlannerRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendInsertPlannerOnceAsync(currentUser), getMessage: r => r.Message, userData: currentUser);

                    if (insertPlannerRes?.Ok != true)
                        throw new InvalidOperationException($"server not ok: {insertPlannerRes?.Message}");

                    MessageBox.Show("플래너 저장이 완료되었습니다.");
                }

            }
        }

        // 플래너 출력
        public async Task PlannerPrint()
        {
            User currentUser = UserSession.Instance.CurrentUser;

            SelectedDate = SelectedDate.Date;

            // 플래너 출력
            var plannerPrintRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendPlannerPrintOnceAsync(currentUser), getMessage: r => r.Message, userData: currentUser);

            if (plannerPrintRes?.Ok != true)
                throw new InvalidOperationException($"server not ok: {plannerPrintRes?.Message}");

            // 특정 날짜의 콤보 박스에 플래너 내역 가져오기
            PlannerGroups = new ObservableCollection<PlannerWithGroup>(plannerPrintRes.PlannerPrint);
        }

        // 특정 날짜에 해당하는 플래너 내역 출력
        public async Task SelectedDatePlanner()
        {
            User currentUser = UserSession.Instance.CurrentUser;

            if (UserSession.Instance.CurrentUser != null)
            {
                // 특정 날짜에 해당하는 플래너 내역 출력
                var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendExerciseListOnceAsync(), getMessage: r => r.Message, userData: currentUser);

                if (res?.Ok != true)
                    throw new InvalidOperationException($"server not ok: {res?.Message}");

                PlannerGroups = new ObservableCollection<PlannerWithGroup>(res.PlannerGroups);

                // 플래너 목록(콤보 박스)의 첫 번째 요소 혹은 null 반환
                SelectedPlannerGroup = PlannerGroups.FirstOrDefault();

                // 선택한 플래너의 총 칼로리
                SelectedTotalCalories();
            }
        }

        // 선택한 플래너의 총 칼로리
        public void SelectedTotalCalories()
        {
            int totalCalorieSum;

            if (SelectedPlannerGroup != null)
            {
                totalCalorieSum = 0;

                foreach (var result in SelectedPlannerGroup.Exercises)
                {
                    totalCalorieSum += result.Calories;
                }
            }
            else
            {
                totalCalorieSum = 0;
            }

            TotalCalories = totalCalorieSum.ToString();
        }

        // 플래너 목록을 수정 이후의 칼로리 계산
        public void TotalCaloriesCalculate()
        {
            int totalCalorieSum;

            if (Items != null)
            {
                totalCalorieSum = 0;

                foreach (var result in Items)
                {
                    totalCalorieSum += result.Calories;
                }
            }
            else
            {
                totalCalorieSum = 0;
            }

            TotalCalories = totalCalorieSum.ToString();
        }

        // 해당 플래너 전체 삭제
        public async Task AllDeletePlanner(object parameter)
        {
            string msg = string.Format("영구 삭제하시겠습니까?");
            MessageBoxResult messageBoxResult = MessageBox.Show(msg, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);

            User currentUser = UserSession.Instance.CurrentUser;

            if (messageBoxResult == MessageBoxResult.No)
            {
                return;
            }
            else
            {
                // 플래너 전체 삭제
                var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendDeletePlannerOnceAsync(), getMessage: r => r.Message, userData: currentUser);

                if (res?.Ok != true)
                {
                    throw new InvalidOperationException($"server not ok: {res?.Message}");
                }
                else
                {
                    Items.Clear();
                    PlannerGroups.Clear();
                    PlannerTitle = null;
                    SelectedDate = DateTime.Now;
                    TotalCalories = null;
                }
            }
        }

        // 운동 선택 이후에 데이터를 리스트 박스에 출력
        public void SelectedPlannerPrint(Exercise exerciseData, string calories, int minutes)
        {
            Items.Add(new PlanItem
            {
                ExerciseID = exerciseData.ExerciseID,
                Name = exerciseData.ExerciseName,
                Minutes = minutes,
                Calories = int.Parse(calories)
            });

            // 선택한 플래너의 총 칼로리
            TotalCaloriesCalculate();
        }

        // 새로 만들기
        public void CleanPlanner(object parameter)
        {
            string msg = string.Format("새로 플래너를 작성하시겠습니까? \n" + "기존의 데이터는 저장되지 않습니다.");
            MessageBoxResult messageBoxResult = MessageBox.Show(msg, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (messageBoxResult == MessageBoxResult.No)
            {
                return;
            }
            else
            {
                Items.Clear();
                PlannerGroups.Clear();
                PlannerTitle = null;
                SelectedDate = DateTime.Now;
                TotalCalories = null;
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

        // 해당 플래너가 존재하는가?
        private async Task<InsertPlannerPrintRes> SendInsertPlannerPrintOnceAsync()
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var insertPlannerPrintReqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.InsertPlannerPrintRes, insertPlannerPrintReqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "InsertPlannerPrint", userID = session.CurrentUser.UserId, plannerGroup = SelectedPlannerGroup.PlannerGroupId, accessToken = UserSession.Instance.AccessToken, requestID = insertPlannerPrintReqId };
            await transport.SendFrameAsync(MessageType.InsertPlannerPrint, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<InsertPlannerPrintRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // 현재 플래너 목록에 없는 플래너 아이디는 모두 삭제
        private async Task<DeletePlannerListRes> SendDeletePlannerListOnceAsync(Guid deletedItems)
        {
            var deleteSession = UserSession.Instance;
            var deleteTransport = deleteSession.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var deletePlannerListReqId = Guid.NewGuid();

            var deleteWaitTask = deleteSession.Responses.WaitAsync(MessageType.DeletePlannerListRes, deletePlannerListReqId, TimeSpan.FromSeconds(5));

            var deleteReq = new { cmd = "DeletePlannerList", userID = deleteSession.CurrentUser.UserId, plannerID = deletedItems, accessToken = UserSession.Instance.AccessToken, requestID = deletePlannerListReqId };
            await deleteTransport.SendFrameAsync(MessageType.DeletePlannerList, JsonSerializer.SerializeToUtf8Bytes(deleteReq));

            var deleteRespPayload = await deleteWaitTask;

            return JsonSerializer.Deserialize<DeletePlannerListRes>(
                deleteRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // 현재 플래너 목록 중 일부 데이터 수정
        private async Task<SavePlannerChangesRes> SendSavePlannerChangesOnceAsync(PlannerExercise updatedItems)
        {
            var updateSession = UserSession.Instance;
            var updateTransport = updateSession.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var updatePlannerListReqId = Guid.NewGuid();

            var updateWaitTask = updateSession.Responses.WaitAsync(MessageType.SavePlannerChangesRes, updatePlannerListReqId, TimeSpan.FromSeconds(5));

            var updateReq = new { cmd = "SavePlannerChanges", userID = updateSession.CurrentUser.UserId, plannerBundle = updatedItems, plannerGroupID = SelectedPlannerGroup.PlannerGroupId, accessToken = UserSession.Instance.AccessToken, requestID = updatePlannerListReqId };
            await updateTransport.SendFrameAsync(MessageType.SavePlannerChanges, JsonSerializer.SerializeToUtf8Bytes(updateReq));

            var updateRespPayload = await updateWaitTask;

            return JsonSerializer.Deserialize<SavePlannerChangesRes>(
                updateRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // 현재 플래너 목록 중 일부 데이터 추가
        private async Task<NotSavedPlannerRes> SendNotSavedPlannerOnceAsync(PlannerExercise updatedItems)
        {
            var updateSession = UserSession.Instance;
            var updateTransport = updateSession.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var updatePlannerListReqId = Guid.NewGuid();

            var updateWaitTask = updateSession.Responses.WaitAsync(MessageType.NotSavedPlannerRes, updatePlannerListReqId, TimeSpan.FromSeconds(5));

            var updateReq = new { cmd = "NotSavedPlanner", userID = updateSession.CurrentUser.UserId, plannerBundle = updatedItems, plannerGroupID = SelectedPlannerGroup.PlannerGroupId, accessToken = UserSession.Instance.AccessToken, requestID = updatePlannerListReqId };
            await updateTransport.SendFrameAsync(MessageType.NotSavedPlanner, JsonSerializer.SerializeToUtf8Bytes(updateReq));

            var updateRespPayload = await updateWaitTask;

            return JsonSerializer.Deserialize<NotSavedPlannerRes>(
                updateRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // 해당 사용자의 플래너 여부
        private async Task<ExerciseCheckRes> SendExerciseCheckOnceAsync(User currentUser)
        {
            var exerciseSession = UserSession.Instance;
            var exerciseTransport = exerciseSession.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var exerciseWaitTask = exerciseSession.Responses.WaitAsync(MessageType.ExerciseCheckRes, reqId, TimeSpan.FromSeconds(5));

            var exerciseReq = new { cmd = "ExerciseCheck", userID = currentUser.UserId, selectedDate = SelectedDate, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await exerciseTransport.SendFrameAsync(MessageType.ExerciseCheck, JsonSerializer.SerializeToUtf8Bytes(exerciseReq));

            var exerciseRespPayload = await exerciseWaitTask;

            return JsonSerializer.Deserialize<ExerciseCheckRes>(
                exerciseRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // 플래너 리스트 전체 수정
        private async Task<UpdatePlannerRes> SendUpdatePlannerOnceAsync()
        {
            var UpdatePlannerSession = UserSession.Instance;
            var UpdatePlannerTransport = UpdatePlannerSession.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var updatePlannerReqId = Guid.NewGuid();

            var UpdatePlannerWaitTask = UpdatePlannerSession.Responses.WaitAsync(MessageType.UpdatePlannerRes, updatePlannerReqId, TimeSpan.FromSeconds(5));

            var UpdatePlannerReq = new { cmd = "UpdatePlanner", userID = UpdatePlannerSession.CurrentUser.UserId, plannerGroupId = SelectedPlannerGroup.PlannerGroupId, plannerTitle = PlannerTitle, items = Items.ToList(), accessToken = UserSession.Instance.AccessToken, requestID = updatePlannerReqId };
            await UpdatePlannerTransport.SendFrameAsync(MessageType.UpdatePlanner, JsonSerializer.SerializeToUtf8Bytes(UpdatePlannerReq));

            var UpdatePlannerRespPayload = await UpdatePlannerWaitTask;

            return JsonSerializer.Deserialize<UpdatePlannerRes>(
                UpdatePlannerRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // 플래너 전체 저장
        private async Task<InsertPlannerRes> SendInsertPlannerOnceAsync(User currentUser)
        {
            var InsertPlannerSession = UserSession.Instance;
            var InsertPlannerTransport = InsertPlannerSession.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var insertPlannerReqId = Guid.NewGuid();

            var InsertPlannerWaitTask = InsertPlannerSession.Responses.WaitAsync(MessageType.InsertPlannerRes, insertPlannerReqId, TimeSpan.FromSeconds(5));

            var InsertPlannerReq = new { cmd = "InsertPlanner", userID = currentUser.UserId, plannerTitle = PlannerTitle, selectedDate = SelectedDate, items = Items.ToList(), accessToken = UserSession.Instance.AccessToken, requestID = insertPlannerReqId };
            await InsertPlannerTransport.SendFrameAsync(MessageType.InsertPlanner, JsonSerializer.SerializeToUtf8Bytes(InsertPlannerReq));

            var InsertPlannerRespPayload = await InsertPlannerWaitTask;

            return JsonSerializer.Deserialize<InsertPlannerRes>(
                InsertPlannerRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // 플래너 출력
        private async Task<PlannerPrintRes> SendPlannerPrintOnceAsync(User currentUser)
        {
            var PlannerPrintSession = UserSession.Instance;
            var PlannerPrintTransport = PlannerPrintSession.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var PlannerPrintWaitTask = PlannerPrintSession.Responses.WaitAsync(MessageType.PlannerPrintRes, reqId, TimeSpan.FromSeconds(5));

            var PlannerPrintReq = new { cmd = "PlannerPrint", userId = currentUser.UserId, selectedDate = SelectedDate, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await PlannerPrintTransport.SendFrameAsync(MessageType.PlannerPrint, JsonSerializer.SerializeToUtf8Bytes(PlannerPrintReq));

            var PlannerPrintRespPayload = await PlannerPrintWaitTask;

            return JsonSerializer.Deserialize<PlannerPrintRes>(
                PlannerPrintRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // 특정 날짜에 해당하는 플래너 내역 출력
        private async Task<ExerciseListRes> SendExerciseListOnceAsync()
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.ExerciseListRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "ExerciseList", userID = UserSession.Instance.CurrentUser.UserId, selectedDate = SelectedDate.Date, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.ExerciseList, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<ExerciseListRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // 플래너 전체 삭제
        private async Task<DeletePlannerRes> SendDeletePlannerOnceAsync()
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.DeletePlannerRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "DeletePlanner", userID = session.CurrentUser.UserId, plannerGroupId = SelectedPlannerGroup.PlannerGroupId, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.DeletePlanner, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<DeletePlannerRes>(
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
