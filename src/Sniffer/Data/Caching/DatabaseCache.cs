using Newtonsoft.Json;
using Sniffer.Persistance;
using Sniffer.Persistance.Entities;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Sniffer.Data.Caching
{
    public class DatabaseCache : ICache
    {
        private readonly IServiceProvider _serviceProvider;

        public DatabaseCache(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public T GetOrCreate<T>(string key, Func<T> factory) where T : class
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<SnifferDbContext>();

            var result = dbContext.CacheEntries.Find(key);

            if (result != null)
            {
                return JsonConvert.DeserializeObject<T>(result.Content);
            }

            _ = factory ?? throw new ArgumentNullException(nameof(factory));

            var val = factory();

            if (val != default)
            {
                dbContext.Add(new CacheEntry
                {
                    Key = key,
                    Content = JsonConvert.SerializeObject(val)
                });
                dbContext.SaveChanges();
            }

            return val;
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory) where T : class
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<SnifferDbContext>();

            var result = await dbContext.CacheEntries.FindAsync(key);

            if (result != null)
            {
                return JsonConvert.DeserializeObject<T>(result.Content);
            }

            _ = factory ?? throw new ArgumentNullException(nameof(factory));

            var val = await factory();

            if (val != default)
            {
                await dbContext.AddAsync(new CacheEntry
                {
                    Key = key,
                    Content = JsonConvert.SerializeObject(val)
                });

                await dbContext.SaveChangesAsync();
            }

            return val;
        }
    }
}
