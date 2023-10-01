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
        public Task<T> GetDataAsync(DateRange dateRange);
        public Task<bool> AuthAsync();
    }
}
