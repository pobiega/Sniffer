using Sniffer.Data.ESI.Models;
using Sniffer.Static.Models;

namespace Sniffer.KillBoard
{
    public record KillData(
        CharacterData Victim,
        CorporationData VictimCorp,
        AllianceData VictimAlliance,
        TypeID VicimShip,
        CharacterData Killer,
        CorporationData KillerCorp,
        AllianceData KillerAlliance,
        TypeID KillerShip,
        string KillLocation);
}
