using ReportYTracker.Helpers;
using ReportYTracker.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ReportYTracker.Context
{
    public class MainContext : INotifyPropertyChanged
    {
        private List<ReportYT> yTrackerData;
        public List<ReportYT> YTrackerData
        {

            get { return yTrackerData; }
            set
            {
                yTrackerData = value;
                OnPropertyChanged(nameof(YTrackerData));
                OnPropertyChanged(nameof(IsEnabledSync));
            }
        }
        private double totalYTrackerHours { get; set; }
        public double TotalYTrackerHours
        {
            get { return totalYTrackerHours; }
            set
            {
                totalYTrackerHours = value;
                OnPropertyChanged(nameof(TotalYTrackerHours));
            }
        }

        private double progress { get; set; }
        public double Progress
        {
            get { return progress; }
            set
            {
                progress = value;
                OnPropertyChanged(nameof(Progress));
            }
        }

        private Brush progressColor { get; set; }
        public Brush ProgressColor
        {
            get { return progressColor; }
            set
            {
                progressColor = value;
                OnPropertyChanged(nameof(ProgressColor));
            }
        }

        private bool progressIsIndeterminate { get; set; }
        public bool ProgressIsIndeterminate
        {
            get { return progressIsIndeterminate; }
            set
            {
                progressIsIndeterminate = value;
                OnPropertyChanged(nameof(ProgressIsIndeterminate));
            }
        }

        #region DatePicker
        private DateTime? dateFrom { get; set; }
        public DateTime? DateFrom
        {
            get
            {
                if (dateFrom == null)
                    return GetDateRange().DateFrom;
                return dateFrom;
            }
            set
            {
                dateFrom = GetDateRange(value).DateFrom;
                OnPropertyChanged(nameof(DateFrom));
                OnPropertyChanged(nameof(DateTo));
            }
        }
        public DateTime? DateTo
        {
            get { return GetDateRange(dateFrom).DateTo; }
        }
        private Calendar Calendar = new CultureInfo("ru-RU").Calendar;
        private DateRange GetDateRange(DateTime? selectedDate = null)
        {
            var date = selectedDate.HasValue ? selectedDate.Value : DateTime.Now;

            var dayOfWeek = Calendar.GetDayOfWeek(date);

            var isSunday = dayOfWeek == DayOfWeek.Sunday;
            var dateFrom = date.AddDays(-(int)dayOfWeek + (isSunday ? -6 : 1));

            var dateTo = dateFrom.AddDays(6);

            if (dateFrom.Month != dateTo.Month)
            {
                if (date.Month == dateFrom.Month)
                {
                    dateTo = new DateTime(dateFrom.Year, dateFrom.Month, DateTime.DaysInMonth(dateFrom.Year, dateFrom.Month));
                }
                else
                {
                    dateFrom = new DateTime(dateTo.Year, dateTo.Month, 1);
                }
            }
            return new DateRange(dateFrom, dateTo);
        }
        #endregion

        #region YTracker
        private string userNameYTracker { get; set; }
        public string UserNameYTracker
        {
            get { return Settings.Default.YTrackerUserName ?? userNameYTracker; }
            set
            {
                userNameYTracker = value;
                Settings.Default.YTrackerUserName = value;
                OnPropertyChanged(nameof(UserNameYTracker));
            }
        }

        private SecureString passwordYTracker { get; set; }
        public SecureString PasswordYTracker
        {
            get { return new NetworkCredential("", Settings.Default.YTrackerPassword).SecurePassword ?? passwordYTracker; }
            set
            {
                passwordYTracker = value;
                Settings.Default.YTrackerPassword = SystemsProcess.SecureStringToString(value);
                OnPropertyChanged(nameof(PasswordYTracker));
            }
        }
        private bool isSaveCredentialsYTracker { get; set; }
        public bool IsSaveCredentialsYTracker
        {
            get { return Settings.Default.IsSaveCredentialsYTracker; }
            set
            {
                if (isSaveCredentialsYTracker != value)
                {
                    if (value == false)
                    {
                        UserNameYTracker = default!;
                        PasswordYTracker = default!;
                    }
                    OnPropertyChanged(nameof(UserNameYTracker));
                    OnPropertyChanged(nameof(PasswordYTracker));
                }
                isSaveCredentialsYTracker = value;
                Settings.Default.IsSaveCredentialsYTracker = value;
                OnPropertyChanged(nameof(IsSaveCredentialsYTracker));
            }
        }
        #endregion

        #region Timetta
        private string userNameTM { get; set; }
        public string UserNameTM
        {
            get { return Settings.Default.TMUserName ?? userNameTM; }
            set
            {
                userNameTM = value;
                Settings.Default.TMUserName = value;
                OnPropertyChanged(nameof(UserNameTM));
            }
        }

        private SecureString passwordTM { get; set; }
        public SecureString PasswordTM
        {
            get { return new NetworkCredential("", Settings.Default.TMPassword).SecurePassword ?? passwordTM; }
            set
            {
                passwordTM = value;
                Settings.Default.TMPassword = SystemsProcess.SecureStringToString(value);
                OnPropertyChanged(nameof(PasswordTM));
            }
        }
        private bool isSaveCredentialsTM { get; set; }
        public bool IsSaveCredentialsTM
        {
            get { return Settings.Default.IsSaveCredentialsTM; }
            set
            {
                if (isSaveCredentialsTM != value)
                {
                    if (value == false)
                    {
                        UserNameTM = default!;
                        PasswordTM = default!;
                    }
                    OnPropertyChanged(nameof(UserNameTM));
                    OnPropertyChanged(nameof(PasswordTM));
                }
                isSaveCredentialsTM = value;
                Settings.Default.IsSaveCredentialsTM = value;
                OnPropertyChanged(nameof(IsSaveCredentialsTM));
            }
        }
        private bool isEnabledSync { get; set; }
        public bool IsEnabledSync
        {
            get { return yTrackerData != null; }
        }
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
        #endregion
    }
}
