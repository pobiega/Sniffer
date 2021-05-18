using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Sniffer.Tests
{
    public class DependencyInjectionTest
    {
        [Fact]
        public void DependencyInjection_ShouldUseAValidConfiguration()
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().Build();

            var startup = new Startup(configuration);
            startup.ConfigureServices(services);

            var provider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
        }
    }
}
