using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace iHerb.Auth.Cache.Interface
{
   public interface  IAtomicOperations
    {

        Task InceremenAsync(string hashKey, int value = 1);

        Task<int> GetAccessCountAsync(string hashKey, DateTime? date = null);
    }
}
