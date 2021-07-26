using System.Collections.Generic;

namespace Sniffer.KillBoard
{
    public class MonitorSettings
    {
        private readonly Dictionary<ulong, (int radius, int systemId)> _channels = new();

        // make indexable by ulong (discordChannelId)

        // support filtering and shit.
        public void Add(ulong discordChannelId, int radius, int systemId)
        {
            if (_channels.ContainsKey(discordChannelId))
            {
                _channels.Remove(discordChannelId);
            }

            _channels.Add(discordChannelId, (radius, systemId));
        }
    }
}
