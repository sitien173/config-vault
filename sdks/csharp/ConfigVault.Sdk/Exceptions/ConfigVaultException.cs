using System;

namespace ConfigVault.Sdk.Exceptions;

public class ConfigVaultException : Exception
{
    public ConfigVaultException(string message) : base(message) { }
    public ConfigVaultException(string message, Exception innerException) : base(message, innerException) { }
}