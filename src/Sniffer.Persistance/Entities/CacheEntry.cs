using System.ComponentModel.DataAnnotations;

namespace Sniffer.Persistance.Entities
{
    public class CacheEntry
    {
        [Key]
        public string Key { get; set; }

        public string Content { get; set; }
    }
}
