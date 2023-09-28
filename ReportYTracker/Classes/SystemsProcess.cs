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

        public static MainWindow GetMainWindow()
        {
            return (MainWindow)System.Windows.Application.Current.MainWindow;
        }

        public static T GetMainContext<T>() where T:class
        {
            return GetMainWindow().context as T ?? throw new ArgumentNullException("Main Context is null");
        }
    }
}
