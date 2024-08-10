using SlimMy.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace SlimMy.ViewModel
{
    public partial class ChattingWindow : Window
    {
        private string chattingPartner = null;
        private TcpClient client = null;
        private ObservableCollection<string> messageList = new ObservableCollection<string>();
        public List<string> chattingPartners = null;
        View.ChattingWindow chattingWindow = new View.ChattingWindow();

        public ChattingWindow(TcpClient client, string chattingPartner)
        {
            this.chattingPartner = chattingPartner;
            this.client = client;
            chattingWindow.messageListView.ItemsSource = messageList;
            messageList.Add(string.Format("{0}님이 입장하였습니다.", chattingPartner));
            this.Title = chattingPartner + "님과의 채팅방";
        }

        public ChattingWindow(TcpClient client, List<string> targetChattingPartners)
        {
            this.client = client;
            this.chattingPartners = targetChattingPartners;
            chattingWindow.messageListView.ItemsSource = messageList;
            string enteredUser = "";
            foreach (var item in targetChattingPartners)
            {
                enteredUser += item;
                enteredUser += "님, ";
            }
            messageList.Add(string.Format("{0}이 입장하였습니다.", enteredUser));
            this.Title = enteredUser + "과의 채팅방";
        }

        public void ReceiveMessage(string sender, string message)
        {
            if (message == "ChattingStart")
            {
                return;
            }

            if (message == "상대방이 채팅방을 나갔습니다.")
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                {
                    string parsedMessage = string.Format("{0}님이 채팅방을 나갔습니다.", sender);
                    messageList.Add(parsedMessage);

                    ScrollToBot();
                }));
                return;
            }

            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                messageList.Add(string.Format("{0}: {1}", sender, message));
                chattingWindow.messageListView.ScrollIntoView(chattingWindow.messageListView.Items[chattingWindow.messageListView.Items.Count - 1]);

                ScrollToBot();
            }));
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            string message = string.Format("{0}님과의 채팅을 종료하시겠습니까?", chattingPartner);

            MessageBoxResult messageBoxResult = MessageBox.Show(message, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (messageBoxResult == MessageBoxResult.No)
            {
                e.Cancel = true;
                return;
            }

            string exitMessage = "상대방이 채팅방을 나갔습니다.";
            string parsedMessage = string.Format("{0}<{1}>", chattingPartner, exitMessage);
            byte[] byteData = Encoding.Default.GetBytes(parsedMessage);
            client.GetStream().Write(byteData, 0, byteData.Length);

            this.DialogResult = true;
        }

        // messageListView 컨트롤의 스크롤 위치를 가장 아래로 이동시키는 역할
        private void ScrollToBot()
        {
            // messageListView에 자식 요소가 있는지 확인
            if (VisualTreeHelper.GetChildrenCount(chattingWindow.messageListView) > 0)
            {
                // messageListView의 첫 번째 자시 ㄱ요소를 Border로 캐스팅하여 가져옴
                Border border = (Border)VisualTreeHelper.GetChild(chattingWindow.messageListView, 0);

                // Border의 첫 번째 자식 요소를 ScrollViewer로 캐스팅하여 가져옴
                ScrollViewer scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);

                // ScrollViewer의 스크롤을 가장 아래로 이동
                scrollViewer.ScrollToBottom();
            }
        }
    }
}
