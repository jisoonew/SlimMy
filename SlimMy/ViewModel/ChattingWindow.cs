using SlimMy.Model;
using SlimMy.Singleton;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SlimMy.ViewModel
{
    public class ChattingWindow : INotifyPropertyChanged
    {
        private string chattingPartner = null;
        private TcpClient client = null;
        public List<string> chattingPartners = null;
        public static string myName = null;
        private Repo _repo;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";

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


        public ICommand UpdateHostCommand { get; }
        public ICommand KickMemberCommand { get; }
        public ICommand LeaveRoomCommand { get; }

        // 채팅방 설정
        public ICommand TogglePopupCommand { get; }

        public ICommand ToggleDelegatePopupCommand { get; }

        public ICommand CloseAllPopupsCommand { get; }
        public ICommand ConfirmDelegateCommand { get; }

        // 채팅방 나가기
        public ICommand ExitChatRoomCommand { get; }

        public ICommand Option1Command { get; }
        public ICommand Option2Command { get; }
        public ICommand Option3Command { get; }


        private void ExecuteOption(string option)
        {
            MessageBox.Show($"{option} Selected");
            IsMainPopupOpen = false; // 선택 후 Popup 닫기
        }

        // 1명 채팅방 입장
        public ChattingWindow(TcpClient client, string chattingPartner)
        {
            User currentUser = UserSession.Instance.CurrentUser;
            ChatRooms currentChattingData = ChattingSession.Instance.CurrentChattingData;

            // Dispatcher를 사용하여 UI 스레드에서 ListView를 찾고 설정합니다.
            Application.Current.Dispatcher.Invoke(() =>
            {
                var listView = Application.Current.MainWindow.FindName("MessageList") as ItemsControl;
                if (listView != null)
                {
                    listView.ItemsSource = MessageList;
                }

                // 내가 참가한 순간부터의 메시지를 가져온다
                var messagePrint = _repo.MessagePrint(currentChattingData.ChatRoomId, currentUser.UserId);

                // 해당 채팅방에 전달한 메시지가 있다면 메시지 출력
                if (messagePrint != null)
                {
                    foreach (var messageList in messagePrint)
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

                this.chattingPartner = chattingPartner;
                this.client = client;

                MessageList.Add(new ChatMessage
                {
                    Message = $"{chattingPartner}님이 입장하였습니다.",
                    Alignment = TextAlignment.Left
                });
                //this.Title = chattingPartner + "님과의 채팅방";

                // 로그인한 사용자가 채팅방 방장이라면 방장 권한 부여(방장 권한 UI True)
                IsHost = currentUser.UserId.ToString() == _repo.GetHostUserIdByRoomId(currentChattingData.ChatRoomId.ToString()).ToString();
            });

            Window_PreviewKeyDownCommand = new Command(Window_PreviewKeyDown);

            // 방장 위임 후보 리스트 초기화
            UpdateHost();

            // KickMemberCommand = new RelayCommand(KickMember);
            // LeaveRoomCommand = new RelayCommand(LeaveRoom);

            // Toggle 메인 팝업
            TogglePopupCommand = new RelayCommand(_ =>
            {
                IsMainPopupOpen = !IsMainPopupOpen;
                if (IsMainPopupOpen) IsDelegatePopupOpen = false;
            });

            // Toggle 방장 위임 팝업
            ToggleDelegatePopupCommand = new RelayCommand(_ =>
            {
                IsDelegatePopupOpen = !IsDelegatePopupOpen;
                if (IsDelegatePopupOpen) IsMainPopupOpen = false;
            });

            // 모든 팝업 닫기
            CloseAllPopupsCommand = new RelayCommand(_ =>
            {
                IsMainPopupOpen = false;
                IsDelegatePopupOpen = false;
            });

            // 방장 위임 기능
            ConfirmDelegateCommand = new Command(UpdateHostBtn);

            // 채팅방 나가기
            ExitChatRoomCommand = new Command(ExitChatRoom);

            Option1Command = new RelayCommand(_ => ExecuteOption("멤버 내보내기"));
            Option2Command = new RelayCommand(_ => ExecuteOption("방장 위임"));
            Option3Command = new RelayCommand(_ => ExecuteOption("채팅방 나가기"));

            // MessageList.CollectionChanged += (s, e) => ScrollToBot(); // 메시지 추가 시 자동 스크롤
            ScrollToBot();
        }

        // 다수 채팅방 입장
        public ChattingWindow(TcpClient client, List<string> targetChattingPartners)
        {
            try
            {
                ChatRooms currentChattingData = ChattingSession.Instance.CurrentChattingData;
                User currentUser = UserSession.Instance.CurrentUser;

                this.client = client;
                this.chattingPartners = targetChattingPartners;

                _repo = new Repo(_connstring); // Repo 초기화

                // Dispatcher를 사용하여 UI 스레드에서 ListView를 찾고 설정합니다.
                Application.Current.Dispatcher.Invoke(() =>
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

                    var messagePrint = _repo.MessagePrint(currentChattingData.ChatRoomId, currentUser.UserId);

                    // 해당 채팅방에 전달한 메시지가 있다면 메시지 출력
                    if (messagePrint != null)
                    {
                        foreach (var messageList in messagePrint)
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

                    // 로그인한 사용자가 채팅방 방장이라면 방장 권한 부여(방장 권한 UI True)
                    IsHost = currentUser.UserId.ToString() == _repo.GetHostUserIdByRoomId(currentChattingData.ChatRoomId.ToString()).ToString();
                });

                SendCommand = new Command(Send_btn_Click);

                Window_PreviewKeyDownCommand = new Command(Window_PreviewKeyDown);

                // 방장 위임 후보 리스트 초기화
                UpdateHost();

                // Toggle 메인 팝업
                TogglePopupCommand = new RelayCommand(_ =>
                {
                    IsMainPopupOpen = !IsMainPopupOpen;
                    if (IsMainPopupOpen) IsDelegatePopupOpen = false;
                });

                // Toggle 방장 위임 팝업
                ToggleDelegatePopupCommand = new RelayCommand(_ =>
                {
                    IsDelegatePopupOpen = !IsDelegatePopupOpen;
                    if (IsDelegatePopupOpen) IsMainPopupOpen = false;
                });

                // 모든 팝업 닫기
                CloseAllPopupsCommand = new RelayCommand(_ =>
                {
                    IsMainPopupOpen = false;
                    IsDelegatePopupOpen = false;
                });

                // 방장 위임 기능
                ConfirmDelegateCommand = new Command(UpdateHostBtn);

                // 채팅방 나가기
                ExitChatRoomCommand = new Command(ExitChatRoom);

                Option1Command = new RelayCommand(_ => ExecuteOption("멤버 내보내기"));
                Option2Command = new RelayCommand(_ => ExecuteOption("방장 위임"));
                Option3Command = new RelayCommand(_ => ExecuteOption("채팅방 나가기"));

                ScrollToBot();
            }
            catch (Exception ex)
            {
                MessageBox.Show("채팅방 Error : " + ex);
            }
        }

        // 메시지 전송
        private void Send_btn_Click(object parameter)
        {
            if (string.IsNullOrEmpty(MessageText))
                return;
            string message = MessageText;
            string parsedMessage = "";
            _repo = new Repo(_connstring); // Repo 초기화

            try
            {
                if (message.Contains('<') || message.Contains('>'))
                {
                    MessageBox.Show("죄송합니다. >,< 기호는 사용하실수 없습니다.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (chattingPartner != null)
                {
                    parsedMessage = string.Format("{0}<{1}>", chattingPartner, message);
                    byte[] byteData = Encoding.Default.GetBytes(parsedMessage);
                    client.GetStream().Write(byteData, 0, byteData.Length);
                }
                // 그룹채팅
                else
                {
                    User currentUser = UserSession.Instance.CurrentUser;
                    ChatRooms currentChatRooms = ChattingSession.Instance.CurrentChattingData;
                    string myName = currentUser.NickName;
                    string myUid = currentUser.UserId.ToString();
                    string partners = myUid;
                    foreach (var item in chattingPartners)
                    {
                        if (item == myUid)
                            continue;
                        partners += "#" + item;
                    }

                    parsedMessage = string.Format("{0}<{1}>", partners, message);
                    byte[] byteData = Encoding.Default.GetBytes(parsedMessage);
                    client.GetStream().Write(byteData, 0, byteData.Length);

                    _repo.InsertMessage(currentChatRooms.ChatRoomId, Guid.Parse(myUid), message);
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
                client.GetStream().Write(byteData, 0, byteData.Length);
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
                client.GetStream().Write(byteData, 0, byteData.Length);
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


        public void ReceiveMessage(string sender, string message)
        {
            if (message == "ChattingStart")
            {
                return;
            }

            if (message == "상대방이 채팅방을 나갔습니다.")
            {
                string parsedMessage = string.Format("{0}님이 채팅방을 나갔습니다.", sender);

                MessageList.Add(new ChatMessage
                {
                    Message = $"{sender}님이 채팅방을 나갔습니다.",
                    Alignment = TextAlignment.Left
                });

                ScrollToBot();
                return;
            }

            string senderNickName = _repo.SendNickName(sender);

            Application.Current.Dispatcher.Invoke(() =>
            {

                MessageList.Add(new ChatMessage
                {
                    Message = $"{senderNickName}: {message}",
                    Alignment = TextAlignment.Left
                });

                ScrollToBot();
            });
        }

        private ChatUserList _chatUserList;

        public ChatUserList ChatUserList
        {
            get { return _chatUserList; }
            set { _chatUserList = value; OnPropertyChanged(nameof(User)); }
        }

        // 방장 위임 알림 출력
        public void ReceiveHostChangedMessage(List<string> hostData, string message)
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
            string hostUserNick = _repo.SendNickName(hostChangedUserID);

            // 현재 입장한 채팅방과 위임을 진행하는 채팅방이 일치하면 방장 위임 채팅방 알림
            if (currentChattingData.ChatRoomId.ToString() == hostChangedChattingRoomID)
            {
                // 방장 위임 문구 출력
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageList.Add(new ChatMessage
                    {
                        Message = $"{hostUserNick}님이 새로운 방장이 되었습니다.",
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
        private void UpdateHost()
        {
            // 같은 채팅방의 사용자 닉네임을 담은 리스트
            DelegateCandidateList = new ObservableCollection<ChatUserList>();

            // 현재 채팅방 ID 가져오기
            ChatRooms currentChattingData = ChattingSession.Instance.CurrentChattingData;
            Guid currentChatRoomId = currentChattingData.ChatRoomId;

            // SelectChatUserNickName으로 데이터 가져오기
            var usersInChatRoom = _repo.SelectChatUserNickName(currentChatRoomId);
            Application.Current.Dispatcher.Invoke(() =>
            {
                // DelegateCandidateList에 추가
                foreach (var user in usersInChatRoom)
                {
                    DelegateCandidateList.Add(user);
                }
            });

            OnPropertyChanged(nameof(DelegateCandidateList));
        }

        // 방장 위임 버튼
        private void UpdateHostBtn(object parameter)
        {
            try
            {
                ChatRooms currentChattingData = ChattingSession.Instance.CurrentChattingData;

                // 방장이었던 사용자는 isowner = 0, 지목 당한 사용자는 isowner = 1
                _repo.UpdateHost(currentChattingData.ChatRoomId, UserSelectedItem.UsersID);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(UserSelectedItem.UsersNickName + "에게 권한 위임 완료");
                });

                // 현재 사용자가 방장인지 확인하여 IsHost 업데이트
                User currentUser = UserSession.Instance.CurrentUser;
                IsHost = currentUser.UserId == _repo.GetHostUserIdByRoomId(currentChattingData.ChatRoomId.ToString());

                // 서버에 방장 변경 업데이트
                string parsedMessage = "";
                string parsedChatRoomId = currentChattingData.ChatRoomId.ToString();

                // 채팅방 아이디와 위임 받을 사용자 아이디
                parsedMessage = string.Format("HostChanged:{0}:{1}", parsedChatRoomId, UserSelectedItem.UsersID);
                byte[] byteData = Encoding.Default.GetBytes(parsedMessage);
                client.GetStream().Write(byteData, 0, byteData.Length);

                // 위임 받을 사용자 닉네임
                string hostUserNick = _repo.SendNickName(UserSelectedItem.UsersID);

                // 방장 위임 공지 DB 저장
                _repo.InsertMessage(Guid.Parse(parsedChatRoomId), Guid.Parse(UserSelectedItem.UsersID), $"{hostUserNick}님이 새로운 방장이 되었습니다.");

                // 위임 팝업 닫기
                IsMainPopupOpen = false;

                // Debug.WriteLine("ChatRoomId : " + currentChattingData.ChatRoomId.ToString() + "\nUsersID : " + UserSelectedItem.UsersID);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("ERROR : " + ex);
                });
            }
        }

        // 채팅방 나가기
        public void ExitChatRoom(object parameter)
        {
            User currentUser = UserSession.Instance.CurrentUser;
            ChatRooms currentChatRoom = ChattingSession.Instance.CurrentChattingData;

            // 현재 채팅방의 방장 아이디 가져오기
            Guid selectUserID = _repo.GetHostUserIdByRoomId(currentChatRoom.ChatRoomId.ToString());
            
            // 현재 사용자와 채팅방 방장이 동일인물이 아니라면 사용자와 채팅방 간의 관계 테이블에서 채팅방을 나가고자 하는 사용자 아이디만 삭제
            if (selectUserID != currentUser.UserId)
            {
                // 사용자와 채팅방 간의 관계 테이블에서 사용자 정보 삭제
                _repo.ExitUserChatRoom(currentUser.UserId, currentChatRoom.ChatRoomId);

                string chatRoomKey = string.Join(",", currentChatRoom.ChatRoomId.ToString());
                
            }

            // 만약 현재 사용자가 채팅방 방장이라면 Message, UserChatRoom, ChatRooms 테이블에서 관련 데이터를 모두 삭제
            if(selectUserID == currentUser.UserId)
            {
                _repo.DeleteChatRoomWithRelations(currentChatRoom.ChatRoomId);
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

        private void ScrollToBot()
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
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
            }));
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
