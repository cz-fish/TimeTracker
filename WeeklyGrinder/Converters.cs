using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace WeeklyGrinder
{
    public class WeekTitleConverter : IValueConverter
    {
        public static DateTime GetWeekStartDay(DateTime weekDay)
        {
            DateTime weekStart = weekDay.Date;
            while (weekStart.DayOfWeek != DayOfWeek.Monday)
                weekStart -= new TimeSpan(1, 0, 0, 0);
            return weekStart;
        }

        public static DateTime GetWeekEndDay(DateTime weekStart)
        {
            return weekStart + new TimeSpan(6, 0, 0, 0);
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is DateTime))
                throw new ArgumentException("value");
            DateTime weekStart = GetWeekStartDay((DateTime)value);
            DateTime weekEnd = GetWeekEndDay(weekStart);
            return string.Format("Week from {0:m} to {1:m}", weekStart, weekEnd);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
