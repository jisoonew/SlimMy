using SlimMy.Model;
using SlimMy.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SlimMy.ViewModel
{
    public class CreateChatRoomViewModel : BaseViewModel
    {
        private readonly CommunityViewModel _communityViewModel;
        private ChatRooms _chat;
        private Repo _repo;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";
        public event EventHandler ChatRoomCreated;

        public static string myName = null;
        TcpClient client = null;

        private ObservableCollection<ChatRooms> _chatRooms;

        public ObservableCollection<ChatRooms> ChatRooms
        {
            get { return _chatRooms; }
            set { _chatRooms = value; OnPropertyChanged(nameof(ChatRooms)); }
        }

        public ChatRooms Chat
        {
            get { return _chat; }
            set { _chat = value; OnPropertyChanged(nameof(Chat)); }
        }

        private string _chatName;

        public string ChatName
        {
            get => _chatName;
            set
            {
                if (_chatName != value)
                {
                    _chatName = value;
                    OnPropertyChanged(nameof(ChatName));
                }
            }
        }

        public ICommand OpenCreateChatRoomCommand { get; private set; }

        public CreateChatRoomViewModel()
        {
            _chat = new ChatRooms();
            _repo = new Repo(_connstring);
            OpenCreateChatRoomCommand = new AsyncRelayCommand(CreateChat);
        }

        // 채팅방 생성
        private async Task CreateChat(object parameter)
        {
            // 생성 시간
            DateTime now = DateTime.Now;

            Guid chatRoomId = await _repo.InsertChatRoom(_chat.ChatRoomName, _chat.Description, _chat.Category, now);

            Guid userId = UserSession.Instance.CurrentUser.UserId;

            // 사용자와 채팅방 간의 관계 생성
            await _repo.InsertUserChatRooms(userId, chatRoomId, now, 1);

            // 이벤트 발생
            ChatRoomCreated?.Invoke(this, EventArgs.Empty);

            CloseWindow();

        }

        private void CloseWindow()
        {
            // 현재 윈도우를 찾아서 닫기
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    window.Close();
                    break;
                }
            }
        }
    }
}
