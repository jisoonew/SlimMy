using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Model
{
    public class Message : INotifyPropertyChanged
    {
        private Guid sendUserID;
        private string sendUserNickName;
        private string sendMessage;
        private Guid messageID;
        private DateTime sentAt;

        public Guid SendUserID
        {
            get { return sendUserID; }
            set { sendUserID = value; OnPropertyChanged(nameof(sendUserID)); }
        }

        public string SendUserNickName
        {
            get { return sendUserNickName; }
            set { sendUserNickName = value; OnPropertyChanged(nameof(sendUserNickName)); }
        }

        public string SendMessage
        {
            get { return sendMessage; }
            set { sendMessage = value; OnPropertyChanged(nameof(sendMessage)); }
        }

        public Guid MessageID
        {
            get { return messageID; }
            set { messageID = value; OnPropertyChanged(nameof(messageID)); }
        }

        public DateTime SentAt
        {
            get { return sentAt; }
            set { sentAt = value; OnPropertyChanged(nameof(SentAt)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
