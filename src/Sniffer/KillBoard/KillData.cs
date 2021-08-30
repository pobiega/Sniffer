using Sniffer.Data.ESI.Models;

namespace Sniffer.KillBoard
{
    public record KillData(CharacterData Victim, CorporationData VictimCorp, AllianceData VictimAlliance, CharacterData Killer, CorporationData KillerCorp, AllianceData KillerAlliance, string KillLocation);
}
