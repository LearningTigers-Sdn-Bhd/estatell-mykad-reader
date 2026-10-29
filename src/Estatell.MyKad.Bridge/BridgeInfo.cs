using System.Reflection;

namespace Estatell.MyKad.Bridge;

internal static class BridgeInfo
{
    public const int Port = 9292;

    /// <summary>The product version, such as 2.0.0, without the commit the SDK appends after a plus.</summary>
    public static string Version { get; } =
        typeof(BridgeInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? "0.0.0";
}
