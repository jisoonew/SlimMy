using SlimMy.Model;
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

        public ICommand Window_PreviewKeyDownCommand { get; private set; }

        private ObservableCollection<string> messageList = new ObservableCollection<string>();

        public ObservableCollection<string> MessageList
        {
            get => messageList;
            set
            {
                messageList = value;
                OnPropertyChanged();
            }
        }

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

            this.chattingPartner = chattingPartner;
            this.client = client;
            MessageList.Add(string.Format("{0}님이 입장하였습니다.", chattingPartner));
            //this.Title = chattingPartner + "님과의 채팅방";

            Window_PreviewKeyDownCommand = new Command(Window_PreviewKeyDown);
        }

        public ChattingWindow(TcpClient client, List<string> targetChattingPartners)
        {
            this.client = client;
            this.chattingPartners = targetChattingPartners;

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

            messageList.Add(string.Format("{0}이 입장하였습니다.", enteredUser));
            //this.Title = enteredUser + "과의 채팅방";

            // Window_PreviewKeyDownCommand = new Command(Window_PreviewKeyDown);
        }

        private void Send_btn_Click(object parameter)
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
                string myName = currentUser.NickName;
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

            //ScrollToBot();
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
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                {
                    string parsedMessage = string.Format("{0}님이 채팅방을 나갔습니다.", sender);
                    messageList.Add(parsedMessage);

                    ScrollToBot();
                }));
                return;
            }

            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                var messageListView = Application.Current.MainWindow.FindName("messageListView") as ListView;
                messageList.Add(string.Format("{0}: {1}", sender, message));
                messageListView.ScrollIntoView(messageListView.Items[messageListView.Items.Count - 1]);

                ScrollToBot();
            }));
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
