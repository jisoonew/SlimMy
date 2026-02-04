using SlimMy.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Model
{
    public class ReportMessage : BaseViewModel
    {
        private Guid _evidenceID;
        private Guid _reportID;
        private Guid? _messageID;
        private Guid? _senderUserID;
        private string _messageContent;
        private DateTime? _sentAt;
        private DateTime _createdAt;

        public Guid EvidenceID
        {
            get { return _evidenceID; }
            set { _evidenceID = value; OnPropertyChanged(nameof(EvidenceID)); }
        }

        public Guid ReportID
        {
            get { return _reportID; }
            set { _reportID = value; OnPropertyChanged(nameof(ReportID)); }
        }

        public Guid? MessageID
        {
            get { return _messageID; }
            set { _messageID = value; OnPropertyChanged(nameof(MessageID)); }
        }

        public Guid? SenderUserID
        {
            get { return _senderUserID; }
            set { _senderUserID = value; OnPropertyChanged(nameof(SenderUserID)); }
        }

        public string MessageContent
        {
            get { return _messageContent; }
            set { _messageContent = value; OnPropertyChanged(nameof(MessageContent)); }
        }

        public DateTime? SentAt
        {
            get { return _sentAt; }
            set { _sentAt = value; OnPropertyChanged(nameof(SentAt)); }
        }

        public DateTime CreatedAt
        {
            get { return _createdAt; }
            set { _createdAt = value; OnPropertyChanged(nameof(CreatedAt)); }
        }
    }
}
