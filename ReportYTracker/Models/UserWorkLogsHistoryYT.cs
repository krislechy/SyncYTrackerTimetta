using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReportYTracker.Models
{
    class UserWorkLogsHistoryYT
    {
        public InvocationInfo invocationinfo { get; set; }
        public Datum[] data { get; set; }
    }
    public class InvocationInfo
    {
        public int perpage { get; set; }
        public string reqid { get; set; }
        public string hostname { get; set; }
        public int execdurationmillis { get; set; }
        public string action { get; set; }
        public string appname { get; set; }
        public string appversion { get; set; }
    }

    public class Datum
    {
        public string self { get; set; }
        public int id { get; set; }
        public int version { get; set; }
        public Issue issue { get; set; }
        public Createdby createdBy { get; set; }
        public Updatedby updatedBy { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
        public DateTime start { get; set; }
        public string duration { get; set; }
        public double durationNormal
        {
            get
            {
                var duration = this.duration?.Replace("PT", "");
                if (duration != null)
                {
                    Double totalHours = 0;
                    foreach (var time in SplitTimeString(duration))
                    {
                        var lastChar = time.LastOrDefault();
                        var str = time.Substring(0, time.Length - 1);
                        var rawHours = Double.Parse(str);
                        switch (lastChar)
                        {
                            case 'S':
                                {
                                    totalHours += (double)rawHours / 3600;
                                    break;
                                }
                            case 'M':
                                {
                                    totalHours += (double)rawHours / 60;
                                    break;
                                }
                            case 'H':
                                {
                                    totalHours += (double)rawHours / 1;
                                    break;
                                }
                            case 'D':
                                {
                                    totalHours += (double)rawHours * 8;
                                    break;
                                }
                            case 'W':
                                {
                                    totalHours += (double)rawHours * 21;
                                    break;
                                }
                        }

                    }
                    return totalHours;
                }
                return 0;
            }
        }
        public bool hasChanges { get; set; }
        public string comment { get; set; }

        private IEnumerable<string> SplitTimeString(string timeString)
        {
            List<string> timeComponents = new List<string>();
            Regex regex = new Regex(@"\d+[A-Z]");
            MatchCollection matches = regex.Matches(timeString);
            foreach (Match match in matches)
            {
                string component = match.Value;
                yield return component;
            }
        }
    }

    public class Issue
    {
        public string self { get; set; }
        public string id { get; set; }
        public string key { get; set; }
        public string display { get; set; }
    }

    public class Createdby
    {
        public string self { get; set; }
        public string id { get; set; }
        public string display { get; set; }
        public string cloudUid { get; set; }
        public long passportUid { get; set; }
    }

    public class Updatedby
    {
        public string self { get; set; }
        public string id { get; set; }
        public string display { get; set; }
        public string cloudUid { get; set; }
        public long passportUid { get; set; }
    }
}
