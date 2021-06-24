using Serilog;
using Sniffer.Data.Caching;
using Sniffer.Data.ESI.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Sniffer.Data
{

    public interface IESIClient
    {
        Task<SystemData> GetSystemDataAsync(int systemId);
    }
}
