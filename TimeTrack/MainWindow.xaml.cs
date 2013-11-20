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
using System.IO;

using Hardcodet.Wpf.TaskbarNotification;
using System.ComponentModel;

namespace TimeTrack
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public readonly string DATA_FILE_PATH;
        public const string DATA_FILE_NAME = "TimeTrack.txt";

        private bool m_Recording = false;
        private DateTime m_RecordStart;
        private DateTime m_RecordStop;
        private string m_Task;

        private List<string> m_TaskNames = new List<string>();
        public List<String> TaskNames
        {
            get { return m_TaskNames; }
            set { m_TaskNames = value; }
        }

        protected void NotifyPropertyChange(string prop)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            DATA_FILE_PATH = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments, Environment.SpecialFolderOption.None), DATA_FILE_NAME);
            InitializeComponent();
        }

        public void ToggleRecording()
        {
            if (this.Visibility == System.Windows.Visibility.Visible)
            {
                MessageBox.Show("Please commit the previous time record before starting a new one", "Commit previous time first", MessageBoxButton.OK, MessageBoxImage.Hand);
                return;
            }

            m_Recording = !m_Recording;
            if (m_Recording)
            {
                TaskbarIcon.IconSource = (ImageSource)FindResource("IcoStop");
                TaskbarIcon.ToolTip = string.Format("Recording time since {0}. Click to stop", m_RecordStart.ToString("HH:mm"));
                m_RecordStart = DateTime.Now;
            }
            else
            {
                TaskbarIcon.IconSource = (ImageSource)FindResource("IcoRecord");
                TaskbarIcon.ToolTip = tbTooltipNotRecording.Text;
                m_RecordStop = DateTime.Now;

                dtFrom.Text = m_RecordStart.ToString("g");
                dtTo.Text = m_RecordStop.ToString("g");
                m_Task = string.Empty;
                RecalcTask();
                this.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void bSave_Click(object sender, RoutedEventArgs e)
        {
            TimeSpan dif = m_RecordStop - m_RecordStart;
            using (FileStream fs = new FileStream(DATA_FILE_PATH, FileMode.Append))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.WriteLine(string.Format("{0},{1},{2},{3},{4},{5}",
                    m_RecordStart.ToString("yyyy/MM/dd"), m_RecordStart.ToString("HH:mm"), m_RecordStop.ToString("HH:mm"), (int)dif.TotalMinutes, dif.TotalHours.ToString("0.00"), m_Task));
            }

            this.Visibility = System.Windows.Visibility.Hidden;

            if (!m_TaskNames.Contains(m_Task))
            {
                m_TaskNames.Insert(0, m_Task);
                NotifyPropertyChange("TaskNames");
            }
        }

        private void dtTo_TextChanged(object sender, TextChangedEventArgs e)
        {
            DateTime.TryParse(dtTo.Text, out m_RecordStop);
            RecalcTask();
        }

        private void dtFrom_TextChanged(object sender, TextChangedEventArgs e)
        {
            DateTime.TryParse(dtFrom.Text, out m_RecordStart);
            RecalcTask();
        }

        private void RecalcTask()
        {
            if (m_RecordStart >= m_RecordStop)
                return;
            TimeSpan t = m_RecordStop - m_RecordStart;
            lbTimeStats.Content = string.Format("{0} minutes; {1:0.00} hr", (int)t.TotalMinutes, t.TotalHours);
        }

        private void bIgnore_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure that you want to ignore this time interval?", "Ignore time interval", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            this.Visibility = System.Windows.Visibility.Hidden;
        }
    }
}
