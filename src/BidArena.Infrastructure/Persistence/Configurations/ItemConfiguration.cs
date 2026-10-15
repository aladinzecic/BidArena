using BidArena.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BidArena.Infrastructure.Persistence.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        // Kategorija se u bazi čuva kao tekst (naziv enuma), pa postojeća kolona ostaje nepromenjena.
        builder.Property(i => i.Category).HasConversion<string>();
    }
}
