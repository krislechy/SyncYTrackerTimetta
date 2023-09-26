using ReportYTracker.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ReportYTracker.Models
{
    public class DateRange
    {
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public DateRange(DateTime? from, DateTime? to)
        {
            DateTime dateFrom = from.HasValue ? from.Value : default;
            DateTime dateTo = to.HasValue ? to.Value : default;

            if (dateFrom == default || dateTo == default)
            {
                throw new ArgumentNullException("dateFrom or dateTo is null");
            }

            this.DateFrom = dateFrom;
            this.DateTo = dateTo;
        }
    }
}
