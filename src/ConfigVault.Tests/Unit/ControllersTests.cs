using ConfigVault.Api.Controllers;
using ConfigVault.Api.Models;
using ConfigVault.Core;
using ConfigVault.Core.VaultClient;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ConfigVault.Tests.Unit;

public class ConfigControllerTests
{
    [Fact]
    public async Task Get_ReturnsNotFound_WhenKeyMissing()
    {
        var configServiceMock = new Mock<IConfigurationService>();
        configServiceMock.Setup(x => x.GetAsync("prod/key", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var controller = new ConfigController(configServiceMock.Object, Mock.Of<ILogger<ConfigController>>());

        var result = await controller.Get("prod/key", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Get_ReturnsConfigResponse_WhenKeyExists()
    {
        var configServiceMock = new Mock<IConfigurationService>();
        configServiceMock.Setup(x => x.GetAsync("prod/key", It.IsAny<CancellationToken>()))
            .ReturnsAsync("value");

        var controller = new ConfigController(configServiceMock.Object, Mock.Of<ILogger<ConfigController>>());

        var result = await controller.Get("prod/key", CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<ConfigResponse>().Subject;
        payload.Key.Should().Be("prod/key");
        payload.Value.Should().Be("value");
    }

    [Fact]
    public async Task List_ReturnsBadRequest_WhenPrefixMissing()
    {
        var controller = new ConfigController(Mock.Of<IConfigurationService>(), Mock.Of<ILogger<ConfigController>>());

        var result = await controller.List(string.Empty, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}

public class HealthControllerTests
{
    [Fact]
    public async Task Get_Returns503_WhenVaultIsLocked()
    {
        var vaultClientMock = new Mock<IVaultClient>();
        vaultClientMock.Setup(x => x.IsVaultUnlockedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var controller = new HealthController(vaultClientMock.Object);

        var result = await controller.Get(CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }
}
