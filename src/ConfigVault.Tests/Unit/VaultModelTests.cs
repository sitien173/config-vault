using ConfigVault.Core.VaultClient.Models;

namespace ConfigVault.Tests.Unit;

public class VaultModelTests
{
    [Fact]
    public void VaultListData_DefaultsToEmptyCollection()
    {
        var listData = new VaultListData<VaultFolder>();

        Assert.NotNull(listData.Data);
        Assert.Empty(listData.Data);
        Assert.Equal(string.Empty, listData.Object);
    }

    [Fact]
    public void VaultItem_DefaultsSecureNoteTypeToZero()
    {
        var secureNote = new SecureNote();

        Assert.Equal(0, secureNote.Type);
    }
}
