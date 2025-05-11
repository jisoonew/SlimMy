using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SlimMy.Model
{
    public class ExportChartParameter
    {
        public string FilePath { get; set; }
        public (FrameworkElement Chart, string Title, IEnumerable<(string Label, string Value)> DataTable)[] ChartInfos { get; set; }
    }
}
