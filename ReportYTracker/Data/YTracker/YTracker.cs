using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using ReportYTracker.Context;
using ReportYTracker.Helpers;
using ReportYTracker.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Formats.Asn1;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Security.Policy;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace ReportYTracker.Data.YTracker
{
    public class YTracker : BaseService<IEnumerable<ReportYT>>, IService<IEnumerable<ReportYT>>
    {
        private MainContext context { get => SystemsProcess.GetMainContext<MainContext>(); }
        public string federation { get; set; } = "";
        private string domain { get; set; }
        private string userName { get; set; }
        private string authMethod { get; }

        private NetworkCredential credential { get; set; }

        private const string cookieSessionFileName = "ytracker.json";
        private const string cookieSessionName = "yc_session";
        private const string csrfToken = "CSRF-TOKEN";
        public YTracker()
        {
            base.protocol = "https";
            base.host = "tracker.yandex.ru";
            this.authMethod = "FormsAuthentication";

            InitHeaders();
            LoadSession(cookieSessionFileName);
        }
        private void CheckFields()
        {
            if (String.IsNullOrEmpty(federation?.Trim())) throw new ArgumentNullException(nameof(federation), $"{nameof(federation)} cannot be null");
        }
        public override async Task<bool> Auth(NetworkCredential networkCredential)
        {
            this.credential = networkCredential;

            CheckFields();

            if (networkCredential.UserName.Contains("\\"))
            {
                var split = networkCredential.UserName.Split("\\");
                domain = split[0];
                userName = split[1];
            }
            else throw new InvalidDataException("Логин должен содержать домен, domain\\username");

            if (IsSessionExpired())
            {
                HttpResponseMessage response;

                var federationLink = $"{protocol}://{host}/federations/{federation}";

                response = await proxy.GetAsync(federationLink);

                if (!CheckCaptcha(response)) return await Auth(networkCredential);

                var location = response.RequestMessage?.RequestUri?.AbsoluteUri;

                if (location.Contains("/pages/my")) return true;

                if (String.IsNullOrEmpty(location)) return false;

                var content = GetParamsContent(new Dictionary<string, string>()
                    {
                        {"UserName", networkCredential.UserName },
                        {"Password", networkCredential.Password },
                        {"AuthMethod", authMethod },
                    });

                response = await proxy.PostAsync(location, content);
                if (!response.IsSuccessStatusCode) throw new Exception("YTracker: Неверный логин или пароль");

                var responseContent = await response.Content.ReadAsStringAsync();
                var parameters = HtmlParser.ParseHtmlInputs(responseContent);
                var actionForm = HtmlParser.GetFormAction(responseContent);

                response = await proxy.PostAsync(actionForm, GetParamsContent(parameters));
                if (!response.IsSuccessStatusCode) throw new Exception("YTracker: Неверный логин или пароль");

                if (!CheckCaptcha(response)) return await Auth(networkCredential);

                SaveSession(cookieSessionFileName);

                return true;
            }
            else return true;
        }
        private void SaveSession(string pathFile)
        {
            var yc_session = cookies.GetCookies(proxy.BaseAddress).FirstOrDefault(x => x.Name == cookieSessionName);
            var yc_csrfToken = cookies.GetCookies(proxy.BaseAddress).FirstOrDefault(x => x.Name == csrfToken);
            if (yc_session != null)
            {
                var coll = new List<YTStorageModel>
                {
                    new YTStorageModel()
                    {
                        Name = yc_session.Name,
                        Value = yc_session.Value,
                        Expires = yc_session.Expires,
                    },
                    new YTStorageModel()
                    {
                        Name = yc_csrfToken.Name,
                        Value = yc_csrfToken.Value,
                        Expires = yc_csrfToken.Expires,
                    }
                };
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(coll);
                File.WriteAllText(pathFile, json);
            }
        }
        private void LoadSession(string pathFile)
        {
            if (File.Exists(pathFile))
            {
                var json = File.ReadAllText(pathFile);
                var ytsts = JsonConvert.DeserializeObject<IEnumerable<YTStorageModel>>(json);
                if (cookies != null)
                {
                    foreach (var ytst in ytsts)
                    {
                        var cookie = new Cookie(ytst.Name, ytst.Value)
                        {
                            Domain = proxy.BaseAddress.Host,
                            Expires = ytst.Expires,
                        };

                        if (cookie.Expired) return;
                        cookies.Add(cookie);
                    }
                }
            }
        }
        private bool IsSessionExpired()
        {
            var yc_session = cookies.GetCookies(proxy.BaseAddress).FirstOrDefault(x => x.Name == cookieSessionName);
            if (yc_session != null)
                return yc_session.Expired;
            return true;
        }
        public override async Task<IEnumerable<ReportYT>> GetData(DateRange dateRange)
        {
            var data = await ExportReport(dateRange);

            ///
            var dates = new List<DateTime>();

            for (var dt = dateRange.DateFrom; dt <= dateRange.DateTo; dt = dt.AddDays(1))
            {
                dates.Add(dt);
            }

            var newData = new List<ReportYT>();

            var totalCount = (100 - context.Progress) / (double)data.Count();

            foreach (var report in data)
            {
                var content = new StringContent(JsonConvert.SerializeObject(new
                {
                    issueKey = report.TaskId,
                    perPage = 9999,
                    allRecords = true,
                }), Encoding.UTF8, "application/json");
                var response = await proxy.PostAsync($"{protocol}://{host}/gateway/root/tracker/getUserWorklogsHistory", content);
                var contentResponse = await response.Content.ReadAsStringAsync();
                var histories = JsonConvert.DeserializeObject<UserWorkLogsHistoryYT>(contentResponse);
                if (histories == null) throw new Exception("getUserWorklogsHistory is null");
                foreach (var date in dates)
                {
                    foreach (var history in histories.data)
                    {
                        if (history.start.Date == date.Date)
                        {
                            var sameTask = newData.FirstOrDefault(x => x.Date.Date == date.Date && x.TaskId == history.issue.key);
                            if (sameTask == null)
                            {
                                newData.Add(new ReportYT()
                                {
                                    TaskId = history.issue.key,
                                    Date = date.Date,
                                    Hours = history.durationNormal,
                                    Name = history.issue.display,
                                });
                            }
                            else
                            {
                                sameTask.Hours += history.durationNormal;
                            }
                        }
                    }
                }

                context.Progress += totalCount;
            }
            return newData;
        }
        private async Task<IEnumerable<ReportYT>> ExportReport(DateRange dateRange)
        {
            var data = new List<ReportYT>();
            var response = await proxy.GetAsync($"{protocol}://{host}/export/pivot?reportType=pivot&from={dateRange.DateFrom:yyyy-MM-dd}&to={dateRange.DateTo:yyyy-MM-dd}&queue=DIT&format=csv&users={userName}&orgid=null");
            if (response?.RequestMessage?.RequestUri?.Host == "auth.cloud.yandex.ru")
            {
                cookies = new CookieContainer();
                if (await Auth(credential))
                {
                    return await ExportReport(dateRange);
                }
                else throw new UnauthorizedAccessException("Не удалось авторизоваться");
            }
            if (!CheckCaptcha(response)) await ExportReport(dateRange);
            if (response.Content?.Headers?.ContentType?.MediaType == "text/csv")
            {
                var stream = response.Content.ReadAsStream();
                CreateModel(stream, ref data);
            }
            return data;
        }
        public int countTry = 0;
        private bool CheckCaptcha(HttpResponseMessage responseMessage)
        {
            var path = responseMessage.RequestMessage?.RequestUri?.AbsoluteUri;
            if (path == null) return true;
            if ((path.Contains("/showcaptcha")))
            {
                countTry++;
                if (countTry > 5) throw new Exception("Попробуйте попозже");
                var allcookies = cookies.GetAllCookies();

                var window = new BrowserView(path, allcookies);
                window.Owner = System.Windows.Application.Current.MainWindow;
                window.ShowDialog();

                cookies = new CookieContainer();

                foreach (Cookie cookie in window.cookies)
                    cookies.Add(cookie);

                context.ProgressColor = Brushes.Orange;
                return false;
            }
            return true;
        }

        private void CreateModel(Stream stream, ref List<ReportYT> data)
        {
            int row = 1;
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    row++;
                    if (row > 5 && !String.IsNullOrEmpty(line?.Trim()))
                    {
                        var matches = Regex.Matches(line, "(?<=^|,)(\"(?:[^\"]|\"\")*\"|[^,]*)");

                        string[] values = new string[matches.Count];

                        for (int i = 0; i < matches.Count; i++)
                            values[i] = matches[i].Value.Trim('\"');
                        var hoursRaw = values[4]?.Replace("ч", "")?.Replace(",", ".");
                        double hours = 0;
                        if (!String.IsNullOrEmpty(hoursRaw))
                            hours = Double.Parse(hoursRaw, CultureInfo.InvariantCulture);

                        data.Add(new ReportYT()
                        {
                            TaskId = values[1],
                            Name = values[2],
                            Hours = hours,
                        });
                    }
                }
            }
        }
    }
}
