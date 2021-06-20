using Npgsql;

namespace Sniffer.Persistance
{
    public static class ConnectionProvider
    {
        public static NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection("Host=localhost; Port=5432; Database=sniffer; Username=postgres; Password=aaaa");
        }
    }
}
