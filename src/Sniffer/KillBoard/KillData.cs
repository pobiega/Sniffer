using Sniffer.Data.ESI.Models;
using Sniffer.Static.Models;

namespace Sniffer.KillBoard
{
    public record KillData(
        CharacterData Victim,
        CorporationData VictimCorp,
        AllianceData VictimAlliance,
        TypeID VicimShip,
        int AttackerCount,
        CharacterData Killer,
        CorporationData KillerCorp,
        AllianceData KillerAlliance,
        TypeID KillerShip,
        TypeID MostExpensiveAttackerShip,
        string KillLocation,
        bool NPCOnlyKill = false)
    {
        public KillData(CharacterData victim,
                        CorporationData victimCorp,
                        AllianceData victimAlliance,
                        TypeID vicimShip,
                        string killLocation)
            : this(victim,
                   victimCorp,
                   victimAlliance,
                   vicimShip,
                   0,
                   null,
                   null,
                   null,
                   null,
                   null,
                   killLocation,
                   true)
        {
        }
    }
}
