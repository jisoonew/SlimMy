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

        private bool _isPopupOpen;

        // Popup 열림 상태 관리
        public bool IsPopupOpen
        {
            get => _isPopupOpen;
            set
            {
                _isPopupOpen = value;
                OnPropertyChanged(nameof(IsPopupOpen));
            }
        }

        public ICommand UpdateHostCommand { get; }
        public ICommand KickMemberCommand { get; }
        public ICommand LeaveRoomCommand { get; }

        // Commands
        public ICommand TogglePopupCommand { get; }
        public ICommand Option1Command { get; }
        public ICommand Option2Command { get; }
        public ICommand Option3Command { get; }


        private void ExecuteOption(string option)
        {
            MessageBox.Show($"{option} Selected");
            IsPopupOpen = false; // 선택 후 Popup 닫기
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


                if (currentUser.UserId.ToString() == _repo.GetHostUserIdByRoomId(currentChattingData.ChatRoomId).ToString())
                {
                    IsHost = true; // 방장
                }
                else
                {
                    IsHost = false;
                }
            });

            Window_PreviewKeyDownCommand = new Command(Window_PreviewKeyDown);

            UpdateHostCommand = new RelayCommand(UpdateHost);
            // KickMemberCommand = new RelayCommand(KickMember);
            // LeaveRoomCommand = new RelayCommand(LeaveRoom);

            TogglePopupCommand = new RelayCommand(_ => IsPopupOpen = !IsPopupOpen);
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

                    if (currentUser.UserId.ToString() == _repo.GetHostUserIdByRoomId(currentChattingData.ChatRoomId).ToString())
                    {
                        IsHost = true; // 방장
                    }
                    else
                    {
                        IsHost = false;
                    }
                });

                SendCommand = new Command(Send_btn_Click);

                Window_PreviewKeyDownCommand = new Command(Window_PreviewKeyDown);

                TogglePopupCommand = new RelayCommand(_ => IsPopupOpen = !IsPopupOpen);
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

        // 방장 변경하기(= 위임)
        private void UpdateHost(object parameter)
        {

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
