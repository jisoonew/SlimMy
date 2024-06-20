using SlimMy.Model;
using SlimMy.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
    public class Community : INotifyPropertyChanged
    {
        private Chat _chat;
        private Repo _repo;
        private string _connstring = "Data Source = 125.240.254.199; User Id = system; Password = 1234;";
        private CreateChatRoom _createChatRoomViewModel;

        public static string myName = null;
        TcpClient client = null;
        private ChattingWindow chattingWindow;

        private ObservableCollection<Chat> _chatRooms;

        public ObservableCollection<Chat> ChatRooms
        {
            get { return _chatRooms; }
            set { _chatRooms = value; OnPropertyChanged(); }
        }

        public Chat Chat
        {
            get => _chat;
            set
            {
                if (_chat != value)
                {
                    _chat = value;
                    OnPropertyChanged(nameof(Chat));
                }
            }
        }

        private Chat _selectedChatRoom;
        public Chat SelectedChatRoom
        {
            get { return _selectedChatRoom; }
            set { _selectedChatRoom = value; OnPropertyChanged(); }
        }

        public ICommand OpenCreateChatRoomCommand { get; private set; }
        public Command InsertCommand { get; set; }

        public Community()
        {
            _chat = new Chat();
            _repo = new Repo(_connstring);

            InsertCommand = new Command(Print);

            RefreshChatRooms();
        }


        private void Print(object parameter)
        {
            if (parameter is Chat selectedChatRoom)
            {
                // 선택된 채팅방 정보 처리
                MessageBox.Show($"선택된 채팅방: {selectedChatRoom.ChatRoomId}");
            }
        }

        // 채팅방 목록 생성
        private void RefreshChatRooms()
        {
            ChatRooms = new ObservableCollection<Chat>(_repo.SelectChatRoom());
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
