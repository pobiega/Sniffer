using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sniffer.Data.ESI.Models
{
    public class SystemData
    {
        [JsonProperty("system_id")]
        public int SystemId { get; set; }

        public string Name { get; set; }
    }
}
