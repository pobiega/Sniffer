using Newtonsoft.Json;

namespace Sniffer.Data.ESI.Models
{
    public class CorporationData
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("ticker")]
        public string Ticker { get; set; }
        public int Id { get; set; }
    }
}
