using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Sniffer.KillBoard.ZKill
{

    public interface IZKillClient
    {
        Task<ZKillRedisQResponse> GetKillmail();
    }

    public class ZKillClient : IZKillClient
    {
        private const string ClientName = "ZKillboard";
        private const string RequestUri = "https://redisq.zkillboard.com/listen.php";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptions<KillBoardMonitorSettings> _options;
        private readonly ILogger _logger;

        private readonly JsonSerializer _jsonSerializer = new();

        public ZKillClient(ILogger logger, IHttpClientFactory httpClientFactory, IOptions<KillBoardMonitorSettings> options)
        {
            _httpClientFactory = httpClientFactory;
            _options = options;
            _logger = logger;
        }

        public async Task<ZKillRedisQResponse> GetKillmail()
        {
            var client = _httpClientFactory.CreateClient(ClientName);

            var actualRequestUri = $"{RequestUri}?queueID={_options.Value.ZKillBoardCustomId}";

            var httpResponse = await client.GetAsync(RequestUri);

            if (!httpResponse.IsSuccessStatusCode)
            {
                // TODO: handle error. I mean, probably just repeat? Idk
            }

            using var contentStream = await httpResponse.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(contentStream);
            using var jsonReader = new JsonTextReader(reader);

            try
            {
                return _jsonSerializer.Deserialize<ZKillRedisQResponse>(jsonReader);
            }
            catch(Exception ex)
            {
                _logger.Error(ex, "Exception when deserializing response object from ZKill RedisQ.");
                return null;
            }
        }
    }
}
