using SlimMy.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Model
{
    public class WarningNoticeRecipient : BaseViewModel
    {
        private Guid _noticeRecipientID;
        private Guid _noticeID;
        private Guid _userID;
        private DateTime? _deliveredAt;
        private DateTime? _readAt;
        private DateTime? _popUpShownAt;
        private string _warningText;

        public Guid NoticeRecipientID
        {
            get { return _noticeRecipientID; }
            set { _noticeRecipientID = value; OnPropertyChanged(nameof(NoticeRecipientID)); }
        }

        public Guid NoticeID
        {
            get { return _noticeID; }
            set { _noticeID = value; OnPropertyChanged(nameof(NoticeID)); }
        }

        public Guid UserID
        {
            get { return _userID; }
            set { _userID = value; OnPropertyChanged(nameof(UserID)); }
        }

        public DateTime? DeliveredAt
        {
            get { return _deliveredAt; }
            set { _deliveredAt = value; OnPropertyChanged(nameof(DeliveredAt)); }
        }

        public DateTime? ReadAt
        {
            get { return _readAt; }
            set { _readAt = value; OnPropertyChanged(nameof(ReadAt)); }
        }

        public DateTime? PopUpShownAt
        {
            get { return _popUpShownAt; }
            set { _popUpShownAt = value; OnPropertyChanged(nameof(PopUpShownAt)); }
        }

        public string WarningText
        {
            get { return _warningText; }
            set { _warningText = value; OnPropertyChanged(nameof(WarningText)); }
        }
    }
}
