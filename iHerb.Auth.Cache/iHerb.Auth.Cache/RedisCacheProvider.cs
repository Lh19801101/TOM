using iHerb.Auth.Cache.Interface;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.MsgPack;

namespace iHerb.Auth.Cache
{
    public class RedisCacheProvider: IRepository, IAtomicOperations, ILock, IQueue
    {
        #region Constructor
        private static IRedisDatabase _cachedb;
        private const int MaxRecordCount = 10000;
        private RedisCacheProvider()
        {

        }

        private static ISerializer _serializer;
        #endregion

        public static RedisCacheProvider CreateInstance(IConfiguration configuration, ISerializer sz = null)
        {
            if (_cachedb == null)
            {
                _cachedb = RedisConnectManger.Instance(configuration, sz); 
            }
            if (sz == null)
            {
                sz = new MsgPackObjectSerializer();
            }
            _serializer = sz;
            return new RedisCacheProvider();
        }

        #region Hash Repository
        public async Task ClearAsync(string hashkey)
        {
            if (String.IsNullOrWhiteSpace(hashkey) || _cachedb == null) return;
            var keys = _cachedb.HashKeysAsync(hashkey).Result;
            if(keys.Any())
            {
                await _cachedb.HashDeleteAsync(hashkey, keys);
            }
        }

        public async Task DeleteAsync(string hashkey, string key = "default")
        {
            if (String.IsNullOrWhiteSpace(hashkey) || _cachedb == null||string.IsNullOrWhiteSpace(key)) return;
            var isExists = await _cachedb.HashExistsAsync(hashkey, key);
            if (isExists)
            {
                await _cachedb.HashDeleteAsync(hashkey, key);
            }
        }

        public async Task<List<string>> GetAllKeysAsync(string hashkey)
        {
            if (String.IsNullOrWhiteSpace(hashkey) || _cachedb == null) return new List<string>();
            var keys = await _cachedb.HashKeysAsync(hashkey);
            return keys.ToList();
        }

        public async Task<T> GetOrAddAsync<T>(string hashkey, string key = "default", TimeSpan? expiry = null, Func<T> actionFunc = null)
        {
            if (String.IsNullOrWhiteSpace(hashkey) || _cachedb == null) return default(T);
            var isExists = _cachedb.HashExistsAsync(hashkey, key).Result;
            if (isExists)
            {
                T data = await _cachedb.HashGetAsync<T>(hashkey, key);
                return data;
            }
            if (actionFunc != null)
            {
                T data = actionFunc();
                if (data != null)
                {
                     await _cachedb.HashSetAsync<T>(hashkey, key, data);
                     await SetExpireSync(hashkey, expiry);
                 
                }
                return data;
            }
            return default(T);
        }

        public async Task UpdateOrAddAsync<T>(T entity, string hashkey, TimeSpan? expiry = null, string key = "default", Action aftetAction = null, bool isSetExpire = true)
        {
            if (string.IsNullOrWhiteSpace(key) || _cachedb == null || entity == null) return;
            var isExists = await _cachedb.HashExistsAsync(hashkey, key);
            await _cachedb.HashSetAsync(hashkey, key, entity);
            if (isSetExpire || !isExists)
            {
                await SetExpireSync(hashkey, expiry);
            }

            aftetAction?.Invoke();
        }
        #endregion 

        #region Atomic operation
        public async Task<int> GetAccessCountAsync(string hashKey, DateTime? date = null)
        {
            if (String.IsNullOrWhiteSpace(hashKey) || _cachedb == null) return 0;
            var isExists = await _cachedb.HashExistsAsync(hashKey, "default");
            if (isExists)
            {
                return 0;
            }
            var count = await _cachedb.HashGetAsync<int>(hashKey, "default");
            return count;
        }
        public async Task InceremenAsync(string hashKey, int value = 1)
        {
            if (string.IsNullOrEmpty(hashKey) || _cachedb == null) return;

            await _cachedb.HashIncerementByAsync(hashKey, "default", value);
        }
        #endregion

