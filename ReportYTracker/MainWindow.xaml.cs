using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ReportYTracker.Data.Timetta;
using ReportYTracker.Data.YTracker;
using ReportYTracker.Helpers;
using ReportYTracker.Models;
using Calendar = System.Globalization.Calendar;

namespace ReportYTracker
{

    public class Context : INotifyPropertyChanged
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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private YTracker yt { get; set; }
        private Timetta tm { get; set; }
        private string? YTrackerFederation { get; set; }
        private DateRange dateRange { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            SetConfiguration();
        }
        //test
        private void Default_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (Settings.Default.IsSaveCredentialsYTracker || Settings.Default.IsSaveCredentialsTM)
                Settings.Default.Save();
        }

        private void SetConfiguration()
        {
            DataContext = new Context();

            Settings.Default.PropertyChanged += Default_PropertyChanged;

            yt_password.Password = Settings.Default.YTrackerPassword;
            tm_password.Password = Settings.Default.TMPassword;

            YTrackerFederation = ConfigurationManager.AppSettings["YTrackerFederation"];
            if (YTrackerFederation == null)
                throw new ArgumentNullException(nameof(YTrackerFederation), nameof(YTrackerFederation) + " is null");
        }

        private async void GetYaTrackerBtn_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            MainWindowWindow.IsEnabled = false;
            try
            {
                var context = ((Context)DataContext);

                dateRange = new DateRange(context.DateFrom, context.DateTo);

                yt ??= new YTracker()
                {
                    federation = YTrackerFederation!,
                };

                var resultAuth = await yt.Auth(new NetworkCredential(Settings.Default.YTrackerUserName, Settings.Default.YTrackerPassword));
                if (resultAuth)
                {
                    var data = await yt.GetData(dateRange);

                    if (data != null)
                    {
                        context.YTrackerData = (List<ReportYT>)data;
                        context.TotalYTrackerHours = data.Sum(x => x.Hours);
                    }
                }
                else throw new Exception("При авторизации что-то пошло не так");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                MainWindowWindow.IsEnabled = true;
            }
        }

        private async void SyncBtn_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            MainWindowWindow.IsEnabled = false;
            try
            {
                var context = ((Context)DataContext);

                dateRange = new DateRange(context.DateFrom, context.DateTo);

                tm ??= new Timetta();

                var result = await tm.Auth(new NetworkCredential(Settings.Default.TMUserName, Settings.Default.TMPassword));

                if (!result) throw new Exception("Неверные логин или пароль");

                var ts = await tm.GetData(dateRange);
                if (ts != null)
                {
                    var dataTm = await tm.ConvertFromReportYT(context.YTrackerData);

                    tm.PutTimeSheet(ts, dataTm);
                }
                else throw new Exception($"Не найдено подходящих timesheets, ожидаемый: {dateRange.DateFrom:dd.MM.yyyy} - {dateRange.DateTo:dd.MM.yyyy}, в статусе \"Черновик\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                MainWindowWindow.IsEnabled = true;
            }
        }
        private void yt_password_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var context = ((Context)DataContext);
            context.PasswordYTracker = ((PasswordBox)sender).SecurePassword;
        }

        private void tm_password_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var context = ((Context)DataContext);
            context.PasswordTM = ((PasswordBox)sender).SecurePassword;
        }
    }
}