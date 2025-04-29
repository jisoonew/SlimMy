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
    /// Planner.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Planner : Page
    {
        public Planner()
        {
            InitializeComponent();

            DataContext = PlannerViewModel.Instance;

            Loaded += Planner_Loaded;
        }

        private void Planner_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is PlannerViewModel vm)
            {
                vm.PlannerPrint();
            }
        }
    }
}
