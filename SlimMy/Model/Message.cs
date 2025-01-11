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
        private String sendUserNickName;
        private String sendMessage;

        public Guid SendUserID
        {
            get { return sendUserID; }
            set { sendUserID = value; OnPropertyChanged(nameof(sendUserID)); }
        }

        public String SendUserNickName
        {
            get { return sendUserNickName; }
            set { sendUserNickName = value; OnPropertyChanged(nameof(sendUserNickName)); }
        }

        public String SendMessage
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
