using SlimMy.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SlimMy.Response
{
    public class MyChatRoomSearchWordRes
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
        public List<ChatRooms> rooms { get; set; } = new();
    }
}
