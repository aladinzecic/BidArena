using BidArena.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BidArena.Infrastructure.Persistence.Configurations;

public class DisputeConfiguration : IEntityTypeConfiguration<Dispute>
{
    public void Configure(EntityTypeBuilder<Dispute> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Reason).IsRequired().HasMaxLength(2000);
        builder.Property(d => d.Resolution).HasMaxLength(2000);

        // Slike spora se brisu kaskadno zajedno sa sporom.
        builder.HasMany(d => d.Photos).WithOne().HasForeignKey(p => p.DisputeId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => d.AuctionId);
        builder.HasIndex(d => d.Status);
        builder.HasIndex(d => d.OpenedByUserId);
    }
}
