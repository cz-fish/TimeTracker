using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.ObjectModel;

namespace WeeklyGrinder
{
    public class WeekTaskData
    {
        // Each public property is a column in the DataGrid
        public string TaskName { get; set; }
        public string Mon { get { return ((decimal)WorkedMinutes[0] / 60.0m).ToString("0.00"); } }
        public string Tue { get { return ((decimal)WorkedMinutes[1] / 60.0m).ToString("0.00"); } }
        public string Wed { get { return ((decimal)WorkedMinutes[2] / 60.0m).ToString("0.00"); } }
        public string Thu { get { return ((decimal)WorkedMinutes[3] / 60.0m).ToString("0.00"); } }
        public string Fri { get { return ((decimal)WorkedMinutes[4] / 60.0m).ToString("0.00"); } }
        public string Sat { get { return ((decimal)WorkedMinutes[5] / 60.0m).ToString("0.00"); } }
        public string Sun { get { return ((decimal)WorkedMinutes[6] / 60.0m).ToString("0.00"); } }

        public int[] WorkedMinutes { private get; set; }
        public int[] GetWorkedMinutes()
        {
            return WorkedMinutes;
        }
        public DateTime WeekStart { private get; set; }

        public WeekTaskData(string taskName, int dayIndex, int minutes)
        {
            TaskName = taskName;
            WorkedMinutes = new int[7];
            WorkedMinutes[dayIndex] = minutes;
        }

        public WeekTaskData(DateTime weekStart, string taskName, int[] workedMinutes)
        {
            WeekStart = weekStart;
            TaskName = taskName;
            WorkedMinutes = workedMinutes;
        }

        public static int[] Condense(IEnumerable<int[]> dayData)
        {
            int[] result = new int[7];
            foreach (var i in dayData)
                for (int j = 0; j < 7; j++)
                    result[j] += i[j];
            return result;
        }

        private static Dictionary<string, int> _DayIndices = new Dictionary<string, int>()
        {
            { "Mon", 0 },
            { "Tue", 1 },
            { "Wed", 2 },
            { "Thu", 3 },
            { "Fri", 4 },
            { "Sat", 5 },
            { "Sun", 6 }
        };

        public string GetColumnTitle(PropertyDescriptor desc)
        {
            string propName = desc.Name;
            if (propName == "TaskName")
                return "Task";
            else if (_DayIndices.ContainsKey(propName))
                return (WeekStart + new TimeSpan(_DayIndices[propName], 0, 0, 0)).ToString("ddd dd");
            else
                return propName;
        }

        public WeekTaskData MergeLine(WeekTaskData other)
        {
            TaskName += " + " + other.TaskName;
            for (int i = 0; i < 7; i++)
            {
                WorkedMinutes[i] += other.WorkedMinutes[i];
            }
            return this;
        }
    }

    class DataModel: INotifyPropertyChanged
    {
        public readonly string DATA_FILE_PATH;
        public const string DATA_FILE_NAME = "TimeTrack.txt";

        /// <summary>
        /// Maps date and task name to minutes spent on the given day with the given task
        /// </summary>
        private Dictionary<KeyValuePair<DateTime, string>, int> m_TaskTimes = new Dictionary<KeyValuePair<DateTime, string>, int>();
        private ObservableCollection<WeekTaskData> m_CurrentWeekData;
        public ObservableCollection<WeekTaskData> CurrentWeekData
        {
            get { return m_CurrentWeekData; }
            set
            {
                m_CurrentWeekData = value;
                NotifyPropertyChange("CurrentWeekData");
            }
        }

        private DateTime m_WeekStartDay;
        public DateTime WeekStartDay
        {
            get { return m_WeekStartDay; }
            set
            {
                m_WeekStartDay = WeekTitleConverter.GetWeekStartDay(value);
                NotifyPropertyChange("WeekStartDay");
            }
        }

        private bool m_IsJoiningRows = false;
        public bool IsJoiningRows
        {
            get { return m_IsJoiningRows; }
            set
            {
                m_IsJoiningRows = value;
                if (value)
                    m_LineToJoinTo = -1;
                NotifyPropertyChange("IsJoiningRows");
                NotifyPropertyChange("JoinLinesButtonText");
            }
        }

