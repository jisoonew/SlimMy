using SlimMy.Model;
using SlimMy.Singleton;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
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
        private View.ChattingWindow _chattingWindow;
        public static string myName = null;
        private Repo _repo;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";

        public ICommand Window_PreviewKeyDownCommand { get; private set; }

        private ObservableCollection<object> messageList = new ObservableCollection<object>();

        public ObservableCollection<object> MessageList
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

        // 1명 채팅방 입장
        public ChattingWindow(TcpClient client, string chattingPartner)
        {
            // Dispatcher를 사용하여 UI 스레드에서 ListView를 찾고 설정합니다.
            Application.Current.Dispatcher.Invoke(() =>
            {
                var listView = Application.Current.MainWindow.FindName("messageListView") as ListView;
                if (listView != null)
                {
                    listView.ItemsSource = messageList;
                }
            });

            ChatRooms currentChattingData = ChattingSession.Instance.CurrentChattingData;
            var messagePrint = _repo.MessagePrint(currentChattingData.ChatRoomId);

            foreach (var messageList in messagePrint)
            {
                MessageList.Add(string.Format("{0}: {1}", messageList.SendUser, messageList.SendMessage));
            }

            this.chattingPartner = chattingPartner;
            this.client = client;
            MessageList.Add(string.Format("{0}님이 입장하였습니다.", chattingPartner));
            //this.Title = chattingPartner + "님과의 채팅방";

            //Window_PreviewKeyDownCommand = new Command(Window_PreviewKeyDown);
        }

        // 다수 채팅방 입장
        public ChattingWindow(TcpClient client, List<string> targetChattingPartners)
        {
            try
            {
                this.client = client;
                this.chattingPartners = targetChattingPartners;

                _repo = new Repo(_connstring); // Repo 초기화

                // Dispatcher를 사용하여 UI 스레드에서 ListView를 찾고 설정합니다.
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var listView = Application.Current.MainWindow.FindName("messageListView") as ListView;
                    if (listView != null)
                    {
                        listView.ItemsSource = MessageList;
                    }
                });

                string enteredUser = "";
                foreach (var item in targetChattingPartners)
                {
                    enteredUser += item;
                    enteredUser += "님, ";
                }

                ChatRooms currentChattingData = ChattingSession.Instance.CurrentChattingData;
                var messagePrint = _repo.MessagePrint(currentChattingData.ChatRoomId);

                if(messagePrint != null)
                {
                    foreach (var messageDataList in messagePrint)
                    {
                        //MessageBox.Show(string.Format("{0}: {1}", messageDataList.SendUser, messageDataList.SendMessage));
                        messageList.Add(string.Format("{0}: {1}", messageDataList.SendUser, messageDataList.SendMessage));
                    }
                }

                messageList.Add(string.Format("{0}이 입장하였습니다.", enteredUser));
                //this.Title = enteredUser + "과의 채팅방";

                SendCommand = new Command(Send_btn_Click);

                // Window_PreviewKeyDownCommand = new Command(Window_PreviewKeyDown);
            } catch (Exception ex)
            {
                MessageBox.Show("Error : " + ex);
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
                messageList.Add("나: " + message);
                // 메시지 전송 후 초기화
                MessageText = string.Empty;

                //ScrollToBot();
            }
            catch (Exception ex)
            {
                MessageBox.Show("오류 발생 : " + ex);
            }
        }

        private void Window_PreviewKeyDown(object parameter)
        {
            var Send_Text_Box = Application.Current.MainWindow.FindName("Send_Text_Box") as TextBox;

            if (string.IsNullOrEmpty(Send_Text_Box.Text))
                return;
            string message = Send_Text_Box.Text;
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

            messageList.Add("나: " + message);
            Send_Text_Box.Clear();

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
                messageList.Add(parsedMessage);

                ScrollToBot();
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                messageList.Add($"{sender}: {message}");
                //    messageListView.ScrollIntoView(messageListView.Items[messageListView.Items.Count - 1]);

                //    ScrollToBot();
            });
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
            var messageListView = Application.Current.MainWindow.FindName("messageListView") as ListView;
            if (VisualTreeHelper.GetChildrenCount(messageListView) > 0)
            {
                Border border = (Border)VisualTreeHelper.GetChild(messageListView, 0);
                ScrollViewer scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                scrollViewer.ScrollToBottom();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
