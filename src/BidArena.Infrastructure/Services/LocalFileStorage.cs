using BidArena.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace BidArena.Infrastructure.Services;

public class LocalFileStorage : IFileStorage
{
    private readonly string _root;
    private readonly string _publicPath;

    public LocalFileStorage(IOptions<FileStorageSettings> options)
    {
        var settings = options.Value;
        _root = Path.GetFullPath(settings.RootPath);
        _publicPath = "/" + settings.PublicPath.Trim('/');
    }

    public async Task<string> SaveAsync(
        string folder, string extension, Stream content, CancellationToken cancellationToken)
    {
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var directory = ResolveInsideRoot(folder);
        Directory.CreateDirectory(directory);

        var fullPath = Path.Combine(directory, fileName);
        await using (var target = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await content.CopyToAsync(target, cancellationToken);
        }

        return $"{_publicPath}/{folder}/{fileName}";
    }

    public Task DeleteAsync(string url, CancellationToken cancellationToken)
    {
        var prefix = _publicPath + "/";
        if (!url.StartsWith(prefix, StringComparison.Ordinal))
            return Task.CompletedTask;

        var relative = url[prefix.Length..];
        string fullPath;
        try
        {
            fullPath = ResolveInsideRoot(relative);
        }
        catch (InvalidOperationException)
        {
            return Task.CompletedTask; // nije nas fajl
        }

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    // Zastita od path traversal-a (../): putanja mora ostati unutar root foldera.
    private string ResolveInsideRoot(string relative)
    {
        var full = Path.GetFullPath(Path.Combine(_root, relative));
        var rootWithSeparator = _root.EndsWith(Path.DirectorySeparatorChar)
            ? _root
            : _root + Path.DirectorySeparatorChar;

        if (!full.StartsWith(rootWithSeparator, StringComparison.Ordinal))
            throw new InvalidOperationException("Putanja izlazi iz foldera za otpremanje");

        return full;
    }
}
