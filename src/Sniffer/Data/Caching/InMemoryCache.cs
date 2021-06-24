using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sniffer.Data.Caching
{
    public class InMemoryCache : ICache
    {
        private readonly Dictionary<string, object> _dict = new();

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory)
        {
            if (_dict.TryGetValue(key, out var value))
            {
                return (T)value;
            }

            var val = await factory();

            _dict.Add(key, val);

            return val;
        }

        public T GetOrCreate<T>(string key, Func<T> factory)
        {
            if (_dict.TryGetValue(key, out var value))
            {
                return (T)value;
            }

            var val = factory();

            _dict.Add(key, val);

            return val;
        }
    }
}
