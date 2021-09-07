using Sniffer.Persistance.Model;

namespace Sniffer.KillBoard
{
    public record ChannelRange(ulong ChannelKey, (int radius, int systemId, KillType killType) Value, int Range);
}
