using BidArena.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BidArena.Infrastructure.Persistence.Configurations;

public class RatingConfiguration : IEntityTypeConfiguration<Rating>
{
    public void Configure(EntityTypeBuilder<Rating> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Comment).HasMaxLength(1000);

        // Jedna ocena po korisniku i aukciji.
        builder.HasIndex(r => new { r.AuctionId, r.RaterId }).IsUnique();
        builder.HasIndex(r => r.RatedUserId);
    }
}
