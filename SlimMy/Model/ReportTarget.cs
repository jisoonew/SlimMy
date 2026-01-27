using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Model
{
    public enum ReportTargetType
    {
        ChatRoom,
        User
    }

    public sealed class ReportTarget
    {
        public ReportTargetType TargetType { get; init; }

        public Guid ChatRoomId { get; init; }
        public string? ChatRoomTitle { get; init; }

        public Guid? TargetUserId { get; init; }
        public string? TargetUserNickName { get; init; }
    }
}
