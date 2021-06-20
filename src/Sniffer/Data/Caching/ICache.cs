using System;
using System.Threading.Tasks;

namespace Sniffer.Data.Caching
{
    public interface ICache
    {
        Task<T> GetOrCreate<T>(string key, Func<Task<T>> factory);
    }
}
