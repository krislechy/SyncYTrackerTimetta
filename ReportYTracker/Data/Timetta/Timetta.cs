using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReportYTracker.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ReportYTracker.Data.Timetta
{
    public class Timetta : BaseService<Timesheet?>, IService<Timesheet?>
    {
        private class TokenInfo
        {
            private int? expiresToken { get; set; }
            private DateTime? startTimeToken { get; set; }
            public bool isExpiredToken
            {
                get
                {
                    if (expiresToken.HasValue && startTimeToken.HasValue)
                    {
                        var diff = DateTime.Now - startTimeToken;
                        return diff.Value.TotalHours >= 0.9;
                    }
                    return true;
                }
            }
            public TokenInfo(int expiresToken)
            {
                this.expiresToken = expiresToken;
                this.startTimeToken = DateTime.Now;
            }
        }
        private const string authLink = "https://auth.timetta.com/connect/token";
        private int rowVersion = 0;
        private TokenInfo tokenInfo { get; set; }
        public Timetta(NetworkCredential credential) : base(credential)
        {
            base.protocol = "https";
            base.host = "api.timetta.com";
        }
        public override async Task<Timesheet?> GetDataAsync(DateRange dateRange)
        {
            var odataRequest = $"$select=id&$filter=(timeSheet/dateFrom le {dateRange.DateFrom:yyyy-MM-dd}) and (timeSheet/dateTo ge {dateRange.DateTo:yyyy-MM-dd})&$top=50&$expand=timeSheet($select=id,dateFrom,dateTo;$expand=state($select=id,code))&$orderby=timeSheet/name";
            var result = await proxy.GetAsync($"{protocol}://{host}/odata/TimeSheetTotals?{odataRequest}");
            if (result.IsSuccessStatusCode)
            {
                var json = await result.Content.ReadAsStringAsync();
                if (json != null)
                {
                    var content = JsonConvert.DeserializeObject<TimeSheetTotal>(json);
                    if (content != null)
                    {
                        var timeSheet = content.value.Select(x => x.timeSheet).FirstOrDefault(x => x.dateFrom == dateRange.DateFrom.Date.ToString("yyyy-MM-dd") && x.dateTo == dateRange.DateTo.Date.ToString("yyyy-MM-dd") && x.state.code == "Draft");
                        if (timeSheet != null)
                        {

                            var _timeSheet = await proxy.GetAsync($"{protocol}://{host}/odata/TimeSheets({timeSheet.id})");
                            var timeSheetContent = await _timeSheet.Content.ReadAsStringAsync();
                            var _rowVersion = JToken.Parse(timeSheetContent)["rowVersion"]?.Value<int>();
                            this.rowVersion = _rowVersion.HasValue ? _rowVersion.Value : 0;
                            return timeSheet;
                        }
                    }
                }
            }
            else
            {
                throw new Exception($"Таймета: вернулся код {result.StatusCode}, прервано");
            }
            return null;
        }
        public override async Task<bool> AuthAsync()
        {
            if (proxy.DefaultRequestHeaders.Authorization != null && tokenInfo != null && !tokenInfo.isExpiredToken)
            {
                return true;
            }

            var response = await proxy.PostAsync(authLink, GetParamsContent(new Dictionary<string, string>()
            {
                { "client_id" ,"external"},
                { "scope" ,"all offline_access"},
                { "grant_type" ,"password"},
                { "username" ,credential.UserName},
                { "password" ,credential.Password},
            }));
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var token = JToken.Parse(content)["access_token"]?.Value<string>();
                var expiresToken = JToken.Parse(content)["expires_in"]?.Value<int>();

                if (token != null && expiresToken != null)
                {
                    tokenInfo = new TokenInfo(expiresToken.Value);
                    proxy.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }
                else
                {
                    throw new Exception("Неверный ответ от Timetta при аутентификации");
                }
                return true;
            }
            return false;
        }

        public async void PutTimeSheet(Timesheet ts, UpdateTimeSheet updateTimeSheet)
        {
            updateTimeSheet.id = ts.id;
            updateTimeSheet.rowVersion = this.rowVersion;

            var result = await proxy.PutAsync($"{protocol}://{host}/odata/TimeSheets({ts.id})", new StringContent(JsonConvert.SerializeObject(updateTimeSheet), Encoding.UTF8, "application/json"));
            if (result.StatusCode != HttpStatusCode.NoContent)
                throw new Exception("Timetta: что то пошло не так, не удалось обновить");
        }

        public async Task<UpdateTimeSheet> ConvertFromReportYT(IEnumerable<ReportYT> reports)
        {
            var ProjectId = ConfigurationManager.AppSettings["ProjectId"];
            if (ProjectId == null)
                throw new ArgumentNullException(nameof(ProjectId), nameof(ProjectId) + " is null");

            var ProjectTaskId = ConfigurationManager.AppSettings["ProjectTaskId"];
            if (ProjectTaskId == null)
                throw new ArgumentNullException(nameof(ProjectTaskId), nameof(ProjectTaskId) + " is null");

            return await Task.Factory.StartNew(() =>
            {
                var ts = new UpdateTimeSheet();
                ts.timeSheetLines = new List<Timesheetline>();

                var group = reports.GroupBy(x => x.TaskId);

                foreach (var d in group)
                {
                    var tsl = new Timesheetline()
                    {
                        orderNumber = 0,
                        projectId = ProjectId,
                        projectTaskId = ProjectTaskId,
                        timeAllocations = new List<Timeallocation>(),
                    };
                    foreach (var d2 in d)
                    {
                        tsl.timeAllocations.Add(new Timeallocation()
                        {
                            date = d2.Date.ToString("yyyy-MM-dd"),
                            comments = $"[{d2.TaskId}] {d2.Name}",
                            duration = d2.Hours,
                        });
                    }
                    ts.timeSheetLines.Add(tsl);
                }
                return ts;
            });
        }
    }
}
