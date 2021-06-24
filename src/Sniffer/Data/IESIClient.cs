using Sniffer.Data.ESI.Models;
using System.Threading.Tasks;

namespace Sniffer.Data
{

    public interface IESIClient
    {
        Task<SystemData> GetSystemDataAsync(int systemId);
    }
}
