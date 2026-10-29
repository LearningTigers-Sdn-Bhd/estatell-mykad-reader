using System.Security.Cryptography;

namespace Estatell.MyKad.Bridge;

/// <summary>Whether this computer is connected to a hotel. Public and safe to show in <c>/status</c>.</summary>
internal interface IConnectionState
{
    /// <summary>Random, names the computer, not a secret.</summary>
    string DeviceId { get; }

    bool Paired { get; }

    /// <summary>For display only. Null when not connected.</summary>
    string? PropertyName { get; }
}

/// <summary>
/// Never connected, with a new <see cref="DeviceId"/> on each start.
/// The pairing item replaces it with the shared pairing file.
/// </summary>
internal sealed class MemoryConnectionState : IConnectionState
{
    public string DeviceId { get; } = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));

    public bool Paired => false;

    public string? PropertyName => null;
}
