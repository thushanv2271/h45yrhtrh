using Domain.EfaConfigs;
using IntegrationTests.Common;
using IntegrationTests.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using FluentAssertions;

namespace IntegrationTests.EfaConfigurations;
public class EfaConfigurationEndpointsTests : BaseIntegrationTest
{
    private const string BaseUrl = "efa-configurations";

    public EfaConfigurationEndpointsTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
    }

    #region Create Tests

    [Fact]
    public async Task CreateEfaConfiguration_ShouldReturnOk_WhenSingleItemIsValid()
    {
        // Arrange
        object[] request = new[]
        {
            new { Year = 2025, EfaRate = 45.75m }
        };

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<EfaConfigResponse>? result = await response.Content.ReadFromJsonAsync<List<EfaConfigResponse>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result![0].Id.Should().NotBeEmpty();
        result[0].Year.Should().Be(2025);
        result[0].EfaRate.Should().Be(45.75m);
        result[0].UpdatedBy.Should().NotBeEmpty();
        result[0].UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // Verify in database
        EfaConfiguration? efaConfig = await DbContext.EfaConfigurations
            .FirstOrDefaultAsync(e => e.Id == result[0].Id);
        efaConfig.Should().NotBeNull();
        efaConfig!.Year.Should().Be(2025);
        efaConfig.EfaRate.Should().Be(45.75m);
    }

    [Fact]
    public async Task CreateEfaConfiguration_ShouldReturnOk_WhenMultipleItemsAreValid()
    {
        // Arrange
        object[] request = new[]
        {
            new { Year = 2025, EfaRate = 45.75m },
            new { Year = 2026, EfaRate = 50.20m },
            new { Year = 2027, EfaRate = 55.00m }
        };

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<EfaConfigResponse>? result = await response.Content.ReadFromJsonAsync<List<EfaConfigResponse>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        result![0].Year.Should().Be(2025);
        result[0].EfaRate.Should().Be(45.75m);
        result[1].Year.Should().Be(2026);
        result[1].EfaRate.Should().Be(50.20m);
        result[2].Year.Should().Be(2027);
        result[2].EfaRate.Should().Be(55.00m);

        // Verify all are in database
        List<EfaConfiguration> dbConfigs = await DbContext.EfaConfigurations.ToListAsync();
        dbConfigs.Should().HaveCount(3);
    }

    [Fact]
    public async Task CreateEfaConfiguration_ShouldReturnBadRequest_WhenYearIsInvalid()
    {
        // Arrange
        object[] request = new[]
        {
            new { Year = 1800, EfaRate = 10m }
        };

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateEfaConfiguration_ShouldReturnBadRequest_WhenEfaRateIsNegative()
    {
        // Arrange
        object[] request = new[]
        {
            new { Year = 2025, EfaRate = -5m }
        };

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateEfaConfiguration_ShouldReturnBadRequest_WhenDuplicateYearsInRequest()
    {
        // Arrange
        object[] request = new[]
        {
            new { Year = 2025, EfaRate = 10m },
            new { Year = 2025, EfaRate = 15m }
        };

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateEfaConfiguration_ShouldReturnBadRequest_WhenEmptyArray()
    {
        // Arrange
        object[] request = Array.Empty<object>();

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Get All Tests

    [Fact]
    public async Task GetAllEfaConfigurations_ShouldReturnOk_WithEmptyList_WhenNoConfigurationsExist()
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync(BaseUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<EfaConfigResponse>? result = await response.Content.ReadFromJsonAsync<List<EfaConfigResponse>>();
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllEfaConfigurations_ShouldReturnOk_WithAllConfigurations()
    {
        // Arrange
        EfaConfiguration[] configs = new[]
        {
            new EfaConfiguration
            {
                Id = Guid.NewGuid(),
                Year = 2023,
                EfaRate = 8.5m,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = Guid.NewGuid()
            },
            new EfaConfiguration
            {
                Id = Guid.NewGuid(),
                Year = 2024,
                EfaRate = 9.25m,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = Guid.NewGuid()
            },
            new EfaConfiguration
            {
                Id = Guid.NewGuid(),
                Year = 2025,
                EfaRate = 10.75m,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = Guid.NewGuid()
            }
        };
        DbContext.EfaConfigurations.AddRange(configs);
        await DbContext.SaveChangesAsync();

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync(BaseUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<EfaConfigResponse>? result = await response.Content.ReadFromJsonAsync<List<EfaConfigResponse>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        // Verify ordered by year descending
        result![0].Year.Should().Be(2025);
        result[1].Year.Should().Be(2024);
        result[2].Year.Should().Be(2023);

        result.Should().Contain(c => c.Year == 2023 && c.EfaRate == 8.5m);
        result.Should().Contain(c => c.Year == 2024 && c.EfaRate == 9.25m);
        result.Should().Contain(c => c.Year == 2025 && c.EfaRate == 10.75m);
    }

    #endregion

    #region Edit Tests

    [Fact]
    public async Task EditEfaConfiguration_ShouldReturnOk_WhenOnlyEfaRateIsChanged()
    {
        // Arrange
        var existingConfig = new EfaConfiguration
        {
            Id = Guid.NewGuid(),
            Year = 2025,
            EfaRate = 45.75m,
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedBy = Guid.NewGuid()
        };
        DbContext.EfaConfigurations.Add(existingConfig);
        await DbContext.SaveChangesAsync();

        object request = new
        {
            Year = 2025,
            EfaRate = 50.00m
        };

        // Act
        HttpResponseMessage response = await HttpClient.PutAsJsonAsync(
            $"{BaseUrl}/{existingConfig.Id}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        EditResponse? result = await response.Content.ReadFromJsonAsync<EditResponse>();
        result.Should().NotBeNull();
        result!.EfaRate.Should().Be(50.00m);
        result.Year.Should().Be(2025);
    }

    [Fact]
    public async Task EditEfaConfiguration_ShouldReturnNotFound_WhenIdDoesNotExist()
    {
        // Arrange
        object request = new
        {
            Year = 2025,
            EfaRate = 45.75m
        };

        // Act
        HttpResponseMessage response = await HttpClient.PutAsJsonAsync(
            $"{BaseUrl}/{Guid.NewGuid()}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EditEfaConfiguration_ShouldReturnConflict_WhenYearAlreadyExistsForDifferentRecord()
    {
        // Arrange
        var  existingConfig1 = new EfaConfiguration
        {
            Id = Guid.NewGuid(),
            Year = 2024,
            EfaRate = 40m,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = Guid.NewGuid()
        };
        var existingConfig2 = new EfaConfiguration
        {
            Id = Guid.NewGuid(),
            Year = 2025,
            EfaRate = 45m,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = Guid.NewGuid()
        };
        DbContext.EfaConfigurations.AddRange(existingConfig1, existingConfig2);
        await DbContext.SaveChangesAsync();

        object request = new
        {
            Year = 2024, // Trying to change to year that exists in config1
            EfaRate = 50m
        };

        // Act
        HttpResponseMessage response = await HttpClient.PutAsJsonAsync(
            $"{BaseUrl}/{existingConfig2.Id}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task EditEfaConfiguration_ShouldReturnBadRequest_WhenYearIsInvalid()
    {
        // Arrange
        var existingConfig = new EfaConfiguration
        {
            Id = Guid.NewGuid(),
            Year = 2025,
            EfaRate = 45.75m,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = Guid.NewGuid()
        };
        DbContext.EfaConfigurations.Add(existingConfig);
        await DbContext.SaveChangesAsync();

        object request = new
        {
            Year = 1800,
            EfaRate = 45.75m
        };

        // Act
        HttpResponseMessage response = await HttpClient.PutAsJsonAsync(
            $"{BaseUrl}/{existingConfig.Id}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EditEfaConfiguration_ShouldReturnBadRequest_WhenEfaRateIsNegative()
    {
        // Arrange
        var existingConfig = new EfaConfiguration
        {
            Id = Guid.NewGuid(),
            Year = 2025,
            EfaRate = 45.75m,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = Guid.NewGuid()
        };
        DbContext.EfaConfigurations.Add(existingConfig);
        await DbContext.SaveChangesAsync();

        object request = new
        {
            Year = 2025,
            EfaRate = -10m
        };

        // Act
        HttpResponseMessage response = await HttpClient.PutAsJsonAsync(
            $"{BaseUrl}/{existingConfig.Id}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task DeleteEfaConfiguration_ShouldReturnOk_WhenIdExists()
    {
        // Arrange
        var existingConfig = new EfaConfiguration
        {
            Id = Guid.NewGuid(),
            Year = 2026,
            EfaRate = 50.20m,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = Guid.NewGuid()
        };
        DbContext.EfaConfigurations.Add(existingConfig);
        await DbContext.SaveChangesAsync();

        // Act
        HttpResponseMessage response = await HttpClient.DeleteAsync($"{BaseUrl}/{existingConfig.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        DeleteResponse? result = await response.Content.ReadFromJsonAsync<DeleteResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(existingConfig.Id);
        result.Year.Should().Be(2026);
        result.EfaRate.Should().Be(50.20m);
        result.DeletedBy.Should().NotBeEmpty();
        result.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // Verify deleted from database
        EfaConfiguration? deletedConfig = await DbContext.EfaConfigurations
            .FirstOrDefaultAsync(e => e.Id == existingConfig.Id);
        deletedConfig.Should().BeNull();
    }

    [Fact]
    public async Task DeleteEfaConfiguration_ShouldReturnNotFound_WhenIdDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        HttpResponseMessage response = await HttpClient.DeleteAsync($"{BaseUrl}/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteEfaConfiguration_ShouldNotAffectOtherRecords()
    {
        // Arrange
        var config1 = new EfaConfiguration
        {
            Id = Guid.NewGuid(),
            Year = 2024,
            EfaRate = 40m,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = Guid.NewGuid()
        };
        var config2 = new EfaConfiguration
        {
            Id = Guid.NewGuid(),
            Year = 2025,
            EfaRate = 45m,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = Guid.NewGuid()
        };
        var config3 = new EfaConfiguration
        {
            Id = Guid.NewGuid(),
            Year = 2026,
            EfaRate = 50m,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = Guid.NewGuid()
        };
        DbContext.EfaConfigurations.AddRange(config1, config2, config3);
        await DbContext.SaveChangesAsync();

        // Act
        HttpResponseMessage response = await HttpClient.DeleteAsync($"{BaseUrl}/{config2.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        List<EfaConfiguration> remainingConfigs = await DbContext.EfaConfigurations.ToListAsync();
        remainingConfigs.Should().HaveCount(2);
        remainingConfigs.Should().Contain(c => c.Id == config1.Id);
        remainingConfigs.Should().Contain(c => c.Id == config3.Id);
        remainingConfigs.Should().NotContain(c => c.Id == config2.Id);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task EfaConfiguration_FullCRUD_Workflow()
    {
        // Create
        object[] createRequest = new[]
        {
            new { Year = 2025, EfaRate = 45.75m },
            new { Year = 2026, EfaRate = 50.20m }
        };

        HttpResponseMessage createResponse = await HttpClient.PostAsJsonAsync(BaseUrl, createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        List<EfaConfigResponse>? createdConfigs = await createResponse.Content.ReadFromJsonAsync<List<EfaConfigResponse>>();
        createdConfigs.Should().HaveCount(2);

        // Get All
        HttpResponseMessage getAllResponse = await HttpClient.GetAsync(BaseUrl);
        getAllResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        List<EfaConfigResponse>? allConfigs = await getAllResponse.Content.ReadFromJsonAsync<List<EfaConfigResponse>>();
        allConfigs.Should().HaveCount(2);

        // Edit
        object editRequest = new
        {
            Year = 2025,
            EfaRate = 48.00m
        };
        HttpResponseMessage editResponse = await HttpClient.PutAsJsonAsync(
            $"{BaseUrl}/{createdConfigs![0].Id}",
            editRequest);
        editResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify Edit
        HttpResponseMessage getAfterEdit = await HttpClient.GetAsync(BaseUrl);
        List<EfaConfigResponse>? configsAfterEdit = await getAfterEdit.Content.ReadFromJsonAsync<List<EfaConfigResponse>>();
        configsAfterEdit.Should().Contain(c => c.Id == createdConfigs[0].Id && c.EfaRate == 48.00m);

        // Delete
        HttpResponseMessage deleteResponse = await HttpClient.DeleteAsync($"{BaseUrl}/{createdConfigs[1].Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify Delete
        HttpResponseMessage getAfterDelete = await HttpClient.GetAsync(BaseUrl);
        List<EfaConfigResponse>? configsAfterDelete = await getAfterDelete.Content.ReadFromJsonAsync<List<EfaConfigResponse>>();
        configsAfterDelete.Should().HaveCount(1);
        configsAfterDelete.Should().NotContain(c => c.Id == createdConfigs[1].Id);
    }

    #endregion

    #region Response DTOs

    private sealed record EfaConfigResponse(
        Guid Id,
        int Year,
        decimal EfaRate,
        DateTime UpdatedAt,
        Guid UpdatedBy);

    private sealed record EditResponse(
        Guid Id,
        int Year,
        decimal EfaRate,
        DateTime UpdatedAt,
        Guid UpdatedBy);

    private sealed record DeleteResponse(
        Guid Id,
        int Year,
        decimal EfaRate,
        DateTime DeletedAt,
        Guid DeletedBy);

    #endregion
}
