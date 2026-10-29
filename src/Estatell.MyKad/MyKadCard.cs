using Estatell.MyKad.Reader;

namespace Estatell.MyKad;

/// <summary>Reads a MyKad through an <see cref="IReader"/>, in the order of the Python bridge.</summary>
public static class MyKadCard
{
    /// <summary>
    /// Selects JPN and reads the identity, the address and the portrait.
    /// Throws <see cref="CardReadException"/> when the card refuses a step, except for the portrait:
    /// a refused portrait gives a null photo, as in the Python bridge.
    /// Reader problems throw <see cref="ReaderException"/>, and a fired token throws <see cref="OperationCanceledException"/>.
    /// </summary>
    public static MyKadData Read(IReader reader, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reader);

        using var session = reader.Connect(cancellationToken);
        Send(session, "Select JPN", Apdu.SelectJpn, Apdu.SelectJpnAccept);

        byte[] identity = [], address = [], portrait = [];
        try
        {
            identity = ReadFile(session, JpnFile.Identity);
            address = ReadFile(session, JpnFile.Address);
            try
            {
                portrait = ReadFile(session, JpnFile.Portrait);
            }
            catch (CardReadException)
            {
                // The portrait is the least needed field. Fill the form without it.
            }

            return Decode.Data(identity, address, portrait);
        }
        finally
        {
            Array.Clear(identity);
            Array.Clear(address);
            Array.Clear(portrait);
        }
    }

    private static byte[] ReadFile(IReaderSession session, JpnFile file)
    {
        Send(session, $"{file.Name} set length", Apdu.SetLength(file.Length), Apdu.SetLengthAccept);
        Send(session, $"{file.Name} select", Apdu.SelectFile(file), Apdu.FileAccept);

        var data = new byte[file.Length];
        try
        {
            var read = 0;
            while (read < data.Length)
            {
                var chunk = (byte)Math.Min(data.Length - read, Apdu.MaxChunk);
                var response = Send(session, $"{file.Name} read", Apdu.ReadChunk(chunk), Apdu.FileAccept);
                try
                {
                    if (response.Data.Length != chunk)
                    {
                        throw new CardReadException($"{file.Name} read", response.Sw1, response.Sw2, $"{response.Data.Length} of {chunk} bytes");
                    }

                    response.Data.CopyTo(data, read);
                    read += chunk;
                }
                finally
                {
                    Array.Clear(response.Data);
                }
            }

            return data;
        }
        catch
        {
            Array.Clear(data);
            throw;
        }
    }

    private static ResponseApdu Send(IReaderSession session, string step, byte[] command, byte[] acceptSw1)
    {
        var response = session.Transmit(command);
        if (!acceptSw1.Contains(response.Sw1))
        {
            Array.Clear(response.Data);
            throw new CardReadException(step, response.Sw1, response.Sw2);
        }

        return response;
    }
}
