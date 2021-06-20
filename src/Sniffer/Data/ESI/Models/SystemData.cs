using Newtonsoft.Json;

namespace Sniffer.Data.ESI.Models
{
    public class SystemData
    {
        [JsonProperty("system_id")]
        public int SystemId { get; set; }

        public string Name { get; set; }
    }
}
