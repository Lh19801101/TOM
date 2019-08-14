using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Core.Implementations;
using StackExchange.Redis.Extensions.MsgPack;

namespace iHerb.Auth.Cache
{
    internal class RedisConnectManger
    {
        #region Fields and constructors
        private static readonly ConcurrentDictionary<App, IRedisCacheClient> CacheClientArry = new ConcurrentDictionary<App, IRedisCacheClient>();
        private RedisConnectManger()
        {

        }
        #endregion

        #region Public function
        public static IRedisDatabase Instance(IConfiguration config, ISerializer sz = null)
        {
           
            if (config == null)
            {
                throw new ArgumentException("Redis：Configuration is not Exists");
            }
            var app = config.GetSection("App").Get<string>();
            if(string.IsNullOrEmpty(app))
            {
                throw new ArgumentException("Redis：APP is not Exists");
            }
            var apptype = (App)Enum.Parse(typeof(App), app);
            var key = String.Format("Reis{0}", app);
            var redisConfig = config.GetSection(key).Get<RedisConfiguration>();
            if (redisConfig == null)
            {
                throw new ArgumentException("Redis：redisConfig is not Exists");
            }
            if (!CacheClientArry.ContainsKey(apptype))
            {
                var cacheClient = GetCacheClient(redisConfig, sz);
                if (cacheClient != null)
                {
                    CacheClientArry.TryAdd(apptype, cacheClient);
                }

            }
            IRedisCacheClient client = CacheClientArry[apptype];
            var db = client.GetDb((int)apptype);
            return db;
        }

     
        #endregion

        #region Private function
        private static IRedisCacheClient GetCacheClient(RedisConfiguration redisConfig, ISerializer sz = null)
        {
            var poolManager = new RedisCacheConnectionPoolManager(redisConfig);
            if (sz == null)
            {
                sz = new MsgPackObjectSerializer();
            }
            var cacheClient = new RedisCacheClient(poolManager, sz, redisConfig);
            return cacheClient;
        }
        #endregion
    }

    public enum App
    {
        Oauth
    }
}
