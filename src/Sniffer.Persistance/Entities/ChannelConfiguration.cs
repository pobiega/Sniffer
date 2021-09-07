using Sniffer.Persistance.Model;
using System.ComponentModel.DataAnnotations;

namespace Sniffer.Persistance.Entities
{
    public class ChannelConfiguration
    {
        [Key]
        public ulong DiscordChannelId { get; set; }

        public int Radius { get; set; }

        public int SystemId { get; set; }

        public KillType KillType { get; set; }
    }
}
