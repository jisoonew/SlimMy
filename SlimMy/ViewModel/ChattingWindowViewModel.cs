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
using System.Threading;
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
                OnPropertyChanged(nameof(UserSelectedItem));
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
                OnPropertyChanged(nameof(UserBanSelectedItem));
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
                // 내가 참가한 순간부터 메시지를 가져온다
                var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendMessagePrintOnceAsync(roomId, currentUser), getMessage: r => r.Message, userData: currentUser);

                if (res?.Ok != true)
                    throw new InvalidOperationException($"server not ok: {res?.Message}");

                // 해당 채팅방에 전달한 메시지가 있다면 메시지 출력
                if (res.MessageBundle != null)
                {
                    foreach (var messageList in res.MessageBundle)
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

            // KickMemberCommand = new RelayCommand(KickMember);
            // LeaveRoomCommand = new RelayCommand(LeaveRoom);

            // Toggle 메인 팝업
            TogglePopupCommand = new RelayCommand(_ =>
            {
                IsMainPopupOpen = !IsMainPopupOpen;
                if (IsMainPopupOpen) IsDelegatePopupOpen = false; IsBanPopupOpen = false;
            });

            // Toggle 방장 위임 팝업
            ToggleDelegatePopupCommand = new AsyncRelayCommand(async _ =>
            {
                await UpdateHost();
                IsDelegatePopupOpen = true;
                IsMainPopupOpen = false;
                IsBanPopupOpen = false;
            });

            // Toggle 방출하기 팝업
            ToggleBanPopupCommand = new AsyncRelayCommand(async _ =>
            {
                await UpdateHost();
                IsBanPopupOpen = true;
                IsMainPopupOpen = false;
                IsDelegatePopupOpen = false;
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

            var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendGetHostUserIdByRoomIdOnceAsync(currentChattingData), getMessage: r => r.Message, userData: currentUser);

            if (res?.Ok != true)
                throw new InvalidOperationException($"server not ok: {res?.Message}");

            // 비교 후 IsHost 설정 (Guid 타입끼리 비교)
            IsHost = (currentUserId == res.HostUserId);

            // 디버깅용 로그
            Debug.WriteLine($"currentUserId: {currentUserId}");
            Debug.WriteLine($"hostUserId   : {res.HostUserId}");
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

                    var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendMessagePrintOnceAsync(currentChattingData, currentUser), getMessage: r => r.Message, userData: currentUser);

                    if (res?.Ok != true)
                        throw new InvalidOperationException($"server not ok: {res?.Message}");

                    // 해당 채팅방에 전달한 메시지가 있다면 메시지 출력
                    if (res.MessageBundle != null)
                    {
                        foreach (var messageList in res.MessageBundle)
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

                    var hostUserIDRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendGetHostUserIdByRoomIdOnceAsync(currentChattingData), getMessage: r => r.Message, userData: currentUser);

                    if (hostUserIDRes?.Ok != true)
                        throw new InvalidOperationException($"server not ok: {hostUserIDRes?.Message}");

                    IsHost = currentUser.UserId.ToString() == hostUserIDRes.HostUserId.ToString();
                });

                SendCommand = new AsyncRelayCommand(Send_btn_Click);

                Window_PreviewKeyDownCommand = new Command(Window_PreviewKeyDown);

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

                    var transport = UserSession.Instance.CurrentUser?.Transport;
                    if (transport == null)
                    {
                        MessageBox.Show("네트워크 세션이 없습니다. 다시 로그인해 주세요.");
                        return;
                    }

                    // 페이로드
                    parsedMessage = $"{currentChatRooms.ChatRoomId}:{message}:{myUid}";
                    byte[] parsedMessageByte = Encoding.UTF8.GetBytes(parsedMessage);

                    await transport.SendFrameAsync(MessageType.ChatContent, parsedMessageByte);
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
            User currentUser = UserSession.Instance.CurrentUser;

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

            var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendSendNickNameOnceAsync(sender), getMessage: r => r.Message, userData: currentUser);

            if (res?.Ok != true)
                throw new InvalidOperationException($"server not ok: {res?.Message}");

            // 메시지를 보낸 사용자와 로그인 사용자가 같은 사람이 아니라면
            if (Guid.TryParse(sender, out var senderGuid) && senderGuid != currentUser.UserId)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageList.Add(new ChatMessage
                    {
                        Message = $"{res.SenderNickName}: {message}",
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

            var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendSendNickNameOnceAsync(sender), getMessage: r => r.Message, userData: currentUser);

            if (res?.Ok != true)
                throw new InvalidOperationException($"server not ok: {res?.Message}");

            // 메시지를 보낸 사용자와 로그인 사용자가 같은 사람이 아니라면
            if (!sender.Equals(currentUser.UserId.ToString()))
            {
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    MessageList.Add(new ChatMessage
                    {
                        Message = $"{res.SenderNickName}님이 참여하였습니다.",
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

            User currentUser = UserSession.Instance.CurrentUser;

            // 방장 위임을 진행하는 채팅방 아이디
            string hostChangedChattingRoomID = hostData[0];
            // 방장 위임을 받는 사용자 아이디
            string hostChangedUserID = hostData[1];

            ChatRooms currentChattingData = ChattingSession.Instance.CurrentChattingData;

            // 방장 위임을 받는 사용자 닉네임
            var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendSendNickNameOnceAsync(hostChangedUserID), getMessage: r => r.Message, userData: currentUser);

            if (res?.Ok != true)
                throw new InvalidOperationException($"server not ok: {res?.Message}");

            // 현재 입장한 채팅방과 위임을 진행하는 채팅방이 일치하면 방장 위임 채팅방 알림
            if (currentChattingData.ChatRoomId.ToString() == hostChangedChattingRoomID)
            {
                // 방장 위임 문구 출력
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageList.Add(new ChatMessage
                    {
                        Message = $"{res.SenderNickName}님이 새로운 방장이 되었습니다.",
                        Alignment = TextAlignment.Left
                    });
                });
            }

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

            User currentUser = UserSession.Instance.CurrentUser;

            // SelectChatUserNickName으로 데이터 가져오기
            var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendSelectChatUserNickNameOnceAsync(currentChatRoomId), getMessage: r => r.Message, userData: currentUser);

            if (res?.Ok != true)
                throw new InvalidOperationException($"server not ok: {res?.Message}");

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // DelegateCandidateList에 추가
                foreach (var user in res.UsersInChatRoom)
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
                User currentUser = UserSession.Instance.CurrentUser;

                ChatRooms currentChattingData = ChattingSession.Instance.CurrentChattingData;

                // 방장이었던 사용자는 isowner = 0, 지목 당한 사용자는 isowner = 1
                var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendUpdateHostOnceAsync(currentChattingData), getMessage: r => r.Message, userData: currentUser);

                if (res?.Ok != true)
                    throw new InvalidOperationException($"server not ok: {res?.Message}");

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(UserSelectedItem.UsersNickName + "에게 권한 위임 완료");
                });

                // 현재 사용자가 방장인지 확인하여 IsHost 업데이트
                var hostUserIDRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendGetHostUserIdByRoomIdOnceAsync(currentChattingData), getMessage: r => r.Message, userData: currentUser);

                if (hostUserIDRes?.Ok != true)
                    throw new InvalidOperationException($"server not ok: {hostUserIDRes?.Message}");

                IsHost = currentUser.UserId == hostUserIDRes.HostUserId;

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

                await transport.SendFrameAsync(MessageType.HostChanged, parsedMsgData);

                // 위임 받을 사용자 닉네임
                var hostUserNickRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendSendNickNameOnceAsync(UserSelectedItem.UsersID), getMessage: r => r.Message, userData: currentUser);

                // 세션이 만료되면 로그인 창만 실행
                if (HandleAuthError(hostUserNickRes?.Message))
                    return;

                if (hostUserNickRes?.Ok != true)
                    throw new InvalidOperationException($"server not ok: {hostUserNickRes?.Message}");

                // 방장 위임 공지 DB 저장
                var insertMessageNickRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendInsertMessageOnceAsync(parsedChatRoomId, hostUserNickRes), getMessage: r => r.Message, userData: currentUser);

                if (insertMessageNickRes?.Ok != true)
                    throw new InvalidOperationException($"server not ok: {insertMessageNickRes?.Message}");

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
            var hostUserIDRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendGetHostUserIdByRoomIdOnceAsync(currentChatRoom), getMessage: r => r.Message, userData: currentUser);

            if (hostUserIDRes?.Ok != true)
                throw new InvalidOperationException($"server not ok: {hostUserIDRes?.Message}");

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
                if (!hostUserIDRes.HostUserId.Equals(currentUser.UserId))
                {
                    // 사용자와 채팅방 간의 관계 테이블에서 사용자 정보 삭제
                    var exitUserChatRoomRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendExitUserChatRoomOnceAsync(currentUser, currentChatRoom), getMessage: r => r.Message, userData: currentUser);

                    if (exitUserChatRoomRes?.Ok != true)
                        throw new InvalidOperationException($"server not ok: {exitUserChatRoomRes?.Message}");

                    // 클라이언트에게 메시지 전송
                    await transport.SendFrameAsync(MessageType.UserLeaveRoom, leaveRoomDataByte);
                }
                else
                {
                    // 채팅방 데이터 및 연관된 데이터 삭제
                    var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendDeleteChatRoomWithRelationsOnceAsync(currentChatRoom), getMessage: r => r.Message, userData: currentUser);

                    if (res?.Ok != true)
                        throw new InvalidOperationException($"server not ok: {res?.Message}");

                    // 클라이언트에게 메시지 전송
                    await transport.SendFrameAsync(MessageType.UserLeaveRoom, leaveRoomDataByte);
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
            var hostUserIDRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendGetHostUserIdByRoomIdOnceAsync(currentChatRoom), getMessage: r => r.Message, userData: currentUser);

            if (hostUserIDRes?.Ok != true)
                throw new InvalidOperationException($"server not ok: {hostUserIDRes?.Message}");

            string msg = string.Format("\"{0}\"님을 방출하시겠습니까?", UserBanSelectedItem.UsersNickName);
            MessageBoxResult messageBoxResult = MessageBox.Show(msg, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (messageBoxResult == MessageBoxResult.No)
            {
                return;
            }
            else
            {
                // 만약 현재 사용자가 채팅방 방장이라면 Message, UserChatRoom, ChatRooms 테이블에서 관련 데이터를 모두 삭제
                if (hostUserIDRes.HostUserId == currentUser.UserId)
                {
                    // 사용자와 채팅방 간의 관계 정보 삭제
                    var res = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendDeleteBanUserChatRoomOnceAsync(currentChatRoom), getMessage: r => r.Message, userData: currentUser);

                    if (res?.Ok != true)
                        throw new InvalidOperationException($"server not ok: {res?.Message}");

                    // 방출 사용자 정보 저장
                    var insertBanUserRes = await SendWithRefreshRetryOnceAsync(sendOnceAsync: () => SendInsertBanUserOnceAsync(currentChatRoom), getMessage: r => r.Message, userData: currentUser);

                    if (insertBanUserRes?.Ok != true)
                        throw new InvalidOperationException($"server not ok: {insertBanUserRes?.Message}");

                    string banRoomData = $"{currentChatRoom.ChatRoomId}:{UserBanSelectedItem.UsersID}";
                    byte[] banRoomDataByte = Encoding.UTF8.GetBytes(banRoomData);

                    // 방출 당한 사용자에게 알림 보내기
                    await transport.SendFrameAsync(MessageType.RoomBanMessage, banRoomDataByte);
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

        private static readonly SemaphoreSlim _refreshLock = new(1, 1);

        // 토큰 발급
        private async Task<bool> TryRefreshAsync(User userData)
        {
            await _refreshLock.WaitAsync();

            try
            {
                var session = UserSession.Instance;
                var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

                var authErrorResReqId = Guid.NewGuid();
                var authErrorWaitTask = session.Responses.WaitAsync(MessageType.UserRefreshTokenRes, authErrorResReqId, TimeSpan.FromSeconds(5));

                var authErrorReq = new { cmd = "UserRefreshToken", userID = userData.UserId, accessToken = UserSession.Instance.AccessToken, requestID = authErrorResReqId };
                await transport.SendFrameAsync(MessageType.UserRefreshToken, JsonSerializer.SerializeToUtf8Bytes(authErrorReq));

                var authErrorRespPayload = await authErrorWaitTask;

                var authErrorWeightRes = JsonSerializer.Deserialize<UserRefreshTokenRes>(
                    authErrorRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                Debug.WriteLine($"[확인] Refresh OK, newToken={authErrorWeightRes.NewAccessToken}, " + authErrorWeightRes.Ok);

                if (authErrorWeightRes.Ok == true)
                {
                    UserSession.Instance.AccessToken = authErrorWeightRes.NewAccessToken;

                    Debug.WriteLine($"[CLIENT] Refresh OK, newToken={UserSession.Instance.AccessToken}");

                    return true;
                }
                return false;
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        private async Task<TRes?> SendWithRefreshRetryOnceAsync<TRes>(Func<Task<TRes?>> sendOnceAsync, Func<TRes?, string?> getMessage, User userData)
        {
            var res = await sendOnceAsync();

            // 토큰 만료가 아니라면
            if (!IsAuthExpired(getMessage(res)))
            {
                return res;
            }

            // 토큰 발급
            var refreched = await TryRefreshAsync(userData);

            // 토큰 발급이 정상적으로 진행이 안되었다면
            if (!refreched)
            {
                return res;
            }

            return await sendOnceAsync();
        }

        private async Task<MessagePrintRes> SendMessagePrintOnceAsync(Guid roomId, User currentUser)
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.MessagePrintRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "MessagePrint", chatRoomID = roomId, userID = currentUser.UserId, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.MessagePrint, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<MessagePrintRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task<GetHostUserIdByRoomIdRes> SendGetHostUserIdByRoomIdOnceAsync(ChatRooms currentChattingData)
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.GetHostUserIdByRoomIdRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "GetHostUserIdByRoomId", userID = session.CurrentUser.UserId, chatRoomID = currentChattingData.ChatRoomId.ToString(), accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.GetHostUserIdByRoomId, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<GetHostUserIdByRoomIdRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task<MessagePrintRes> SendMessagePrintOnceAsync(ChatRooms currentChattingData, User currentUser)
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.MessagePrintRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "MessagePrint", chatRoomID = currentChattingData.ChatRoomId, userID = currentUser.UserId, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.MessagePrint, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<MessagePrintRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task<SendNickNameRes> SendSendNickNameOnceAsync(string sender)
        {
            var session = UserSession.Instance;
            User currentUser = UserSession.Instance.CurrentUser;

            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.SendNickNameRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "SendNickName", userID = currentUser.UserId, sender = sender, accessToken = UserSession.Instance.AccessToken, requestID = reqId };

            var json = Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(req));
            Debug.WriteLine("[SendNickName][Client JSON] " + json);

            await transport.SendFrameAsync(MessageType.SendNickName, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<SendNickNameRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task<SelectChatUserNickNameRes> SendSelectChatUserNickNameOnceAsync(Guid currentChatRoomId)
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.SelectChatUserNickNameRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "SelectChatUserNickName", userID = session.CurrentUser.UserId, currentChatRoomId = currentChatRoomId, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.SelectChatUserNickName, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<SelectChatUserNickNameRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task<UpdateHostRes> SendUpdateHostOnceAsync(ChatRooms currentChattingData)
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var waitTask = session.Responses.WaitAsync(MessageType.UpdateHostRes, reqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "UpdateHost", userID = session.CurrentUser.UserId, chatRoomID = currentChattingData.ChatRoomId, userIDBundle = UserSelectedItem.UsersID, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.UpdateHost, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<UpdateHostRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task<InsertMessageRes> SendInsertMessageOnceAsync(string parsedChatRoomId, SendNickNameRes hostUserNickRes)
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var reqId = Guid.NewGuid();

            var insertMessageWaitTask = session.Responses.WaitAsync(MessageType.InsertMessageRes, reqId, TimeSpan.FromSeconds(5));

            var insertMessageNickReq = new { cmd = "InsertMessage", parsedChatRoomId = Guid.Parse(parsedChatRoomId), userID = Guid.Parse(UserSelectedItem.UsersID), senderNickName = hostUserNickRes.SenderNickName, accessToken = UserSession.Instance.AccessToken, requestID = reqId };
            await transport.SendFrameAsync(MessageType.InsertMessage, JsonSerializer.SerializeToUtf8Bytes(insertMessageNickReq));

            var insertMessageRespPayload = await insertMessageWaitTask;

            return JsonSerializer.Deserialize<InsertMessageRes>(
                insertMessageRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task<ExitUserChatRoomRes> SendExitUserChatRoomOnceAsync(User currentUser, ChatRooms currentChatRoom)
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var exitUserChatRoomReqId = Guid.NewGuid();

            // 사용자와 채팅방 간의 관계 테이블에서 사용자 정보 삭제
            var exitUserChatRoomWaitTask = session.Responses.WaitAsync(MessageType.ExitUserChatRoomRes, exitUserChatRoomReqId, TimeSpan.FromSeconds(5));

            var exitUserChatRoomReq = new { cmd = "ExitUserChatRoom", userID = currentUser.UserId, ChatRoomID = currentChatRoom.ChatRoomId, accessToken = UserSession.Instance.AccessToken, requestID = exitUserChatRoomReqId };
            await transport.SendFrameAsync(MessageType.ExitUserChatRoom, JsonSerializer.SerializeToUtf8Bytes(exitUserChatRoomReq));

            var exitUserChatRoomRespPayload = await exitUserChatRoomWaitTask;

            return JsonSerializer.Deserialize<ExitUserChatRoomRes>(
                exitUserChatRoomRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task<DeleteChatRoomWithRelationRes> SendDeleteChatRoomWithRelationsOnceAsync(ChatRooms currentChatRoom)
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var deleteChatRoomWithRelationReqId = Guid.NewGuid();

            // 채팅방 데이터 및 연관된 데이터 삭제
            var waitTask = session.Responses.WaitAsync(MessageType.DeleteChatRoomWithRelationsRes, deleteChatRoomWithRelationReqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "DeleteChatRoomWithRelations", userID = session.CurrentUser.UserId, chatRoomID = currentChatRoom.ChatRoomId, accessToken = UserSession.Instance.AccessToken, requestID = deleteChatRoomWithRelationReqId };
            await transport.SendFrameAsync(MessageType.DeleteChatRoomWithRelations, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<DeleteChatRoomWithRelationRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task<DeleteBanUserChatRoomRes> SendDeleteBanUserChatRoomOnceAsync(ChatRooms currentChatRoom)
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var deleteBanUserChatRoomReqId = Guid.NewGuid();

            // 사용자와 채팅방 간의 관계 정보 삭제
            var waitTask = session.Responses.WaitAsync(MessageType.DeleteBanUserChatRoomRes, deleteBanUserChatRoomReqId, TimeSpan.FromSeconds(5));

            var req = new { cmd = "DeleteBanUserChatRoom", userID = session.CurrentUser.UserId, chatRoomID = currentChatRoom.ChatRoomId, userIDBundle = Guid.Parse(UserBanSelectedItem.UsersID), accessToken = UserSession.Instance.AccessToken, requestID = deleteBanUserChatRoomReqId };
            await transport.SendFrameAsync(MessageType.DeleteBanUserChatRoom, JsonSerializer.SerializeToUtf8Bytes(req));

            var respPayload = await waitTask;

            return JsonSerializer.Deserialize<DeleteBanUserChatRoomRes>(
                respPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task<InsertBanUserRes> SendInsertBanUserOnceAsync(ChatRooms currentChatRoom)
        {
            var session = UserSession.Instance;
            var transport = session.CurrentUser?.Transport ?? throw new InvalidOperationException("not connected");

            var insertBanUserReqId = Guid.NewGuid();

            // 방출 사용자 정보 저장
            var insertBanUserWaitTask = session.Responses.WaitAsync(MessageType.InsertBanUserRes, insertBanUserReqId, TimeSpan.FromSeconds(5));

            var insertBanUserReq = new { cmd = "InsertBanUser", userID = session.CurrentUser.UserId, chatRoomID = currentChatRoom.ChatRoomId, userIDBundle = UserBanSelectedItem.UsersID, accessToken = UserSession.Instance.AccessToken, requestID = insertBanUserReqId };
            await transport.SendFrameAsync(MessageType.InsertBanUser, JsonSerializer.SerializeToUtf8Bytes(insertBanUserReq));

            var insertBanUserRespPayload = await insertBanUserWaitTask;

            return JsonSerializer.Deserialize<InsertBanUserRes>(
                insertBanUserRespPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // 토큰 만료
        private bool IsAuthExpired(string? message) => string.Equals(message, "expired token", StringComparison.OrdinalIgnoreCase) || string.Equals(message, "unauthorized", StringComparison.OrdinalIgnoreCase);

        // 세션 만료
        private bool HandleAuthError(string message)
        {
            if (message == "unauthorized" || message == "expired token")
            {
                UserSession.Instance.Clear();

                // 모든 창을 닫고 로그인 창만 생성
                _navigationService.NavigateToLoginOnly();

                return true;
            }
            return false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}