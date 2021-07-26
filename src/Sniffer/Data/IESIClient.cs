using Sniffer.Data.ESI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sniffer.Data
{

    public interface IESIClient
    {
        Task<AllianceData> GetAllianceDataAsync(int allianceId);
        Task<CharacterData> GetCharacterDataAsync(int characterId);
        Task<CorporationData> GetCorporationDataAsync(int corpId);
        Task<List<int>> GetRouteDataAsync(int originSystemId, int destinationSystemId);
        Task<SystemData> GetSystemDataAsync(int systemId);
    }
}
