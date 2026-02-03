namespace ConfigVault.Sdk.Exceptions;

public class ServiceUnavailableException : ConfigVaultException
{
    public ServiceUnavailableException(string message = "ConfigVault service unavailable") : base(message) { }
}