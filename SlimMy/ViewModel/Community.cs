using SlimMy.Interface;
using SlimMy.Model;
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

namespace SlimMy.ViewModel
{
    public class Community : INotifyPropertyChanged
    {
        private ChatRooms _chat;
        private Repo _repo;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";
        private User _user;
        private ChatUserList _chatUserList;

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

        private ObservableCollection<ChatRooms> _chatRooms;

        public ObservableCollection<ChatRooms> ChatRooms
        {
            get { return _chatRooms; }
            set
            {
                if (_chatRooms != value)
                {
                    _chatRooms = value;
                    OnPropertyChanged(nameof(ChatRooms));
                }
            }
        }

        // 그룹 채팅의 참가자 목록을 저장
        private List<ChatUserList> groupChattingReceivers { get; set; }
        public List<ChatUserList> GroupChattingReceivers
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
        public ICommand InsertCommand { get; set; }

        public Community(string userEmail)
        {
            //_user = new User { Email = userEmail };
            ChatUser = new ObservableCollection<User> { _user }; // 현재 사용자를 ChatUser에 추가
        }

        public Community()
        {
            _repo = new Repo(_connstring);


            ChatRooms = new ObservableCollection<ChatRooms>();

            // 채팅방 목록 선택 시
            InsertCommand = new Command(ChatRoomSelected);

            // 채팅방 목록 출력
            RefreshChatRooms();
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

        // 채팅 목록 선택
        private void ChatRoomSelected(object parameter)
        {
            //User currentUser = UserSession.Instance.CurrentUser;
            //if (currentUser != null)
            //{
            //    MessageBox.Show($"여기는 싱글톤: {currentUser.Email}");
            //}

            if (parameter is ChatRooms selectedChatRoom)
            {
                MessageBox.Show($"채팅방 이름: {selectedChatRoom.ChatRoomName}\n설명: {selectedChatRoom.Description}\n카테고리: {selectedChatRoom.Category}");
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