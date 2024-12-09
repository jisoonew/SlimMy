using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Model
{
    public class UserChatRooms : INotifyPropertyChanged
    {
        // 사용자와 채팅방의 관계를 이어주는 Model

        private string userChatRoomId; // 고유 아이디
        private string userID; // 사용자 아이디
        private string chatRoomId; // 채팅방 아이디

        public string UserChatRoomId
        {
            get { return userChatRoomId; }
            set { userChatRoomId = value; OnPropertyChanged(nameof(userChatRoomId)); }
        }

        public string UserID
        {
            get { return userID; }
            set { userID = value; OnPropertyChanged(nameof(userID)); }
        }

        public string ChatRoomId
        {
            get { return chatRoomId; }
            set { chatRoomId = value; OnPropertyChanged(nameof(chatRoomId)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
