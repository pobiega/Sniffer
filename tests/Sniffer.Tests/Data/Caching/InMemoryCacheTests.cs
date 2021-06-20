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
        public void GetOrCreate_ShouldCallFactoryMethod_WhenKeyDoesNotExist()
        {
            //Arrange
            var key = "key_1";
            var factoryWasRun = false;
            var value = new object();

            object Factory()
            {
                factoryWasRun = true;
                return value;
            }

            //Act
            var actual = _subject.GetOrCreate(key, Factory);

            //Assert
            factoryWasRun.Should().Be(true);
            actual.Should().Be(value);
        }

        [Fact]
        public void GetOrCreate_ShouldNotCallFactoryMethod_WhenKeyExists()
        {
            //Arrange
            var key = "key_1";
            var factoryWasRun = false;
            var value = new object();

            _subject.GetOrCreate(key, () => value);

            object Factory()
            {
                factoryWasRun = true;
                return "";
            }

            //Act
            var actual = _subject.GetOrCreate(key, Factory);

            //Assert
            factoryWasRun.Should().Be(false);
            actual.Should().Be(value);
        }
    }
}
