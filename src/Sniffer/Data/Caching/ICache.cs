using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sniffer.Data.Caching
{
    interface ICache
    {
        T GetOrCreate<T>(string key, Func<T> factory);
    }
}
