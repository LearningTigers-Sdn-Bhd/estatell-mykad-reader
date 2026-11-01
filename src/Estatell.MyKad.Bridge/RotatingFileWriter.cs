using System.Text;

namespace Estatell.MyKad.Bridge;

/// <summary>
/// Appends lines to a file. Before a line would push the file past <paramref name="maxBytes"/>, the file becomes
/// <c>.1</c>, <c>.1</c> becomes <c>.2</c>, and so on, and the oldest of <paramref name="oldFiles"/> is deleted.
/// A write that fails is dropped: the log must never stop the bridge.
/// </summary>
internal sealed class RotatingFileWriter(string path, long maxBytes = 1024 * 1024, int oldFiles = 2)
{
    private readonly Lock _lock = new();

    public void WriteLine(string line)
    {
        var text = line + Environment.NewLine;
        lock (_lock)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                Rotate(Encoding.UTF8.GetByteCount(text));
                File.AppendAllText(path, text);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    private void Rotate(int incoming)
    {
        var file = new FileInfo(path);
        if (!file.Exists || file.Length == 0 || file.Length + incoming <= maxBytes)
        {
            return;
        }

        File.Delete($"{path}.{oldFiles}");
        for (var i = oldFiles - 1; i >= 1; i--)
        {
            if (File.Exists($"{path}.{i}"))
            {
                File.Move($"{path}.{i}", $"{path}.{i + 1}");
            }
        }

        File.Move(path, $"{path}.1");
    }
}
