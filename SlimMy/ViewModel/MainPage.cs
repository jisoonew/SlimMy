using GalaSoft.MvvmLight.Messaging;
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
    class MainPage : INotifyPropertyChanged
    {
        private User _user;
        private string _username;
        private Repo _repo;
        private string nickName;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";

        public static string myName = null;
        TcpClient client = null;
        Thread ReceiveThread = null;
        
        Dictionary<string, ChattingThreadData> chattingThreadDic = new Dictionary<string, ChattingThreadData>();
        Dictionary<int, ChattingThreadData> groupChattingThreadDic = new Dictionary<int, ChattingThreadData>();

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

        public MainPage()
        {
            _user = User;

            // 서버 시작
            //ServerStart();

            //ClientManager.messageParsingAction += MessageParsing;
            //ClientManager.ChangeListViewAction += ChangeListView;
            //conntectCheckThread = new Task(ConnectCheckLoop);
            //conntectCheckThread.Start();
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

        private void ServerStart()
        {
            Server server = new Server();
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
