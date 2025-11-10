using SlimMy.Model;
using SlimMy.Response;
using SlimMy.Service;
using SlimMy.Singleton;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SlimMy.ViewModel
{
    public class ChattingWindowViewModel : INotifyPropertyChanged
    {
        private string chattingPartner = null;
        private readonly INetworkTransport transport;
        public List<string> chattingPartners = null;
        public static string myName = null;
        private readonly INavigationService _navigationService;

        public ICommand Window_PreviewKeyDownCommand { get; private set; }

        private ObservableCollection<ChatMessage> messageList = new ObservableCollection<ChatMessage>();

        public ObservableCollection<ChatMessage> MessageList
        {
            get => messageList;
            set
            {
                messageList = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<ChatUserList> delegateCandidateList = new ObservableCollection<ChatUserList>();

        public ObservableCollection<ChatUserList> DelegateCandidateList
        {
            get => delegateCandidateList;
            set
            {
                delegateCandidateList = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<ChatUserList> banCandidateList = new ObservableCollection<ChatUserList>();

        public ObservableCollection<ChatUserList> BanCandidateList
        {
            get => banCandidateList;
            set
            {
                banCandidateList = value;
                OnPropertyChanged();
            }
        }

        public ICommand CancelDelegateCommand { get; private set; }

        private string _messageText;
        public string MessageText
        {
            get => _messageText;
            set
            {
                _messageText = value;
                OnPropertyChanged(nameof(MessageText));
            }
        }

        public ICommand SendCommand { get; }

        // 사용자 권한
        private bool _isHost; // 사용자 권한 정보

        public bool IsHost
        {
            get => _isHost;
            set
            {
                _isHost = value;
                OnPropertyChanged(nameof(IsHost));
            }
        }

        // 방장 권환 위임
        private ChatUserList _userSelectedItem; // 사용자 권한 정보

        public ChatUserList UserSelectedItem
        {
            get => _userSelectedItem;
            set
            {
                _userSelectedItem = value;
                OnPropertyChanged(nameof(_userSelectedItem));
            }
        }

        // 방출하기
        private ChatUserList _userBanSelectedItem; // 사용자 권한 정보

        public ChatUserList UserBanSelectedItem
        {
            get => _userBanSelectedItem;
            set
            {
                _userBanSelectedItem = value;
                OnPropertyChanged(nameof(_userBanSelectedItem));
            }
        }

        // 팝업 열림/닫힘을 각각 다른 속성으로
        private bool _isMainPopupOpen;
        public bool IsMainPopupOpen
        {
            get => _isMainPopupOpen;
            set { _isMainPopupOpen = value; OnPropertyChanged(); }
        }

        private bool _isDelegatePopupOpen;
        public bool IsDelegatePopupOpen
        {
            get => _isDelegatePopupOpen;
            set { _isDelegatePopupOpen = value; OnPropertyChanged(); }
        }

        private bool _isBanPopupOpen;
        public bool IsBanPopupOpen
        {
            get => _isBanPopupOpen;
            set { _isBanPopupOpen = value; OnPropertyChanged(); }
        }


        public ICommand UpdateHostCommand { get; }
        public ICommand KickMemberCommand { get; }
        public ICommand LeaveRoomCommand { get; }

        // 채팅방 설정
        public ICommand TogglePopupCommand { get; }

        public ICommand ToggleDelegatePopupCommand { get; }
        // 방출하기
        public ICommand ToggleBanPopupCommand { get; }

        public ICommand CloseAllPopupsCommand { get; }
        public ICommand ConfirmDelegateCommand { get; }

        // 채팅방 나가기
        public ICommand ExitChatRoomCommand { get; }

        // 멤버 방출하기
        public ICommand BanCommand { get; }


        private void ExecuteOption(string option)
        {
            MessageBox.Show($"{option} Selected");
            IsMainPopupOpen = false; // 선택 후 Popup 닫기
        }

        // 1명 채팅방 입장
        public ChattingWindowViewModel(INetworkTransport client, Guid roomId, string chattingPartner)
        {
            ChatRooms currentChattingData = ChattingSession.Instance.CurrentChattingData;
            User currentUser = UserSession.Instance.CurrentUser;

            this.transport = client;
            this.chattingPartner = chattingPartner;

            // Dispatcher를 사용하여 UI 스레드에서 ListView를 찾고 설정합니다.
            Application.Current.Dispatcher.Invoke(async () =>
            {
                // var listView = Application.Current.MainWindow.FindName("MessageList") as ItemsControl;
                //if (listView != null)
                //{
                //    listView.ItemsSource = MessageList;
                //}

                // 내가 참가한 순간부터 메시지를 가져온다
                var session = UserSession.Instance;
                var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

                var reqId = Guid.NewGuid();

                var waitTask = session.Responses.WaitAsync(MessageType.MessagePrintRes, reqId, TimeSpan.FromSeconds(5));

                var req = new { cmd = "MessagePrint", chatRoomID = roomId, userID = currentUser.UserId, requestID= reqId };
                await transport.SendFrameAsync((byte)MessageType.MessagePrint, JsonSerializer.SerializeToUtf8Bytes(req));

                var respPayload = await waitTask;

                var res = JsonSerializer.Deserialize<MessagePrintRes>(
                    respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (res?.ok != true)
                    throw new InvalidOperationException($"server not ok: {res?.message}");

                // 해당 채팅방에 전달한 메시지가 있다면 메시지 출력
                if (res.messageBundle != null)
                {
                    foreach (var messageList in res.messageBundle)
                    {
                        if (currentUser.UserId == messageList.SendUserID)
                        {
                            MessageList.Add(new ChatMessage
                            {
                                Message = $"나: {messageList.SendMessage}",
                                Alignment = TextAlignment.Right
                            });
                        }
                        else
                        {
                            MessageList.Add(new ChatMessage
                            {
                                Message = $"{messageList.SendUserNickName}: {messageList.SendMessage}",
                                Alignment = TextAlignment.Left
                            });
                        }
                    }
                }

                MessageList.Add(new ChatMessage
                {
                    Message = $"{chattingPartner}님이 입장하였습니다.",
                    Alignment = TextAlignment.Left
                });
                //this.Title = chattingPartner + "님과의 채팅방";

                InitializeHostCheck();
            });

            SendCommand = new AsyncRelayCommand(Send_btn_Click);

            Window_PreviewKeyDownCommand = new Command(Window_PreviewKeyDown);

            // 방장 위임 후보 리스트 초기화
            UpdateHost();

            // KickMemberCommand = new RelayCommand(KickMember);
            // LeaveRoomCommand = new RelayCommand(LeaveRoom);

            // Toggle 메인 팝업
            TogglePopupCommand = new RelayCommand(_ =>
            {
                IsMainPopupOpen = !IsMainPopupOpen;
                if (IsMainPopupOpen) IsDelegatePopupOpen = false; IsBanPopupOpen = false;
            });

            // Toggle 방장 위임 팝업
            ToggleDelegatePopupCommand = new RelayCommand(_ =>
            {
                IsDelegatePopupOpen = !IsDelegatePopupOpen;
                if (IsDelegatePopupOpen) IsMainPopupOpen = false; IsBanPopupOpen = false;
            });

            // Toggle 방출하기 팝업
            ToggleBanPopupCommand = new RelayCommand(_ =>
            {
                IsBanPopupOpen = !IsBanPopupOpen;
                if (IsBanPopupOpen) IsDelegatePopupOpen = false; IsMainPopupOpen = false;
            });

            // 모든 팝업 닫기
            CloseAllPopupsCommand = new RelayCommand(_ =>
            {
                IsMainPopupOpen = false;
                IsDelegatePopupOpen = false;
                IsBanPopupOpen = false;
            });

            _navigationService = new NavigationService();

            // 방장 위임 기능
            ConfirmDelegateCommand = new AsyncRelayCommand(UpdateHostBtn);

            // 채팅방 나가기
            ExitChatRoomCommand = new AsyncRelayCommand(ExitChatRoom);

            // 멤버 방출하기
            BanCommand = new AsyncRelayCommand(BanMember);

            // MessageList.CollectionChanged += (s, e) => ScrollToBot(); // 메시지 추가 시 자동 스크롤
            ScrollToBot();
        }

        private async void InitializeHostCheck()
        {
            // 현재 로그인한 사용자 ID를 Guid로 가져옴
            User currentUser = UserSession.Instance.CurrentUser;
            Guid currentUserId = Guid.Parse(currentUser.UserId.ToString()); // 또는 currentUser.UserId가 이미 Guid면 Parse 불필요

            // DB에서 호스트(방장) GUID를 받아옴
            ChatRooms currentChattingData = ChattingSession.Instance.CurrentChattingData;

            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.GetHostUserIdByRoomIdRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "GetHostUserIdByRoomId", chatRoomID = currentChattingData.ChatRoomId.ToString(), requestID = reqId };
            await transport.SendFrameAsync((byte)MessageType.GetHostUserIdByRoomId, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            var res = JsonSerializer.Deserialize<GetHostUserIdByRoomIdRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (res?.ok != true)
                throw new InvalidOperationException($"server not ok: {res?.message}");

            // 비교 후 IsHost 설정 (Guid 타입끼리 비교)
            IsHost = (currentUserId == res.hostUserId);

            // 디버깅용 로그
            Debug.WriteLine($"currentUserId: {currentUserId}");
            Debug.WriteLine($"hostUserId   : {res.hostUserId}");
            Debug.WriteLine($"IsHost       : {IsHost}");
        }

        // 다수 채팅방 입장
        public ChattingWindowViewModel(INetworkTransport client, List<string> targetChattingPartners)
        {
            try
            {
                ChatRooms currentChattingData = ChattingSession.Instance.CurrentChattingData;
                User currentUser = UserSession.Instance.CurrentUser;

                this.transport = client;
                this.chattingPartners = targetChattingPartners;

                // Dispatcher를 사용하여 UI 스레드에서 ListView를 찾고 설정합니다.
                Application.Current.Dispatcher.Invoke(async () =>
                {
                    var listView = Application.Current.MainWindow.FindName("MessageList") as ItemsControl;
                    if (listView != null)
                    {
                        listView.ItemsSource = MessageList;
                    }


                    string enteredUser = "";
                    foreach (var item in targetChattingPartners)
                    {
                        enteredUser += item;
                        enteredUser += "님, ";
                    }

                    var session = UserSession.Instance;
                    var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

                    var reqId = Guid.NewGuid();

                    var waitTask = session.Responses.WaitAsync(MessageType.MessagePrintRes, reqId, TimeSpan.FromSeconds(5));

                    var req = new { cmd = "MessagePrint", chatRoomID = currentChattingData.ChatRoomId, userID = currentUser.UserId, requestID = reqId };
                    await transport.SendFrameAsync((byte)MessageType.MessagePrint, JsonSerializer.SerializeToUtf8Bytes(req));

                    var respPayload = await waitTask;

                    var res = JsonSerializer.Deserialize<MessagePrintRes>(
                        respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (res?.ok != true)
                        throw new InvalidOperationException($"server not ok: {res?.message}");

                    // 해당 채팅방에 전달한 메시지가 있다면 메시지 출력
                    if (res.messageBundle != null)
                    {
                        foreach (var messageList in res.messageBundle)
                        {
                            if (currentUser.UserId == messageList.SendUserID)
                            {
                                MessageList.Add(new ChatMessage
                                {
                                    Message = $"나: {messageList.SendMessage}",
                                    Alignment = TextAlignment.Right
                                });
                            }
                            else
                            {
                                MessageList.Add(new ChatMessage
                                {
                                    Message = $"{messageList.SendUserNickName}: {messageList.SendMessage}",
                                    Alignment = TextAlignment.Left
                                });
                            }
                        }
                    }

                    MessageList.Add(new ChatMessage
                    {
                        Message = $"{enteredUser}이 입장하였습니다.",
                        Alignment = TextAlignment.Left
                    });
                    //this.Title = enteredUser + "과의 채팅방";

                    var getHostUserIdByRoomIdReqId = Guid.NewGuid();

                    // 로그인한 사용자가 채팅방 방장이라면 방장 권한 부여(방장 권한 UI True)
                    var hostUserIDWaitTask = session.Responses.WaitAsync(MessageType.GetHostUserIdByRoomIdRes, getHostUserIdByRoomIdReqId, TimeSpan.FromSeconds(5));

                    var hostUserIDReq = new { cmd = "GetHostUserIdByRoomId", chatRoomID = currentChattingData.ChatRoomId.ToString(), requestID = getHostUserIdByRoomIdReqId };
                    await transport.SendFrameAsync((byte)MessageType.GetHostUserIdByRoomId, JsonSerializer.SerializeToUtf8Bytes(hostUserIDReq));

                    var hostUserIDRespPayload = await hostUserIDWaitTask;

                    var hostUserIDRes = JsonSerializer.Deserialize<GetHostUserIdByRoomIdRes>(
                        hostUserIDRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (hostUserIDRes?.ok != true)
                        throw new InvalidOperationException($"server not ok: {hostUserIDRes?.message}");

                    IsHost = currentUser.UserId.ToString() == hostUserIDRes.hostUserId.ToString();
                });

                SendCommand = new AsyncRelayCommand(Send_btn_Click);

                Window_PreviewKeyDownCommand = new Command(Window_PreviewKeyDown);

                // 방장 위임 후보 리스트 초기화
                UpdateHost();

                // Toggle 메인 팝업
                TogglePopupCommand = new RelayCommand(_ =>
                {
                    IsMainPopupOpen = !IsMainPopupOpen;
                    if (IsMainPopupOpen) IsDelegatePopupOpen = false; IsBanPopupOpen = false;
                });

                // Toggle 방장 위임 팝업
                ToggleDelegatePopupCommand = new RelayCommand(_ =>
                {
                    IsDelegatePopupOpen = !IsDelegatePopupOpen;
                    if (IsDelegatePopupOpen) IsMainPopupOpen = false; IsBanPopupOpen = false;
                });

                // Toggle 방출하기 팝업
                ToggleBanPopupCommand = new RelayCommand(_ =>
                {
                    IsBanPopupOpen = !IsBanPopupOpen;
                    if (IsBanPopupOpen) IsDelegatePopupOpen = false; IsMainPopupOpen = false;
                });

                // 모든 팝업 닫기
                CloseAllPopupsCommand = new RelayCommand(_ =>
                {
                    IsMainPopupOpen = false;
                    IsDelegatePopupOpen = false;
                    IsBanPopupOpen = false;
                });

                _navigationService = new NavigationService();

                // 방장 위임 기능
                ConfirmDelegateCommand = new AsyncRelayCommand(UpdateHostBtn);

                // 채팅방 나가기
                ExitChatRoomCommand = new AsyncRelayCommand(ExitChatRoom);

                // 멤버 방출하기
                BanCommand = new AsyncRelayCommand(BanMember);

                ScrollToBot();
            }
            catch (Exception ex)
            {
                MessageBox.Show("채팅방 Error : " + ex);
            }
        }

        // 메시지 전송
        private async Task Send_btn_Click(object parameter)
        {
            if (string.IsNullOrEmpty(MessageText))
                return;
            string message = MessageText;
            string parsedMessage = "";

            try
            {
                if (message.Contains('<') || message.Contains('>'))
                {
                    MessageBox.Show("죄송합니다. >,< 기호는 사용하실수 없습니다.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (chattingPartner != null)
                {
                    User currentUser = UserSession.Instance.CurrentUser;
                    ChatRooms currentChatRooms = ChattingSession.Instance.CurrentChattingData;
                    string myName = currentUser.NickName;
                    Guid myUid = currentUser.UserId;
                    string partners = myUid.ToString();

                    //parsedMessage = string.Format("{0}:{1}:{2}<ChattingContent>", currentChatRooms.ChatRoomId, message, myUid);
                    //byte[] byteData = Encoding.Default.GetBytes(parsedMessage);
                    //await client.GetStream().WriteAsync(byteData, 0, byteData.Length);

                    var transport = UserSession.Instance.CurrentUser?.Transport;
                    if (transport == null)
                    {
                        MessageBox.Show("네트워크 세션이 없습니다. 다시 로그인해 주세요.");
                        return;
                    }

                    // 페이로드
                    parsedMessage = $"{currentChatRooms.ChatRoomId}:{message}:{myUid}";
                    byte[] parsedMessageByte = Encoding.UTF8.GetBytes(parsedMessage);

                    await transport.SendFrameAsync((byte)MessageType.ChatContent, parsedMessageByte);
                }

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageList.Add(new ChatMessage
                    {
                        Message = $"나: {message}",
                        Alignment = TextAlignment.Right
                    });
                });

                // 메시지 전송 후 초기화
                MessageText = string.Empty;

                await ScrollToBot();
            }
            catch (Exception ex)
            {
                MessageBox.Show("오류 발생 : " + ex);
            }
        }

        private void Window_PreviewKeyDown(object parameter)
        {
            // var Send_Text_Box = Application.Current.MainWindow.FindName("Send_Text_Box") as TextBox;

            if (string.IsNullOrEmpty(MessageText))
                return;
            string message = MessageText;
            string parsedMessage = "";

            if (message.Contains('<') || message.Contains('>'))
            {
                MessageBox.Show("죄송합니다. >,< 기호는 사용하실수 없습니다.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (chattingPartner != null)
            {
                parsedMessage = string.Format("{0}<{1}>", chattingPartner, message);
                byte[] byteData = Encoding.Default.GetBytes(parsedMessage);
                // transport.GetStream().Write(byteData, 0, byteData.Length);
            }
            // 그룹채팅
            else
            {
                User currentUser = UserSession.Instance.CurrentUser;
                myName = currentUser.NickName;

                string partners = myName;
                foreach (var item in chattingPartners)
                {
                    if (item == myName)
                        continue;
                    partners += "#" + item;
                }

                parsedMessage = string.Format("{0}<{1}>", partners, message);
                byte[] byteData = Encoding.Default.GetBytes(parsedMessage);
                // client.GetStream().Write(byteData, 0, byteData.Length);
            }

            MessageList.Add(new ChatMessage
            {
                Message = $"나: {message}",
                Alignment = TextAlignment.Right
            });

            // 메시지 전송 후 초기화
            MessageText = string.Empty;

            ScrollToBot();
        }

        // 전송 메시지
        public async Task ReceiveMessage(string sender, string message)
        {
            if (message == "ChattingStart")
            {
                return;
            }

            if (message == "상대방이 채팅방을 나갔습니다.")
            {
                string parsedMessage = string.Format("{0}님이 채팅방을 나갔습니다.", sender);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageList.Add(new ChatMessage
                    {
                        Message = $"{sender}님이 채팅방을 나갔습니다.",
                        Alignment = TextAlignment.Left
                    });
                });

                await ScrollToBot();
                return;
            }

            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.SendNickNameRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "SendNickName", sender = sender, requestID = reqId };
            await transport.SendFrameAsync((byte)MessageType.SendNickName, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            var res = JsonSerializer.Deserialize<SendNickNameRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (res?.ok != true)
                throw new InvalidOperationException($"server not ok: {res?.message}");

            User currentUser = UserSession.Instance.CurrentUser;

            // 메시지를 보낸 사용자와 로그인 사용자가 같은 사람이 아니라면
            if (!sender.Equals(currentUser.UserId.ToString()))
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageList.Add(new ChatMessage
                    {
                        Message = $"{res.senderNickName}: {message}",
                        Alignment = TextAlignment.Left
                    });
                });
            }
        }

        // 채팅방 나가기 메시지 수신
        public async Task ReceiveLeaveRoomMessage(string sender, string message)
        {
            User currentUser = UserSession.Instance.CurrentUser;

            // 메시지를 보낸 사용자와 로그인 사용자가 같은 사람이 아니라면
            if (!sender.Equals(currentUser.UserId.ToString()))
            {
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {

                    MessageList.Add(new ChatMessage
                    {
                        Message = $"{sender}님이 채팅방을 나갔습니다.",
                        Alignment = TextAlignment.Left
                    });

                    await ScrollToBot();
                });
            }
        }

        // 채팅방 참가 메시지 수신
        public async Task ReceiveAddRoomMessage(string sender, string message)
        {
            User currentUser = UserSession.Instance.CurrentUser;

            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.SendNickNameRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "SendNickName", sender = sender, requestID = reqId };
            await transport.SendFrameAsync((byte)MessageType.SendNickName, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            var res = JsonSerializer.Deserialize<SendNickNameRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (res?.ok != true)
                throw new InvalidOperationException($"server not ok: {res?.message}");

            // 메시지를 보낸 사용자와 로그인 사용자가 같은 사람이 아니라면
            if (!sender.Equals(currentUser.UserId.ToString()))
            {
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    MessageList.Add(new ChatMessage
                    {
                        Message = $"{res.senderNickName}님이 참여하였습니다.",
                        Alignment = TextAlignment.Left
                    });

                    await ScrollToBot();
                });
            }
        }

        private ChatUserList _chatUserList;

        public ChatUserList ChatUserList
        {
            get { return _chatUserList; }
            set { _chatUserList = value; OnPropertyChanged(nameof(User)); }
        }

        // 방장 위임 알림 출력
        public async Task ReceiveHostChangedMessage(List<string> hostData, string message)
        {
            if (hostData == null || hostData.Count < 2)
            {
                Debug.WriteLine("hostData가 null이거나 값이 충분하지 않습니다.");
                return;
            }

            // 방장 위임을 진행하는 채팅방 아이디
            string hostChangedChattingRoomID = hostData[0];
            // 방장 위임을 받는 사용자 아이디
            string hostChangedUserID = hostData[1];

            ChatRooms currentChattingData = ChattingSession.Instance.CurrentChattingData;

            // 방장 위임을 받는 사용자 닉네임
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.SendNickNameRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "SendNickName", sender = hostChangedUserID, requestID = reqId };
            await transport.SendFrameAsync((byte)MessageType.SendNickName, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            var res = JsonSerializer.Deserialize<SendNickNameRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (res?.ok != true)
                throw new InvalidOperationException($"server not ok: {res?.message}");

            // 현재 입장한 채팅방과 위임을 진행하는 채팅방이 일치하면 방장 위임 채팅방 알림
            if (currentChattingData.ChatRoomId.ToString() == hostChangedChattingRoomID)
            {
                // 방장 위임 문구 출력
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageList.Add(new ChatMessage
                    {
                        Message = $"{res.senderNickName}님이 새로운 방장이 되었습니다.",
                        Alignment = TextAlignment.Left
                    });
                });
            }

            User currentUser = UserSession.Instance.CurrentUser;

            // 위임 팝업
            IsDelegatePopupOpen = false;

            // 채팅방 메인 팝업
            IsMainPopupOpen = false;

            // 위임 받는 사용자와 현재 로그인한 사용자가 같은가?
            if (currentUser.UserId.ToString() == hostChangedUserID)
            {
                // 방장 위임 버튼 off
                IsHost = true;
            }

        }

        // 방장 위임 후보 리스트
        private async Task UpdateHost()
        {
            // 같은 채팅방의 사용자 닉네임을 담은 리스트(방장 위임 리스트)
            DelegateCandidateList = new ObservableCollection<ChatUserList>();

            // 같은 채팅방의 사용자 닉네임을 담은 리스트(멤버 내보내기)
            BanCandidateList = new ObservableCollection<ChatUserList>();

            // 현재 채팅방 ID 가져오기
            ChatRooms currentChattingData = ChattingSession.Instance.CurrentChattingData;
            Guid currentChatRoomId = currentChattingData.ChatRoomId;

            // SelectChatUserNickName으로 데이터 가져오기
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.SelectChatUserNickNameRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "SelectChatUserNickName", currentChatRoomId = currentChatRoomId, requestID = reqId };
            await transport.SendFrameAsync((byte)MessageType.SelectChatUserNickName, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            var res = JsonSerializer.Deserialize<SelectChatUserNickNameRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (res?.ok != true)
                throw new InvalidOperationException($"server not ok: {res?.message}");

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // DelegateCandidateList에 추가
                foreach (var user in res.usersInChatRoom)
                {
                    DelegateCandidateList.Add(user);
                    BanCandidateList.Add(user);
                }
            });

            OnPropertyChanged(nameof(DelegateCandidateList));
            OnPropertyChanged(nameof(BanCandidateList));
        }

        // 방장 위임 버튼
        private async Task UpdateHostBtn(object parameter)
        {
            try
            {
                ChatRooms currentChattingData = ChattingSession.Instance.CurrentChattingData;

                // 방장이었던 사용자는 isowner = 0, 지목 당한 사용자는 isowner = 1
                var session = UserSession.Instance;
                var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

                var reqId = Guid.NewGuid();

                var waitTask = session.Responses.WaitAsync(MessageType.UpdateHostRes, reqId, TimeSpan.FromSeconds(5));

                var req = new { cmd = "UpdateHost", chatRoomID = currentChattingData.ChatRoomId, userIDBundle = UserSelectedItem.UsersID, requestID = reqId };
                await transport.SendFrameAsync((byte)MessageType.UpdateHost, JsonSerializer.SerializeToUtf8Bytes(req));

                var respPayload = await waitTask;

                var res = JsonSerializer.Deserialize<UpdateHostRes>(
                    respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (res?.ok != true)
                    throw new InvalidOperationException($"server not ok: {res?.message}");

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(UserSelectedItem.UsersNickName + "에게 권한 위임 완료");
                });

                // 현재 사용자가 방장인지 확인하여 IsHost 업데이트
                User currentUser = UserSession.Instance.CurrentUser;

                var getHostUserIdByRoomIDReqId = Guid.NewGuid();

                var hostUserIDWaitTask = session.Responses.WaitAsync(MessageType.GetHostUserIdByRoomIdRes, getHostUserIdByRoomIDReqId, TimeSpan.FromSeconds(5));

                var hostUserIDReq = new { cmd = "GetHostUserIdByRoomId", chatRoomID = currentChattingData.ChatRoomId.ToString(), requestID = getHostUserIdByRoomIDReqId };
                await transport.SendFrameAsync((byte)MessageType.GetHostUserIdByRoomId, JsonSerializer.SerializeToUtf8Bytes(hostUserIDReq));

                var hostUserIDRespPayload = await hostUserIDWaitTask;

                var hostUserIDRes = JsonSerializer.Deserialize<GetHostUserIdByRoomIdRes>(
                    hostUserIDRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (hostUserIDRes?.ok != true)
                    throw new InvalidOperationException($"server not ok: {hostUserIDRes?.message}");

                IsHost = currentUser.UserId == hostUserIDRes.hostUserId;

                // 서버에 방장 변경 업데이트
                string parsedMsg = "";
                string parsedChatRoomId = currentChattingData.ChatRoomId.ToString();

                if (transport == null)
                {
                    MessageBox.Show("네트워크 세션이 없습니다. 다시 로그인해 주세요.");
                    return;
                }

                // 채팅방 위임 메시지
                parsedMsg = $"{parsedChatRoomId}:{UserSelectedItem.UsersID}";
                byte[] parsedMsgData = Encoding.UTF8.GetBytes(parsedMsg);

                await transport.SendFrameAsync((byte)MessageType.HostChanged, parsedMsgData);

                var sendNickNameReqId = Guid.NewGuid();

                // 위임 받을 사용자 닉네임
                var hostUserNickWaitTask = session.Responses.WaitAsync(MessageType.SendNickNameRes, sendNickNameReqId, TimeSpan.FromSeconds(5));

                var hostUserNickReq = new { cmd = "SendNickName", sender = UserSelectedItem.UsersID, requestID = sendNickNameReqId };
                await transport.SendFrameAsync((byte)MessageType.SendNickName, JsonSerializer.SerializeToUtf8Bytes(hostUserNickReq));

                var hostUserNickRespPayload = await hostUserNickWaitTask;

                var hostUserNickRes = JsonSerializer.Deserialize<SendNickNameRes>(
                    hostUserNickRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (hostUserNickRes?.ok != true)
                    throw new InvalidOperationException($"server not ok: {hostUserNickRes?.message}");

                var sendNickNameReqID = Guid.NewGuid();

                // 방장 위임 공지 DB 저장
                var insertMessageWaitTask = session.Responses.WaitAsync(MessageType.InsertMessageRes, sendNickNameReqID, TimeSpan.FromSeconds(5));

                var insertMessageNickReq = new { cmd = "InsertMessage", parsedChatRoomId = Guid.Parse(parsedChatRoomId), userID = Guid.Parse(UserSelectedItem.UsersID), senderNickName = hostUserNickRes.senderNickName, requestID = sendNickNameReqID };
                await transport.SendFrameAsync((byte)MessageType.InsertMessage, JsonSerializer.SerializeToUtf8Bytes(insertMessageNickReq));

                var insertMessageRespPayload = await insertMessageWaitTask;

                var insertMessageNickRes = JsonSerializer.Deserialize<InsertMessageRes>(
                    insertMessageRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (insertMessageNickRes?.ok != true)
                    throw new InvalidOperationException($"server not ok: {insertMessageNickRes?.message}");

                // 위임 팝업 닫기
                IsMainPopupOpen = false;
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show("ERROR : " + ex);
                });
            }
        }

        // 채팅방 나가기
        public async Task ExitChatRoom(object parameter)
        {
            User currentUser = UserSession.Instance.CurrentUser;
            ChatRooms currentChatRoom = ChattingSession.Instance.CurrentChattingData;

            // 현재 채팅방의 방장 아이디 가져오기
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var hostUserIDWaitTask = session.Responses.WaitAsync(MessageType.GetHostUserIdByRoomIdRes, reqId, TimeSpan.FromSeconds(5));

            var hostUserIDReq = new { cmd = "GetHostUserIdByRoomId", chatRoomID = currentChatRoom.ChatRoomId.ToString(), requestID = reqId };
            await transport.SendFrameAsync((byte)MessageType.GetHostUserIdByRoomId, JsonSerializer.SerializeToUtf8Bytes(hostUserIDReq));

            var hostUserIDRespPayload = await hostUserIDWaitTask;

            var hostUserIDRes = JsonSerializer.Deserialize<GetHostUserIdByRoomIdRes>(
                hostUserIDRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (hostUserIDRes?.ok != true)
                throw new InvalidOperationException($"server not ok: {hostUserIDRes?.message}");

            string msg = string.Format($"{currentChatRoom.ChatRoomName} 채팅방을 나가시겠습니까?");
            MessageBoxResult messageBoxResult = MessageBox.Show(msg, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (messageBoxResult == MessageBoxResult.No)
            {
                return;
            }

            string leaveRoomData = $"{currentChatRoom.ChatRoomId}:{currentUser.UserId}";
            byte[] leaveRoomDataByte = Encoding.UTF8.GetBytes(leaveRoomData);

            int leavePayLoad = 1 + leaveRoomData.Length;
            byte[] leavePayLoadData = BitConverter.GetBytes(leavePayLoad);

            try
            {
                if (!hostUserIDRes.hostUserId.Equals(currentUser.UserId))
                {
                    var exitUserChatRoomReqId = Guid.NewGuid();

                    // 사용자와 채팅방 간의 관계 테이블에서 사용자 정보 삭제
                    var exitUserChatRoomWaitTask = session.Responses.WaitAsync(MessageType.ExitUserChatRoomRes, exitUserChatRoomReqId, TimeSpan.FromSeconds(5));

                    var exitUserChatRoomReq = new { cmd = "ExitUserChatRoom", userID = currentUser.UserId, ChatRoomID = currentChatRoom.ChatRoomId, requestID = exitUserChatRoomReqId };
                    await transport.SendFrameAsync((byte)MessageType.ExitUserChatRoom, JsonSerializer.SerializeToUtf8Bytes(exitUserChatRoomReq));

                    var exitUserChatRoomRespPayload = await exitUserChatRoomWaitTask;

                    var exitUserChatRoomRes = JsonSerializer.Deserialize<ExitUserChatRoomRes>(
                        exitUserChatRoomRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (exitUserChatRoomRes?.ok != true)
                        throw new InvalidOperationException($"server not ok: {exitUserChatRoomRes?.message}");

                    // 클라이언트에게 메시지 전송
                    await transport.SendFrameAsync((byte)MessageType.UserLeaveRoom, leaveRoomDataByte);
                }
                else
                {
                    var deleteChatRoomWithRelationReqId = Guid.NewGuid();

                    // 채팅방 데이터 및 연관된 데이터 삭제
                    var waitTask = session.Responses.WaitAsync(MessageType.DeleteChatRoomWithRelationsRes, deleteChatRoomWithRelationReqId, TimeSpan.FromSeconds(5));

                    var req = new { cmd = "DeleteChatRoomWithRelations", chatRoomID = currentChatRoom.ChatRoomId, requestID = deleteChatRoomWithRelationReqId };
                    await transport.SendFrameAsync((byte)MessageType.DeleteChatRoomWithRelations, JsonSerializer.SerializeToUtf8Bytes(req));

                    var respPayload = await waitTask;

                    var res = JsonSerializer.Deserialize<DeleteChatRoomWithRelationRes>(
                        respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (res?.ok != true)
                        throw new InvalidOperationException($"server not ok: {res?.message}");

                    // 클라이언트에게 메시지 전송
                    await transport.SendFrameAsync((byte)MessageType.UserLeaveRoom, leaveRoomDataByte);
                }

                // UI 이동
                _navigationService.NavigateToClose("ChattingWindow");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ExitChatRoom] Error: {ex.Message}");
            }
        }

        // 사용자 방출하기
        public async Task BanMember(object parameter)
        {
            User currentUser = UserSession.Instance.CurrentUser;
            ChatRooms currentChatRoom = ChattingSession.Instance.CurrentChattingData;

            // 현재 채팅방의 방장 아이디 가져오기
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var hostUserIDWaitTask = session.Responses.WaitAsync(MessageType.GetHostUserIdByRoomIdRes, reqId, TimeSpan.FromSeconds(5));

            var hostUserIDReq = new { cmd = "GetHostUserIdByRoomId", chatRoomID = currentChatRoom.ChatRoomId.ToString(),requestID = reqId };
            await transport.SendFrameAsync((byte)MessageType.GetHostUserIdByRoomId, JsonSerializer.SerializeToUtf8Bytes(hostUserIDReq));

            var hostUserIDRespPayload = await hostUserIDWaitTask;

            var hostUserIDRes = JsonSerializer.Deserialize<GetHostUserIdByRoomIdRes>(
                hostUserIDRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (hostUserIDRes?.ok != true)
                throw new InvalidOperationException($"server not ok: {hostUserIDRes?.message}");

            string msg = string.Format("\"{0}\"님을 방출하시겠습니까?", UserBanSelectedItem.UsersNickName);
            MessageBoxResult messageBoxResult = MessageBox.Show(msg, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (messageBoxResult == MessageBoxResult.No)
            {
                return;
            }
            else
            {
                // 만약 현재 사용자가 채팅방 방장이라면 Message, UserChatRoom, ChatRooms 테이블에서 관련 데이터를 모두 삭제
                if (hostUserIDRes.hostUserId == currentUser.UserId)
                {
                    var deleteBanUserChatRoomReqId = Guid.NewGuid();

                    // 사용자와 채팅방 간의 관계 정보 삭제
                    var waitTask = session.Responses.WaitAsync(MessageType.DeleteBanUserChatRoomRes, deleteBanUserChatRoomReqId, TimeSpan.FromSeconds(5));

                    var req = new { cmd = "DeleteBanUserChatRoom", chatRoomID = currentChatRoom.ChatRoomId, userIDBundle = Guid.Parse(UserBanSelectedItem.UsersID) };
                    await transport.SendFrameAsync((byte)MessageType.DeleteBanUserChatRoom, JsonSerializer.SerializeToUtf8Bytes(req));

                    var respPayload = await waitTask;

                    var res = JsonSerializer.Deserialize<DeleteBanUserChatRoomRes>(
                        respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (res?.ok != true)
                        throw new InvalidOperationException($"server not ok: {res?.message}");

                    var insertBanUserReqId = Guid.NewGuid();

                    // 방출 사용자 정보 저장
                    var insertBanUserWaitTask = session.Responses.WaitAsync(MessageType.InsertBanUserRes, insertBanUserReqId, TimeSpan.FromSeconds(5));

                    var insertBanUserReq = new { cmd = "InsertBanUser", chatRoomID = currentChatRoom.ChatRoomId, userIDBundle = UserBanSelectedItem.UsersID, requestID = insertBanUserReqId };
                    await transport.SendFrameAsync((byte)MessageType.InsertBanUser, JsonSerializer.SerializeToUtf8Bytes(insertBanUserReq));

                    var insertBanUserRespPayload = await insertBanUserWaitTask;

                    var insertBanUserRes = JsonSerializer.Deserialize<InsertBanUserRes>(
                        insertBanUserRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (insertBanUserRes?.ok != true)
                        throw new InvalidOperationException($"server not ok: {insertBanUserRes?.message}");
                }
            }
        }

        //protected override void OnClosing(CancelEventArgs e)
        //{
        //    string message = string.Format("{0}님과의 채팅을 종료하시겠습니까?", chattingPartner);

        //    MessageBoxResult messageBoxResult = MessageBox.Show(message, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
        //    if (messageBoxResult == MessageBoxResult.No)
        //    {
        //        e.Cancel = true;
        //        return;
        //    }

        //    string exitMessage = "상대방이 채팅방을 나갔습니다.";
        //    string parsedMessage = string.Format("{0}<{1}>", chattingPartner, exitMessage);
        //    byte[] byteData = Encoding.Default.GetBytes(parsedMessage);
        //    client.GetStream().Write(byteData, 0, byteData.Length);

        //    this.DialogResult = true;
        //}

        private async Task ScrollToBot()
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                var messageListView = GetMessageListView();
                if (messageListView != null && messageListView.Items.Count > 0)
                {
                    // 마지막 항목 선택
                    messageListView.SelectedIndex = messageListView.Items.Count - 1;

                    // UI 강제 갱신
                    messageListView.UpdateLayout();

                    // 선택 항목으로 스크롤
                    messageListView.ScrollIntoView(messageListView.SelectedItem);
                }
                await Task.Yield(); // 추가된 부분 (UI 스레드에서 자연스럽게 실행되도록 함)
            }, DispatcherPriority.Background);
        }

        private ListView GetMessageListView()
        {
            if (Application.Current.MainWindow is FrameworkElement mainWindow)
            {
                return mainWindow.FindName("messageListView") as ListView;
            }
            return null;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}