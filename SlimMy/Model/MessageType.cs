using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Model
{
    public enum MessageType : byte
    {
        UserJoinChatRoom = 1,
        ChatContent = 2,
        UserLeaveRoom = 3,
        HostChanged = 4,
        GiveMeUserList = 5,
        UserLogin = 9,
        Heartbeat = 11,
        Sign_Up = 12,
        ChatRoomList = 13,
        ChatRoomListRes = 14,
        ChatRoomUserList = 15,
        ChatRoomUserListRes = 16,
        ChatRoomPageList = 17,
        ChatRoomPageListRes = 18,
        MyChatRoomSearchWord = 19,
        MyChatRoomSearchWordRes = 20,
        MyData = 21,
        MyDataRes = 22,
        TodayWeightCompleted = 23,
        TodayWeightCompletedRes = 24,
        UpdateMyPageUserData = 25,
        UpdateMyPageUserDataRes = 26,
        UpdatetMyPageWeight = 27,
        UpdatetMyPageWeightRes = 28,
        InsertMyPageWeight = 29,
        InsertMyPageWeightRes = 30,
        VerifyPassword = 31,
        VerifyPasswordRes = 32,
        DeleteAccountView = 33,
        DeleteAccountViewRes = 34,
        NickNameCheckPrint = 35,
        NickNameCheckPrintRes = 36,
        NickNameSave = 37,
        NickNameSaveRes = 38
    }
}
