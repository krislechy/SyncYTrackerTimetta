using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportYTracker.Models
{
    public class ReportYT
    {
        public string TaskId { get; set; }
        public string Name { get; set; }
        public double Hours { get; set; }
        public DateTime Date { get; set; }
    }
}
