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
