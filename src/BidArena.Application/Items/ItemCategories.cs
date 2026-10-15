using BidArena.Domain.Entities;

namespace BidArena.Application.Items;

public static class ItemCategories
{
    public static readonly string[] Names = Enum.GetNames<ItemCategory>();

    public static string AllowedList => string.Join(", ", Names);

    // Poredi se sa nazivima enuma (bez razlike u veličini slova); Enum.TryParse bi prihvatio i brojeve poput "99".
    public static bool TryParse(string? value, out ItemCategory category)
    {
        category = default;
        var name = Names.FirstOrDefault(n => string.Equals(n, value?.Trim(), StringComparison.OrdinalIgnoreCase));
        return name is not null && Enum.TryParse(name, out category);
    }

    public static bool IsKnown(string? value) => TryParse(value, out _);
}
