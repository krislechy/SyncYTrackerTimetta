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
    /// <summary>
    /// Логика взаимодействия для BrowserView.xaml
    /// </summary>
    public partial class BrowserView : Window
    {
        private string path { get; set; }
        public CookieCollection cookies { get; private set; }
        public BrowserView(string path, CookieCollection cookies)
        {
            this.path = path;
            this.cookies = cookies;

            InitializeComponent();

            wv2c.CoreWebView2InitializationCompleted += Wv2c_CoreWebView2InitializationCompleted;
            Loaded += BrowserView_Loaded;
        }

        private async void BrowserView_Loaded(object sender, RoutedEventArgs e)
        {
            await wv2c.EnsureCoreWebView2Async();
        }

        private void Wv2c_CoreWebView2InitializationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            wv2c.CoreWebView2.Navigate(path);

            foreach (Cookie s in cookies)
            {
                var cookie = wv2c.CoreWebView2.CookieManager.CreateCookie(s.Name, s.Value, s.Domain, s.Path);
                cookie.Expires = s.Expires;
                cookie.IsSecure = s.Secure;
                cookie.IsHttpOnly = s.HttpOnly;

                wv2c.CoreWebView2.CookieManager.AddOrUpdateCookie(cookie);
            }

            wv2c.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
        }
        private bool isAllowClosing = false;
        private async void CoreWebView2_NavigationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            var vw = (CoreWebView2)sender!;
            if (!vw.Source.Contains("/showcaptcha"))
            {
                cookies = new CookieCollection();
                foreach (var u in new[] { "tracker.yandex.ru" })
                {
                    var wv_cookies = await wv2c.CoreWebView2.CookieManager.GetCookiesAsync($"https://{u}");
                    foreach (var c in wv_cookies)
                        cookies.Add(c.ToSystemNetCookie());
                }
                isAllowClosing = true;
                this.Close();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!isAllowClosing)
                e.Cancel = true;
        }
    }
}
