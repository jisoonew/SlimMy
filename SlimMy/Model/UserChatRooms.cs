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
        private int UserChatRoomId; // 고유 아이디
        private string UserID; // 사용자 아이디
        private string ChatRoomId; // 채팅방 아이디

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