        public string JoinLinesButtonText
        {
            get
            {
                return m_IsJoiningRows ? "Done Joining" : "Join Lines";
            }
        }

        private int m_LineToJoinTo = -1;

        private bool m_CanSplitLines = false;
        public bool CanSplitLines
        {
            get { return m_CanSplitLines; }
            set
            {
                m_CanSplitLines = value;
                NotifyPropertyChange("CanSplitLines");
                if (!value)
                    IsJoiningRows = false;
            }
        }

        protected void NotifyPropertyChange(string prop)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public DataModel()
        {
            DATA_FILE_PATH = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments, Environment.SpecialFolderOption.None), DATA_FILE_NAME);
            WeekStartDay = WeekTitleConverter.GetWeekStartDay(DateTime.Now);

            new Task(() =>
                {
                    LoadLog();
                    UpdateCurrentWeekData();
                }).Start();
        }

        private void LoadLog()
        {
            List<string> errors = new List<string>();
            m_TaskTimes.Clear();

            try
            {
                int lineno = 0;
                using (FileStream fs = new FileStream(DATA_FILE_PATH, FileMode.Open))
                using (StreamReader sr = new StreamReader(fs))
                {
                    while (!sr.EndOfStream)
                    {
                        lineno++;
                        string line = sr.ReadLine();
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        string[] parts = line.Split(new char[] { '|' }, 6);
                        if (parts.Length < 6)
                        {
                            errors.Add(string.Format("Line #{0}: {1}{2}Not enough columns (expected 6, got {3})", lineno, line, Environment.NewLine, parts.Length));
                            continue;
                        }
                        DateTime date;
                        int minutes;
                        if (!DateTime.TryParse(parts[0], out date)
                            || !Int32.TryParse(parts[3], out minutes))
                        {
                            errors.Add(string.Format("Line #{0}: {1}{2}Failed to parse date or duration in minutes", lineno, line, Environment.NewLine));
                            continue;
                        }
                        string taskName = parts[5];

                        var key = new KeyValuePair<DateTime, string>(date, taskName);
                        if (!m_TaskTimes.ContainsKey(key))
                        {
                            m_TaskTimes.Add(key, 0);
                        }
                        m_TaskTimes[key] += minutes;
                    }
                }
            }
            catch (IOException e)
            {
                errors.Add(string.Format("File IO exception: {0}", e.Message));
            }

            // TODO: show errors somehow
        }

        public void UpdateCurrentWeekData()
        {
            // Compute week boundaries
            DateTime weekStart = WeekStartDay;
            DateTime weekEnd = WeekTitleConverter.GetWeekEndDay(weekStart);

            CurrentWeekData = new ObservableCollection<WeekTaskData>(
                m_TaskTimes
                    // Filter data for the given week
                    .Where(t => t.Key.Key >= weekStart && t.Key.Key <= weekEnd)
                    // Create a partial WeekTaskData structure for each task and day
                    .Select(t => new WeekTaskData(t.Key.Value, (t.Key.Key - weekStart).Days, t.Value))
                    // Group partial day records of the same tasks together
                    .GroupBy(t => t.TaskName, t => t.GetWorkedMinutes(), (key, days) => new WeekTaskData(weekStart, key, WeekTaskData.Condense(days)))
            );
        }

        public void JoinLine(int lineNo)
        {
            if (m_LineToJoinTo == -1)
            {
                m_LineToJoinTo = lineNo;
                return;
            }
            if (m_LineToJoinTo == lineNo)
                return;

            // It's not sufficient to just change contents of the element on the m_LineToJoinTo position.
            // To refresh the DataGrid contents, we insert a new row and delete the old two.
            CurrentWeekData.Insert(m_LineToJoinTo, CurrentWeekData[m_LineToJoinTo].MergeLine(CurrentWeekData[lineNo]));
            CurrentWeekData.RemoveAt(m_LineToJoinTo + 1);
            CurrentWeekData.RemoveAt(lineNo);
            // Update the index of the target line, if the deleted line was before it in the list
            if (lineNo < m_LineToJoinTo)
                m_LineToJoinTo--;
            CanSplitLines = true;
        }
    }
}
