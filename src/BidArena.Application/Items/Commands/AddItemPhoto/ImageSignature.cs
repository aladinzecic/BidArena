namespace BidArena.Application.Items.Commands.AddItemPhoto;

// Proverava magic bytes, da se pod "image/png" ne bi uploadovalo nesto drugo.
// Stream mora biti seekable (IFormFile.OpenReadStream jeste); pozicija se vraca na pocetak.
internal static class ImageSignature
{
    public static bool Matches(Stream stream, string contentType)
    {
        if (!stream.CanSeek)
            throw new ArgumentException("Tok podataka slike mora podržavati pretragu (seek)");

        stream.Position = 0;
        var header = new byte[12];
        var read = 0;
        while (read < header.Length)
        {
            var n = stream.Read(header, read, header.Length - read);
            if (n == 0) break;
            read += n;
        }
        stream.Position = 0;

        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/png" => read >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E
                           && header[3] == 0x47 && header[4] == 0x0D && header[5] == 0x0A
                           && header[6] == 0x1A && header[7] == 0x0A,
            "image/webp" => read >= 12 && header[0] == (byte)'R' && header[1] == (byte)'I'
                            && header[2] == (byte)'F' && header[3] == (byte)'F'
                            && header[8] == (byte)'W' && header[9] == (byte)'E'
                            && header[10] == (byte)'B' && header[11] == (byte)'P',
            _ => false
        };
    }
}
