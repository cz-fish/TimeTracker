using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            var model = DataContext as DataModel;
            if (model.CurrentWeekData == null || model.CurrentWeekData.Count < 1)
                return;
            e.Column.Header = model.CurrentWeekData[0].GetColumnTitle(e.PropertyDescriptor as PropertyDescriptor);

            if (e.Column.Header.ToString() == "Task")
                e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            else
                e.Column.Width = DataGridLength.Auto;
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

        private void gData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var model = DataContext as DataModel;
            if (!model.IsJoiningLines)
                return;
            if (gData.SelectedIndex != -1)
                model.JoinLine(gData.SelectedIndex);
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
                MessageBox.Show("The week doesn't have at least 40 hours in total. Thus it can't be reorganized so that each workday has at least 8 hours",
                    "Not enough hours to reorganize", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
