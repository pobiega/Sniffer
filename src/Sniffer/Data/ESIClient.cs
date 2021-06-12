using Sniffer.Data.ESI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sniffer.Data
{
    public class ESIClient : CacheBase
    {
        private const string ESI_BASE_URL = "https://esi.evetech.net/latest";

        public async Task<SystemData> GetSystemDataAsync(int systemId)
        {
            return await CachedHttpGetAsync<SystemData>($"{ESI_BASE_URL}/universe/systems/{systemId}/?datasource=tranquility&language=en", 180);
        }
    }

    public abstract class CacheBase
    {
        public async Task<T> CachedHttpGetAsync<T>(string url, int cacheTime)
        {
            return await Task.FromResult(default(T));
        }
    }
}
