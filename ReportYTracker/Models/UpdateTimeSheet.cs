using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportYTracker.Models
{
    public class UpdateTimeSheet
    {
        public string id { get; set; }
        public int rowVersion { get; set; }
        public List<Timesheetline> timeSheetLines { get; set; }
    }

    public class Timesheetline
    {
        public List<Timeallocation> timeAllocations { get; set; }
        public string projectId { get; set; }
        public string projectTaskId { get; set; }
        public int orderNumber { get; set; }
    }

    public class Timeallocation
    {
        public double duration { get; set; }
        public string comments { get; set; }
        public string date { get; set; }
    }
}
