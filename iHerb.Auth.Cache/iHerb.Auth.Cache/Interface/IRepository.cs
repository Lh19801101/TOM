using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace iHerb.Auth.Cache.Interface
{
  public interface IRepository
    {
        /// <summary>
        /// 获取或添加实例;
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="hashkey"></param>
        /// <param name="actionFunc"></param>
        /// <param name="expiry"></param>
        Task<T> GetOrAddAsync<T>(string hashkey, string key = "default", TimeSpan? expiry = null, Func<T> actionFunc = null);

        /// <summary>
        /// 更新或添加实例;
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="key"></param>
        /// <param name="hashkey"></param>
        /// <param name="expiry"></param>
        Task UpdateOrAddAsync<T>(T entity, string hashkey, TimeSpan? expiry = null, string key = "default", Action aftetAction = null, bool isSetExpire = true);

        /// <summary>
        ///     删除实例
        /// </summary>
        /// <param name="key"></param>
        /// <param name="hashkey"></param>
        Task DeleteAsync(string hashkey, string key = "default");

        /// <summary>
        /// 清理区域中的所有key
        /// </summary>
        /// <param name="hashkey"></param>
        Task ClearAsync(string hashkey);

        /// <summary>
        /// 读取缓存中的所有的key值
        /// </summary>
        /// <param name="hashkey"></param>
        /// <returns></returns>
        Task<List<string>> GetAllKeysAsync(string hashkey);
    }
}
