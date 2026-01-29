using SlimMy.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Model
{
    public class ReportItem : BaseViewModel
    {
        private Guid evidenceID;
        private Guid reportID;
        private Guid messageID;
        private Guid senderUserID;
        private string messageContent;
        private string targetType;
        private Guid targetUserID;
        private Guid targetRoomID;
        private string reasonCode;
        private string detailText;
        private string status;
        private DateTime sentAt;
        private DateTime createdAt;

        public Guid EvidenceID
        {
            get { return evidenceID; }
            set { evidenceID = value; OnPropertyChanged(nameof(EvidenceID)); }
        }

        public Guid ReportID
        {
            get { return reportID; }
            set { reportID = value; OnPropertyChanged(nameof(ReportID)); }
        }

        public Guid MessageID
        {
            get { return messageID; }
            set { messageID = value; OnPropertyChanged(nameof(MessageID)); }
        }

        public Guid SenderUserID
        {
            get { return senderUserID; }
            set { senderUserID = value; OnPropertyChanged(nameof(SenderUserID)); }
        }

        public Guid TargetUserID
        {
            get { return targetUserID; }
            set { targetUserID = value; OnPropertyChanged(nameof(TargetUserID)); }
        }

        public Guid TargetRoomID
        {
            get { return targetRoomID; }
            set { targetRoomID = value; OnPropertyChanged(nameof(TargetRoomID)); }
        }

        public string MessageContent
        {
            get { return messageContent; }
            set { messageContent = value; OnPropertyChanged(nameof(MessageContent)); }
        }

        public string TargetType
        {
            get { return targetType; }
            set { targetType = value; OnPropertyChanged(nameof(TargetType)); }
        }

        public string ReasonCode
        {
            get { return reasonCode; }
            set { reasonCode = value; OnPropertyChanged(nameof(ReasonCode)); }
        }

        public string DetailText
        {
            get { return detailText; }
            set { detailText = value; OnPropertyChanged(nameof(DetailText)); }
        }

        public string Status
        {
            get { return status; }
            set { status = value; OnPropertyChanged(nameof(Status)); }
        }

        public DateTime SentAt
        {
            get { return sentAt; }
            set { sentAt = value; OnPropertyChanged(nameof(SentAt)); }
        }

        public DateTime CreatedAt
        {
            get { return createdAt; }
            set { createdAt = value; OnPropertyChanged(nameof(CreatedAt)); }
        }
    }
}
