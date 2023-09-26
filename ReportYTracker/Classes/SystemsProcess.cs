using ReportYTracker.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace ReportYTracker.Helpers
{
    internal class SystemsProcess
    {
        public static void OpenLink(string link)
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = link,
                UseShellExecute = true
            });
        }

        public static String? SecureStringToString(SecureString value)
        {
            if (value == default) return default;
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }
        public static DateRange GetDateRangeCurrentWeek(DateTime? now = null)
        {
            var dt = !now.HasValue ? DateTime.MinValue : now!.Value;
            if (now == null)
                dt = DateTime.Today;
            CultureInfo cultureInfo = CultureInfo.CurrentCulture;
            DateTime dateFrom = dt;
            if (dt.DayOfWeek != DayOfWeek.Monday)
            {
                dateFrom = dt.AddDays(-(int)dt.DayOfWeek + 1);
                while (dateFrom.DayOfWeek != cultureInfo.DateTimeFormat.FirstDayOfWeek)
                    dateFrom = dateFrom.AddDays(-1);
            }
            var dateTo = dateFrom.AddDays(6);
            return new DateRange(dateFrom, dateTo);
        }
    }
}
