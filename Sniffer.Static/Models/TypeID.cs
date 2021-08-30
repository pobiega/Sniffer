using System.Collections.Generic;

namespace Sniffer.Static.Models
{
    public class TypeID
    {
        public decimal BasePrice { get; set; }
        public int GroupID { get; set; }
        public Dictionary<string, string> Name { get; set; }
        public string EnglishName => Name?.GetValueOrDefault("en");
    }
}
