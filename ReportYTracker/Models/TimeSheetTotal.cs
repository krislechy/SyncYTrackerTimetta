using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportYTracker.Models
{
    class TimeSheetTotal
    {
        public string odatacontext { get; set; }
        public Value[] value { get; set; }
    }
    public class Value
    {
        public string id { get; set; }
        public Timesheet timeSheet { get; set; }
    }

    public class Timesheet
    {
        public string dateFrom { get; set; }
        public string dateTo { get; set; }
        public string id { get; set; }
        public State state { get; set; }
    }

    public class State
    {
        public string code { get; set; }
        public string id { get; set; }
    }
}
