namespace ConfigVault.Core.Exceptions;

public class VaultLockedException : Exception
{
    public VaultLockedException()
        : base("The vault is locked. Please unlock it using 'bw unlock' before using this service.")
    {
    }

    public VaultLockedException(string message) : base(message)
    {
    }
}
