using SlimMy.Interface;
using SlimMy.Model;
using SlimMy.Test;
using SlimMy.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Newtonsoft.Json;
using SlimMy.Singleton;

namespace SlimMy.ViewModel
{
    public class Community : INotifyPropertyChanged
    {
        private User _user;
        private ChatRooms _chat;
        private Repo _repo;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";
        private ChatUserList _chatUserList;
        private readonly IView _view;
        private View.ChattingWindow _chattingWindow;
        public static event Action CountChanged;

        private static int _count;
        public static int Count
        {
            get => _count;
            set
            {
                if (_count != value)
                {
                    _count = value;
                    CountChanged?.Invoke(); // 이벤트 발생
                }
            }
        }

        // 현재 사용자 목록을 저장하는 ObservableCollection입니다. 이 컬렉션은 XAML에서 ListView에 데이터를 바인딩하는 데 사용
        private static ObservableCollection<ChatUserList> _currentUserList = new ObservableCollection<ChatUserList>();

        public static ObservableCollection<ChatUserList> CurrentUserList
        {
            get { return _currentUserList; }
            set
            {
                _currentUserList = value;
                // static 속성에서는 OnPropertyChanged를 호출할 수 없습니다.
                // 속성이 static이 아닌 경우 UI 바인딩이 필요하다면, static 메서드를 사용하는 것이 적절하지 않을 수 있습니다.
            }
        }

        public static string myName = null;
        public static Guid myUid;

        //private ObservableCollection<ChatRooms> _chatRooms;
        //public ObservableCollection<ChatRooms> ChatRooms
        //{
        //    get { return _chatRooms; }
        //    set
        //    {
        //        _chatRooms = value;
        //        OnPropertyChanged(nameof(ChatRooms));
        //    }
        //}

        // 그룹 채팅의 참가자 목록을 저장
        private static List<ChatUserList> groupChattingReceivers { get; set; }
        public static List<ChatUserList> GroupChattingReceivers
        {
            get
            {
                return groupChattingReceivers;
            }
            set
            {
                groupChattingReceivers = value;
            }
        }

        private ObservableCollection<User> _chatUser;

        public ObservableCollection<User> ChatUser
        {
            get { return _chatUser; }
            set { _chatUser = value; OnPropertyChanged(); }
        }

        public ChatRooms Chat
        {
            get => _chat;
            set
            {
                if (_chat != value)
                {
                    _chat = value;
                    OnPropertyChanged(nameof(Chat));
                }
            }
        }

        private ChatRooms _selectedChatRoom;
        public ChatRooms SelectedChatRoom
        {
            get { return _selectedChatRoom; }
            set { _selectedChatRoom = value; OnPropertyChanged(); }
        }

        private string _userEmail;
        public string UserEmail
        {
            get { return _userEmail; }
            set { _userEmail = value; OnPropertyChanged(_userEmail); }
        }

        private ObservableCollection<User> userList = new ObservableCollection<User>();
        public ObservableCollection<User> UserList
        {
            get
            {
                return userList;
            }
        }

        public User UserTest
        {
            get { return _user; }
            set { _user = value; OnPropertyChanged(nameof(UserTest)); }
        }

        public User User
        {
            get
            {
                return userList.LastOrDefault();
            }
            set
            {
                userList.Add(value);
                OnPropertyChanged("UserAdded");
                OnPropertyChanged("UserList");
            }
        }

        public ChatUserList ChatUserList
        {
            get { return _chatUserList; }
            set { _chatUserList = value; OnPropertyChanged(nameof(User)); }
        }

        public ICommand OpenCreateChatRoomCommand { get; private set; }
        public ICommand InsertCommand { get; private set; }

        public Community(string userEmail)
        {
            //_user = new User { Email = userEmail };
            ChatUser = new ObservableCollection<User> { _user }; // 현재 사용자를 ChatUser에 추가
        }

