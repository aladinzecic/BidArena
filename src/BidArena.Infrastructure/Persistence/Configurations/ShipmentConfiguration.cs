using BidArena.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BidArena.Infrastructure.Persistence.Configurations;

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.TrackingNumber).HasMaxLength(100);

        // Jedna aukcija ima najviše jednu pošiljku.
        builder.HasIndex(s => s.AuctionId).IsUnique();
        builder.HasIndex(s => s.DueDate);
    }
}
