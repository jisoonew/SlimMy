using Microsoft.Win32;
using SlimMy.Model;
using SlimMy.ViewModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SlimMy.View
{
    /// <summary>
    /// ExerciseHistory.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ExerciseHistory : Page
    {
        public ExerciseHistory()
        {
            InitializeComponent();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ExerciseHistoryViewModel vm)
            {
                if (vm.SelectedExportFormat == "PDF")
                {
                    var dialog = new SaveFileDialog
                    {
                        FileName = "운동기록",
                        DefaultExt = ".pdf",
                        Filter = "PDF 파일 (*.pdf)|*.pdf"
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        var categoryStats = vm.FilteredExerciseLogs
                    .GroupBy(x => x.Category)
                    .Select(g => (
                        Label: $"{g.Key} ({g.Count()}회)",
                        Value: $"{(double)g.Count() / vm.FilteredExerciseLogs.Count * 100:0.0}%"))
                    .ToList();

                        var calorieTrendStats = vm.FilteredExerciseLogs
                            .GroupBy(x => x.PlannerDate.Date)
                            .OrderBy(g => g.Key)
                            .Select(g => (
                                Label: g.Key.ToString("yyyy-MM-dd"),
                                Value: $"{g.Sum(x => x.Calories)} kcal"))
                            .ToList();

                        var durationTrendStats = vm.FilteredExerciseLogs
                            .GroupBy(x => x.PlannerDate.Date)
                            .OrderBy(g => g.Key)
                            .Select(g => (
                                Label: g.Key.ToString("yyyy-MM-dd"),
                                Value: $"{g.Sum(x => x.Minutes)} 분"))
                            .ToList();

                        var param = new ExportChartParameter
                        {
                            FilePath = dialog.FileName,
                            ChartInfos = new (FrameworkElement, string, IEnumerable<(string, string)>)[]
{
    (CalorieTrendChart, "칼로리 소모 추세", calorieTrendStats),
    (CategoryDistributionChart, "운동 종류별 비율", categoryStats),
    (DurationTrendChart, "운동 시간 추세", durationTrendStats)
}
                        };

                        vm.ExportCommand.Execute(param);
                    }
                }
                else
                {
                    vm.ExportCommand.Execute(null); // CSV/Excel은 null로 처리
                }
            }
        }
    }
}
