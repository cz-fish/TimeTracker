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
    /// <summary>
    /// Converts one day representing the current week to the text that is at the top of the window
    /// </summary>
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

    /// <summary>
    /// Converts an error string to Visibility.
    /// An empty or null string means hidden, unless the boolean parameter 'negate' is set.
    /// </summary>
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

    /// <summary>
    /// Converts a week index to boolean. Returns true iff the index is above 0
    /// </summary>
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

    /// <summary>
    /// Converts a week index to boolean. Returns true iff the index (first value) is not the last index in the list of DateTime given in second value
    /// </summary>
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

    /// <summary>
    /// Abstract base for converters that set datagrid cell text style based on the cell contents.
    /// First value is the given DataGridCell. Second value is week task data of the given table row.
    /// </summary>
    public abstract class CellStyleConverter : IMultiValueConverter
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
                    // last column (index == 8) or last row (IsTotals)
                    return GetValueForTotalsCell();
                }
                else if (index > 0 && index < 8 && data.GetWorkedHours()[index - 1] == 0.0m)
                {
                    return GetValueForZeroCell();
                }
            }
            //Keep default value
            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        protected abstract object GetValueForTotalsCell();
        protected abstract object GetValueForZeroCell();
    }

    /// <summary>
    /// Determines foreground text color of a datagrid cell
    /// </summary>
    public class CellForegroundConverter : CellStyleConverter
    {
        protected override object GetValueForTotalsCell()
        {
            return DependencyProperty.UnsetValue;
        }

        protected override object GetValueForZeroCell()
        {
            return new SolidColorBrush(Colors.LightGray);
        }
    }

    /// <summary>
    /// Determines background color of a datagrid cell
    /// </summary>
    public class CellBackgroundConverter : CellStyleConverter
    {
        protected override object GetValueForTotalsCell()
        {
            return new SolidColorBrush(Colors.LightSkyBlue);
        }

        protected override object GetValueForZeroCell()
        {
            return DependencyProperty.UnsetValue;
        }
    }

    /// <summary>
    /// Determines fond style of a datagrid cell
    /// </summary>
    public class CellFontStyleConverter : CellStyleConverter
    {
        protected override object GetValueForTotalsCell()
        {
            return FontStyles.Italic;
        }

        protected override object GetValueForZeroCell()
        {
            return FontStyles.Normal;
        }
    }
}
