using BidArena.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BidArena.Infrastructure.Persistence.Configurations;

public class AuctionConfiguration : IEntityTypeConfiguration<Auction>
{
    public void Configure(EntityTypeBuilder<Auction> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.StartingPrice).HasPrecision(18, 2);
        builder.Property(a => a.ReservePrice).HasPrecision(18, 2);
        builder.Property(a => a.BidIncrement).HasPrecision(18, 2);
        builder.Property(a => a.CurrentPrice).HasPrecision(18, 2);

        builder.Property(a => a.Version).IsRowVersion();

        builder.HasIndex(a => a.EndTime);
        builder.HasIndex(a => a.Status);

        builder.HasMany(a => a.Bids)
            .WithOne()
            .HasForeignKey(b => b.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}