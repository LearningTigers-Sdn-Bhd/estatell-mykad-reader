using Estatell.MyKad.Reader;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Estatell.MyKad.Bridge.Tests;

/// <summary>The bridge in a test host, with a <see cref="FakeReader"/> in place of PC/SC.</summary>
internal sealed class BridgeFactory(FakeReader reader) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureServices(services => services.AddSingleton<IReader>(reader));
}
