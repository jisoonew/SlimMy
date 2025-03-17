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
    public class MainPage : INotifyPropertyChanged
    {
        private User _user;
        private string _username;
        private Repo _repo;
        private string nickName;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";

        Community community = null;
        public static string myName = null;
        static TcpClient client = null;
        Thread ReceiveThread = null;
        ChattingWindow chattingWindow = null;
        Dictionary<string, ChattingThreadData> chattingThreadDic = new Dictionary<string, ChattingThreadData>();
        Dictionary<int, ChattingThreadData> groupChattingThreadDic = new Dictionary<int, ChattingThreadData>();

        private readonly INavigationService _navigationService;

        // 테스트 코드
        Dictionary<string, ChattingThreadData> groupChattingThreadDicTest = new Dictionary<string, ChattingThreadData>();

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

        //public ICommand LoginCommand { get; }
        public Command NickNameCommand { get; set; }
        public AsyncRelayCommand CommunityBtnCommand { get; set; }
        public AsyncRelayCommand MyChatsCommand { get; set; }
        public AsyncRelayCommand CommunityCommand { get; set; }
        public AsyncRelayCommand DashBoardCommand { get; set; }

        private Community _communityViewModel; // Community ViewModel 인스턴스 추가

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

        public ICommand LoginCommand { get; }

        public MainPage(IDataService dataService, IView view)
        {
            _dataService = dataService;
            _view = view;
            LoginCommand = new AsyncRelayCommand(LoginSuccess, CanLogin);

            _user = User;

            _repo = new Repo(_connstring);

            _user = new User();

            User.BirthDate = new DateTime(1990, 1, 1);
        }

        public Command PlannerCommand { get; set; }
        public MainPage(NavigationService navigationService)
        {
            _navigationService = navigationService;

            PlannerCommand = new Command(NavigateToPlanner);

            InsertCommand = new AsyncRelayCommand(InsertUser);

            MyChatsCommand = new AsyncRelayCommand(MyChatsBtn);

            CommunityCommand = new AsyncRelayCommand(CommunityBtn);

            // DashBoardCommand = new AsyncRelayCommand(DashBoardBtn);
        }

        // 플래너 화면 전환
        private void NavigateToPlanner(object parameter)
        {
            _navigationService.NavigateToFrame(typeof(Planner));
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
                    _view.Close();

                    // MainView 열기
                    var mainView = new MainHome
                    {
                        DataContext = this
                    };
                    mainView.Show();
                });
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

        //public async Task DashBoardBtn(object parameter)
        //{
        //    await _navigationService.NavigateToDashBoardFrameAsync(typeof(View.DashBoard));
        //}


        // 사용자 채팅
        public async Task RecieveMessage()
        {
            List<string> receiveMessageList = new List<string>();
            while (true)
            {
                try
                {
                    byte[] receiveByte = new byte[1024];
                    await client.GetStream().ReadAsync(receiveByte, 0, receiveByte.Length);

                    string receiveMessage = Encoding.UTF8.GetString(receiveByte).Trim();

                    // MessageBox.Show($"수신된 메시지: {receiveMessage}");

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
                    MessageBox.Show("서버와의 연결이 끊어졌습니다.", "Server Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                Debug.WriteLine($"[CLIENT] Received Message: {item}");

                if (item.Contains("GroupChattingUserStart"))
                {
                    Debug.WriteLine("[CLIENT] 🔥 GroupChattingUserStart 메시지 수신!");
                }

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
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            Community.ChangeUserListView(tempUserList);
                        });

                        // 처리한 메시지 리스트를 비우기
                        messageList.Clear();
                        return;
                    }

                    // 그룹채팅
                    // Contains 해당 문자열에 "#"가 포함되어 있는지 확인 true or false
                    // 문자열을 # 문자를 기준으로 나누는 메서드
                    else if (message.Contains("GroupChattingUserStart"))
                    {
                        await HandleGroupChattingUserStart(chattingPartner, message);

                        // 처리한 메시지 리스트를 비우기
                        messageList.Clear();
                        return;
                    }

                    // 사용자가 채팅방을 나가게 된다면
                    else if (message.Contains("leaveRoom"))
                    {
                        await HandleLeaveRoom(chattingPartner, message);

                        // 처리한 메시지 리스트를 비우기
                        messageList.Clear();
                        return;
                    }

                    // 테스트 코드
                    else if (chattingPartner.Contains("+"))
                    {
                        await HandleUserBundleChanged(chattingPartner, message);
                        
                        // 처리한 메시지 리스트를 비우기
                        messageList.Clear();
                        return;
                    }

                    // 방장 위임
                    else if (message.Contains("HostChanged"))
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

        private async Task HandleGroupChattingUserStart(string chattingPartner, string message)
        {
            string[] splitedChattingPartner = chattingPartner.Split("#");
            List<string> chattingPartners = splitedChattingPartner.Where(el => !string.IsNullOrEmpty(el)).ToList();

            string sender = chattingPartners[1];
            string chattingRoomNum = GetChattingRoomNumTest(chattingPartners);

            if (chattingRoomNum == "-1")
            {
                ChatRooms currentChatRoom = ChattingSession.Instance.CurrentChattingData;

                await Task.Run(() =>
                {
                    Thread groupChattingThread = new Thread(() => ThreadStartingPointTest(currentChatRoom.ChatRoomId.ToString(), chattingPartners[0]));
                    groupChattingThread.SetApartmentState(ApartmentState.STA);
                    groupChattingThread.IsBackground = true;
                    groupChattingThread.Start();
                });
            }
            else
            {
                if (groupChattingThreadDicTest.ContainsKey(chattingRoomNum) &&
                    groupChattingThreadDicTest[chattingRoomNum].chattingThread.IsAlive)
                {

                    await groupChattingThreadDicTest[chattingRoomNum].chattingWindow.ReceiveAddRoomMessage(sender, message);
                }
            }
        }

        private async Task HandleLeaveRoom(string chattingPartner, string message)
        {
            string[] splitedChattingPartner = chattingPartner.Split(":");
            List<string> chattingPartners = splitedChattingPartner.Where(el => !string.IsNullOrEmpty(el)).ToList();

            string sender = chattingPartners[1];
            string messageContent = message;

            string chattingRoomNum = GetChattingRoomNumTest(chattingPartners);

            if (chattingRoomNum != "-1")
            {
                if (groupChattingThreadDicTest.ContainsKey(chattingRoomNum) &&
                    groupChattingThreadDicTest[chattingRoomNum].chattingThread.IsAlive)
                {
                    await groupChattingThreadDicTest[chattingRoomNum].chattingWindow.ReceiveMessage(sender, messageContent);
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
                if (groupChattingThreadDicTest.ContainsKey(hostChangedChattingRoomNum) &&
                    groupChattingThreadDicTest[hostChangedChattingRoomNum].chattingThread.IsAlive)
                {
                    await groupChattingThreadDicTest[hostChangedChattingRoomNum].chattingWindow.ReceiveHostChangedMessage(hostChangedList, message);
                }
            }
        }

        private async Task HandleUserBundleChanged(string chattingPartner, string message)
        {
            //MessageBox.Show("그룹 채팅 시작 메시지를 받았습니다!");

            // '#' 기준으로 수신자들을 분리
            // 상대 사용자가 전송한 메시지
            string[] splitedChattingPartner = chattingPartner.Split("+");
            List<string> chattingPartners = new List<string>();

            foreach (var el in splitedChattingPartner)
            {
                if (string.IsNullOrEmpty(el))
                    continue;
                chattingPartners.Add(el);
            }

            // 메시지를 발송한 발신자는 리스트의 첫번째 요소
            string sender = chattingPartners[3];

            // 메시지 내용
            string messageContent = chattingPartners[1];

            // 채팅 방 번호 가져오기
            string chattingRoomNum = GetChattingRoomNumTest(chattingPartners);

            // Debug.WriteLine("+chattingPartner+ : " + splitedChattingPartner[0] + "\n" + splitedChattingPartner[1] + "\n" + splitedChattingPartner[2] + "\n" + splitedChattingPartner[3]);

            // 채팅 방 번호가 음수인 경우 새로운 스레드를 생성하여 처리
            // 현재 사용자가 참여하고 있는 그룹 채팅 방이 존재하지 않음을 의미
            if (chattingRoomNum == "-1")
            {
                // 현재 채팅방 데이터
                ChatRooms currentChatRoom = ChattingSession.Instance.CurrentChattingData;

                // 람다식을 사용하여 메서드 호출을 스레드의 실행 단위로 전달
                // Thread groupChattingThread = new Thread(() => ThreadStartingPoint(chattingPartners));
                Thread groupChattingThread = new Thread(() => ThreadStartingPointTest(currentChatRoom.ChatRoomId.ToString(), chattingPartners[0]));
                // WPF 애플리케이션의 UI 요소는 STA 상태에서 실행
                groupChattingThread.SetApartmentState(ApartmentState.STA);
                // 백그라운드 스레드는 애플리케이션이 종료되면 자동으로 종료
                groupChattingThread.IsBackground = true;
                groupChattingThread.Start();
            }
            else
            {
                // 이미 존재하는 채팅 스레드가 활성화된 경우 메시지를 전달
                if (groupChattingThreadDicTest[chattingRoomNum].chattingThread.IsAlive)
                {
                    await groupChattingThreadDicTest[chattingRoomNum].chattingWindow.ReceiveMessage(sender, messageContent);
                }
            }
        }

        // 테스트 코드
        private string GetChattingRoomNumTest(List<string> chattingPartners)
        {
            // 요청한 채팅 방 멤버를 문자열로 변환
            string reqMember = $"{string.Join(",", chattingPartners[0])}";

            Debug.WriteLine($"[Debug] Checking for existing chat room: {reqMember}");

            // 기존 채팅방 멤버와 비교하여 존재하는 채팅 방 번호를 찾음
            foreach (var item in groupChattingThreadDicTest)
            {
                string originMember = item.Value.chattingRoomNumStr;

                Debug.WriteLine($"[Debug] Comparing with existing chat room: {originMember}");

                // 요청한 멤버와 기존 채팅방의 멤버가 동일하면 채팅방 번호 반환
                if (originMember == reqMember)
                {
                    string roomKey = $"{item.Value.chattingRoomNumStr}";
                    Debug.WriteLine($"[Found] Chat room exists: {roomKey}");
                    return roomKey;
                }
            }

            Debug.WriteLine("[Not Found] No matching chat room found.");
            return "-1"; // 채팅방이 없으면 -1 반환
        }


        private Dictionary<string, View.ChattingWindow> chattingWindows = new Dictionary<string, View.ChattingWindow>();

        private string GetHostChangedChattingRoomNum(List<string> chattingPartners)
        {
            string reqMember = $"{string.Join(",", chattingPartners[0])}";

            foreach (var item in groupChattingThreadDicTest)
            {
                string originMember = item.Value.chattingRoomNumStr;
                // 채팅 방 번호가 요청한 채팅 방 멤버와 일치하는지 확인
                if (originMember == reqMember)
                    return item.Value.chattingRoomNumStr; // 일치하는 채팅 방 번호를 반환
            }

            // 일치하는 채팅 방 번호가 없는 경우 -1을 반환
            return "-1";
        }

        private readonly Dispatcher _dispatcher = Application.Current.Dispatcher;


        // 테스트 코드
        private void ThreadStartingPointTest(string chattingPartners, string chattingPartnersBundle)
        {
            string currentUser = UserSession.Instance.CurrentUser.NickName; // 현재 로그인한 사용자의 닉네임
            string chatRoomKey = $"{string.Join(",", chattingPartners)}"; // 고유 키 생성

            Debug.WriteLine("chatRoomKey : " + chatRoomKey);

            _dispatcher.Invoke(() =>
            {
                // IsLoaded 속성: ChattingWindow 객체의 속성으로, 윈도우가 UI 스레드에서 완전히 로드되었는지 확인, 창이 로드되지 않았거나 닫혀 있는 경우 IsLoaded는 false
                // 창이 없거나 닫혔다면 조건문 내부로 진입하여 새 창을 생성
                if (!chattingWindows.ContainsKey(chatRoomKey) || chattingWindows[chatRoomKey].IsLoaded == false)
                {
                    // 창이 닫혔거나 존재하지 않으면 새 창 생성
                    var viewModel = new ChattingWindow(client, chattingPartnersBundle);
                    var newChatWindow = new View.ChattingWindow
                    {
                        DataContext = viewModel
                    };

                    chattingWindows[chatRoomKey] = newChatWindow;

                    // ChattingThreadData 저장
                    ChattingThreadData tempThreadData = new ChattingThreadData(Thread.CurrentThread, viewModel, chattingPartners);
                    if (!groupChattingThreadDicTest.ContainsKey(tempThreadData.chattingRoomNumStr))
                    {
                        groupChattingThreadDicTest.Add(tempThreadData.chattingRoomNumStr, tempThreadData);
                    }

                    // 창이 닫힐 때(Closed 이벤트 발생) chattingWindows 딕셔너리에서 해당 키를 제거
                    // 창 닫힘 이벤트 연결
                    newChatWindow.Closed += (s, e) =>
                    {
                        if (chattingWindows.ContainsKey(chatRoomKey))
                        {
                            chattingWindows.Remove(chatRoomKey);
                        }

                        // groupChattingThreadDic에서 제거
                        var keysToRemove = groupChattingThreadDicTest
                            .Where(pair => pair.Value.chattingWindow == viewModel)
                            .Select(pair => pair.Key)
                            .ToList();

                        foreach (var key in keysToRemove)
                        {
                            groupChattingThreadDicTest.Remove(key);
                        }
                    };

                    newChatWindow.Show();
                }
                else
                {
                    // 창이 이미 열려 있으면 활성화
                    chattingWindows[chatRoomKey].Activate();
                }
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}