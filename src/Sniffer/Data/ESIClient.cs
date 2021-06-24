﻿using Serilog;
using Sniffer.Data.Caching;
using Sniffer.Data.ESI.Models;
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

        private async Task<TResponse> GetAsync<TResponse>(string requestUri)
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
            var requestUri = $"{ESI_BASE_URL}/universe/systems/{systemId}/?datasource=tranquility&language=en";
            return GetAsync<SystemData>(requestUri);
        }
    }
}
