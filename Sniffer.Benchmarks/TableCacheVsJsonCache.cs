using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Npgsql;
using Sniffer.Persistance;
using System.Threading.Tasks;

namespace Sniffer.Benchmarks
{
    [BenchmarkCategory("EF")]
    public class TableCacheVsJsonCacheEF
    {
        private readonly SnifferDbContext _context;

        public TableCacheVsJsonCacheEF()
        {
            _context = new SnifferDbContext();
        }

        [Benchmark]
        public async Task<SystemData> TableCacheEF()
        {
            return await _context.SystemData
                .AsNoTracking()
                .FirstAsync(s => s.SystemId == 2);
        }

        [Benchmark]
        public async Task<SystemData> JsonCacheEF()
        {
            var thing = await _context.CacheEntries
                .AsNoTracking()
                .FirstAsync(c => c.Key.Equals("systemdata_2"));

            return JsonConvert.DeserializeObject<SystemData>(thing.Content);
        }
    }

    [BenchmarkCategory("Dapper")]
    public class TableCacheVsJsonCacheDapper
    {
        private NpgsqlConnection _connection;

        public TableCacheVsJsonCacheDapper()
        {
            _connection = ConnectionProvider.GetConnection();
            _connection.Open();
        }

        [Benchmark]
        public async Task<SystemData> TableCacheDapper()
        {
            return await _connection.QueryFirstAsync<SystemData>("SELECT id systemId, name FROM system_data fetch first 1 row only;");
        }

        [Benchmark]
        public async Task<SystemData> JsonCacheDapper()
        {
            var entry = await _connection.QueryFirstAsync<CacheEntry>("SELECT key, content FROM cache_entries  fetch first 1 row only;");
            return JsonConvert.DeserializeObject<SystemData>(entry.Content);
        }
    }
}
