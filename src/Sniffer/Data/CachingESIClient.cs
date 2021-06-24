using Serilog;
using Sniffer.Data.Caching;
using Sniffer.Data.ESI.Models;
using System.Net.Http;
using System.Threading.Tasks;

namespace Sniffer.Data
{
    public class CachingESIClient : ESIClient
    {
        private readonly ICache _cache;

        public CachingESIClient(
            ILogger logger,
            IHttpClientFactory httpClientFactory,
            ICache cache
            ) : base(logger, httpClientFactory)
        {
            _cache = cache;
        }

        public override async Task<SystemData> GetSystemDataAsync(int systemId)
        {
            return await _cache.GetOrCreate(
                GetKey(nameof(SystemData), systemId),
                () => base.GetSystemDataAsync(systemId))
                .ConfigureAwait(false);
        }

        private static string GetKey(params object[] parts)
            => string.Join('_', parts);
    }
}
