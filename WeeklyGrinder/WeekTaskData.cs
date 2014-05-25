using System;
using System.Collections.Generic;
using System.Linq;

namespace WeeklyGrinder
{
    /// <summary>
    /// Data of a single task (a single row of the table) for a whole week
    /// </summary>
    public class WeekTaskData
    {
        private const string c_TitleColumnName = "TaskName";

        // Each public property is a column in the DataGrid
        public string TaskName { get; set; }
        public string Mon { get { return HoursToString(WorkedHours[0]); } }
        public string Tue { get { return HoursToString(WorkedHours[1]); } }
        public string Wed { get { return HoursToString(WorkedHours[2]); } }
        public string Thu { get { return HoursToString(WorkedHours[3]); } }
        public string Fri { get { return HoursToString(WorkedHours[4]); } }
        public string Sat { get { return HoursToString(WorkedHours[5]); } }
        public string Sun { get { return HoursToString(WorkedHours[6]); } }
        public string Weekly { get { return HoursToString(WorkedHours.Sum()); } }

        private string HoursToString(decimal hours)
        {
            return hours.ToString("0.00");
        }

        // This property doesn't have a public getter so that it's not displayed as a column in the DataGrid.
        // Use the GetWorkedHours() method instead
        public decimal[] WorkedHours { private get; set; }
        public decimal[] GetWorkedHours()
        {
            return WorkedHours;
        }
        public DateTime WeekStart { private get; set; }

        /// <summary>
        /// Initialize week task data with only a single day value
        /// </summary>
        /// <param name="taskName">Name of the task</param>
        /// <param name="dayIndex">Index of the day for which we have data</param>
        /// <param name="hours">Hours spent on the task on the given day</param>
        /// <param name="isTotals">Is this a totals record or not</param>
        public WeekTaskData(string taskName, int dayIndex, decimal hours, bool isTotals = false)
        {
            TaskName = taskName;
            WorkedHours = new decimal[7];
            WorkedHours[dayIndex] = hours;
            m_IsTotals = isTotals;
        }

        /// <summary>
        /// Initialize week task data with values for each day of the week
        /// </summary>
        /// <param name="weekStart">Date of the first day of the week</param>
        /// <param name="taskName">Name of the task</param>
        /// <param name="hours">Hours spent on the task on each day</param>
        /// <param name="isTotals">Is this a totals record or not</param>
        public WeekTaskData(DateTime weekStart, string taskName, decimal[] hours, bool isTotals = false)
        {
            WeekStart = weekStart;
            TaskName = taskName;
            WorkedHours = hours;
            m_IsTotals = isTotals;
        }

        public static decimal[] SumUpDailyHoursOfMultipleTasks(IEnumerable<decimal[]> taskHours)
        {
            decimal[] result = new decimal[7];
            foreach (var task in taskHours)
                for (int day = 0; day < 7; day++)
                    result[day] += task[day];
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

        public string GetColumnHeader(string propertyName)
        {
            if (propertyName == c_TitleColumnName)
                return "Task";
            else if (_DayIndices.ContainsKey(propertyName))
                return (WeekStart + new TimeSpan(_DayIndices[propertyName], 0, 0, 0)).ToString("ddd dd");
            else
                return propertyName;
        }

        public bool IsTitleColumn(string propertyName)
        {
            return (propertyName == c_TitleColumnName);
        }

        /// <summary>
        /// Merge another line into this one
        /// </summary>
        /// <param name="other">The other line to merge into this one</param>
        /// <param name="doNotTransfer">If true, do not remove the time from the other task. Used for the totals line</param>
        /// <returns>Updated line</returns>
        public WeekTaskData MergeOtherLine(WeekTaskData other, bool doNotTransfer = false)
        {
            for (int i = 0; i < 7; i++)
            {
                if (!doNotTransfer)
                {
                    TransferPartialTask(other, this, i, i, other.WorkedHours[i]);
                }
                WorkedHours[i] += other.WorkedHours[i];
            }
            TaskName += " + " + other.TaskName;
            return this;
        }

        private bool m_IsTotals = false;
        public bool IsTotals()
        {
            return m_IsTotals;
        }

        #region Partial tasks
        // Partial tasks are a subdivision of the task. When two lines are merged together, the worked hours values represent
        // the sum of the merged tasks. It may however be useful to know the partial times of the merged tasks, so the partial
        // tasks structure contains the times of the tasks that were joined together.

        private class PartialTask
        {
            public decimal Hours { get; set; }
            public string Name { get; set; }
            public override string ToString()
            {
                return string.Format("{0:0.00}: {1}", Hours, Name);
            }
        }

        private List<PartialTask>[] m_PartialTasks = new List<PartialTask>[7];

        public List<string> GetPartialTaskDescriptions(int weekday)
        {
            var partTasks = m_PartialTasks[weekday] ?? ConvertTaskToPartialTask(weekday);
            return partTasks.Where(pt => pt.Hours > 0m)
                            .Select(pt => pt.ToString()).ToList();
        }

        private List<PartialTask> ConvertTaskToPartialTask(int weekday)
        {
            return new List<PartialTask>() { new PartialTask() { Hours = WorkedHours[weekday], Name = TaskName } };
        }

        /// <summary>
        /// Transfer time from one cell to another. Update partial tasks of the source and of the target cell.
        /// </summary>
        /// <param name="sourceTask">Task to take time from</param>
        /// <param name="destTask">Task to transfer time to</param>
        /// <param name="sourceDay">Index of the day to take time from</param>
        /// <param name="destDay">Index of the day to transfer time to</param>
        /// <param name="amount">Amount of time (hours) to transfer</param>
        public static void TransferPartialTask(WeekTaskData sourceTask, WeekTaskData destTask, int sourceDay, int destDay, decimal amount)
        {
            var sourcePartTasks = sourceTask.m_PartialTasks[sourceDay] ?? sourceTask.ConvertTaskToPartialTask(sourceDay);
            var destPartTasks = destTask.m_PartialTasks[destDay] ?? destTask.ConvertTaskToPartialTask(destDay);
            
            foreach (var pt in sourcePartTasks)
            {
                decimal transferAmount = amount < pt.Hours ? amount : pt.Hours;
                amount -= transferAmount;
                pt.Hours -= transferAmount;

                // Try to find an existing partial task with the same name in the target cell and add time to it
                bool newPt = true;
                foreach (var destPt in destPartTasks)
                {
                    if (destPt.Name == pt.Name)
                    {
                        destPt.Hours += transferAmount;
                        newPt = false;
                        break;
                    }
                }
                // If an existing partial task was not found, create a new one in the target cell
                if (newPt)
                {
                    destPartTasks.Add(new PartialTask() { Hours = transferAmount, Name = pt.Name });
                }

                if (amount == 0m)
                    break;
            }

            sourceTask.m_PartialTasks[sourceDay] = sourcePartTasks;
            destTask.m_PartialTasks[destDay] = destPartTasks;
        }
        #endregion
    }
}