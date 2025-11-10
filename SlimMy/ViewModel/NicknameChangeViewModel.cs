using SlimMy.Model;
using SlimMy.Repository;
using SlimMy.Response;
using SlimMy.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SlimMy.ViewModel
{
    public class NicknameChangeViewModel : BaseViewModel
    {
        private UserRepository _repo;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";

        // 화면 전환
        private INavigationService _navigationService;

        private Guid _userID;
        public Guid UserID
        {
            get { return _userID; }
            set { _userID = value; OnPropertyChanged(nameof(UserID)); }
        }

        private string _newNickname;
        public string NewNickname
        {
            get => _newNickname;
            set
            {
                _newNickname = value;
                OnPropertyChanged(nameof(NewNickname));
            }
        }

        // 닉네임 성공 메시지
        private bool _nickNameCheck;
        public bool NickNameCheck
        {
            get { return _nickNameCheck; }
            set { _nickNameCheck = value; OnPropertyChanged(nameof(NickNameCheck)); }
        }

        // 닉네임 실패 메시지
        private bool _nickNameNoCheck;
        public bool NickNameNoCheck
        {
            get { return _nickNameNoCheck; }
            set { _nickNameNoCheck = value; OnPropertyChanged(nameof(NickNameNoCheck)); }
        }

        // 닉네임 중복 확인
        public ICommand CheckNicknameCommand { get; set; }

        // 닉네임 저장
        public ICommand SaveCommand { get; set; }

        public NicknameChangeViewModel(string initialNickname)
        {
            NewNickname = initialNickname;
        }

        public NicknameChangeViewModel() { }

        private async Task Initialize()
        {
            _repo = new UserRepository(_connstring); // Repo 초기화
            _navigationService = new NavigationService();

            CheckNicknameCommand = new AsyncRelayCommand(NickNameCheckPrint);

            SaveCommand = new AsyncRelayCommand(NickNameSave);
        }

        public static async Task<NicknameChangeViewModel> CreateAsync()
        {
            try
            {
                var vm = new NicknameChangeViewModel();
                await vm.Initialize();
                return vm;
            }
            catch (Exception ex)
            {
                MessageBox.Show("NicknameChangeViewModel 생성 실패: " + ex.Message);
                return null;
            }
        }

        // 닉네임 여부
        public async Task NickNameCheckPrint(object parameter)
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.NickNameCheckPrintRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "NickNameCheckPrint"};
            await transport.SendFrameAsync((byte)MessageType.NickNameCheckPrint, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            var res = JsonSerializer.Deserialize<NickNameCheckPrintRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (res?.ok != true)
                throw new InvalidOperationException($"server not ok: {res?.message}");

            bool isDuplicate = res.nickNames.Any(name => name.Equals(NewNickname));

            // 닉네임 중복
            if (isDuplicate)
            {
                NickNameCheck = false;
                NickNameNoCheck = true;
            }
            else
            {
                if (Validator.Validator.ValidateNickName(NewNickname))
                {
                    NickNameCheck = true;
                    NickNameNoCheck = false;
                }
            }
        }

        // 닉네임 지정
        public async Task NickNameSave(object parameter)
        {
            User userData = UserSession.Instance.CurrentUser;

            string msg = string.Format("'{0}'로 닉네임을 변경하시겠습니까?", NewNickname);
            MessageBoxResult messageBoxResult = MessageBox.Show(msg, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (messageBoxResult == MessageBoxResult.No)
            {
                return;
            }
            else
            {
                // 닉네임 중복
                if (NickNameNoCheck)
                {
                    return;
                }

                // 닉네임 지정
                if (NickNameCheck)
                {
                    await _repo.NickNameSave(userData.UserId, NewNickname);

                    var session = UserSession.Instance;
                    var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

                    var reqId = Guid.NewGuid();

                    var waitTask = session.Responses.WaitAsync(MessageType.NickNameSaveRes, reqId, TimeSpan.FromSeconds(5));

                    var req = new { cmd = "NickNameSave", userID = userData.UserId, userNickName = NewNickname };
                    await transport.SendFrameAsync((byte)MessageType.NickNameSave, JsonSerializer.SerializeToUtf8Bytes(req));

                    var respPayload = await waitTask;

                    var res = JsonSerializer.Deserialize<NickNameSaveRes>(
                        respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (res?.ok != true)
                        throw new InvalidOperationException($"server not ok: {res?.message}");

                    // 닉네임 화면 닫기
                    await _navigationService.NavigateToNickNameClose();
                }
            }
        }
    }
}
