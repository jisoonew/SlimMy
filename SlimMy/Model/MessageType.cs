using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Model
{
    enum MessageType : byte
    {
        UserJoinChatRoom = 1,
        ChatContent = 2,
        UserLeaveRoom = 3,
        HostChanged = 4,
        GiveMeUserList = 5,
        GroupChattingStart = 6,
        UserLogin = 9,
        Heartbeat = 11
    }
}
