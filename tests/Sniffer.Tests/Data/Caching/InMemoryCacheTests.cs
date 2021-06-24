using FluentAssertions;
using Sniffer.Data.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Sniffer.Tests.Data.Caching
{
    public class InMemoryCacheTests
    {
        private readonly InMemoryCache _subject;

        public InMemoryCacheTests()
        {
            _subject = new InMemoryCache();
        }

        [Fact]
        public async Task GetOrCreate_ShouldCallFactoryMethod_WhenKeyDoesNotExist()
        {
            //Arrange
            var key = "key_1";
            var factoryWasRun = false;
            var value = new object();

            Task<object> Factory()
            {
                factoryWasRun = true;
                return Task.FromResult(value);
            }

            //Act
            var actual = await _subject.GetOrCreateAsync(key, Factory);

            //Assert
            factoryWasRun.Should().Be(true);
            actual.Should().Be(value);
        }

        [Fact]
        public async Task GetOrCreate_ShouldNotCallFactoryMethod_WhenKeyExists()
        {
            //Arrange
            var key = "key_1";
            var factoryWasRun = false;
            var value = new object();

             _subject.GetOrCreate(key, () => value);

            Task<object> Factory()
            {
                factoryWasRun = true;
                return Task.FromResult((object)"");
            }

            //Act
            var actual = await _subject.GetOrCreateAsync(key, Factory);

            //Assert
            factoryWasRun.Should().Be(false);
            actual.Should().Be(value);
        }
    }
}
