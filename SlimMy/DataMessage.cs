using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy
{
    public class DataMessage<T> : MessageBase
    {
        public T Data { get; private set; }

        public DataMessage(T data)
        {
            Data = data;
        }
    }
}
