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
        private String sendUser;
        private String sendMessage;

        public Guid SendUserID
        {
            get { return sendUserID; }
            set { sendUserID = value; OnPropertyChanged(nameof(sendUserID)); }
        }

        public String SendUser
        {
            get { return sendUser; }
            set { sendUser = value; OnPropertyChanged(nameof(sendUser)); }
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
