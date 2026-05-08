// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Extensions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AdrPlus.Tests.Extensions
{
    /// <summary>
    /// Tests for ServiceCollectionExtensions.
    /// These tests verify that the extension method properly registers services
    /// without requiring a full application Host setup.
    /// </summary>
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddAdrPlusServices_ReturnsServiceCollection_ForMethodChaining()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddAdrPlusServices();

            // Assert
            result.Should().BeSameAs(services);
        }

        [Fact]
        public void AddAdrPlusServices_WithEmptyServiceCollection_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = () => services.AddAdrPlusServices();

            // Assert
            result.Should().NotThrow();
        }

        [Fact]
        public void AddAdrPlusServices_CalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = () =>
            {
                services.AddAdrPlusServices();
                services.AddAdrPlusServices();
            };

            // Assert
            result.Should().NotThrow();
        }

        [Fact]
        public void AddAdrPlusServices_PreservesExistingServices_InCollection()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<string>("test-value");

            // Act
            services.AddAdrPlusServices();

            // Assert
            services.Should().Contain(sd => sd.ServiceType == typeof(string));
        }

        [Fact]
        public void AddAdrPlusServices_RegistersServices_AsDescribed()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddAdrPlusServices();

            // Assert - verify that services are registered (without trying to resolve them, which requires ILogger, IConfiguration, etc.)
            services.Count.Should().BeGreaterThan(0);
            // Verify common interface registrations are present
            services.Should().Contain(sd => sd.ServiceType.Name.Contains("FileSystem"));
            services.Should().Contain(sd => sd.ServiceType.Name.Contains("ProcessService"));
            services.Should().Contain(sd => sd.ServiceType.Name.Contains("ConsoleWriter"));
        }
    }
}
