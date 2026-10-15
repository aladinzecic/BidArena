namespace BidArena.Domain.Entities;

public enum ItemCondition
{
    Novo,
    KaoNovo,
    Koriscen,
    ZaPopravku
}

// Dozvoljene kategorije predmeta (korisnik bira iz liste, slobodan unos nije moguć).
public enum ItemCategory
{
    Satovi,
    Nakit,
    Elektronika,
    Racunari,
    Odeca,
    Obuca,
    Namestaj,
    DomIBasta,
    Umetnost,
    Kolekcionarstvo,
    Knjige,
    Muzika,
    Vozila,
    Sport,
    Igracke,
    Ostalo
}

public class Item
{
    public Guid Id { get; set; }
    public Guid SellerId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string DescriptionMarkdown { get; set; } = string.Empty;
    public ItemCategory Category { get; set; } = ItemCategory.Ostalo;
    public ItemCondition Condition { get; set; }
    public string Location { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ItemPhoto> Photos { get; set; } = new();
}

public class ItemPhoto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public string Url { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
}