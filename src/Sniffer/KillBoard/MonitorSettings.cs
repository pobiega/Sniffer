using System.Collections;
using System.Collections.Generic;

namespace Sniffer.KillBoard
{
    public class MonitorSettings : IEnumerable<KeyValuePair<ulong, (int radius, int systemId)>>
    {
        private readonly Dictionary<ulong, (int radius, int systemId)> _channels = new();

        public void Add(ulong discordChannelId, int radius, int systemId)
        {
            if (_channels.ContainsKey(discordChannelId))
            {
                _channels.Remove(discordChannelId);
            }

            _channels.Add(discordChannelId, (radius, systemId));
        }

        public IEnumerator<KeyValuePair<ulong, (int radius, int systemId)>> GetEnumerator() => _channels.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _channels.GetEnumerator();
    }
}
