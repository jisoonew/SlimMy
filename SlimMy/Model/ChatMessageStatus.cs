using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Model
{
    public class ChatMessageStatus
    {
        public string ChatName { get; set; } // 채팅방 이름
        public string LastMessage { get; set; } // 마지막 메시지
        public bool HasUnreadMessages { get; set; } // 읽지 않은 메시지가 있는지 여부
    }
}
