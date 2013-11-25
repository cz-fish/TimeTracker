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

        }

        private void NextWeek_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            var model = DataContext as DataModel;
            if (model.CurrentWeekData == null || model.CurrentWeekData.Count < 1)
                return;
            e.Column.Header = model.CurrentWeekData[0].GetColumnTitle(e.PropertyDescriptor as PropertyDescriptor);
        }

        private void bJoin_Click(object sender, RoutedEventArgs e)
        {
            var model = DataContext as DataModel;
            if (!model.IsJoiningRows)
            {
                // Reset selection index so that the the first line that the user selects from now is the target line
                // for joining. If we didn't reset SelectedIndex here, the gData_SelectionChanged callback won't be
                // triggered if the user selects the line with the SelectedIndex.
                gData.SelectedIndex = -1;
            }
            model.IsJoiningRows = !model.IsJoiningRows;
        }

        private void bSplit_Click(object sender, RoutedEventArgs e)
        {
            var model = DataContext as DataModel;
            model.CanSplitLines = false;
            model.UpdateCurrentWeekData();
        }

        private void bClear_Click(object sender, RoutedEventArgs e)
        {

        }

        private void gData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var model = DataContext as DataModel;
            if (!model.IsJoiningRows)
                return;
            if (gData.SelectedIndex != -1)
                model.JoinLine(gData.SelectedIndex);
        }
    }
}
