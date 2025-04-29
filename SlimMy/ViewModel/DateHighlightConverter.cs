using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SlimMy.ViewModel
{
    public class DateHighlightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2 || !(values[0] is DateTime) || !(values[1] is IEnumerable<DateTime>))
                return false;

            DateTime date = (DateTime)values[0];
            var highlightDates = values[1] as IEnumerable<DateTime>;

            return highlightDates != null && highlightDates.Any(d => d.Date == date.Date);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
