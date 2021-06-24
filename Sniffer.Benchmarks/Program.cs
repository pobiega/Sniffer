using BenchmarkDotNet.Running;
using System.Reflection;

namespace Sniffer.Benchmarks
{
    public static class Program
    {
        static void Main(string[] args)
        {
            new BenchmarkSwitcher(Assembly.GetExecutingAssembly()).Run(args);
        }
    }
}
