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
using System.Collections.ObjectModel;

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
        private DateTime m_Today = DateTime.Now.Date;
        private TimeSpan m_WorkedToday = new TimeSpan(0);

        private ObservableCollection<string> m_TaskNames = new ObservableCollection<string>();
        public ObservableCollection<String> TaskNames
        {
            get { return m_TaskNames; }
            set {
                m_TaskNames = value;
                NotifyPropertyChange("TaskNames");
            }
        }

        private string m_IconTooltip = "";
        public string IconTooltip
        {
            get { return m_IconTooltip; }
            set
            {
                m_IconTooltip = value;
                NotifyPropertyChange("IconTooltip");
            }
        }

        public TimeSpan WorkedToday
        {
            get { return m_WorkedToday; }
            set
            {
                m_WorkedToday = value;
                NotifyPropertyChange("WorkedToday");
            }
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

        private void RootWindow_Initialized(object sender, EventArgs e)
        {
            IconTooltip = tbTooltipNotRecording.Text;
            new Task(() =>
            {
                LoadHistory();
            }).Start();
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
                m_RecordStart = DateTime.Now;
                SwitchIconToRecording();
            }
            else
            {
                // Stop recording
                SwitchIconToStopped();
                m_RecordStop = DateTime.Now;

                // Update the date of "today" if it changed since the last task
                if (DateTime.Now.Date > m_Today)
                {
                    m_Today = DateTime.Now.Date;
                    WorkedToday = new TimeSpan(0);
                }

                // Calculate the task's length
                dtFrom.Text = m_RecordStart.ToString("g");
                dtTo.Text = m_RecordStop.ToString("g");
                m_Task = string.Empty;
                RecalcTask();

                // Show the task window
                this.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void bSave_Click(object sender, RoutedEventArgs e)
        {
            m_Task = cbTask.Text;
            if (string.IsNullOrWhiteSpace(m_Task))
            {
                MessageBox.Show("Task name mustn't be empty", "Insufficient data");
                return;
            }

            TimeSpan dif = m_RecordStop - m_RecordStart;
            if (dif < new TimeSpan(0))
                dif = new TimeSpan(0);
            using (FileStream fs = new FileStream(DATA_FILE_PATH, FileMode.Append))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.WriteLine(string.Format("{0}|{1}|{2}|{3}|{4}|{5}",
                    m_RecordStart.ToString("yyyy/MM/dd"), m_RecordStart.ToString("HH:mm"), m_RecordStop.ToString("HH:mm"), (int)dif.TotalMinutes, dif.TotalHours.ToString("0.00"), m_Task));
            }

            DateTime taskDate = m_RecordStart.Date;
            if (taskDate == m_Today)
            {
                WorkedToday += dif;
            }

            this.Visibility = System.Windows.Visibility.Hidden;

            if (!m_TaskNames.Contains(m_Task))
            {
                m_TaskNames.Insert(0, m_Task);
                NotifyPropertyChange("TaskNames");
            }
        }

        private void bIgnore_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure that you want to ignore this time interval?", "Ignore time interval", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            this.Visibility = System.Windows.Visibility.Hidden;
        }

        private void bContinue_Click(object sender, RoutedEventArgs e)
        {
            m_Recording = true;
            SwitchIconToRecording();
            this.Visibility = System.Windows.Visibility.Hidden;
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

            DateTime taskDate = m_RecordStart.Date;
            TimeSpan workedToday = m_WorkedToday;

            if (taskDate == m_Today)
            {
                workedToday = m_WorkedToday + t;
            }
            else if (taskDate > m_Today)
            {
                workedToday = t;
            }

            lbTimeStats.Content = string.Format("{1:0.00} hrs ({0} min); today: {2:0.00} hrs", (int)t.TotalMinutes, t.TotalHours, workedToday.TotalHours);
        }

        private void LoadHistory()
        {
            ObservableCollection<string> newTasks = new ObservableCollection<string>(m_TaskNames);
            int pos = newTasks.Count();
            TimeSpan workedToday = new TimeSpan(0);
            try
            {
                using (FileStream fs = new FileStream(DATA_FILE_PATH, FileMode.Open))
                using (StreamReader sr = new StreamReader(fs))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine().Trim();
                        if (string.IsNullOrEmpty(line))
                            continue;

                        string[] parts = line.Split(new char[] { '|' }, 6);
                        if (parts.Length < 6)
                            continue;
                        DateTime date;
                        int minutes;
                        if (!DateTime.TryParse(parts[0], out date)
                            || !Int32.TryParse(parts[3], out minutes))
                            continue;
                        string taskName = parts[5];

                        if (!string.IsNullOrWhiteSpace(taskName) && !newTasks.Contains(taskName))
                            newTasks.Insert(pos, taskName);

                        DateTime today = DateTime.Now.Date;
                        if (date == today)
                        {
                            workedToday += new TimeSpan(0, minutes, 0);
                        }
                    }
                }
                TaskNames = newTasks;
                WorkedToday += workedToday;
            }
            catch (IOException)
            { }
        }

        private void SwitchIconToRecording()
        {
            TaskbarIcon.IconSource = (ImageSource)FindResource("IcoStop");
            IconTooltip = string.Format("Recording time since {0}. Click to stop", m_RecordStart.ToString("HH:mm"));
        }

        private void SwitchIconToStopped()
        {
            TaskbarIcon.IconSource = (ImageSource)FindResource("IcoRecord");
            IconTooltip = tbTooltipNotRecording.Text;
        }
    }
}
