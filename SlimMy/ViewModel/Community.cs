using SlimMy.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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

        public ICommand OpenCreateChatRoomCommand { get; private set; }

        public Community()
        {
            _chat = new Chat();
            _repo = new Repo(_connstring);

            _createChatRoomViewModel = new CreateChatRoom();
            _createChatRoomViewModel.ChatRoomCreated += async (sender, args) => await RefreshChatRoomsAsync();

            RefreshChatRooms();
        }

        private async Task RefreshChatRoomsAsync()
        {
            await Task.Run(() =>
            {
                ChatRooms = new ObservableCollection<Chat>(_repo.SelectChatRoom());
            });
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
