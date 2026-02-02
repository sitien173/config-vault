using ConfigVault.Api.Models;

namespace ConfigVault.Tests.Unit;

public class ApiResponseModelsTests
{
    [Fact]
    public void ConfigResponse_DefaultsToEmptyValues()
    {
        var model = new ConfigResponse();

        Assert.Equal(string.Empty, model.Key);
        Assert.Equal(string.Empty, model.Value);
    }

    [Fact]
    public void ConfigListResponse_DefaultsToEmptyValues()
    {
        var model = new ConfigListResponse();

        Assert.Equal(string.Empty, model.Namespace);
        Assert.NotNull(model.Configs);
        Assert.Empty(model.Configs);
    }

    [Fact]
    public void HealthResponse_DefaultsToHealthyAndConnected()
    {
        var model = new HealthResponse();

        Assert.Equal("healthy", model.Status);
        Assert.Equal("connected", model.Vault);
    }
}
