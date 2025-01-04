using SlimMy.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SlimMy.View
{
    /// <summary>
    /// ChattingWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ChattingWindow : Window, IView
    {
        public ChattingWindow()
        {
            InitializeComponent();
        }
    }
}
