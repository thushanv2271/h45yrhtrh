using System.Net;
using System.Net.Http.Json;
using Domain.EfaConfigs;
using FluentAssertions;
using IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IntegrationTests.EfaConfigs;

public class EfaConfigurationIntegrationTests 
{

    [Fact]
    public void TestRunner_ShouldWork()
    {
        // Arrange
        int a = 2;
        int b = 3;

        // Act
        int sum = a + b;

        // Assert
        sum.Should().Be(5);
    }
}
