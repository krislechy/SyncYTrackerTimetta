using ReportYTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ReportYTracker.Data
{
    public interface IService<T> where T : class
    {
        public abstract Task<T> GetData(DateRange dateRange);
        public Task<bool> Auth(NetworkCredential credential);
    }
}
