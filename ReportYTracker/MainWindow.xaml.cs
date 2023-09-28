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
using ReportYTracker.Context;
using ReportYTracker.Data.Timetta;
using ReportYTracker.Data.YTracker;
using ReportYTracker.Helpers;
using ReportYTracker.Models;
using Calendar = System.Globalization.Calendar;

namespace ReportYTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainContext context { get => (MainContext)DataContext; }
        private YTracker yt { get; set; }
        private Timetta tm { get; set; }
        private string? YTrackerFederation { get; set; }
        private DateRange dateRange { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            SetConfiguration();
        }
        private void Default_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (Settings.Default.IsSaveCredentialsYTracker || Settings.Default.IsSaveCredentialsTM)
                Settings.Default.Save();
        }

        private void SetConfiguration()
        {
            DataContext = new MainContext();

            Settings.Default.PropertyChanged += Default_PropertyChanged;

            yt_password.Password = Settings.Default.YTrackerPassword;
            tm_password.Password = Settings.Default.TMPassword;

            YTrackerFederation = ConfigurationManager.AppSettings["YTrackerFederation"];
            if (YTrackerFederation == null)
                throw new ArgumentNullException(nameof(YTrackerFederation), nameof(YTrackerFederation) + " is null");
        }

        private async void GetYaTrackerBtn_Click(object sender, RoutedEventArgs e)
        {
            context.ProgressColor = Brushes.Green;
            context.Progress = 0;

            var btn = (Button)sender;
            MainWindowWindow.IsEnabled = false;
            try
            {
                dateRange = new DateRange(context.DateFrom, context.DateTo);

                yt ??= new YTracker()
                {
                    federation = YTrackerFederation!,
                };
                yt.countTry = 0;
                var resultAuth = await yt.Auth(new NetworkCredential(Settings.Default.YTrackerUserName, Settings.Default.YTrackerPassword));
                context.Progress = 20;
                if (resultAuth)
                {
                    var data = await yt.GetData(dateRange);

                    if (data != null)
                    {
                        context.YTrackerData = (List<ReportYT>)data;
                        context.TotalYTrackerHours = data.Sum(x => x.Hours);
                    }
                    //context.Progress = 100;
                }
                else throw new Exception("При авторизации что-то пошло не так");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                context.ProgressColor = Brushes.Red;
            }
            finally
            {
                MainWindowWindow.IsEnabled = true;
            }
        }

        private async void SyncBtn_Click(object sender, RoutedEventArgs e)
        {
            context.ProgressColor = Brushes.Green;
            context.Progress = 0;
            context.ProgressIsIndeterminate = true;

            var btn = (Button)sender;
            MainWindowWindow.IsEnabled = false;
            try
            {
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
                context.ProgressIsIndeterminate = false;
            }
        }
        private void yt_password_PasswordChanged(object sender, RoutedEventArgs e)
        {
            context.PasswordYTracker = ((PasswordBox)sender).SecurePassword;
        }

        private void tm_password_PasswordChanged(object sender, RoutedEventArgs e)
        {
            context.PasswordTM = ((PasswordBox)sender).SecurePassword;
        }
    }
}