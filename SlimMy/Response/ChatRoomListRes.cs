using SlimMy.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SlimMy.Response
{
    public class ChatRoomListRes
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
        public List<ChatRooms> Rooms { get; set; } = new();
    }
}
