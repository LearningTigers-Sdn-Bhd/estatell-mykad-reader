namespace Estatell.MyKad.Bridge.Tests;

public class OriginPolicyTests
{
    [Theory]
    [InlineData("https://app.estatell.com")]
    [InlineData("https://demo.estatell.com")]
    [InlineData("https://APP.estatell.com")]
    [InlineData("https://app.estatell.com:443")]
    public void Allows(string origin) => Assert.True(new OriginPolicy().IsAllowed(origin));

    [Theory]
    [InlineData("http://localhost")]
    [InlineData("http://localhost:5173")]
    [InlineData("https://localhost:8443")]
    [InlineData("http://127.0.0.1:8000")]
    [InlineData("https://estatell.test")]
    [InlineData("http://hotel.estatell.test:8080")]
    public void AllowsDevelopmentHostsInDebugBuilds(string origin) =>
        Assert.True(new OriginPolicy(allowDevelopmentHosts: true).IsAllowed(origin));

    [Theory]
    [InlineData("http://localhost")]
    [InlineData("http://localhost:5173")]
    [InlineData("https://localhost:8443")]
    [InlineData("http://127.0.0.1:8000")]
    [InlineData("https://estatell.test")]
    [InlineData("http://hotel.estatell.test:8080")]
    public void RefusesDevelopmentHostsInReleaseBuilds(string origin) => Assert.False(new OriginPolicy().IsAllowed(origin));

    [Fact]
    public void ReleaseBuildAllowsDevelopmentHostsListedInTheVariable()
    {
        var policy = new OriginPolicy("http://localhost:5173,https://estatell.test");

        Assert.True(policy.IsAllowed("http://localhost:5173"));
        Assert.True(policy.IsAllowed("https://estatell.test"));
        Assert.False(policy.IsAllowed("http://localhost:3000"));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("")]
    [InlineData("http://evil.com")]
    [InlineData("http://app.estatell.com")]
    [InlineData("https://app.estatell.com.evil.com")]
    [InlineData("https://evilapp.estatell.com")]
    [InlineData("https://foo.test.evil.com")]
    [InlineData("https://localhost.evil.com")]
    [InlineData("https://test")]
    [InlineData("https://app.estatell.com/")]
    [InlineData("https://app.estatell.com/path")]
    [InlineData("https://user@app.estatell.com")]
    [InlineData("ftp://localhost")]
    [InlineData("file://")]
    public void Refuses(string origin)
    {
        Assert.False(new OriginPolicy().IsAllowed(origin));
        Assert.False(new OriginPolicy(allowDevelopmentHosts: true).IsAllowed(origin));
    }

    [Fact]
    public void AddsOriginsFromTheVariable()
    {
        var policy = new OriginPolicy(" https://pms.example.com/ , http://10.0.0.5:8080");

        Assert.True(policy.IsAllowed("https://pms.example.com"));
        Assert.True(policy.IsAllowed("http://10.0.0.5:8080"));
        Assert.False(policy.IsAllowed("http://10.0.0.5"));
        Assert.Empty(policy.Skipped);
    }

    [Fact]
    public void SkipsEntriesThatAreNotOrigins()
    {
        var policy = new OriginPolicy("https://ok.example.com,not a url,https://x.example.com/path,ftp://f.example.com");

        Assert.True(policy.IsAllowed("https://ok.example.com"));
        Assert.Equal(["not a url", "https://x.example.com/path", "ftp://f.example.com"], policy.Skipped);
    }
}
