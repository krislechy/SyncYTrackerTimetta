using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ReportYTracker.Models;

namespace ReportYTracker.Data
{
    public abstract class BaseService<T> : IService<T> where T : class
    {
        protected string host { get; set; }
        protected string protocol { get; set; }

        private readonly HttpClient client;

        protected CookieContainer cookies;
        protected HttpClient proxy { get=>this.client; }
        protected BaseService()
        {
            this.cookies = new CookieContainer();
            this.client = new HttpClient(
                new HttpClientHandler()
                {
                    AllowAutoRedirect = true,
                    UseCookies = true,
                    CookieContainer = cookies,
                }
            );
        }

        protected void InitHeaders()
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/116.0.0.0 Safari/537.36");

            this.client.BaseAddress ??= new Uri($"{protocol}://{host}");
        }

        public abstract Task<T> GetData(DateRange dateRange);
        public abstract Task<bool> Auth(NetworkCredential credential);

        protected virtual FormUrlEncodedContent GetParamsContent(Dictionary<string, string> parameters)
        {
            return new FormUrlEncodedContent(parameters);
        }
    }
}
