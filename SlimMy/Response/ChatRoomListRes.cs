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
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }
        [JsonPropertyName("message")]
        public string Message { get; set; } = "ok";
        [JsonPropertyName("rooms")]
        public List<ChatRooms> Rooms { get; set; } = new();
    }
}
