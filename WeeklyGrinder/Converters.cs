using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

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

    public class VisibleIfError : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (parameter == null)
                throw new ArgumentNullException("parameter");
            bool negate = (bool)parameter;

            if (!(value is string) || string.IsNullOrEmpty(value as string))
                // assume no error
                return negate ? Visibility.Hidden : Visibility.Visible;
            return negate ? Visibility.Visible : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class EnabledIfNotFirst : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is int))
                return false;

            int index = (int)value;
            return index > 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class EnabledIfNotLast : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values == null || values.Length < 2 || !(values[0] is int) || values[1] == null || !(values[1] is List<DateTime>))
                return false;

            int index = (int)values[0];
            var list = values[1] as List<DateTime>;
            return index < list.Count - 1;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }


    public class CellForegroundConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values[0] is DataGridCell && values[1] is WeekTaskData)
            {
                var cell = values[0] as DataGridCell;
                var data = values[1] as WeekTaskData;
                int index = cell.Column.DisplayIndex;

                if (index > 0 && index < 8 && data.GetWorkedMinutes()[index-1] == 0.0m && !data.IsTotals())
                {
                    return new SolidColorBrush(Colors.LightGray);
                }
            }
            return new SolidColorBrush(Colors.Black);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class CellBackgroundConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values[0] is DataGridCell && values[1] is WeekTaskData)
            {
                var cell = values[0] as DataGridCell;
                var data = values[1] as WeekTaskData;
                int index = cell.Column.DisplayIndex;

                if (index == 8 || data.IsTotals())
                {
                    return new SolidColorBrush(Colors.LightGray);
                }
            }
            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class CellFontWeightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values[0] is DataGridCell && values[1] is WeekTaskData)
            {
                var cell = values[0] as DataGridCell;
                var data = values[1] as WeekTaskData;
                int index = cell.Column.DisplayIndex;

                if (index == 8 || data.IsTotals())
                {
                    return FontStyles.Italic;
                }
            }
            return FontStyles.Normal;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
