using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using BidArena.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace BidArena.Infrastructure.Services;

/// <summary>
/// IFileStorage nad Cloudflare R2 (S3 API). Fajlovi se čuvaju kao {folder}/{guid}{ext}, a klijentu se vraća
/// apsolutni javni URL (PublicBaseUrl + "/" + ključ).
/// </summary>
public sealed class R2FileStorage : IFileStorage, IDisposable
{
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
        [".gif"] = "image/gif",
    };

    private readonly AmazonS3Client _client;
    private readonly string _bucket;
    private readonly string _publicBaseUrl;

    public R2FileStorage(IOptions<R2StorageSettings> options)
    {
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.ServiceUrl) || string.IsNullOrWhiteSpace(settings.Bucket)
            || string.IsNullOrWhiteSpace(settings.AccessKeyId) || string.IsNullOrWhiteSpace(settings.SecretAccessKey)
            || string.IsNullOrWhiteSpace(settings.PublicBaseUrl))
        {
            throw new InvalidOperationException(
                "R2 skladište nije podešeno: potrebni su R2:ServiceUrl, Bucket, AccessKeyId, SecretAccessKey i PublicBaseUrl");
        }

        _bucket = settings.Bucket;
        _publicBaseUrl = settings.PublicBaseUrl.TrimEnd('/');

        _client = new AmazonS3Client(
            new BasicAWSCredentials(settings.AccessKeyId, settings.SecretAccessKey),
            new AmazonS3Config
            {
                ServiceURL = settings.ServiceUrl,
                ForcePathStyle = true,
                // R2 ne podržava dodatne checksum-e koje noviji AWS SDK šalje podrazumevano.
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
            });
    }

    public async Task<string> SaveAsync(
        string folder, string extension, Stream content, CancellationToken cancellationToken)
    {
        var key = $"{folder.Trim('/')}/{Guid.NewGuid():N}{extension}";

        // SDK traži stream poznate dužine; slike su male (ograničene validacijom), pa se čitaju u memoriju.
        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = buffer,
            ContentType = ContentTypes.GetValueOrDefault(extension, "application/octet-stream"),
            DisablePayloadSigning = true
        }, cancellationToken);

        return $"{_publicBaseUrl}/{key}";
    }

    public async Task DeleteAsync(string url, CancellationToken cancellationToken)
    {
        var prefix = _publicBaseUrl + "/";
        if (!url.StartsWith(prefix, StringComparison.Ordinal))
            return; // nije naš fajl (npr. stari lokalni URL)

        var key = url[prefix.Length..];

        // S3 DeleteObject ne baca grešku ako objekat ne postoji.
        await _client.DeleteObjectAsync(_bucket, key, cancellationToken);
    }

    public void Dispose() => _client.Dispose();
}
