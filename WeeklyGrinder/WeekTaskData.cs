using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace WeeklyGrinder
{
    public class WeekTaskData
    {
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

        public WeekTaskData(string taskName, int dayIndex, decimal hours, bool isTotals = false)
        {
            TaskName = taskName;
            WorkedHours = new decimal[7];
            WorkedHours[dayIndex] = hours;
            m_IsTotals = isTotals;
        }

        public WeekTaskData(DateTime weekStart, string taskName, decimal[] hours, bool isTotals = false)
        {
            WeekStart = weekStart;
            TaskName = taskName;
            WorkedHours = hours;
            m_IsTotals = isTotals;
        }

        public static decimal[] Condense(IEnumerable<decimal[]> dayData)
        {
            decimal[] result = new decimal[7];
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

        public WeekTaskData MergeLine(WeekTaskData other, bool doNotTransfer = false)
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
            var partTasks = m_PartialTasks[weekday] ?? GetDefaultPartialTask(weekday);
            return partTasks.Where(pt => pt.Hours > 0m)
                            .Select(pt => pt.ToString()).ToList();
        }

        private List<PartialTask> GetDefaultPartialTask(int weekday)
        {
            return new List<PartialTask>() { new PartialTask() { Hours = WorkedHours[weekday], Name = TaskName } };
        }

        public static void TransferPartialTask(WeekTaskData sourceTask, WeekTaskData destTask, int sourceDay, int destDay, decimal amount)
        {
            var sourcePartTasks = sourceTask.m_PartialTasks[sourceDay] ?? sourceTask.GetDefaultPartialTask(sourceDay);
            var destPartTasks = destTask.m_PartialTasks[destDay] ?? destTask.GetDefaultPartialTask(destDay);
            foreach (var pt in sourcePartTasks)
            {
                decimal transferAmount = amount < pt.Hours ? amount : pt.Hours;
                amount -= transferAmount;
                pt.Hours -= transferAmount;
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
    }
}