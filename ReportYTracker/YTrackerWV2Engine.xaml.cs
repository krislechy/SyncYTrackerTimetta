using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ReportYTracker
{
    public class YTrackerEngineParams
    {
        public string pathRequest { get; set; }
        public NetworkCredential credential { get; set; }
        public string host { get; set; }
        public string protocol { get; set; }
        public string federation { get; set; }
    }
    /// <summary>
    /// Логика взаимодействия для YTrackerWV2Engine.xaml
    /// </summary>
    public partial class YTrackerWV2Engine : Window
    {
        public List<Cookie> cookies = new List<Cookie>();

        private YTrackerEngineParams engineParams { get; }
        public YTrackerWV2Engine(YTrackerEngineParams engineParams)
        {
            InitializeComponent();

            this.engineParams = engineParams;

            wv2c.CoreWebView2InitializationCompleted += Wv2c_CoreWebView2InitializationCompleted;
            Loaded += YTrackerWV2Engine_Loaded;

        }

        private async void YTrackerWV2Engine_Loaded(object sender, RoutedEventArgs e)
        {
            await wv2c.EnsureCoreWebView2Async();
        }

        private void Wv2c_CoreWebView2InitializationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            wv2c.CoreWebView2.Navigate(String.IsNullOrEmpty(engineParams.pathRequest) ? $"{engineParams.protocol}://{engineParams.host}/federations/{engineParams.federation}" : engineParams.pathRequest);

            wv2c.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            wv2c.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;

        }

        private void CoreWebView2_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (e.Uri.Contains("export/pivot"))
                e.Cancel = true;
        }
        
        private async void CoreWebView2_NavigationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                isAllowClosing = true;
                IsContinue.TrySetResult(true);
                Close();
                return;
            };

            var vw = (CoreWebView2)sender!;

            var location = vw.Source;

            if (location.StartsWith("https://adfs.gpbl.ru/adfs/ls/"))
            {
                await vw.ExecuteScriptAsync($@"
                            document.getElementById('userNameInput').value='{engineParams.credential.UserName}';
                            document.getElementById('passwordInput').value='{engineParams.credential.Password}';
                            Login.submitLoginRequest();
                            ");
            }
            else if (location.StartsWith("https://tracker.yandex.ru/pages/my"))
            {
                foreach (var u in new[] { engineParams.host })
                {
                    var wv_cookies = await wv2c.CoreWebView2.CookieManager.GetCookiesAsync($"{engineParams.protocol}://{u}");
                    wv_cookies.ForEach(c => cookies.Add(c.ToSystemNetCookie()));
                }

                isAllowClosing = true;

                IsContinue.TrySetResult(true);
            }
            else if (location.Contains("/captcha"))
            {
                this.Visibility = Visibility.Visible;
            }
        }
        public TaskCompletionSource<bool> IsContinue = new TaskCompletionSource<bool>();
        private bool isAllowClosing = false;
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!isAllowClosing)
                e.Cancel = true;
        }
    }
}
