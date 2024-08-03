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
    public class CreateChatRoom : INotifyPropertyChanged
    {
        private Chat _chat;
        private Repo _repo;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";
        public event EventHandler ChatRoomCreated;

        public static string myName = null;
        TcpClient client = null;

        private ObservableCollection<Chat> _chatRooms;

        public ObservableCollection<Chat> ChatRooms
        {
            get { return _chatRooms; }
            set { _chatRooms = value; OnPropertyChanged(); }
        }

        public Chat Chat
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

        public CreateChatRoom()
        {
            _chat = new Chat();
            _repo = new Repo(_connstring);
            OpenCreateChatRoomCommand = new Command(CreateChat);
        }

        // 채팅방 생성
        private void CreateChat(object parameter)
        {
            // 생성 시간
            DateTime now = DateTime.Now;

            _repo.InsertChatRoom(_chat.ChatRoomName, _chat.Description, _chat.Category, now);

            // 이벤트 발생
            ChatRoomCreated?.Invoke(this, EventArgs.Empty);

            CloseWindow();
        }

        private void CloseWindow()
        {
            // 싱글톤
            User currentUser = UserSession.Instance.CurrentUser;

            // 정상 출력
            // MessageBox.Show("여기는 크리에이트 챗 : " + currentUser.NickName);

            // 선택된 그룹 채팅 참여자들의 정보를 문자열
            string getUserProtocol = currentUser.NickName + "<GiveMeUserList>";
            byte[] byteData = new byte[getUserProtocol.Length];
            byteData = Encoding.Default.GetBytes(getUserProtocol);

            // 서버 사용자가 서버 접속을 안해서 null 값이 출력되고 있다
            // 로그인할 때 IP을 입력 받게 한다
            client.GetStream().Write(byteData, 0, byteData.Length);

            // MessageBox.Show("내가 방장 : " + getUserProtocol);

            //Community userListWindow = new Community(StaticDefine.GROUP_CHATTING);

            //    string groupChattingUserStrData = MainPage.myName;
            //    foreach (var item in userListWindow.GroupChattingReceivers)
            //    {
            //        groupChattingUserStrData += "#";
            //        groupChattingUserStrData += item.NickName;
            //    }

            //    string chattingStartMessage = string.Format("{0}<GroupChattingStart>", groupChattingUserStrData);
            //    byte[] chattingStartByte = Encoding.Default.GetBytes(chattingStartMessage);

            //    // 입력했던 주소가 차례대로 출력된다 -> 127.0.0.3#127.0.0.1#127.0.0.1<GroupChattingStart>
            //    // MessageBox.Show("Sending to server: " + chattingStartMessage);

            //    client.GetStream().Write(chattingStartByte, 0, chattingStartByte.Length);
            

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

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