        public ChatRooms LoginSuccessCom(string userEmail)
        {
            // 여기서 사용자 정보를 설정하고 필요한 데이터를 가져올 수 있습니다.
            Chat = new ChatRooms
            {
                CreatorEmail = userEmail
            };

            return Chat;
        }

        public IDataService _dataService;

        public ObservableCollection<ChatRooms> ChatRooms { get; private set; }

        public Community(IDataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService)); // _dataService 필드 초기화

            InsertCommand = new Command(ChatRoomSelected);

            Initialize(); // 필요한 다른 초기화 작업 수행
        }

        // 기본 생성자 (IDataService의 기본 구현 사용)
        public Community()
        {
            _dataService = new DataService();
            Initialize(); // 초기화 메서드 호출
        }

        // 초기화 메서드
        private void Initialize()
        {
            _repo = new Repo(_connstring); // Repo 초기화
            ChatRooms = new ObservableCollection<ChatRooms>(); // 컬렉션 초기화
            RefreshChatRooms(); // 채팅 방 불러오기
            InsertCommand = new Command(ChatRoomSelected);
        }

        // 채팅 목록 선택
        public void ChatRoomSelected(object parameter)
        {
            User currentUser = UserSession.Instance.CurrentUser;
            if (parameter is ChatRooms selectedChatRoom)
            {
                //MessageBox.Show($"채팅방 아이디 : {currentUser.Email} \n채팅방 이름: {selectedChatRoom.ChatRoomName}\n설명: {selectedChatRoom.Description}\n카테고리: {selectedChatRoom.Category}");
                string msg = string.Format("{0}에 입장하시겠습니까?", selectedChatRoom.ChatRoomName);
                MessageBoxResult messageBoxResult = MessageBox.Show(msg, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (messageBoxResult == MessageBoxResult.No)
                {
                    return;
                }
                else
                {

                    myName = currentUser.NickName;
                    myUid = currentUser.UserId;
                    _dataService.SetUserId(currentUser.UserId.ToString()); // UserId 설정

                    // 만약 사용자가 채팅방에 참가하지 않은 상태라면?
                    // UserChatRooms테이블에 사용자와 채팅방 관계를 추가한다
                    if (_repo.CheckUserChatRooms(currentUser.UserId, selectedChatRoom.ChatRoomId))
                    {

                        // 사용자와 채팅방 간의 관계 생성
                        _repo.InsertUserChatRooms(currentUser.UserId, selectedChatRoom.ChatRoomId);

                        string groupChattingUserStrData = currentUser.IpNum;

                        ServerInfo serverInfo = new ServerInfo
                        {
                            UserID = currentUser.UserId.ToString(),
                            IPAddress = groupChattingUserStrData
                        };

                        string testText = string.Empty;

                        var userIds = _repo.GetChatRoomUserIds(selectedChatRoom.ChatRoomId.ToString());

                        //foreach (var item in userIds)
                        //{
                        //    testText += "#";
                        //    testText += item;
                        //}

                        //testText = "127.0.0.3#127.0.0.1#127.0.0.1<GroupChattingStart>";

                        // 로그인한 사용자 데이터 JSON 직렬화
                        //string json = JsonConvert.SerializeObject(serverInfo);

                        // DB에 저장된 특정 채팅방에 대한 사용자 아이디 모음
                        //string chattingStartMessage = string.Format("{0}<GroupChattingStart>", testText);
                        //byte[] chattingStartByte = Encoding.UTF8.GetBytes(chattingStartMessage);

                        // byte형으로 로그인한 사용자 데이터 변환
                        // byte[] Serverdata = Encoding.UTF8.GetBytes(json);

                        //입력했던 주소가 차례대로 출력된다 -> 127.0.0.3#127.0.0.1#127.0.0.1<GroupChattingStart>
                        //MessageBox.Show("나야 클라이언트 UUID: " + chattingStartMessage.ToString());

                        //currentUser.Client.GetStream().Write(chattingStartByte, 0, chattingStartByte.Length);
                        //currentUser.Client.GetStream().Write(Serverdata, 0, Serverdata.Length);
                    }
                    else
                    {
                        string testText = _repo.GetUserUUId(currentUser.Email);

                        // 싱글톤에 저장
                        // 채팅방마다 각각의 고유한 아이디를 부여해서 해당 채팅방의 실행 여부 확인
                        ChattingSession.Instance.CurrentChattingData = new ChatRooms
                        {
                            ChatRoomId = selectedChatRoom.ChatRoomId
                        };

                        try
                        {

                            // 특정 채팅방에 참가한 사용자들 아이디 모음
                            var userIds = _repo.GetChatRoomUserIds(selectedChatRoom.ChatRoomId.ToString());

                            if (userIds == null || userIds.Count == 0)
                            {
                                MessageBox.Show("No user IDs found for the selected chat room.", "Debug Info");
                                return;
                            }

                            // 문자열을 ChatUserList 객체로 변환
                            List<ChatUserList> userList = userIds.Select(id => new ChatUserList(id)).ToList();

                            Community.GroupChattingReceivers = userList;

                            // 사용자의 Uid 가져오기
                            string groupChattingUserStrData = myUid.ToString();

                            // DB에서 채팅방 참가한 사용자 Uid들을 groupChattingUserStrData 담아서 서버로 전송
                            foreach (var item in Community.GroupChattingReceivers)
                            {
                                // groupChattingUserStrData에 포함되지 않는 사용자 Uid만 저장
                                // 조건문이 없다면 userList에 중복된 내용이 서버로 전송됨
                                if (!groupChattingUserStrData.Contains(item.UsersName))
                                {
                                    groupChattingUserStrData += "#";
                                    groupChattingUserStrData += item.UsersName;
                                }
                            }

                            string chattingStartMessage = string.Format("{0}<GroupChattingStart>", groupChattingUserStrData);

                            byte[] chattingStartByte = Encoding.UTF8.GetBytes(chattingStartMessage);

                            currentUser.Client.GetStream().Write(chattingStartByte, 0, chattingStartByte.Length);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error fetching user IDs: {ex.Message}", "Error");
                        }
                    }

                }
            }
            else
            {
                MessageBox.Show("선택된 채팅방이 없습니다.");
            }
        }

        // 생성자에서는 초기화 작업을 수행하고, 채팅 타입에 따라 UI 설정
        public Community(int chattingType)
        {
            // List<User> groupChattingUser = UserListView.SelectedItems.Cast<User>().ToList();

            // 그룹 채팅 참여자 리스트 초기화
            groupChattingReceivers = new List<ChatUserList>();

            // View 초기화 및 바인딩
            View.Community viewCommunity = new View.Community();
            //viewCommunity.UserListView.ItemsSource = CurrentUserList;
        }

        // 채팅 목록
        public void RefreshChatRooms()
        {
            var chatRooms = _repo.SelectChatRoom();

            ChatRooms.Clear(); // 기존 데이터 제거

            foreach (var chatRoom in chatRooms)
            {
                ChatRooms.Add(chatRoom); // 새 데이터 추가
            }
        }

        // 사용자 목록이 변경될 때마다 호출되는 메서드로, 현재 사용자 목록을 업데이트
        public static void ChangeUserListView(IEnumerable<ChatUserList> tempUserList)
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                CurrentUserList.Clear(); // 기존 목록을 비우고

                foreach (var item in tempUserList) // 새로운 목록으로 채운다
                {
                    CurrentUserList.Add(item);
                }

                // 현재 서버에 접속한 사용자 닉네임 출력
                //foreach (var user in CurrentUserList)
                //{
                //    // user 객체의 속성 출력 (예: Name과 ID 속성 출력)
                //    MessageBox.Show($"Name: {user.UsersName}");
                //}
            }));
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}