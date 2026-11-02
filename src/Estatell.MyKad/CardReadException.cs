namespace Estatell.MyKad;

/// <summary>The card refused a step of the read. The message never holds card data.</summary>
public sealed class CardReadException(string step, byte sw1, byte sw2, string? detail = null)
    : Exception($"{step} failed (SW {sw1:X2} {sw2:X2}){(detail is null ? "" : $": {detail}")}.")
{
    /// <summary>The step that failed, for example "Select JPN" or "JPN1-4 read".</summary>
    public string Step { get; } = step;

    public byte Sw1 { get; } = sw1;

    public byte Sw2 { get; } = sw2;
}
