using SlimMy.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SlimMy.Response
{
    public class ChatRoomPageListRes
    {
        [JsonPropertyName("ok")]
        public bool ok { get; set; }
        [JsonPropertyName("message")]
        public string message { get; set; }
        [JsonPropertyName("rooms")]
        public List<ChatRooms> rooms { get; set; } = new();
    }
}
