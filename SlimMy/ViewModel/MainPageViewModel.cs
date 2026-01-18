using GalaSoft.MvvmLight.Messaging;
using MVVM2.ViewModel;
using SlimMy.Model;
using SlimMy.Response;
using SlimMy.Service;
using SlimMy.Singleton;
using SlimMy.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace SlimMy.ViewModel
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private User _user;
        private string _username;
        private string nickName;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";

        CommunityViewModel community = null;
        public static string myName = null;
        static TcpClient client = null;
        Thread ReceiveThread = null;
        ChattingWindowViewModel chattingWindow = null;

        private CancellationTokenSource _recvCts;

        public SslStream _ssl;

        private INavigationService _navigationService;

        // 테스트 코드
        Dictionary<string, ChattingThreadData> chattingThreadDic = new Dictionary<string, ChattingThreadData>();

        // 여성 혹은 남성중 어떤 선택을 할 것인지
        private bool _isMaleChecked;
        private bool _isFemaleChecked;

        // 이벤트 정의: 로그인 성공 시 발생하는 이벤트
        public event EventHandler<ChatUserList> DataPassed; // 데이터 전달을 위한 이벤트 정의

        private SignUp _signUp;

        List<User> UserList = new List<User>();

        // 연결 확인 쓰레드
        Task conntectCheckThread = null;

        public AsyncRelayCommand InsertCommand { get; set; }

        public User User
        {
            get { return _user; }
            set { _user = value; OnPropertyChanged(nameof(User)); }
        }

        private string _nickName;
        public string NickName
        {
            get { return _nickName; }
            set { _nickName = value; OnPropertyChanged(nameof(NickName)); }
        }

        public ICommand NavigateToCommunityCommand { get; }

        private string _receivedUserName;
        public string ReceivedUserName
        {
            get { return _receivedUserName; }
            set
            {
                if (_receivedUserName != value)
                {
                    _receivedUserName = value;
                    OnPropertyChanged(nameof(ReceivedUserName));
                }
            }
        }

        private ICommand saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                return saveCommand ?? (this.saveCommand = new DelegateCommand(SaveUser));
            }
        }

        private string _userId;

        public string UserId
        {
            get => _userId;
            set
            {
                _userId = value;
                OnPropertyChanged(nameof(UserId));
            }
        }

        //public ICommand LoginCommand { get; }
        public Command NickNameCommand { get; set; }
        public AsyncRelayCommand CommunityBtnCommand { get; set; }
        public AsyncRelayCommand MyChatsCommand { get; set; }
        public AsyncRelayCommand CommunityCommand { get; set; }
        public AsyncRelayCommand DashBoardCommand { get; set; }
        public AsyncRelayCommand ExerciseHistoryCommand { get; set; }
        public AsyncRelayCommand WeightHistoryCommand { get; set; }
        public AsyncRelayCommand MyPageCommand { get; set; }
        public AsyncRelayCommand LogoutCommand { get; set; }

        private CommunityViewModel _communityViewModel; // Community ViewModel 인스턴스 추가

        private void SaveUser()
        {
            User user = new User
            {
                Email = User.Email
            };

            UserList.Add(user);

            OnPropertyChanged("UserAdded");
        }

        // 로그인 성공 시 호출되는 메서드 예시
        public void LoginSuccessfulPage(string userEmail)
        {
            // 여기서 사용자 정보를 설정하고 필요한 데이터를 가져올 수 있습니다.
            User = new User
            {
                Email = userEmail
            };
        }

        // 공인 IP 주소 가져오기
        public static string GetPublicIP()
        {
            string url = "http://checkip.amazonaws.com/";
            string publicIp = "";

            try
            {
                WebRequest request = WebRequest.Create(url);
                using (WebResponse response = request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    publicIp = reader.ReadToEnd().Trim();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error retrieving public IP address: " + ex.Message);
            }

            return publicIp;
        }

        private string _textData;

        public string TextData
        {
            get { return _textData; }
            set
            {
                _textData = value;
                OnPropertyChanged(nameof(TextData));
            }
        }

        public bool IsMaleChecked
        {
            get { return _isMaleChecked; }
            set
            {
                _isMaleChecked = value;
                OnPropertyChanged(nameof(IsMaleChecked));
                if (value)
                    User.Gender = "남성"; // 선택된 경우 User의 Gender 값을 업데이트합니다.
            }
        }

        public bool IsFemaleChecked
        {
            get { return _isFemaleChecked; }
            set
            {
                _isFemaleChecked = value;
                OnPropertyChanged(nameof(IsFemaleChecked));
                if (value)
                    User.Gender = "여성"; // 선택된 경우 User의 Gender 값을 업데이트합니다.
            }
        }

        private readonly IDataService _dataService;
        private readonly IView _view;

        public ICommand LoginCommand { get; }

        public MainPageViewModel(IDataService dataService, IView view)
        {
            _dataService = dataService;
            _view = view;
            LoginCommand = new AsyncRelayCommand(LoginSuccess, CanLogin);

            _user = User;

            _user = new User();

            User.BirthDate = new DateTime(1990, 1, 1);

            PlannerCommand = new AsyncRelayCommand(NavigateToPlanner);

            MyChatsCommand = new AsyncRelayCommand(MyChatsBtn);

            CommunityCommand = new AsyncRelayCommand(CommunityBtn);

            DashBoardCommand = new AsyncRelayCommand(DashBoardBtn);

            ExerciseHistoryCommand = new AsyncRelayCommand(NavigateToExerciseHistory);

            WeightHistoryCommand = new AsyncRelayCommand(NavigateToWeightHistory);

            MyPageCommand = new AsyncRelayCommand(NavigateToMyPage);

            LogoutCommand = new AsyncRelayCommand(LogoutBtn);
        }

        public AsyncRelayCommand PlannerCommand { get; set; }


        // 플래너 화면 전환
        private async Task NavigateToPlanner(object parameter)
        {
            await _navigationService.NavigateToPlannerFrameAsync(typeof(Planner));
        }

        // 몸무게 내역
        public async Task NavigateToExerciseHistory(object parameter)
        {
            await _navigationService.NavigateToExerciseHistoryFrameAsync(typeof(ExerciseHistory));
        }

        // 내 정보 수정
        public async Task NavigateToMyPage(object parameter)
        {
            await _navigationService.NavigateToMyPageFrameAsync(typeof(MyPage));
        }

        // 몸무게
        public async Task NavigateToWeightHistory(object parameter)
        {
            await _navigationService.NavigateToWeightHistoryFrameAsync(typeof(WeightHistory));
        }

        // 프레임 지정
        public void SetNavigationService(NavigationService navService)
        {
            _navigationService = navService;
        }

        // 로그인
        private async Task LoginSuccess(object parameter)
        {
            var passwordBox = Application.Current.MainWindow.FindName("passwordBox") as PasswordBox;
            var ipTextBox = Application.Current.MainWindow.FindName("IpTextBox") as TextBox;
            string password = passwordBox.Password;
            var ip = IPAddress.Parse(ipTextBox.Text);
            var localEndPoint = new IPEndPoint(ip, 0);

            string host = "localhost"; // 개발용: cert의 CN/SAN과 일치해야 함
            int port = 9999;

            var transport = new TlsTcpTransport();
            await transport.ConnectAsync(host, port);

            string email = UserId; // 입력된 아이디(이메일) 바인딩 값
            string passwordWD = passwordBox?.Password ?? "";

            var hub = UserSession.Instance.Responses;
            // hub.StartReceiveLoopIfNeeded(transport);

            _recvCts?.Cancel();
            _recvCts = new CancellationTokenSource();
            _ = Task.Run(() => RecieveMessage(transport, _recvCts.Token));

            var reqId = Guid.NewGuid();

            var wait = hub.WaitAsync(MessageType.UserLoginRes, reqId, TimeSpan.FromSeconds(5));

            var loginReq = new { cmd = "LOGIN", email = email, password = passwordWD, requestID = reqId };
            await transport.SendFrameAsync(MessageType.UserLogin, JsonSerializer.SerializeToUtf8Bytes(loginReq));

            var payload = await wait;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var loginRes = JsonSerializer.Deserialize<LoginReply>(payload, opts);

            var isInactive = string.Equals(loginRes?.status, "INACTIVE", StringComparison.OrdinalIgnoreCase);
            var hasStatus = !string.IsNullOrWhiteSpace(loginRes?.status);

            if (loginRes?.ok == true && hasStatus && !isInactive)
            {
                NickName = loginRes.nick;
                User.Password = password;

                // 성공 처리: 세션 저장, 수신 루프 시작 등
                UserSession.Instance.CurrentUser = new User
                {
                    Email = email,
                    UserId = loginRes.userId,
                    NickName = loginRes.nick,
                    Transport = transport
                };

                UserSession.Instance.AccessToken = loginRes.accessTokenID;

                // 이후 모든 송수신은 ssl을 사용
                // _ = Task.Run(() => RecieveMessage(transport));

                var reqNotifiedId = Guid.NewGuid();

                var waitNotified = hub.WaitAsync(MessageType.NotifiedCheckRes, reqNotifiedId, TimeSpan.FromSeconds(5));

                // 받지 못한 방출 메시지 출력
                var loginNotifiedReq = new { cmd = "NotifiedCheck", userId = UserSession.Instance.CurrentUser.UserId, accessToken = UserSession.Instance.AccessToken, requestID = reqNotifiedId };
                await transport.SendFrameAsync(MessageType.NotifiedCheck, JsonSerializer.SerializeToUtf8Bytes(loginNotifiedReq));

                var payNotifiedload = await waitNotified;
                var notifiedRes = JsonSerializer.Deserialize<NotifiedCheckRes>(payNotifiedload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    var mainView = new MainHome(this);
                    Application.Current.MainWindow = mainView;
                    mainView.Show();

                    if(notifiedRes.NotifiedCheck != null)
                    {
                        foreach (var msg in notifiedRes.NotifiedCheck)
                        {
                            MessageBox.Show("방장에 의해 채팅방에서 퇴장되었습니다.\n" + msg.ChatRoomName + "\n" + msg.BanAt);

                            // CHATROOM_BAN 테이블 ISNOTIFIED 1로 update하는게 서버에 요청
                            User userData = UserSession.Instance.CurrentUser;

                            var selectChatRoomRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendBanIsNotifiedOnceAsync(userData, msg.BanID.ToString()), getMessage: r => r.Message, userData: userData);

                            if (selectChatRoomRes?.Ok != true)
                                throw new InvalidOperationException($"server not ok: {selectChatRoomRes?.Message}");
                        }
                    }

                    // 떠 있는 로그인 창 닫기
                    var loginWindow = Application.Current.Windows
                        .OfType<View.Login>()
                        .FirstOrDefault();
                    loginWindow?.Close();
                });
            }
            else
            {
                var msg = loginRes?.ok != true ? (loginRes?.message ?? "로그인 실패") : !hasStatus ? "계정 상태를 확인할 수 없습니다. 다시 시도해주세요." : "탈퇴한 계정은 로그인할 수 없습니다.";

                MessageBox.Show(msg);
                _recvCts?.Cancel();
                transport.Dispose();
                return;
            }
        }

        private bool CanLogin(object parameter)
        {
            return true;  // 항상 true로 설정하여 버튼이 활성화되도록 함
        }

        // 커뮤니티 버튼 기능
        public async Task CommunityBtn(object parameter)
        {
            User currentUser = UserSession.Instance.CurrentUser;

            await _navigationService.NavigateToCommunityFrameAsync(typeof(View.Community));
        }

        public async Task MyChatsBtn(object parameter)
        {
            await _navigationService.NavigateToFrameAsync(typeof(View.MyChats));
        }

        public async Task DashBoardBtn(object parameter)
        {
            await _navigationService.NavigateToDashBoardFrameAsync(typeof(View.DashBoard));
        }

        // 로그아웃
        public async Task LogoutBtn(object parameter)
        {
            _recvCts?.Cancel();

            // 연결 해제
            UserSession.Instance.CurrentUser?.Client?.Close();
            if (UserSession.Instance.CurrentUser != null)
                UserSession.Instance.CurrentUser.Client = null;
            UserSession.Instance.CurrentUser = null;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // 로그인 창 먼저 띄우기
                var loginView = new View.Login();
                loginView.DataContext = new MainPageViewModel(new DataService(), loginView);
                Application.Current.MainWindow = loginView;
                loginView.Show();

                // 기존 창 닫기 (MainHome는 IsLoggingOut=true 세팅 후 닫기)
                foreach (Window w in Application.Current.Windows.OfType<Window>().ToList())
                {
                    if (ReferenceEquals(w, loginView)) continue;

                    if (w is View.MainHome mh)
                        mh.IsLoggingOut = true;

                    w.Close();
                }
            });
        }


        // 사용자 채팅
        public async Task RecieveMessage(INetworkTransport transport, CancellationToken ct = default)
        {
            Debug.WriteLine("[RECV] loop start");
            try
            {
                var hub = UserSession.Instance.Responses;

                while (!ct.IsCancellationRequested)
                {
                    var (rawType, payload) = await transport.ReadFrameAsync(ct);
                    var type = (MessageType)rawType;
                    var text = Encoding.UTF8.GetString(payload).Trim();

                    Debug.WriteLine($"[RECV] type={type}({rawType}), len={payload.Length}, text='{text}'");

                    var reqId = TryExtractRequestId(payload);
                    if (hub.TryResolve(type, reqId, payload))
                        continue; // 내 요청의 응답이면 여기서 끝

                    var list = new List<string>();


                    if (type == MessageType.Heartbeat) continue; // 하트비트 페이로드는 스킵

                    foreach (var seg in text.Split('>'))
                    {
                        if (!seg.Contains('<')) continue;
                        list.Add(seg);
                    }

                    if (list.Count > 0)
                        _ = Task.Run(() => ParsingReceiveMessage(list, type));
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // 로그아웃/종료로 인한 정상 취소
                Debug.WriteLine("[RECV] canceled (normal shutdown).");
            }
            catch (ObjectDisposedException)
            {
                // 소켓/스트림 닫힘
                Debug.WriteLine("[RECV] disposed (normal shutdown).");
            }
            catch (IOException ioEx)
            {
                // 네트워크 단절
                Debug.WriteLine($"[RECV] IO error: {ioEx.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RECV] exception: {ex}");
            }
        }

        // 클라이언트가 받은 메시지를 분석 및 처리
        private async Task ParsingReceiveMessage(List<string> messageList, MessageType type)
        {
            foreach (var item in messageList)
            {
                // Debug.WriteLine($"[CLIENT] Received Message: {item}");

                string chattingPartner = "";
                string message = "";

                // 메시지가 '<' 문자를 포함하는 경우 처리
                if (item.Contains('<'))
                {
                    string[] splitedMsg = item.Split('<');

                    // 수신자와 메시지를 추출
                    chattingPartner = splitedMsg[0]; // "관리자"
                    message = splitedMsg[1]; // "TEST"

                    // 관리자가 보낸 하트비트 메시지인 경우
                    if (chattingPartner == "관리자")
                    {
                        // 사용자 목록을 업데이트
                        ObservableCollection<ChatUserList> tempUserList = new ObservableCollection<ChatUserList>();
                        string[] splitedUser = message.Split('$');

                        foreach (var el in splitedUser)
                        {
                            if (string.IsNullOrEmpty(el))
                                continue;

                            tempUserList.Add(new ChatUserList(el, ""));
                        }

                        // 사용자 목록을 출력하기 위한 ChangeUserListView에 데이터 전송
                        await Application.Current.Dispatcher.InvokeAsync(async () =>
                        {
                            await CommunityViewModel.ChangeUserListView(tempUserList);
                        });

                        // 처리한 메시지 리스트를 비우기
                        messageList.Clear();
                        return;
                    }

                    // 그룹채팅
                    // Contains 해당 문자열에 "#"가 포함되어 있는지 확인 true or false
                    // 문자열을 # 문자를 기준으로 나누는 메서드
                    if (type == MessageType.UserJoinChatRoom)
                    {
                        await HandleGroupChattingUserStart(chattingPartner, message);

                        // 처리한 메시지 리스트를 비우기
                        messageList.Clear();
                        return;
                    }

                    // 사용자가 채팅방을 나가게 된다면
                    if (message.Contains("leaveRoom"))
                    {
                        await HandleLeaveRoom(chattingPartner, message);

                        // 처리한 메시지 리스트를 비우기
                        messageList.Clear();
                        return;
                    }

                    // 사용자 메시지 송수신
                    if (type == MessageType.ChatContent)
                    {
                        await HandleUserBundleChanged(chattingPartner, message);

                        // 처리한 메시지 리스트를 비우기
                        messageList.Clear();
                        return;
                    }

                    // 방장 위임
                    if (type == MessageType.HostChanged)
                    {
                        await HandleHostChanged(chattingPartner, message);

                        // 처리한 메시지 리스트를 비우기
                        messageList.Clear();
                        return;
                    }

                    // 방출 메시지
                    if (type == MessageType.RoomBanMessageRes)
                    {
                        await HandleBanMessage(chattingPartner, message);

                        // 처리한 메시지 리스트를 비우기
                        messageList.Clear();
                        return;
                    }
                }
            }
            messageList.Clear();
        }

        // 사용자 채팅방 참여 메시지
        private async Task HandleGroupChattingUserStart(string chattingPartner, string message)
        {
            Debug.WriteLine($"[CLIENT] Received GroupChattingUserStart from {chattingPartner} with message: {message}");

            string[] splitedChattingPartner = chattingPartner.Split("#");

            // 채팅방 아이디와 사용자 아이디 담기
            List<string> chattingPartners = splitedChattingPartner.Where(el => !string.IsNullOrEmpty(el)).ToList();

            // 사용자 아이디
            var roomId = chattingPartners[0];
            var joinedUserId = chattingPartners[1];

            // 현재 사용자가 채팅방을 실행하고 있는지 확인
            // 채팅방을 실행하고 있다면 "사용자 아이디:채팅방 아이디" 리턴
            var chatKey = GetChattingRoomNumTest(roomId);
            Debug.WriteLine($"[CLIENT] GetChattingRoomNumTest returned: {chatKey}");

            // 사용자가 해당 채팅방을 실행하고 있지 않다면
            if (chatKey == "-1")
            {
                await MainHome.MainHomeLoaded.Task;
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await ThreadStartingPoint(roomId, chattingPartner); // 세션 말고 roomId 사용
                }, DispatcherPriority.Normal);
            }
            else if (chattingThreadDic.TryGetValue(chatKey, out var data) && data.chattingThread.IsAlive)
            {
                await data.chattingWindow.ReceiveAddRoomMessage(joinedUserId, message);
            }
        }

        private async Task HandleLeaveRoom(string chattingPartner, string message)
        {
            string[] splitedChattingPartner = chattingPartner.Split(":");
            List<string> chattingPartners = splitedChattingPartner.Where(el => !string.IsNullOrEmpty(el)).ToList();

            string sender = chattingPartners[1];
            string messageContent = message;

            string chattingRoomNum = GetChattingRoomNumTest(chattingPartners[0]);

            if (chattingRoomNum != "-1")
            {
                if (chattingThreadDic.ContainsKey(chattingRoomNum) &&
                    chattingThreadDic[chattingRoomNum].chattingThread.IsAlive)
                {
                    await chattingThreadDic[chattingRoomNum].chattingWindow.ReceiveMessage(sender, messageContent);
                }
            }
        }

        private async Task HandleHostChanged(string chattingPartner, string message)
        {
            var parts = chattingPartner.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return;

            var roomId = parts[0]; // 방 ID
            var newHostUserId = parts[1]; // 새 방장 사용자 ID

            var currentUserId = UserSession.Instance.CurrentUser.UserId.ToString();
            var chatKey = $"{currentUserId}:{roomId}";

            if (chattingThreadDic.TryGetValue(chatKey, out var data) && data.chattingThread.IsAlive)
            {
                await data.chattingWindow.ReceiveHostChangedMessage(parts.ToList(), message);
                Debug.WriteLine($"[HOST_CHANGED] delivered -> chatKey={chatKey}");
            }
            else
            {
                Debug.WriteLine($"[HOST_CHANGED] window not found -> chatKey={chatKey}");
            }
        }
        
        // 방출 메시지
        private async Task HandleBanMessage(string chattingPartner, string message)
        {
            User currentUser = UserSession.Instance.CurrentUser;

            var parts = chattingPartner.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return;

            var roomId = parts[0]; // 방 ID
            var newHostUserId = parts[1]; // 방출 사용자 ID
            var banId = parts[2];

            await MainHome.MainHomeLoaded.Task;
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await CloseChatRoomAsync(roomId);
            }, DispatcherPriority.Normal);

            MessageBox.Show(currentUser.NickName + " 방출되었습니다.");

            // CHATROOM_BAN 테이블 ISNOTIFIED 1로 update하는게 서버에 요청
            User userData = UserSession.Instance.CurrentUser;

            var selectChatRoomRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendBanIsNotifiedOnceAsync(userData, banId), getMessage: r => r.Message, userData: userData);

            if (selectChatRoomRes?.Ok != true)
                throw new InvalidOperationException($"server not ok: {selectChatRoomRes?.Message}");
        }

        private async Task HandleUserBundleChanged(string chattingPartner, string message)
        {
            var parts = chattingPartner.Split('+', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4) return;

            string roomId = parts[0];
            string messageContent = parts[1];
            string sender = parts[3];

            var userId = UserSession.Instance.CurrentUser.UserId.ToString();
            var chatKey = $"{userId}:{roomId}";

            if (chattingThreadDic.TryGetValue(chatKey, out var data) && data.chattingThread.IsAlive)
                await data.chattingWindow.ReceiveMessage(sender, messageContent);

            // 아직 창이 열려 있지 않으면
            if (!chattingThreadDic.ContainsKey(chatKey))
            {
                var t = new Thread(async () => await ThreadStartingPoint(roomId, string.Join(",", parts)));
                t.SetApartmentState(ApartmentState.STA);
                t.IsBackground = true;
                t.Start();
                return;
            }
        }

        // 테스트 코드
        private string GetChattingRoomNumTest(string chatRoomId)
        {
            string currentUserId = UserSession.Instance.CurrentUser.UserId.ToString();
            string reqKey = $"{currentUserId}:{chatRoomId}";

            return chattingThreadDic.ContainsKey(reqKey) ? reqKey : "-1";
        }

        private Dictionary<string, View.ChattingWindow> chattingWindows = new Dictionary<string, View.ChattingWindow>();

        private string GetHostChangedChattingRoomNum(List<string> chattingPartners)
        {
            string reqMember = $"{string.Join(",", chattingPartners[0])}";

            foreach (var item in chattingThreadDic)
            {
                string originMember = item.Value.chattingRoomNumStr;
                // 채팅 방 번호가 요청한 채팅 방 멤버와 일치하는지 확인
                if (originMember == reqMember)
                    return item.Value.chattingRoomNumStr; // 일치하는 채팅 방 번호를 반환
            }

            // 일치하는 채팅 방 번호가 없는 경우 -1을 반환
            return "-1";
        }

        private readonly Dictionary<string, TaskCompletionSource<bool>> chatWindowReadyMap = new();

        // 채팅창을 생성하면서 각 채팅창의 키 값을 부여하여 사용자끼리의 소통이 가능하게 함
        private async Task ThreadStartingPoint(string chatroomID, string chattingPartnersBundle)
        {
            var userId = UserSession.Instance.CurrentUser.UserId.ToString();
            var chatKey = $"{userId}:{chatroomID}";

            // 이미 한 번 열어 놓았으면 바로 리턴
            if (chatWindowReadyMap.ContainsKey(chatKey))
                return;

            var readyTcs = new TaskCompletionSource<bool>();
            chatWindowReadyMap[chatKey] = readyTcs;

            await MainHome.MainHomeLoaded.Task;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    var roomId = Guid.Parse(chatroomID);
                    var vm = new ChattingWindowViewModel(UserSession.Instance.CurrentUser?.Transport, roomId, chattingPartnersBundle);
                    var win = new ChattingWindow { DataContext = vm };

                    // 닫힐 때도 동일한 chatKey 로 제거
                    win.Closed += async (s, e) =>
                    {
                        chattingThreadDic.Remove(chatKey);
                        chatWindowReadyMap.Remove(chatKey);

                        var transport = UserSession.Instance.CurrentUser?.Transport;

                        string leaveRoomData = $"{chatroomID}:{userId}";
                        byte[] leaveRoomDataByte = Encoding.UTF8.GetBytes(leaveRoomData);

                        await transport.SendFrameAsync(MessageType.UserLeaveRoom, leaveRoomDataByte);
                    };

                    // 열 때, 동일한 chatKey 로 저장
                    chattingThreadDic[chatKey] = new ChattingThreadData(Thread.CurrentThread, vm, chatKey, win);

                    win.Loaded += (s2, e2) => readyTcs.TrySetResult(true);
                    win.Show();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ChatWindow.Create] {ex}");
                    readyTcs.TrySetException(ex);
                    chatWindowReadyMap.Remove(chatKey);
                }
            }, DispatcherPriority.ContextIdle);

            await readyTcs.Task;
        }

        // 채팅방 닫기
        private async Task CloseChatRoomAsync(string chatroomID)
        {
            var userId = UserSession.Instance.CurrentUser.UserId.ToString();
            var chatKey = $"{userId}:{chatroomID}";

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (!chattingThreadDic.TryGetValue(chatKey, out var data))
                {
                    Debug.WriteLine($"[CloseChatRoomAsync] not found: ");
                    return;
                }

                data.Window.Close();

                chattingThreadDic.Remove(chatKey);
                chatWindowReadyMap.Remove(chatKey);
            });
        }

        private static Guid TryExtractRequestId(byte[] payload)
        {
            if (payload == null || payload.Length < 2) return Guid.Empty;
            byte first = payload[0], last = payload[^1];
            if (first != (byte)'{' || last != (byte)'}') return Guid.Empty;

            if (payload.Length == 0) return Guid.Empty;
            if (payload[0] != '{') return Guid.Empty;

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(payload);
                if (doc.RootElement.TryGetProperty("requestID", out var p) &&
                    p.ValueKind == System.Text.Json.JsonValueKind.String &&
                    Guid.TryParse(p.GetString(), out var g))
                    return g;
            }
            catch (System.Text.Json.JsonException) { }
            return Guid.Empty;
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

        // 방출 메시지 출력 완료
        private async Task<BanIsNotifiedRes?> SendBanIsNotifiedOnceAsync(User userData, string banID)
        {
            var session = UserSession.Instance;

            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.BanIsNotifiedRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { Cmd = "BanIsNotified", userID = session.CurrentUser.UserId, banID = banID, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.BanIsNotified, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<BanIsNotifiedRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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

        // 토큰 만료
        private bool IsAuthExpired(string? message) => string.Equals(message, "expired token", StringComparison.OrdinalIgnoreCase) || string.Equals(message, "unauthorized", StringComparison.OrdinalIgnoreCase);

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}