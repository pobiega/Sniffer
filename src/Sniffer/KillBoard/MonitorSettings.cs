using Sniffer.Persistance.Model;
using System.Collections;
using System.Collections.Generic;

namespace Sniffer.KillBoard
{
    public class MonitorSettings : IEnumerable<KeyValuePair<ulong, (int radius, int systemId, KillType killType)>>
    {
        private readonly Dictionary<ulong, (int radius, int systemId, KillType killType)> _channels = new();

        public void Add(ulong discordChannelId, int radius, int systemId, KillType killType)
        {
            if (_channels.ContainsKey(discordChannelId))
            {
                _channels.Remove(discordChannelId);
            }

            _channels.Add(discordChannelId, (radius, systemId, killType));
        }

        public IEnumerator<KeyValuePair<ulong, (int radius, int systemId, KillType killType)>> GetEnumerator() => _channels.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _channels.GetEnumerator();
    }
}
