namespace ConfigVault.Sdk.Exceptions;

public class AuthenticationException : ConfigVaultException
{
    public AuthenticationException(string message = "Invalid or missing API key") : base(message) { }
}