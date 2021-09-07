using Serilog;
using Sniffer.Data.Caching;
using Sniffer.Data.ESI.Models;
using System.Collections.Generic;
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
            return await _cache.GetOrCreateAsync(
                GetKey(nameof(SystemData), systemId),
                () => base.GetSystemDataAsync(systemId));
        }

        public override async Task<AllianceData> GetAllianceDataAsync(int allianceId)
        {
            return await _cache.GetOrCreateAsync(
                GetKey(nameof(AllianceData), allianceId),
                () => base.GetAllianceDataAsync(allianceId));
        }

        public override async Task<CorporationData> GetCorporationDataAsync(int corpId)
        {
            return await _cache.GetOrCreateAsync(
                GetKey(nameof(CorporationData), corpId),
                () => base.GetCorporationDataAsync(corpId));
        }

        public override async Task<CharacterData> GetCharacterDataAsync(int characterId)
        {
            return await _cache.GetOrCreateAsync(
                GetKey(nameof(CharacterData), characterId),
                () => base.GetCharacterDataAsync(characterId));
        }

        public override async Task<List<int>> GetRouteDataAsync(int originSystemId, int destinationSystemId)
        {
            if (destinationSystemId < originSystemId)
            {
                (destinationSystemId, originSystemId) = (originSystemId, destinationSystemId);
            }

            return await _cache.GetOrCreateAsync(
                GetKey("Route", originSystemId, destinationSystemId),
                () => base.GetRouteDataAsync(originSystemId, destinationSystemId));
        }

        private static string GetKey(params object[] parts)
            => string.Join('_', parts);
    }
}
