using Serilog;
using Sniffer.Data.ESI.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Sniffer.Data
{
    public class ESIClient : IESIClient
    {
        public ESIClient(
            ILogger logger,
            IHttpClientFactory httpClientFactory
            )
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        private const string HTTP_CLIENT_NAME = "ESICLIENT";
        private const string ESI_BASE_URL = "https://esi.evetech.net/latest";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;

        private async Task<TResponse> GetAsync<TResponse>(Uri requestUri)
        {
            var client = _httpClientFactory.CreateClient(HTTP_CLIENT_NAME);

            var response = await client.GetAsync(requestUri).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.Error("ESI returned non-success code {statusCode} when requesting resource at {requestUri}", response.StatusCode, requestUri);
                return default;
            }

            return await response.Content.ReadFromJsonAsync<TResponse>().ConfigureAwait(false);
        }

        public virtual Task<SystemData> GetSystemDataAsync(int systemId)
        {
            if (systemId == default)
            {
                return Task.FromResult<SystemData>(null);
            }

            var requestUri = new Uri($"{ESI_BASE_URL}/universe/systems/{systemId}/?datasource=tranquility&language=en");
            return GetAsync<SystemData>(requestUri);
        }

        public virtual async Task<AllianceData> GetAllianceDataAsync(int allianceId)
        {
            if (allianceId == default)
            {
                return null;
            }

            var requestUri = new Uri($"{ESI_BASE_URL}/alliances/{allianceId}/?datasource=tranquility&language=en");

            var response = await GetAsync<AllianceData>(requestUri);
            response.Id = allianceId;

            return response;
        }

        public virtual async Task<CorporationData> GetCorporationDataAsync(int corpId)
        {
            if (corpId == default)
            {
                return null;
            }

            var requestUri = new Uri($"{ESI_BASE_URL}/corporations/{corpId}/?datasource=tranquility&language=en");

            var response = await GetAsync<CorporationData>(requestUri);
            response.Id = corpId;

            return response;
        }

        public virtual async Task<CharacterData> GetCharacterDataAsync(int characterId)
        {
            if (characterId == default)
            {
                return null;
            }

            var requestUri = new Uri($"{ESI_BASE_URL}/characters/{characterId}/?datasource=tranquility&language=en");

            var response = await GetAsync<CharacterData>(requestUri);
            response.Id = characterId;

            return response;
        }

        public virtual Task<List<int>> GetRouteDataAsync(int originSystemId, int destinationSystemId)
        {
            var requestUri = new Uri($"{ESI_BASE_URL}/route/{originSystemId}/{destinationSystemId}/?datasource=tranquility&language=en");
            return GetAsync<List<int>>(requestUri);
        }
    }
}
