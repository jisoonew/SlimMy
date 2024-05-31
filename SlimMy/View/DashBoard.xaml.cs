using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace SlimMy.View
{
    /// <summary>
    /// DashBoard.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class DashBoard : Page
    {

        public SeriesCollection SeriesData { get; private set; }
        public string[] XLabel { get; set; }
        public Func<int, string> Values { get; set; }

        public DashBoard()
        {
            InitializeComponent();

            SeriesData = new SeriesCollection {
        new ColumnSeries  // ColumnSeries 는 막대그래프, LineSeries 는 선 그래프
        {
          Title = "2020",
          Values = new ChartValues<int> { 3, 5, 7, 4, 7 }
        },
        new ColumnSeries
        {
          Title = "2021",
          Values = new ChartValues<int> { 5, 6, 2, 7, 8 }
        },
        new ColumnSeries
        {
          Title = "2022",
          Values = new ChartValues<int> { 8, 7, 6, 9, 7}
        }
      };
            XLabel = new string[] { "Kang", "Kim", "Cho", "Ko", "Song" };
            Values = value => value.ToString("N");

            DataContext = this; // Data binding 할때 필요함

        }
    }
}
