using System;
using System.Collections.Generic;
using ConfigVault.Sdk.Exceptions;
using ConfigVault.Sdk.Models;
using FluentAssertions;
using Xunit;

namespace ConfigVault.Sdk.Tests;

public class ModelsAndExceptionsTests
{
    [Fact]
    public void ConfigResponse_StoresKeyAndValue()
    {
        var response = new ConfigResponse("prod/db/host", "localhost");

        response.Key.Should().Be("prod/db/host");
        response.Value.Should().Be("localhost");
    }

    [Fact]
    public void ConfigListResponse_StoresNamespaceAndConfigs()
    {
        IReadOnlyDictionary<string, string> configs = new Dictionary<string, string>
        {
            { "db/host", "localhost" },
            { "db/port", "5432" }
        };

        var response = new ConfigListResponse("production", configs);

        response.Namespace.Should().Be("production");
        response.Configs.Should().ContainKey("db/host").WhoseValue.Should().Be("localhost");
        response.Configs.Should().HaveCount(2);
    }

    [Fact]
    public void HealthResponse_StoresStatusVaultAndTimestamp()
    {
        var timestamp = DateTimeOffset.Parse("2026-02-02T12:00:00Z");

        var response = new HealthResponse("healthy", "unlocked", timestamp);

        response.Status.Should().Be("healthy");
        response.Vault.Should().Be("unlocked");
        response.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public void ConfigNotFoundException_IncludesKeyInMessage()
    {
        var exception = new ConfigNotFoundException("prod/missing");

        exception.Message.Should().Be("Configuration key 'prod/missing' not found");
        exception.Key.Should().Be("prod/missing");
    }

    [Fact]
    public void AuthenticationException_HasDefaultMessage()
    {
        var exception = new AuthenticationException();

        exception.Message.Should().Be("Invalid or missing API key");
    }

    [Fact]
    public void ServiceUnavailableException_HasDefaultMessage()
    {
        var exception = new ServiceUnavailableException();

        exception.Message.Should().Be("ConfigVault service unavailable");
    }
}