using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Windows;

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
        public string Weekly { get { return ((decimal)WorkedMinutes.Sum() / 60.0m).ToString("0.00"); } }

        // This property doesn't have a public getter so that it's not displayed as a column in the DataGrid.
        // Use the GetWorkedMinutes() method instead
        public int[] WorkedMinutes { private get; set; }
        public int[] GetWorkedMinutes()
        {
            return WorkedMinutes;
        }
        public DateTime WeekStart { private get; set; }

        public WeekTaskData(string taskName, int dayIndex, int minutes, bool isTotals = false)
        {
            TaskName = taskName;
            WorkedMinutes = new int[7];
            WorkedMinutes[dayIndex] = minutes;
            m_IsTotals = isTotals;
        }

        public WeekTaskData(DateTime weekStart, string taskName, int[] workedMinutes, bool isTotals = false)
        {
            WeekStart = weekStart;
            TaskName = taskName;
            WorkedMinutes = workedMinutes;
            m_IsTotals = isTotals;
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

        private bool m_IsTotals = false;
        public bool IsTotals()
        {
            return m_IsTotals;
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
        /// <summary>
        /// A subset of TaskTimes for the current week (determined by WeekStartDay)
        /// </summary>
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
        /// <summary>
        /// Date of Monday of the currently displayed week
        /// </summary>
        public DateTime WeekStartDay
        {
            get { return m_WeekStartDay; }
            set
            {
                var newDay = WeekTitleConverter.GetWeekStartDay(value);
                if (newDay == m_WeekStartDay)
                    return;
                m_WeekStartDay = newDay;
                NotifyPropertyChange("WeekStartDay");
                UpdateCurrentWeekData();
            }
        }

        private bool m_IsJoiningLines = false;
        /// <summary>
        /// If true, the user is currently able to join datagrid lines by clicking on them. Otherwise clicking will have no effect
        /// </summary>
        public bool IsJoiningLines
        {
            get { return m_IsJoiningLines; }
            set
            {
                m_IsJoiningLines = value;
                if (value)
                    m_LineToJoinTo = -1;
                NotifyPropertyChange("IsJoiningLines");
                NotifyPropertyChange("JoinLinesButtonText");
            }
        }

        /// <summary>
        /// Text shown on the JoinLines button
        /// </summary>
        public string JoinLinesButtonText
        {
            get
            {
                return m_IsJoiningLines ? "Done Joining" : "Join Lines";
            }
        }

        /// <summary>
        /// Index of the task (in CurrentWeekData) to which to join other task in the JoinLine method (i.e. the target line).
        /// -1 denotes an unset value.
        /// </summary>
        private int m_LineToJoinTo = -1;

        private string m_FileIOError = null;
        /// <summary>
        /// Description of the last FileIO error that occurred during file operations. Should be null if
        /// there was no error or if the last error was already dismissed by the user
        /// </summary>
        public string FileIOError
        {
            get { return m_FileIOError; }
            set
            {
                m_FileIOError = value;
                NotifyPropertyChange("FileIOError");
            }
        }

        /// <summary>
        /// Returns standard error icon (white cross in red field) as a bitmap source usable by WPF controls
        /// </summary>
        public BitmapSource ErrorIconSource
        {
            get
            {
                return Imaging.CreateBitmapSourceFromHIcon(System.Drawing.SystemIcons.Error.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
        }

        private List<DateTime> m_Weeks = new List<DateTime>();
        /// <summary>
        /// List of all weeks (represented by the date of Monday of that week) for which we have data
        /// </summary>
        public List<DateTime> Weeks
        {
            get { return m_Weeks; }
            private set { m_Weeks = value; }
        }

        private int m_WeekIndex = -1;
        /// <summary>
        /// Index of the currently displayed week in the Weeks array
        /// </summary>
        public int WeekIndex
        {
            get { return m_WeekIndex; }
            set
            {
                if (value < 0 || value >= Weeks.Count)
                    return;
                m_WeekIndex = value;
                NotifyPropertyChange("WeekIndex");
                WeekStartDay = Weeks[m_WeekIndex];
                IsJoiningLines = false;
            }
        }

        #region INotifyPropertyChanged
        protected void NotifyPropertyChange(string prop)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

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

        /// <summary>
        /// Parses the TimeTrack log file. Reads all the data and then focuses on the last week, for which there is data in the log file.
        /// </summary>
        private void LoadLog()
        {
            List<string> errors = new List<string>();
            m_TaskTimes.Clear();
            var weeks = new SortedSet<DateTime>();

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

                        weeks.Add(WeekTitleConverter.GetWeekStartDay(date));
                    }
                }
            }
            catch (IOException e)
            {
                errors.Add(string.Format("File IO exception: {0}", e.Message));
            }
            catch (Exception e)
            {
                errors.Add(string.Format("{0} - {1}", e.GetType().Name, e.Message));
            }

            Weeks = weeks.ToList();
            WeekIndex = Weeks.Count - 1;

            if (errors.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Loading the log file resulted in the following error(s):");
                sb.AppendLine();
                foreach (string err in errors)
                {
                    sb.AppendLine(err);
                }
                FileIOError = sb.ToString();
            }
        }

        /// <summary>
        /// Recalculates tasks for the week determined by the WeekStartDay property; updates CurrentWeekData property
        /// </summary>
        public void UpdateCurrentWeekData()
        {
            // Compute week boundaries
            DateTime weekStart = WeekStartDay;
            DateTime weekEnd = WeekTitleConverter.GetWeekEndDay(weekStart);

            CurrentWeekData = new ObservableCollection<WeekTaskData>(
                m_TaskTimes
                    // Filter data for the given week
                    .Where(t => t.Key.Key >= weekStart && t.Key.Key <= weekEnd && t.Value > 0)
                    // Create a partial WeekTaskData structure for each task and day
                    .Select(t => new WeekTaskData(t.Key.Value, (t.Key.Key - weekStart).Days, t.Value))
                    // Group partial day records of the same tasks together
                    .GroupBy(t => t.TaskName, t => t.GetWorkedMinutes(), (key, days) => new WeekTaskData(weekStart, key, WeekTaskData.Condense(days)))
            );

            CurrentWeekData.Add(CalculateTotals());
        }

        /// <summary>
        /// Sums up all tasks and produces a record with total times for each day. The record has the IsTotals flag set.
        /// </summary>
        /// <returns>Aggregate record</returns>
        private WeekTaskData CalculateTotals()
        {
            WeekTaskData totals = new WeekTaskData(WeekStartDay, "Totals", new int[7], true);
            foreach (var tsk in CurrentWeekData.Where(t => !t.IsTotals()))
                totals = totals.MergeLine(tsk);
            totals.TaskName = "Totals";
            return totals;
        }

        /// <summary>
        /// Joins line with the given index to the line that is current join target. If the join target
        /// is not set yet (right after clicking the Join button) then the line with the given index
        /// will be selected as the target instead. The totals line cannot be joined.
        /// </summary>
        /// <param name="lineNo">Index of the line to be joined to the target, or of the target line itself if this is the first call after clicking Join</param>
        public void JoinLine(int lineNo)
        {
            // The last line contains daily totals and we don't want the user to join this line
            if (lineNo >= CurrentWeekData.Count - 1)
                return;

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
        }

        /// <summary>
        /// Delete all records from the log file. If requested, empty records (having 0 minutes each) will be
        /// created in the new file, one for each distinct task. That way, the new log will contain no time
        /// records, but all task names available for the user to select from the task name combo box (in the
        /// TimeTrack program).
        /// </summary>
        /// <param name="keepTaskNames">If true, empty record for each task name will be created in the new log</param>
        public void ClearLog(bool keepTaskNames)
        {
            try
            {
                // Overwrite the log file with an empty one
                using (FileStream fs = new FileStream(DATA_FILE_PATH, FileMode.Create))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    if (keepTaskNames)
                    {
                        // Create an empty task for each unique task name
                        ISet<string> uniqueTasks = new HashSet<string>(m_TaskTimes.Select(t => t.Key.Value).Distinct());
                        foreach (string taskName in uniqueTasks)
                        {
                            sw.WriteLine(string.Format("{0}|{1}|{1}|0|{2}|{3}",
                                DateTime.Now.ToString("yyyy.MM.dd"),
                                new DateTime().ToString("HH:mm"),
                                0.00m,
                                taskName));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                FileIOError = string.Format("Clearing the log raised the following error:{0}{0}{1} - {2}", Environment.NewLine, e.GetType().Name, e.Message);
            }
        }

        /// <summary>
        /// Try to move time around between days, so that each workday (Mon to Fri) has at least 8 hours, while the
        /// totals for each task and the overall weekly totals remain the same
        /// </summary>
        /// <returns>true if the values were equalized; false if it was not possible</returns>
        public bool Equalize8()
        {
            var totalsLine = CurrentWeekData.Where(l => l.IsTotals()).First();
            Dictionary<int, int> fund = new Dictionary<int, int>();
            Dictionary<int, int> lacking = new Dictionary<int, int>();
            for (int i = 0; i < 7; i++)
            {
                int min = totalsLine.GetWorkedMinutes()[i];
                if (i < 5 && min > 8 * 60)
                    fund.Add(i, min - 8 * 60);
                else if (i < 5 && min < 8 * 60)
                    lacking.Add(i, 8 * 60 - min);
                else if (i >= 5 && min > 0)
                    fund.Add(i, min);
            }

            if (fund.Values.Sum() < lacking.Values.Sum())
                // Impossible to solve
                return false;

            // For each column that needs some time added
            foreach (var receiver in lacking)
            {
                int lackingMinutes = receiver.Value;

                // Order tasks so that those that have nonzero values in the receiver column come first. We'd prefer to
                // just increase time of a task that really went on that day rather than introduce a new task.
                var orderedTasks =
                    CurrentWeekData
                        .Where(t => !t.IsTotals())
                        .Select((t, ind) => new { Task = t, Index = ind })
                        .OrderBy(t => t.Task.GetWorkedMinutes()[receiver.Key])
                        .Reverse()
                        .ToList();    // ToList() needed to prevent lazy linq evaluation, which would result in an InvalidOperationException

                foreach (var task in orderedTasks)
                {
                    var taskMinutes = task.Task.GetWorkedMinutes();
                    // Try to find a column that could miss a few minutes for the given task
                    var providers = fund.Where(f => f.Value > 0).Select(f => f.Key).ToList();
                    foreach (int provider in providers)
                    {
                        int fundAvailable = fund[provider];
                        if (fundAvailable == 0)
                            continue;
                        int transferTime = Minimum3(taskMinutes[provider], fundAvailable, lackingMinutes);
                        if (transferTime == 0)
                            continue;

                        // Updating values in the taskMinutes array automatically updates task.Task
                        taskMinutes[provider] -= transferTime;
                        taskMinutes[receiver.Key] += transferTime;
                        
                        // the row values are now updated, but we still need to reinsert the row to the collection to get the DataGrid updated
                        CurrentWeekData.Insert(task.Index, task.Task);
                        CurrentWeekData.RemoveAt(task.Index + 1);

                        fund[provider] -= transferTime;
                        lackingMinutes -= transferTime;

                        if (lackingMinutes == 0)
                            break;
                    }

                    if (lackingMinutes == 0)
                        break;
                }
            }

            // Update daily totals

            // Remove a totals record if it is already in the data
            foreach (int index in
                CurrentWeekData
                    .Select((t, ind) => new { Index = ind, Task = t })
                    .Where(t => t.Task.IsTotals())
                    .Select(t => t.Index)
                    .OrderBy(i => i)
                    .Reverse()
                    .ToList()  // ToList() needed to prevent lazy linq evaluation, which would result in an InvalidOperationException
                    )
                CurrentWeekData.RemoveAt(index);
            // Insert a new totals record
            CurrentWeekData.Add(CalculateTotals());

            return true;
        }

        private static int Minimum3(int a, int b, int c)
        {
            return a < b ?
                (a < c ? a : c) :
                (b < c ? b : c);
        }
    }
}
