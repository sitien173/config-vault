using ConfigVault.Core.Exceptions;

namespace ConfigVault.Tests.Unit;

public class VaultExceptionsTests
{
    [Fact]
    public void VaultLockedException_DefaultMessage_IsExpected()
    {
        var ex = new VaultLockedException();

        Assert.Contains("The vault is locked", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void VaultConnectionException_PreservesInnerException()
    {
        var inner = new InvalidOperationException("inner");

        var ex = new VaultConnectionException("connection failed", inner);

        Assert.Equal("connection failed", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }
}
