namespace BidArena.Application.Common.Interfaces;

public interface IFileStorage
{
    // Cuva fajl u zadati "folder" i vraca javni URL (relativan, npr. /uploads/{folder}/{guid}.jpg).
    Task<string> SaveAsync(string folder, string extension, Stream content, CancellationToken cancellationToken);

    // Brise fajl na osnovu URL-a koji je vratio SaveAsync. Ako fajl ne postoji, nista se ne desava.
    Task DeleteAsync(string url, CancellationToken cancellationToken);
}
