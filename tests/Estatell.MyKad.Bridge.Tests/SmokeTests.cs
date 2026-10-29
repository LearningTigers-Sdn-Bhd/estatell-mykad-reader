using Microsoft.AspNetCore.Mvc.Testing;

namespace Estatell.MyKad.Bridge.Tests;

public class SmokeTests
{
    [Fact]
    public void BridgeStartsInTestHost()
    {
        using var factory = new WebApplicationFactory<Program>();

        using var client = factory.CreateClient();

        Assert.NotNull(client);
    }
}
