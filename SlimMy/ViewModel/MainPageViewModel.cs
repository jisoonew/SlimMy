using GalaSoft.MvvmLight.Messaging;
using MVVM2.ViewModel;
using SlimMy.Model;
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
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
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
        private Repo _repo;
        private string nickName;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";

        CommunityViewModel community = null;
        public static string myName = null;
        static TcpClient client = null;
        Thread ReceiveThread = null;
        ChattingWindowViewModel chattingWindow = null;

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

        // 회원가입
        public async Task InsertUser(object parameter)
        {
            _user.Gender = User.Gender == "남성" ? "남성" : "여성";

            // WPF 애플리케이션에서 현재 활성화된 메인 윈도우에서 이름이 "passwordBox"인 컨트롤을 찾기 위해 사용되는 메서드
            var passwordBox = Application.Current.MainWindow.FindName("passwordBox") as PasswordBox;
            var passwordCheckBox = Application.Current.MainWindow.FindName("passwordCheckBox") as PasswordBox;

            string password = passwordBox.Password;
            string passwordCheck = passwordCheckBox.Password;

            User.Password = password;
            User.PasswordCheck = passwordCheck;

            _signUp = new SignUp();

            // 유효성 검사
            if (Validator.Validator.ValidateName(User.Name) && Validator.Validator.ValidateNickName(User.NickName)
                && Validator.Validator.ValidatePassword(User.Password, User.PasswordCheck) && Validator.Validator.ValidateBirthDate(User.BirthDate) && Validator.Validator.ValidateHeight(User.Height)
                && Validator.Validator.ValidateWeight(User.Weight) && Validator.Validator.ValidateDietGoal(User.DietGoal) && _repo.BuplicateNickName(User.NickName) && SignUp.count == 1)
            {
                await _repo.InsertUser(User.Name, User.Gender, User.NickName, User.Email, User.Password, User.BirthDate, User.Height, User.Weight, User.DietGoal);
            }
            else
            {
                // 유효성 검사에 실패한 경우 처리
                MessageBox.Show("회원가입에 실패하였습니다.");
            }

            if (SignUp.count == 0)
            {
                MessageBox.Show("인증 번호가 일치하지 않습니다.");
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

            _repo = new Repo(_connstring);

            _user = new User();

            User.BirthDate = new DateTime(1990, 1, 1);

            PlannerCommand = new Command(NavigateToPlanner);

            InsertCommand = new AsyncRelayCommand(InsertUser);

            MyChatsCommand = new AsyncRelayCommand(MyChatsBtn);

            CommunityCommand = new AsyncRelayCommand(CommunityBtn);

            DashBoardCommand = new AsyncRelayCommand(DashBoardBtn);

            ExerciseHistoryCommand = new AsyncRelayCommand(NavigateToExerciseHistory);

            WeightHistoryCommand = new AsyncRelayCommand(NavigateToWeightHistory);

            MyPageCommand = new AsyncRelayCommand(NavigateToMyPage);

            LogoutCommand = new AsyncRelayCommand(LogoutBtn);
        }

        public Command PlannerCommand { get; set; }


        // 플래너 화면 전환
        private void NavigateToPlanner(object parameter)
        {
            _navigationService.NavigateToFrame(typeof(Planner));
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
            string ip = ipTextBox.Text;
            string parsedName = "%^&";

            User.Password = password;
            bool isSuccess = await _repo.LoginSuccess(UserId, password);

            View.Login login = new View.Login();

            if (isSuccess)
            {
                // 로그인 이후 사용자의 닉네임 가져오기
                string loggedInNickName = await _repo.NickName(UserId);
                Guid selectUserID = await _repo.UserID(UserId);
                parsedName += selectUserID.ToString();

                User.NickName = loggedInNickName;
                User.IpNum = ip;
                User.UserId = selectUserID;

                NickName = loggedInNickName;

                client = new TcpClient();
                await client.ConnectAsync(ip, 9999);

                byte[] byteData = Encoding.UTF8.GetBytes(parsedName);
                await client.GetStream().WriteAsync(byteData, 0, byteData.Length);

                // 싱글톤 저장
                UserSession.Instance.CurrentUser = new User
                {
                    Email = UserId,
                    NickName = User.NickName,
                    IpNum = User.IpNum,
                    UserId = User.UserId,
                    Client = client
                };

                // 로그인 시간 업데이트
                await _repo.LastLogin();

                myName = User.NickName;

                // ReceiveThread는 서버로부터 데이터를 수신하고 처리하는 역할
                // 이 스레드는 별도의 실행 경로를 가지며, 주 스레드(주로 UI 스레드)의 블로킹을 방지하여 원활한 사용자 경험을 제공
                _ = Task.Run(RecieveMessage);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var mainView = new MainHome(this); // this = MainPageViewModel 인스턴스
                    mainView.Show();
                });

                await MainHome.MainHomeLoaded.Task;

                // 로그인 창 닫기
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is View.Login)
                    {
                        window.Close();
                        break;
                    }
                }
            }
            else
            {
                MessageBox.Show("로그인에 실패했습니다. 이메일과 비밀번호를 확인해 주세요.");
                client = null;
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

            // 선택된 그룹 채팅 참여자들의 정보를 문자열
            string getUserProtocol = $"{currentUser.UserId}" + "<GiveMeUserList>";

            byte[] byteData = Encoding.UTF8.GetBytes(getUserProtocol);

            await client.GetStream().WriteAsync(byteData, 0, byteData.Length);

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
            // 서버 연결 해제
            if (UserSession.Instance.CurrentUser?.Client != null)
            {
                UserSession.Instance.CurrentUser.Client.Close();
                UserSession.Instance.CurrentUser.Client = null;
            }

            // 세션 제거
            UserSession.Instance.CurrentUser = null;

            // 현재 모든 창 닫기 (MainHome 등)
            foreach (Window window in Application.Current.Windows.OfType<Window>().ToList())
            {
                if (window is not View.Login)
                    window.Close();
            }

            // 로그인 창 새로 열기
            var loginView = new View.Login();
            var loginViewModel = new MainPageViewModel(new DataService(), loginView);
            loginView.DataContext = loginViewModel;

            Application.Current.MainWindow = loginView;
            loginView.Show();
        }


        // 사용자 채팅
        public async Task RecieveMessage()
        {
            List<string> receiveMessageList = new List<string>();
            while (true)
            {
                try
                {
                    byte[] receiveByte = new byte[1024];
                    int bytesRead = await client.GetStream().ReadAsync(receiveByte, 0, receiveByte.Length);

                    if (bytesRead == 0) continue;

                    string receiveMessage = Encoding.UTF8.GetString(receiveByte).Trim();

                    string[] receiveMessageArray = receiveMessage.Split('>');

                    // receiveMessageArray => "관리자<TEST"
                    foreach (var item in receiveMessageArray)
                    {
                        if (!item.Contains('<'))
                            continue;
                        if (item.Contains("관리자<TEST"))
                            continue;

                        receiveMessageList.Add(item);
                    }

                    await ParsingReceiveMessage(receiveMessageList);
                }
                catch (Exception e)
                {
                    // MessageBox.Show("서버와의 연결이 끊어졌습니다.", "Server Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    // MessageBox.Show(e.Message);
                    // MessageBox.Show(e.StackTrace);
                    Environment.Exit(1);
                }
                await Task.Delay(500);
            }
        }

        // 클라이언트가 받은 메시지를 분석 및 처리
        private async Task ParsingReceiveMessage(List<string> messageList)
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

                    // chattingPartner -> 사용자 아이디 출력
                    //MessageBox.Show("chattingPartner : " + chattingPartner);

                    // message -> <> 안에 있는 내용 출력
                    // MessageBox.Show("message : " + message);

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
                    if (message.Contains("GroupChattingUserStart"))
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
                    if (chattingPartner.Contains("+"))
                    {
                        await HandleUserBundleChanged(chattingPartner, message);

                        // 처리한 메시지 리스트를 비우기
                        messageList.Clear();
                        return;
                    }

                    // 방장 위임
                    if (message.Contains("HostChanged"))
                    {
                        await HandleHostChanged(chattingPartner, message);

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
            string sender = chattingPartners[1];

            // 현재 사용자가 채팅방을 실행하고 있는지 확인
            // 채팅방을 실행하고 있다면 "사용자 아이디:채팅방 아이디" 리턴
            string chattingRoomNum = GetChattingRoomNumTest(chattingPartners[0]);

            Debug.WriteLine($"[CLIENT] GetChattingRoomNumTest returned: {chattingRoomNum}");

            // 사용자가 해당 채팅방을 실행하고 있지 않다면
            if (chattingRoomNum == "-1")
            {
                ChatRooms currentChatRoom = ChattingSession.Instance.CurrentChattingData;

                if (currentChatRoom == null)
                {
                    Debug.WriteLine("[ERROR] 채팅방 데이터가 없습니다.");
                    return;
                }

                // 메인 화면 로딩 완료 대기
                await MainHome.MainHomeLoaded.Task;

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Thread groupChattingThread = new Thread(async () =>
                    {
                        await ThreadStartingPoint(currentChatRoom.ChatRoomId.ToString(), chattingPartners[0]);
                    });
                    groupChattingThread.SetApartmentState(ApartmentState.STA);
                    groupChattingThread.IsBackground = true;
                    groupChattingThread.Start();
                }, DispatcherPriority.ContextIdle);
            }
            else
            {
                if (chattingThreadDic.ContainsKey(chattingRoomNum) &&
                    chattingThreadDic[chattingRoomNum].chattingThread.IsAlive)
                {

                    await chattingThreadDic[chattingRoomNum].chattingWindow.ReceiveAddRoomMessage(sender, message);
                }
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
            string[] hostChangedPartner = chattingPartner.Split(":");
            List<string> hostChangedList = hostChangedPartner.Where(el => !string.IsNullOrEmpty(el)).ToList();

            string hostChangedChattingRoomNum = GetHostChangedChattingRoomNum(hostChangedList);

            if (hostChangedChattingRoomNum != "-1")
            {
                if (chattingThreadDic.ContainsKey(hostChangedChattingRoomNum) &&
                    chattingThreadDic[hostChangedChattingRoomNum].chattingThread.IsAlive)
                {
                    await chattingThreadDic[hostChangedChattingRoomNum].chattingWindow.ReceiveHostChangedMessage(hostChangedList, message);
                }
            }
        }

        private async Task HandleUserBundleChanged(string chattingPartner, string message)
        {
            var parts = chattingPartner.Split('+', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4) return;

            string roomId = parts[0];
            string messageContent = parts[1];
            string sender = parts[3];

            // 지금부터 쭉 같은 키를 씁니다
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
                var vm = new ChattingWindowViewModel(client, chattingPartnersBundle);
                var win = new ChattingWindow { DataContext = vm };

                // 닫힐 때도 동일한 chatKey 로 제거
                win.Closed += async (s, e) =>
                {
                    chattingThreadDic.Remove(chatKey);
                    chatWindowReadyMap.Remove(chatKey);
                    // var leaveMsg = $"{chatroomID}:{userId}<leaveRoom>";

                    string leaveRoomData = $"{chatroomID}:{userId}";
                    byte[] leaveRoomDataByte = Encoding.UTF8.GetBytes(leaveRoomData);

                    int leavePayLoad = 1 + leaveRoomData.Length;
                    byte[] leavePayLoadData = BitConverter.GetBytes(leavePayLoad);

                    byte leaveMsgType = (byte)MessageType.UserLeaveRoom;

                    await client.GetStream().WriteAsync(leavePayLoadData, 0, leavePayLoadData.Length);

                    await client.GetStream().WriteAsync(new byte[] { leaveMsgType }, 0, 1);

                    await client.GetStream().WriteAsync(leaveRoomDataByte, 0, leaveRoomDataByte.Length);

                    await client.GetStream().FlushAsync();

                    //var data = Encoding.UTF8.GetBytes(leaveMsg);
                    //await client.GetStream().WriteAsync(data, 0, data.Length);
                };

                // 열 때, 동일한 chatKey 로 저장
                chattingThreadDic[chatKey] = new ChattingThreadData(Thread.CurrentThread, vm, chatKey);
                win.Show();

                win.Loaded += (s2, e2) => readyTcs.TrySetResult(true);
            }, DispatcherPriority.ContextIdle);

            await readyTcs.Task;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}