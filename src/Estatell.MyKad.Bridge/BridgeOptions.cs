namespace Estatell.MyKad.Bridge;

/// <summary>The command-line flags. Unknown flags are ignored.</summary>
internal sealed record BridgeOptions(bool Foreground, bool Verbose, bool Version)
{
    public static BridgeOptions Parse(string[] args) =>
        new(args.Contains("--foreground"), args.Contains("--verbose"), args.Contains("--version"));
}
