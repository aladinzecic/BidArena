using BidArena.Application.Items.Dtos;
using MediatR;

namespace BidArena.Application.Items.Commands.AddItemPhoto;

public record AddItemPhotoCommand(
    Guid ItemId,
    Guid SellerId,
    Stream Content,
    string ContentType,
    long Length
) : IRequest<ItemPhotoDto>
{
    public const long MaxSizeBytes = 5 * 1024 * 1024;

    // Ekstenzija se odredjuje iz (validiranog) content type-a, nikad iz imena fajla koje salje klijent.
    public static readonly IReadOnlyDictionary<string, string> AllowedContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp"
        };
}
