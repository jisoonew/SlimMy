using SlimMy.Model;
using SlimMy.Response;
using SlimMy.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SlimMy.ViewModel
{
    public class CreateChatRoomViewModel : BaseViewModel
    {
        private ChatRooms _chat;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";
        public event EventHandler ChatRoomCreated;

        public static string myName = null;

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
            OpenCreateChatRoomCommand = new AsyncRelayCommand(CreateChat);
        }

        // 채팅방 생성
        private async Task CreateChat(object parameter)
        {
            // 생성 시간
            DateTime now = DateTime.Now;

            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var waitTask = session.Responses.InsertChatRoomAsync(TimeSpan.FromSeconds(5));

            var req = new { cmd = "InsertChatRoom", chatRoomName = _chat.ChatRoomName, description = _chat.Description, category = _chat.Category, dateTime = now };
            await transport.SendFrameAsync((byte)MessageType.InsertChatRoom, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            var res = JsonSerializer.Deserialize<InsertChatRoomRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (res?.ok != true)
                throw new InvalidOperationException($"server not ok: {res?.message}");

            Guid userId = UserSession.Instance.CurrentUser.UserId;

            // 사용자와 채팅방 간의 관계 생성
            var userChatRoomWaitTask = session.Responses.InsertUserChatRoomsAsync(TimeSpan.FromSeconds(5));

            var userChatRoomReq = new { cmd = "InsertUserChatRooms", userID = userId, chatRoomID = res.chatRoomID, dateTime = now, isowner = 1 };
            await transport.SendFrameAsync((byte)MessageType.InsertUserChatRooms, JsonSerializer.SerializeToUtf8Bytes(userChatRoomReq));

            var userChatRoomRespPayload = await userChatRoomWaitTask;

            var userChatRoomRes = JsonSerializer.Deserialize<InsertUserChatRoomsRes>(
                userChatRoomRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (userChatRoomRes?.ok != true)
                throw new InvalidOperationException($"server not ok: {userChatRoomRes?.message}");

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
