using SlimMy.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Model
{
    public class PendingRoomEvent : BaseViewModel
    {
        private Guid _banID;
        private string _chatRoomName;
        private DateTime _banAt;

        public Guid BanID
        {
            get { return _banID; }
            set { _banID = value; OnPropertyChanged(nameof(BanID)); }
        }

        public string ChatRoomName
        {
            get { return _chatRoomName; }
            set { _chatRoomName = value; OnPropertyChanged(nameof(ChatRoomName)); }
        }

        public DateTime BanAt
        {
            get { return _banAt; }
            set { _banAt = value; OnPropertyChanged(nameof(BanAt)); }
        }
    }
}
