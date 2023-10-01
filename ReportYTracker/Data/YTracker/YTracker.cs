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
using System.Windows.Threading;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace ReportYTracker.Data.YTracker
{
    public class YTracker : BaseService<IEnumerable<ReportYT>>, IService<IEnumerable<ReportYT>>
    {
        private MainContext context { get => SystemsProcess.GetMainContext<MainContext>(); }
        public string federation { get; }
        private YTrackerWV2Engine engine { get; }
        public YTracker(NetworkCredential credential, string federation) : base(credential)
        {
            base.protocol = "https";
            base.host = "tracker.yandex.ru";
            this.federation = federation;

            base.OnCompletedRequest += YTracker_OnCompletedRequest;

            InitHeaders();

            engine = new YTrackerWV2Engine(new YTrackerEngineParams()
            {
                credential = credential,
                federation = federation,
                host = host,
                protocol = protocol,
            })
            {
                Visibility = Visibility.Hidden,
                Owner = System.Windows.Application.Current.MainWindow,
            };
        }

        private async void YTracker_OnCompletedRequest(object? sender, HttpResponseMessage e)
        {
            var path = e.RequestMessage?.RequestUri?.AbsoluteUri;
            if ((path.Contains("/showcaptcha")))
            {
                engine.Visibility = Visibility.Visible;
                engine.Show();
                engine.cookies.ForEach(x => cookies.Add(x));

                await engine.IsContinue.Task;

                engine.Visibility = Visibility.Hidden;

                context.ProgressColor = Brushes.Orange;
            }
        }

        public override async Task<bool> AuthAsync()
        {
            context.Progress = 8;

            if (!engine.IsLoaded)
                engine.Show();

            context.Progress = 12;

            await engine.IsContinue.Task;

            context.Progress = 17;

            engine.cookies.ForEach(x => cookies.Add(x));

            context.Progress = 20;

            return true;
        }
        public override async Task<IEnumerable<ReportYT>> GetDataAsync(DateRange dateRange)
        {
            var data = await ExportReport(dateRange);

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

            context.Progress = 100;
            return newData;
        }
        private async Task<IEnumerable<ReportYT>> ExportReport(DateRange dateRange)
        {
            var data = new List<ReportYT>();
            var response = await GetAsync($"{protocol}://{host}/export/pivot?reportType=pivot&from={dateRange.DateFrom:yyyy-MM-dd}&to={dateRange.DateTo:yyyy-MM-dd}&queue=DIT&format=csv&users={credential.UserName}&orgid=null");
            if (response.Content?.Headers?.ContentType?.MediaType == "text/csv")
            {
                var stream = response.Content.ReadAsStream();
                CreateModel(stream, ref data);
            }
            return data;
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
