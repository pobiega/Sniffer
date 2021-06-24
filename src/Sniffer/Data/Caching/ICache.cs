using System;
using System.Threading.Tasks;

namespace Sniffer.Data.Caching
{
    public interface ICache
    {
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory);
        T GetOrCreate<T>(string key, Func<T> factory);
    }
}
