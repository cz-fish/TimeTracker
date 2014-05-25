using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace WeeklyGrinder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void PrevWeek_Click(object sender, RoutedEventArgs e)
        {
            var model = DataContext as DataModel;
            model.WeekIndex--;
        }

        private void NextWeek_Click(object sender, RoutedEventArgs e)
        {
            var model = DataContext as DataModel;
            model.WeekIndex++;
        }

        private void bJoin_Click(object sender, RoutedEventArgs e)
        {
            var model = DataContext as DataModel;
            if (!model.IsJoiningLines)
            {
                // Reset selection index so that the the first line that the user selects from now is the target line
                // for joining. If we didn't reset SelectedIndex here, the gData_SelectionChanged callback won't be
                // triggered if the user selects the line with the SelectedIndex.
                gData.SelectedIndex = -1;
            }
            model.IsJoiningLines = !model.IsJoiningLines;
        }

        private void bReset_Click(object sender, RoutedEventArgs e)
        {
            var model = DataContext as DataModel;
            model.UpdateCurrentWeekData();
        }

        private void bClear_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(string.Format("We are about to clear the log.{0}{0}Do you want to keep task names?", Environment.NewLine), "Keep task names?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.Cancel)
                return;
            var model = DataContext as DataModel;
            model.ClearLog(result == MessageBoxResult.Yes);
        }

        private void bDismissError_Click(object sender, RoutedEventArgs e)
        {
            var model = DataContext as DataModel;
            model.FileIOError = null;
        }

        private void bEqual_Click(object sender, RoutedEventArgs e)
        {
            var model = DataContext as DataModel;
            if (!model.Equalize8())
            {
                MessageBox.Show("The week doesn't have at least 40 hours in total. Thus it can't be reorganized so that each workday has at least 8 hours.",
                    "Not enough hours to reorganize", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void bEliminate_Click(object sender, RoutedEventArgs e)
        {
            var model = DataContext as DataModel;
            if (!model.EliminateWeekend())
            {
                MessageBox.Show("The week has too many hours; we can't move them all to week days without exceeding daily limit.",
                    "You work too much!", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void gData_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            var model = DataContext as DataModel;

            // The week must have at least one task, so that we can query the task objects properties
            if (model.CurrentWeekData == null || model.CurrentWeekData.Count < 1)
                return;
            var task = model.CurrentWeekData[0];

            // Translate the respective property to a name that will be used as the column title and make the
            // title column stretch across all available horizontal space
            string propertyName = (e.PropertyDescriptor as PropertyDescriptor).Name;
            e.Column.Header = task.GetColumnHeader(propertyName);
            if (task.IsTitleColumn(propertyName))
                e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            else
                e.Column.Width = DataGridLength.Auto;
        }

        private void gData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var model = DataContext as DataModel;
            if (!model.IsJoiningLines)
                return;
            if (gData.SelectedIndex != -1)
                model.JoinLine(gData.SelectedIndex);
        }

        private void gData_CurrentCellChanged(object sender, EventArgs e)
        {
            var model = DataContext as DataModel;

            // Update the description detail text box with partial tasks from the selected cell
            if (gData.CurrentCell != null && gData.CurrentCell.Column != null)
            {
                // We only have information for columns 1 through 7 (days of week)
                // The -1 skips the 0th column that contains task title and the >6 test eliminates the totals column
                int col = gData.CurrentCell.Column.DisplayIndex - 1;
                if (col < 0 || col > 6)
                {
                    model.SelectedCellDetail = string.Empty;
                }
                else
                {
                    var selected = gData.CurrentCell.Item as WeekTaskData;
                    model.SelectedCellDetail = string.Join(Environment.NewLine, selected.GetPartialTaskDescriptions(col).ToArray());
                }
            }
        }
    }
}
