using FluentAssertions;

namespace ConfigVault.Tests.Unit;

public class SseClientTests
{
    private const string SseClientTypeName = "ConfigVault.Api.Services.SseClient, ConfigVault.Api";

    [Fact]
    public void MatchesKey_WithNoFilter_ReturnsTrue()
    {
        var client = CreateClient(null);

        MatchesKey(client, "any/key/here").Should().BeTrue();
        MatchesKey(client, "production/database/host").Should().BeTrue();
    }

    [Fact]
    public void MatchesKey_WithExactMatch_ReturnsCorrectly()
    {
        var client = CreateClient("production/database/host");

        MatchesKey(client, "production/database/host").Should().BeTrue();
        MatchesKey(client, "production/database/port").Should().BeFalse();
    }

    [Fact]
    public void MatchesKey_WithSingleWildcard_MatchesSingleSegment()
    {
        var client = CreateClient("production/*/host");

        MatchesKey(client, "production/database/host").Should().BeTrue();
        MatchesKey(client, "production/cache/host").Should().BeTrue();
        MatchesKey(client, "production/database/nested/host").Should().BeFalse();
        MatchesKey(client, "staging/database/host").Should().BeFalse();
    }

    [Fact]
    public void MatchesKey_WithDoubleWildcard_MatchesMultipleSegments()
    {
        var client = CreateClient("production/**");

        MatchesKey(client, "production/database/host").Should().BeTrue();
        MatchesKey(client, "production/cache/redis/url").Should().BeTrue();
        MatchesKey(client, "staging/database/host").Should().BeFalse();
    }

    [Fact]
    public void MatchesKey_WithPrefixWildcard_MatchesAnyNamespace()
    {
        var client = CreateClient("*/database/host");

        MatchesKey(client, "production/database/host").Should().BeTrue();
        MatchesKey(client, "staging/database/host").Should().BeTrue();
        MatchesKey(client, "production/cache/host").Should().BeFalse();
    }

    private static object CreateClient(string? filterPattern)
    {
        var type = Type.GetType(SseClientTypeName);
        type.Should().NotBeNull($"SseClient type '{SseClientTypeName}' should exist.");

        var instance = Activator.CreateInstance(type!, filterPattern);
        instance.Should().NotBeNull("SseClient should be constructible.");

        return instance!;
    }

    private static bool MatchesKey(object client, string key)
    {
        var method = client.GetType().GetMethod("MatchesKey");
        method.Should().NotBeNull("SseClient should expose MatchesKey method.");

        var result = method!.Invoke(client, new object[] { key });
        result.Should().BeOfType<bool>();

        return (bool)result!;
    }
}
