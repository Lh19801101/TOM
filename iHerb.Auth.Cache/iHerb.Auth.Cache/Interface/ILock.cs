using System;
using System.Collections.Generic;
using System.Text;

namespace iHerb.Auth.Cache.Interface
{
    public interface ILock
    {
        /// <summary>
        /// 释放锁
        /// </summary>
        /// <param name="key"></param>
        void UnLock(String key);

        /// <summary>
        /// 获取锁
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool TryLock(String key);

        /// <summary>
        /// 释放锁
        /// </summary>
        /// <param name="key"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        bool TryLock(String key, long timeout);
    }
}