        #region Distributed lock
        public void UnLock(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            RedisValue token = Environment.MachineName;
            _cachedb.Database.LockRelease(key, token);
        }

        public bool TryLock(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            RedisValue token = Environment.MachineName;
            return _cachedb.Database.LockTake(key, token, TimeSpan.FromSeconds(10));
        }

        public bool TryLock(string key, long timeout)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            RedisValue token = Environment.MachineName;
            return _cachedb.Database.LockTake(key, token, TimeSpan.FromSeconds(timeout));
        }
        #endregion

        #region Queue
        public async Task AddQueueAsync<T>(T data, string key)
        {
            if (String.IsNullOrWhiteSpace(key) || _cachedb == null || data == null|| _serializer==null) return;
            if (!IsMaxLength(key))
            {
                IDatabase db = _cachedb.Database;
                RedisValue rValue = _serializer.Serialize(data);
                await db.ListLeftPushAsync(key, rValue);
            }
            else
            {
                throw new ArgumentException($"Exceeded the maximum number of restrictions{MaxRecordCount}");
            }
        }

        public async Task BatchAddQueueAsync<T>(IList<T> data, string key)
        {
            if (String.IsNullOrWhiteSpace(key) || _cachedb == null || data == null || _serializer == null) return;

            if (!IsMaxLength(key, data.Count))
            {
                foreach (var item in data)
                {
                    await AddQueueAsync(item, key);
                }
            }
            else
            {
                throw new ArgumentException($"Exceeded the maximum number of restrictions {MaxRecordCount}");
            }
        }

        public async Task<T> GetAndRemoveQueueAsync<T>(string key)
        {
            if (String.IsNullOrWhiteSpace(key) || _cachedb == null || _serializer == null) return default(T);
            IDatabase db = _cachedb.Database;
            RedisValue rValue = await db.ListRightPopAsync(key);
            if (rValue.IsNullOrEmpty) return default(T);
            T item = _serializer.Deserialize<T>(rValue);
            return item;
        }

        public async Task<IList<T>> BatchGetAndRemoveQueueAsync<T>(string key, int count)
        {
            if (count <= 0 || string.IsNullOrWhiteSpace(key) || _serializer == null || _cachedb == null) return new List<T>();
            IList<T> items = new List<T>();
            for (int i = 0; i < count; i++)
            {
                var item = await GetAndRemoveQueueAsync<T>(key);
                if (item == null) break;
                items.Add(item);
            }
            return items;
        }

        public async Task<int> GetQueueCountAsync(string key)
        {
            if (String.IsNullOrWhiteSpace(key)  || _cachedb == null) return 0;
            IDatabase db = _cachedb.Database;
            var count = await db.ListLengthAsync(key);
            return (int)count;
        }
        #endregion

        #region private function
        private async Task SetExpireSync(string key, TimeSpan? expiry)
        {
            if (string.IsNullOrEmpty(key)|| _cachedb==null) return;
            if (!expiry.HasValue)
            {
                expiry = new TimeSpan(24, 0, 0);
            }
            await _cachedb.UpdateExpiryAsync(key, expiry.Value);
        }

        /// <summary>
        /// 获取条数
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int GetQueueCount(string key)
        {
            if (String.IsNullOrWhiteSpace(key) || _cachedb == null) return 0;
            IDatabase db = _cachedb.Database;
            var count = db.ListLength(key);
            return (int)count;
        }

        /// <summary>
        /// 是否是最大条数
        /// </summary>
        /// <param name="key"></param>
        /// <param name="recordCount"></param>
        /// <returns></returns>
        private bool IsMaxLength(string key, int recordCount = 0)
        {
            if (string.IsNullOrWhiteSpace(key)|| _cachedb == null) return true;
            int count = GetQueueCount(key);
            if (count + recordCount >= MaxRecordCount) return true;
            return false;
        }

        #endregion


    }
}
