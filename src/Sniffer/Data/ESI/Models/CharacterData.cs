using Newtonsoft.Json;

namespace Sniffer.Data.ESI.Models
{
    public class CharacterData
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        public int Id { get; set; }
    }
}
