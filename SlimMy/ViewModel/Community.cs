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
        private Chat _chat;
        private Repo _repo;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";
        private User _user;
        private ChatUserList _chatUserList;

        // 현재 사용자 목록을 저장하는 ObservableCollection입니다. 이 컬렉션은 XAML에서 ListView에 데이터를 바인딩하는 데 사용
        private static ObservableCollection<ChatUserList> currentUserList = new ObservableCollection<ChatUserList>();

        public static string myName = null;

        private ObservableCollection<Chat> _chatRooms;

        public ObservableCollection<Chat> ChatRooms
        {
            get { return _chatRooms; }
            set { _chatRooms = value; OnPropertyChanged(); }
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

        public Chat Chat
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

        private Chat _selectedChatRoom;
        public Chat SelectedChatRoom
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
            //RefreshChatRooms();

            InsertCommand = new Command(Print);
        }

        public Chat LoginSuccessCom(string userEmail)
        {
            // 여기서 사용자 정보를 설정하고 필요한 데이터를 가져올 수 있습니다.
            Chat = new Chat
            {
                CreatorEmail = userEmail
            };

            return Chat;
        }

        // 로그인 사용자 이메일 출력
        private void Print(object parameter)
        {
            // User currentUser = Application.Current.Properties["CurrentUser"] as User;

            User currentUser = UserSession.Instance.CurrentUser;
            if (currentUser != null)
            {
                MessageBox.Show($"여기는 싱글톤: {currentUser.Email}");
            }

            //Application.Current.Properties["CurrentUser"] = null; // 요거는 로그아웃할 때 필요
        }

        // 생성자에서는 초기화 작업을 수행하고, 채팅 타입에 따라 UI 설정
        public Community(int chattingType)
        {
            // XAML에 정의된 UserListView에 currentUserList를 바인딩합니다.
            // UserListView.ItemsSource = currentUserList;

            //groupChattingReceivers = new List<ChatUserList>();

            //View.Community viewCommunity = new View.Community();

            //viewCommunity.UserListView.ItemsSource = currentUserList;

            if(chattingType == 2)
            {
                // 그룹 채팅 참여자 리스트 초기화
                groupChattingReceivers = new List<ChatUserList>();

                // View 초기화 및 바인딩
                View.Community viewCommunity = new View.Community();
                viewCommunity.UserListView.ItemsSource = currentUserList; // currentUserList를 ListView의 ItemsSource에 바인딩
            }
        }

        // 채팅 목록
        private void RefreshChatRooms()
        {
            ChatRooms = new ObservableCollection<Chat>(_repo.SelectChatRoom());
        }

        // 사용자 목록이 변경될 때마다 호출되는 메서드로, 현재 사용자 목록을 업데이트
        public static void ChangeUserListView(IEnumerable<ChatUserList> tempUserList)
        {
            MessageBox.Show("Inside ChangeUserListView"); // 디버깅용 메시지 박스
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                currentUserList.Clear(); // 기존 목록을 비우고

                foreach (var item in tempUserList) // 새로운 목록으로 채운다
                {
                    currentUserList.Add(item);
                }
            }));
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}