using SlimMy.Model;
using SlimMy.Service;
using SlimMy.Singleton;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace SlimMy.ViewModel
{
    public class MyChatsViewModel : BaseViewModel
    {
        private User _user;
        private ChatRooms _chat;
        private Repo _repo;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";
        private ChatUserList _chatUserList;
        public static event Action CountChanged;
        public static string myName = null;
        public static Guid myUid;
        private User currentUser;

        private ObservableCollection<ChatRooms> _currentPageData; // 현재 페이지에 표시할 데이터의 컬렉션
        public ObservableCollection<ChatMessageStatus> ChatMessageStatus { get; set; } // 메시지를 읽지 않았을 때 아이콘으로 표시하는 기능
        private int _currentPage; // 현재 페이지 번호
        private int _totalPages; // 전체 데이터에서 생성된 총 페이지 수
        private int _pageSize = 5; // 페이지당 항목 수

        public ObservableCollection<ChatRooms> CurrentPageData
        {
            get => _currentPageData;
            set
            {
                _currentPageData = value;
                OnPropertyChanged(nameof(CurrentPageData));
            }
        }

        // 현재 페이지 번호를 관리
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage != value)
                {
                    _currentPage = value;
                    UpdateCurrentPageData();
                    OnPropertyChanged(nameof(CurrentPage));
                }
            }
        }

        // 전체 데이터에서 총 몇 개의 페이지가 있는지 계산하여 저장
        public int TotalPages
        {
            get => _totalPages;
            set { _totalPages = value; OnPropertyChanged(nameof(TotalPages)); }
        }

        public ObservableCollection<ChatRooms> AllData { get; set; } // 전체 데이터

        public ICommand NextPageCommand { get; set; }
        public ICommand PreviousPageCommand { get; set; }

        private static String _searchword;
        public static String SearchWord
        {
            get => _searchword;
            set
            {
                if (_searchword != value)
                {
                    _searchword = value;
                    CountChanged?.Invoke(); // 이벤트 발생
                }
            }
        }

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
                // 속성이 static이 아닌 경우 UI 바인딩이 필요하다면, static 메서드를 사용하는 것이 적절하지 않을 수 있음
            }
        }

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
            set { _chatUser = value; OnPropertyChangedVoid(); }
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

        private int _selectedChatRoomIndex;
        public int SelectedChatRoomIndex
        {
            get => _selectedChatRoomIndex;
            set
            {
                _selectedChatRoomIndex = value;
                OnPropertyChanged(nameof(SelectedChatRoomIndex)); // UI에 변경 알림
            }
        }

        private ChatRooms _selectedChatRoom;
        public ChatRooms SelectedChatRoom
        {
            get { return _selectedChatRoom; }
            set
            {
                if (_selectedChatRoom != value)
                {
                    _selectedChatRoom = value;
                    OnPropertyChanged(nameof(SelectedChatRoom));
                }
                else
                {
                    // 동일한 항목을 선택해도 동작하도록 처리
                    OnPropertyChanged(nameof(SelectedChatRoom));
                }
            }
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
        public ICommand SearchCommand { get; private set; }

        public MyChatsViewModel(string userEmail)
        {
            //_user = new User { Email = userEmail };
            ChatUser = new ObservableCollection<User> { _user }; // 현재 사용자를 ChatUser에 추가
        }

        public ChatRooms LoginSuccessCom(string userEmail)
        {
            Chat = new ChatRooms
            {
                CreatorEmail = userEmail
            };

            return Chat;
        }

        public IDataService _dataService;

        public ObservableCollection<ChatRooms> ChatRooms { get; private set; }

        public MyChatsViewModel(IDataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService)); // _dataService 필드 초기화

            InsertCommand = new AsyncRelayCommand(ChatRoomSelected);

            Initialize(); // 필요한 다른 초기화 작업 수행
        }

        // 기본 생성자 (IDataService의 기본 구현 사용)
        public MyChatsViewModel()
        {
            _dataService = new DataService();

            ChatRooms = new ObservableCollection<ChatRooms>(); // 컬렉션 초기화

            ChatMessageStatus = new ObservableCollection<ChatMessageStatus>();
        }

        // 초기화 메서드
        private async Task Initialize()
        {
            _repo = new Repo(_connstring); // Repo 초기화
            await RefreshChatRooms(); // 채팅 방 불러오기
            InsertCommand = new AsyncRelayCommand(ChatRoomSelected);

            SearchCommand = new AsyncRelayCommand(Search);

            // 예제 데이터 생성
            AllData = ChatRooms;
            currentUser = UserSession.Instance.CurrentUser;

            // 총 페이지 수 계산
            TotalPages = (int)Math.Ceiling((double)AllData.Count / _pageSize);

            // 현재 페이지 초기화
            CurrentPage = 1;

            // 명령 초기화
            NextPageCommand = new RelayCommand(
            execute: _ => NextPage(),
            canExecute: _ => CanGoToNextPage());

            PreviousPageCommand = new RelayCommand(
                execute: _ => PreviousPage(),
                canExecute: _ => CanGoToPreviousPage());

            // 현재 페이지 데이터 초기화
            UpdateCurrentPageData();
        }

        public static async Task<MyChatsViewModel> CreateAsync()
        {
            var instance = new MyChatsViewModel();
            await instance.Initialize();
            return instance;
        }

        // 채팅 목록 선택
        public async Task ChatRoomSelected(object parameter)
        {
            if (parameter is ChatRooms selectedChatRoom)
            {
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

                    // 싱글톤에 저장
                    // 채팅방마다 각각의 고유한 아이디를 부여해서 해당 채팅방의 실행 여부 확인
                    ChattingSession.Instance.CurrentChattingData = new ChatRooms
                    {
                        ChatRoomId = selectedChatRoom.ChatRoomId
                    };

                    // 채팅방에 참여한 사용자 아이디들을 서버로 전송
                    await SendRoomUserIds(selectedChatRoom, currentUser);
                }
            }
            else
            {
                MessageBox.Show("선택된 채팅방이 없습니다.");
            }
        }

        // 채팅방에 참여한 사용자 아이디들을 서버로 전송
        public async Task SendRoomUserIds(ChatRooms selectedChatRoom, User currentUser)
        {
            
            try
            {
                // 특정 채팅방에 참가한 사용자들 아이디 모음
                var userIds = await _repo.GetChatRoomUserIds(selectedChatRoom.ChatRoomId.ToString());

                if (userIds == null || userIds.Count == 0)
                {
                    MessageBox.Show("No user IDs found for the selected chat room.", "Debug Info");
                    return;
                }

                // 문자열을 ChatUserList 객체로 변환
                List<ChatUserList> userList = userIds.Select(id => new ChatUserList(id,"")).ToList();

                CommunityViewModel.GroupChattingReceivers = userList;

                // 사용자의 Uid 가져오기
                string groupChattingUserStrData = myUid.ToString();

                // DB에서 채팅방 참가한 사용자 Uid들을 groupChattingUserStrData 담아서 서버로 전송
                foreach (var item in CommunityViewModel.GroupChattingReceivers)
                {
                    // groupChattingUserStrData에 포함되지 않는 사용자 Uid만 저장
                    // 조건문이 없다면 userList에 중복된 내용이 서버로 전송됨
                    // 두명 이상일 때 "#"가 포함되기 때문에 #가 포함된 채팅방만 창이 실행되는거였음
                    if (!groupChattingUserStrData.Contains(item.UsersID))
                    {
                        groupChattingUserStrData += "#";
                        groupChattingUserStrData += item.UsersID;
                    }
                    else
                    {
                        groupChattingUserStrData += "#";
                    }
                }

                //string chattingStartMessage = string.Format("{0}<GroupChattingStart>", groupChattingUserStrData);

                //byte[] chattingStartByte = Encoding.UTF8.GetBytes(chattingStartMessage);

                //await currentUser.Client.GetStream().WriteAsync(chattingStartByte, 0, chattingStartByte.Length);

                ChatRooms currentChatRoom = ChattingSession.Instance.CurrentChattingData;

                string chattingStartMessage = $"{currentUser.UserId}:{currentChatRoom.ChatRoomId}";
                byte[] chattingStartMsg = Encoding.UTF8.GetBytes(chattingStartMessage);

                int header = 1 + chattingStartMessage.Length;
                byte[] headerData = BitConverter.GetBytes(header);

                byte msgType = (byte)MessageType.UserJoinChatRoom;

                await currentUser.Client.GetStream().WriteAsync(headerData, 0, headerData.Length);

                await currentUser.Client.GetStream().WriteAsync(new byte[] { msgType }, 0, 1);

                await currentUser.Client.GetStream().WriteAsync(chattingStartMsg, 0, chattingStartMsg.Length);

                await currentUser.Client.GetStream().FlushAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching user IDs: {ex.Message}", "Error");
            }
        }

        // 생성자에서는 초기화 작업을 수행하고, 채팅 타입에 따라 UI 설정
        public MyChatsViewModel(int chattingType)
        {
            // List<User> groupChattingUser = UserListView.SelectedItems.Cast<User>().ToList();

            // 그룹 채팅 참여자 리스트 초기화
            groupChattingReceivers = new List<ChatUserList>();

            // View 초기화 및 바인딩
            View.Community viewCommunity = new View.Community();
            //viewCommunity.UserListView.ItemsSource = CurrentUserList;
        }

        // 채팅 목록
        public async Task RefreshChatRooms()
        {
            User currentUser = UserSession.Instance.CurrentUser;

            var chatRooms = await _repo.MyChatRooms(currentUser.UserId);

            ChatRooms.Clear(); // 기존 데이터 제거

            foreach (var chatRoom in chatRooms)
            {
                ChatRooms.Add(chatRoom); // 새 데이터 추가
            }

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // ListView에 바인딩된 데이터 업데이트
                OnPropertyChanged(nameof(ChatRooms));
            });
        }

        // 채팅방 검색
        public async Task Search(object parameter)
        {
            var chatRommsSearch = await _repo.MyChatRoomsSearch(SearchWord);

            CurrentPageData.Clear(); // 기존 데이터 제거

            foreach (var chatRoom in chatRommsSearch)
            {
                CurrentPageData.Add(chatRoom); // 새 데이터 추가
            }

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // ListView에 바인딩된 데이터 업데이트
                OnPropertyChanged(nameof(CurrentPageData));
            });
        }

        // 페이징에 따른 채팅 목록
        public async Task ChattingRefreshChatRooms()
        {
            await Application.Current.Dispatcher.InvokeAsync(async() =>
            {
                var chatRooms = await _repo.SelectChatRoom(); // DB에서 최신 데이터 가져오기

                if (AllData == null)
                    AllData = new ObservableCollection<ChatRooms>();

                AllData.Clear();

                foreach (var chatRoom in chatRooms)
                {
                    AllData.Add(chatRoom);
                }

                // 총 페이지 수 재계산
                TotalPages = (int)Math.Ceiling((double)AllData.Count / _pageSize);

                if (CurrentPage > TotalPages)
                    CurrentPage = TotalPages;

                // 현재 페이지 데이터 업데이트
                UpdateCurrentPageData();
            });
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
            }));
        }

        private void UpdateCurrentPageData()
        {
            // Skip: 현재 페이지 이전의 항목을 건너뜀.
            // Tack: 페이지 크기만큼 데이터를 가져옴.
            // PageSize = 10, CurrentPage = 2 → Skip(10).Take(10) → 데이터 11~20번 가져옴.
            if (AllData == null || AllData.Count == 0)
                return;

            int totalDataCount = AllData.Count; // 전체 데이터 수
            int remainingDataCount = totalDataCount - ((CurrentPage - 1) * _pageSize); // 남은 데이터 수

            int dataToTake = Math.Min(_pageSize, remainingDataCount); // 현재 페이지에 가져올 데이터 수

            CurrentPageData = new ObservableCollection<ChatRooms>(
                AllData.Skip((CurrentPage - 1) * _pageSize).Take(dataToTake));

            Application.Current.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(CurrentPageData));
            });
        }

        private void NextPage()
        {
            if (CanGoToNextPage())
            {
                CurrentPage++;
            }
        }

        private void PreviousPage()
        {
            if (CurrentPage > 1)
                CurrentPage--;
        }

        private bool CanGoToNextPage() => CurrentPage < TotalPages;
        private bool CanGoToPreviousPage() => CurrentPage > 1;
    }
}
