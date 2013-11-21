﻿using System;
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
    }
}
