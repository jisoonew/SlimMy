using SlimMy.Model;
using SlimMy.Response;
using System;
using System.Collections.Generic;
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
    public class DietGoalViewModel : BaseViewModel
    {
        private string _mindset;
        public string Mindset
        {
            get { return _mindset; }
            set { _mindset = value; OnPropertyChanged(nameof(Mindset)); }
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

        private double _weight;
        public double Weight
        {
            get { return _weight; }
            set { _weight = value; OnPropertyChanged(nameof(Weight)); BMICalculator(); }
        }

        private double _height;
        public double Height
        {
            get { return _height; }
            set { _height = value; OnPropertyChanged(nameof(Height)); BMICalculator(); }
        }

        private double _goalWeight;
        public double GoalWeight
        {
            get { return _goalWeight; }
            set { _goalWeight = value; OnPropertyChanged(nameof(GoalWeight)); }
        }

        private double _bmi;
        public double BMI
        {
            get { return _bmi; }
            set { _bmi = value; OnPropertyChanged(nameof(BMI)); }
        }

        private DietGoal _dietGoal;
        public DietGoal DietGoal
        {
            get { return _dietGoal; }
            set { _dietGoal = value; OnPropertyChanged(nameof(DietGoal)); }
        }

        public ICommand SaveCommand { get; set; }

        public DietGoalViewModel()
        {

        }

        private async Task Initialize()
        {
            await DietGoalPrint();

            SaveCommand = new AsyncRelayCommand(DietGoalSaveData);
        }

        public static async Task<DietGoalViewModel> CreateAsync()
        {
            var instance = new DietGoalViewModel();
            await instance.Initialize();
            return instance;
        }

        public void BMICalculator()
        {
            var hM = Height / 100.0;
            BMI = (hM > 0) ? Math.Round(Weight / (hM * hM), 2) : 0;
        }

        // 목표 출력
        public async Task DietGoalPrint()
        {
            User currentUser = UserSession.Instance.CurrentUser;

            var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendDietGoalPrintOnceAsync(currentUser), getMessage: r => r.Message, userData: currentUser);

            if (res?.Ok != true)
                throw new InvalidOperationException($"server not ok: {res?.Message}");

            DietGoal = res.DietGoalData;

            Height = res.DietGoalData.Height;
            Weight = res.DietGoalData.Weight;

            BMI = res.DietGoalData.BMI;

            OnPropertyChanged(nameof(DietGoal));
        }

        // 목표 저장
        public async Task DietGoalSaveData(object parameter)
        {
            User currentUser = UserSession.Instance.CurrentUser;

            // 목표 저장
            var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendSaveDietGoalOnceAsync(currentUser, DietGoal), getMessage: r => r.Message, userData: currentUser);

            if (res?.Ok == true)
            {
                MessageBox.Show("운동 목표 설정 완료!");
            }
            else
            {
                throw new InvalidOperationException($"server not ok: {res?.Message}");
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

        // 목표 저장
        private async Task<SaveDietGoalRes> SendSaveDietGoalOnceAsync(User currentUser, DietGoal dietGoal)
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.SaveDietGoalRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "SaveDietGoal", userID = currentUser.UserId, dietGoalData = dietGoal, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.SaveDietGoal, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<SaveDietGoalRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // 목표 데이터 출력
        private async Task<DietGoalPrintRes> SendDietGoalPrintOnceAsync(User currentUser)
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.DietGoalPrintRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "DietGoalPrint", userID = currentUser.UserId, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.DietGoalPrint, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<DietGoalPrintRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // 토큰 만료
        private bool IsAuthExpired(string? message) => string.Equals(message, "expired token", StringComparison.OrdinalIgnoreCase) || string.Equals(message, "unauthorized", StringComparison.OrdinalIgnoreCase);
    }
}
