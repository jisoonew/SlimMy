using GalaSoft.MvvmLight.Messaging;
using MVVM2.ViewModel;
using SlimMy.Model;
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

        public static string myName = null;
        TcpClient client = null;
        Thread ReceiveThread = null;
        ChattingWindow chattingWindow = null;
        Dictionary<string, ChattingThreadData> chattingThreadDic = new Dictionary<string, ChattingThreadData>();
        Dictionary<int, ChattingThreadData> groupChattingThreadDic = new Dictionary<int, ChattingThreadData>();

        // 여성 혹은 남성중 어떤 선택을 할 것인지
        private bool _isMaleChecked;
        private bool _isFemaleChecked;

        // 이벤트 정의: 로그인 성공 시 발생하는 이벤트
        public event EventHandler<ChatUserList> DataPassed; // 데이터 전달을 위한 이벤트 정의

        private SignUp _signUp;

        List<User> UserList = new List<User>();

        private object lockObj = new object();

        // 채팅 로그 리스트
        private ObservableCollection<string> chattingLogList = new ObservableCollection<string>();

        // 사용자 리스트
        private ObservableCollection<string> userList = new ObservableCollection<string>();

        // 접근 로그 리스트
        private ObservableCollection<string> AccessLogList = new ObservableCollection<string>();

        // 연결 확인 쓰레드
        Task conntectCheckThread = null;

        public Command InsertCommand { get; set; }

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

        public Command LoginCommand { get; set; }
        public Command NickNameCommand { get; set; }
        public Command CommunityBtnCommand { get; set; }

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

        // 생성자
        public MainPage()
        {
            _user = User;

            ClientData.isdebug = true;

            InsertCommand = new Command(InsertUser);
            LoginCommand = new Command(LoginSuccess);
            CommunityBtnCommand = new Command(CommunityBtn);

            _repo = new Repo(_connstring);

            _user = new User();

            User.BirthDate = new DateTime(1990, 1, 1);

            MainServerStart();

            // Community ViewModel 인스턴스 생성
            _communityViewModel = new Community();

            ClientManager.messageParsingAction += MessageParsing;
            ClientManager.ChangeListViewAction += ChangeListView;
            conntectCheckThread = new Task(ConnectCheckLoop);
            conntectCheckThread.Start();
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

        // 클라이언트 연결 상태를 주기적으로 확인하는 루프
        private void ConnectCheckLoop()
        {
            while (true)
            {
                // 모든 클라이언트를 반복
                foreach (var item in ClientManager.clientDic)
                {
                    try
                    {
                        string sendStringData = "관리자<TEST>";
                        byte[] sendByteData = new byte[sendStringData.Length];
                        sendByteData = Encoding.Default.GetBytes(sendStringData);

                        item.Value.tcpClient.GetStream().Write(sendByteData, 0, sendByteData.Length);
                    }
                    catch (Exception e)
                    {
                        RemoveClient(item.Value);
                    }
                }
                Thread.Sleep(1000);
            }
        }

        // 클라이언트를 제거하는 메서드
        private void RemoveClient(ClientData targetClient)
        {
            ClientData result = null;
            ClientManager.clientDic.TryRemove(targetClient.clientNumber, out result);
            string leaveLog = string.Format("[{0}] {1} Leave Server", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), result.clientName);
            ChangeListView(leaveLog, StaticDefine.ADD_ACCESS_LIST);
            ChangeListView(result.clientName, StaticDefine.REMOVE_USER_LIST);
        }


        private void ChangeListView(string Message, int key)
        {
            switch (key)
            {
                case StaticDefine.ADD_ACCESS_LIST:
                    {
                        Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            AccessLogList.Add(Message);
                        }));
                        break;
                    }
                case StaticDefine.ADD_CHATTING_LIST:
                    {
                        Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            chattingLogList.Add(Message);
                        }));
                        break;
                    }
                case StaticDefine.ADD_USER_LIST:
                    {
                        Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            userList.Add(Message);
                        }));
                        break;
                    }
                case StaticDefine.REMOVE_USER_LIST:
                    {
                        Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            userList.Remove(Message);
                        }));
                        break;
                    }
                default:
                    break;
            }
        }

        // SendMsgToClient 메서드는 msgList에 저장된 메시지들을 클라이언트에게 전송하는 역할을 수행
        private void MessageParsing(string sender, string message)
        {
            // 한번에 하나의 쓰레드만 실행할 수 있도록 해준다
            // lock()의 파라미터에는 임의의 객체를 사용할 수 있는데, 주로 object 타입의 private 필드를 지정한다.

            // lockObj는 임계 영역을 설정하기 위해 사용되는 객체로, 보통 object 타입의 필드로 선언합니다.
            // lockObj가 잠금 상태인 동안 다른 스레드가 lock (lockObj) 구문을 통과하려 할 때 대기하게 만듭니다.
            lock (lockObj)
            {
                List<string> msgList = new List<string>();

                string[] msgArray = message.Split('>');
                foreach (var item in msgArray)
                {
                    // String.IsNullOrEmpty(String) : 지정된 문자열이 null이거나 빈 문자열("")인지를 나타냅니다.
                    if (string.IsNullOrEmpty(item))
                        continue;
                    msgList.Add(item);
                }
                SendMsgToClient(msgList, sender);
            }
        }

        // 클라이언트로 메시지를 전송하는 메서드
        // 주어진 메시지인 msgList를 반복하면서 각 메시지를 처리하고, 해당한느 클라이언트에게 메시지 전송
        private void SendMsgToClient(List<string> msgList, string sender)
        {
            string parsedMessage = "";
            string receiver = "";

            int senderNumber = -1;
            int receiverNumber = -1;

            // 메시지 리스트를 반복하며 처리
            foreach (var item in msgList)
            {
                // 메시지를 '<' 기준으로 분리
                string[] splitedMsg = item.Split('<');

                // 수신자를 가져온다.
                receiver = splitedMsg[0];

                // 메시지를 형식화하여 준비
                parsedMessage = string.Format("{0}<{1}>", sender, splitedMsg[1]);

                // 그룹 채팅이 시작되는 경우 처리
                if (parsedMessage.Contains("<GroupChattingStart>"))
                {
                    // 수신자를 '#'로 분리
                    string[] groupSplit = receiver.Split('#');


                    // 각 수신자에게 메시지를 전송하고, 로그를 기록합니다.
                    foreach (var el in groupSplit)
                    {
                        if (string.IsNullOrEmpty(el))
                            continue;
                        string groupLogMessage = string.Format(@"[{0}] [{1}] -> [{2}] , {3}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), groupSplit[0], el, splitedMsg[1]);
                        ChangeListView(groupLogMessage, StaticDefine.ADD_CHATTING_LIST);

                        receiverNumber = GetClinetNumber(el);

                        // 그룹 채팅 시작 메시지를 전송
                        parsedMessage = string.Format("{0}<GroupChattingStart>", receiver);
                        byte[] sendGroupByteData = Encoding.Default.GetBytes(parsedMessage);
                        ClientManager.clientDic[receiverNumber].tcpClient.GetStream().Write(sendGroupByteData, 0, sendGroupByteData.Length);
                    }
                    return;
                }

                // '#'를 포함하는 수신자의 경우 처리
                if (receiver.Contains('#'))
                {
                    string[] groupSplit = receiver.Split('#');

                    foreach (var el in groupSplit)
                    {
                        if (string.IsNullOrEmpty(el))
                            continue;
                        if (el == groupSplit[0])
                            continue;
                        string groupLogMessage = string.Format(@"[{0}] [{1}] -> [{2}] , {3}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), groupSplit[0], el, splitedMsg[1]);
                        ChangeListView(groupLogMessage, StaticDefine.ADD_CHATTING_LIST);

                        receiverNumber = GetClinetNumber(el);

                        // 수신자에게 개별 메시지를 전송
                        parsedMessage = string.Format("{0}<{1}>", receiver, splitedMsg[1]);
                        byte[] sendGroupByteData = Encoding.Default.GetBytes(parsedMessage);
                        ClientManager.clientDic[receiverNumber].tcpClient.GetStream().Write(sendGroupByteData, 0, sendGroupByteData.Length);
                    }
                    return;
                }

                // 발신자와 수신자의 클라이언트 번호를 가져온다.
                senderNumber = GetClinetNumber(sender);
                receiverNumber = GetClinetNumber(receiver);

                // 발신자 변호나 수진자 번호가 유효하지 않은 경우 처리
                if (senderNumber == -1 || receiverNumber == -1)
                {
                    //File.AppendAllText("ClientNumberErrorLog.txt", sender + receiver);
                    return;
                }

                // 메시지를 바이트 데이터로 변환
                byte[] sendByteData = new byte[parsedMessage.Length];
                sendByteData = Encoding.Default.GetBytes(parsedMessage);

                // <GiveMeUserList> 메시지인 경우, 사용자 목록을 전송
                if (parsedMessage.Contains("<GiveMeUserList>"))
                {
                    string userListStringData = "관리자<";
                    foreach (var el in userList)
                    {
                        userListStringData += string.Format("${0}", el);
                    }
                    userListStringData += ">";
                    byte[] userListByteData = new byte[userListStringData.Length];
                    userListByteData = Encoding.Default.GetBytes(userListStringData);
                    ClientManager.clientDic[receiverNumber].tcpClient.GetStream().Write(userListByteData, 0, userListByteData.Length);
                    return;
                }



                // 채팅 로그를 기록
                string logMessage = string.Format(@"[{0}] [{1}] -> [{2}] , {3}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), sender, receiver, splitedMsg[1]);
                ChangeListView(logMessage, StaticDefine.ADD_CHATTING_LIST);

                // <ChattingStart> 메시지인 경우, 채팅 시작 메시지를 발신자와 수신자에게 각각 전송
                if (parsedMessage.Contains("<ChattingStart>"))
                {
                    parsedMessage = string.Format("{0}<ChattingStart>", receiver);
                    sendByteData = Encoding.Default.GetBytes(parsedMessage);
                    ClientManager.clientDic[senderNumber].tcpClient.GetStream().Write(sendByteData, 0, sendByteData.Length);

                    parsedMessage = string.Format("{0}<ChattingStart>", sender);
                    sendByteData = Encoding.Default.GetBytes(parsedMessage);
                    ClientManager.clientDic[receiverNumber].tcpClient.GetStream().Write(sendByteData, 0, sendByteData.Length);

                    return;
                }

                if (parsedMessage.Contains(""))
                    // 메시지를 수신자에게 전송
                    ClientManager.clientDic[receiverNumber].tcpClient.GetStream().Write(sendByteData, 0, sendByteData.Length);
            }
        }

        private int GetClinetNumber(string targetClientName)
        {
            foreach (var item in ClientManager.clientDic)
            {
                if (item.Value.clientName == targetClientName)
                {
                    return item.Value.clientNumber;
                }
            }
            return -1;
        }

        // 서버 실행
        private void MainServerStart()
        {
            MainServer a = new MainServer();
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
        public void InsertUser(object parameter)
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
                _repo.InsertUser(User.Name, User.Gender, User.NickName, User.Email, User.Password, User.BirthDate, User.Height, User.Weight, User.DietGoal);
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

        // 로그인
        public void LoginSuccess(object parameter)
        {
            var passwordBox = Application.Current.MainWindow.FindName("passwordBox") as PasswordBox;
            var ipTextBox = Application.Current.MainWindow.FindName("IpTextBox") as TextBox;
            string password = passwordBox.Password;
            string ip = ipTextBox.Text;
            string parsedName = "%^&";

            User.Password = password;

            bool isSuccess = _repo.LoginSuccess(User.Email, password);

            View.Login login = new View.Login();

            if (isSuccess)
            {
                // 로그인 이후 사용자의 닉네임 가져오기
                string loggedInNickName = _repo.NickName(User.Email);
                parsedName += loggedInNickName;
                User.NickName = loggedInNickName;
                User.IpNum = ip;

                // 싱글톤에 저장
                UserSession.Instance.CurrentUser = new User
                {
                    Email = User.Email,
                    NickName = User.NickName,
                    IpNum = User.IpNum
                };

                client = new TcpClient();
                client.Connect(ip, 9999);

                byte[] byteData = new byte[parsedName.Length];
                byteData = Encoding.Default.GetBytes(parsedName);
                client.GetStream().Write(byteData, 0, byteData.Length);

                myName = User.NickName;

                ReceiveThread = new Thread(RecieveMessage);
                ReceiveThread.Start();

                // MainPage 실행
                var mainPage = new View.MainPage();
                mainPage.DataContext = this;

                // 새로운 창을 보여줍니다.
                mainPage.Show();

                // 현재 창을 닫습니다.
                Application.Current.MainWindow.Close();  // 로그인 창 닫기
            }
            else
            {
                MessageBox.Show("로그인에 실패했습니다. 이메일과 비밀번호를 확인해 주세요.");
            }
        }

        // 커뮤니티 버튼 기능
        public void CommunityBtn(object parameter)
        {
            // 선택된 그룹 채팅 참여자들의 정보를 문자열
            string getUserProtocol = myName + "<GiveMeUserList>";
            byte[] byteData = new byte[getUserProtocol.Length];
            byteData = Encoding.Default.GetBytes(getUserProtocol);

            client.GetStream().Write(byteData, 0, byteData.Length);

            //MessageBox.Show("내가 방장 : " + getUserProtocol);

            // 여기서부터 문제가 생김
            Community community = new Community();

            string groupChattingUserStrData = myName;
            foreach (var item in community.GroupChattingReceivers)
            {
                groupChattingUserStrData += "#";
                groupChattingUserStrData += item.UsersName;
            }

            string chattingStartMessage = string.Format("{0}<GroupChattingStart>", groupChattingUserStrData);
            byte[] chattingStartByte = Encoding.Default.GetBytes(chattingStartMessage);

            //입력했던 주소가 차례대로 출력된다 -> 127.0.0.3#127.0.0.1#127.0.0.1<GroupChattingStart>
            MessageBox.Show("Sending to server: " + chattingStartMessage);

            client.GetStream().Write(chattingStartByte, 0, chattingStartByte.Length);
        }

        // 사용자 채팅
        public void RecieveMessage(object parameter)
        {
            string receiveMessage = "";
            List<string> receiveMessageList = new List<string>();
            while (true)
            {
                try
                {
                    byte[] receiveByte = new byte[1024];
                    client.GetStream().Read(receiveByte, 0, receiveByte.Length);

                    receiveMessage = Encoding.Default.GetString(receiveByte);

                    string[] receiveMessageArray = receiveMessage.Split('>');
                    foreach (var item in receiveMessageArray)
                    {
                        if (!item.Contains('<'))
                            continue;
                        if (item.Contains("관리자<TEST"))
                            continue;

                        receiveMessageList.Add(item);
                    }
                    ParsingReceiveMessage(receiveMessageList);
                }
                catch (Exception e)
                {
                    MessageBox.Show("서버와의 연결이 끊어졌습니다.", "Server Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    // MessageBox.Show(e.Message);
                    // MessageBox.Show(e.StackTrace);
                    Environment.Exit(1);
                }
                Thread.Sleep(500);
            }
        }

        // 클라이언트가 받은 메시지를 분석하고, 그에따라 처리를 수행
        private void ParsingReceiveMessage(List<string> messageList)
        {
            foreach (var item in messageList)
            {
                string chattingPartner = "";
                string message = "";

                // 메시지가 '<' 문자를 포함하는 경우 처리
                if (item.Contains('<'))
                {
                    string[] splitedMsg = item.Split('<');

                    // 수신자와 메시지를 추출
                    chattingPartner = splitedMsg[0];
                    message = splitedMsg[1];

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
                            tempUserList.Add(new ChatUserList(el));
                        }
                        Community.ChangeUserListView(tempUserList);

                        // 처리한 메시지 리스트를 비우기
                        messageList.Clear();
                        return;
                    }

                    // 그룹채팅
                    if (chattingPartner.Contains("#"))
                    {
                        // '#' 기준으로 수신자들을 분리
                        string[] splitedChattingPartner = chattingPartner.Split('#');
                        List<string> chattingPartners = new List<string>();

                        foreach (var el in splitedChattingPartner)
                        {
                            if (string.IsNullOrEmpty(el))
                                continue;
                            chattingPartners.Add(el);
                        }

                        // 발신자는 리스트의 첫번째 요소
                        string sender = chattingPartners[0];

                        // 채팅 방 번호 가져오기
                        int chattingRoomNum = GetChattingRoomNum(chattingPartners);

                        // 방 번호 출력해보기
                        // MessageBox.Show(sender);

                        // 채팅 방 번호가 음수인 경우 새로운 스레드를 생성하여 처리
                        if (chattingRoomNum < 0)
                        {
                            Thread groupChattingThread = new Thread(() => ThreadStartingPoint(chattingPartners));
                            groupChattingThread.SetApartmentState(ApartmentState.STA);
                            groupChattingThread.IsBackground = true;
                            groupChattingThread.Start();
                        }
                        else
                        {
                            // 이미 존재하는 채팅 스레드가 활성화된 경우 메시지를 전달
                            if (groupChattingThreadDic[chattingRoomNum].chattingThread.IsAlive)
                            {
                                groupChattingThreadDic[chattingRoomNum].chattingWindow.ReceiveMessage(sender, message);
                            }
                        }

                        // 처리한 메시지 리스트를 비우기
                        messageList.Clear();
                        return;
                    }
                }
            }
            messageList.Clear();
        }

        private int GetChattingRoomNum(List<string> chattingPartners)
        {
            // 채팅 참여자 리스트를 정렬
            chattingPartners.Sort();

            // 요청한 채팅 방 멤버를 구성
            string reqMember = "";
            foreach (var item in chattingPartners)
            {
                reqMember += item;
            }

            // 기존 채팅방 멤버와 비교하여 존재하는 채팅 방 번호를 찾음
            string originMember = "";
            foreach (var item in groupChattingThreadDic)
            {
                foreach (var el in item.Value.chattingWindow.chattingPartners)
                {
                    originMember += el;
                }

                // 채팅 방 번호가 요청한 채팅 방 멤버와 일치하는지 확인
                if (originMember == reqMember)
                    return item.Value.chattingRoomNum; // 일치하는 채팅 방 번호를 반환
                originMember = ""; // 비교를 위해 originMember 초기화
            }

            // 일치하는 채팅 방 번호가 없는 경우 -1을 반환
            return -1;
        }


        private void ThreadStartingPoint(List<string> chattingPartners)
        {
            chattingPartners.Sort();
            chattingWindow = new ChattingWindow(client, chattingPartners);
            ChattingThreadData tempThreadData = new ChattingThreadData(Thread.CurrentThread, chattingWindow);
            groupChattingThreadDic.Add(tempThreadData.chattingRoomNum, tempThreadData);

            if (chattingWindow.ShowDialog() == true)
            {
                MessageBox.Show("채팅이 종료되었습니다.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                groupChattingThreadDic.Remove(tempThreadData.chattingRoomNum);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
