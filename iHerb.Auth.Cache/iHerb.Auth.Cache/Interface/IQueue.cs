using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace iHerb.Auth.Cache.Interface
{
    public interface IQueue
    {
        Task AddQueueAsync<T>(T data, string key);

        Task BatchAddQueueAsync<T>(IList<T> data, string key);

        Task<T> GetAndRemoveQueueAsync<T>(string key);

        Task<IList<T>> BatchGetAndRemoveQueueAsync<T>(string key, int count);

        Task<int> GetQueueCountAsync(string key);
    }
}
