using IISApp.Services;
using Xunit;

namespace IISApp.Tests;

public class PermissionServiceTests
{
    [Fact]
    public void ReadOnly_DisablesWrites() => Assert.False(new PermissionService(FrontendAccessMode.ReadOnly).CanWrite);

    [Fact]
    public void FullAccess_EnablesWrites() => Assert.True(new PermissionService(FrontendAccessMode.FullAccess).CanWrite);
}
