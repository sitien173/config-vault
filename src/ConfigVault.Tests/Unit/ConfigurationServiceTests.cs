using ConfigVault.Core;
using ConfigVault.Core.VaultClient;
using ConfigVault.Core.VaultClient.Models;
using FluentAssertions;
using Moq;

namespace ConfigVault.Tests.Unit;

public class ConfigurationServiceTests
{
    private readonly Mock<IVaultClient> _vaultClientMock;
    private readonly ConfigurationService _sut;

    public ConfigurationServiceTests()
    {
        _vaultClientMock = new Mock<IVaultClient>();
        _sut = new ConfigurationService(_vaultClientMock.Object);
    }

    [Fact]
    public async Task GetAsync_ReturnsValue_WhenKeyExists()
    {
        var folder = new VaultFolder { Id = "folder-1", Name = "production" };
        var item = new VaultItem
        {
            Id = "item-1",
            Name = "database/connection",
            Notes = "Server=localhost",
            Type = 2
        };

        _vaultClientMock.Setup(x => x.GetFolderByNameAsync("production", It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);
        _vaultClientMock.Setup(x => x.GetItemsByFolderIdAsync("folder-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VaultItem> { item });

        var result = await _sut.GetAsync("production/database/connection");

        result.Should().Be("Server=localhost");
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenFolderNotFound()
    {
        _vaultClientMock.Setup(x => x.GetFolderByNameAsync("unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((VaultFolder?)null);

        var result = await _sut.GetAsync("unknown/some/key");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenItemNotFound()
    {
        var folder = new VaultFolder { Id = "folder-1", Name = "production" };

        _vaultClientMock.Setup(x => x.GetFolderByNameAsync("production", It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);
        _vaultClientMock.Setup(x => x.GetItemsByFolderIdAsync("folder-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VaultItem>());

        var result = await _sut.GetAsync("production/nonexistent");

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("nodelimiter")]
    public async Task GetAsync_ThrowsArgumentException_ForInvalidKey(string invalidKey)
    {
        var act = () => _sut.GetAsync(invalidKey);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ListAsync_ReturnsDictionary_WhenFolderExists()
    {
        var folder = new VaultFolder { Id = "folder-1", Name = "production" };
        var items = new List<VaultItem>
        {
            new() { Name = "db/host", Notes = "localhost", Type = 2 },
            new() { Name = "db/port", Notes = "5432", Type = 2 }
        };

        _vaultClientMock.Setup(x => x.GetFolderByNameAsync("production", It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);
        _vaultClientMock.Setup(x => x.GetItemsByFolderIdAsync("folder-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        var result = await _sut.ListAsync("production");

        result.Should().HaveCount(2);
        result["db/host"].Should().Be("localhost");
        result["db/port"].Should().Be("5432");
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_WhenKeyExists()
    {
        var folder = new VaultFolder { Id = "folder-1", Name = "production" };
        var item = new VaultItem { Name = "key", Notes = "value", Type = 2 };

        _vaultClientMock.Setup(x => x.GetFolderByNameAsync("production", It.IsAny<CancellationToken>()))
            .ReturnsAsync(folder);
        _vaultClientMock.Setup(x => x.GetItemsByFolderIdAsync("folder-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VaultItem> { item });

        var result = await _sut.ExistsAsync("production/key");

        result.Should().BeTrue();
    }
}
