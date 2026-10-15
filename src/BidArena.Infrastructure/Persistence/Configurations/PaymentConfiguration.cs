using BidArena.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BidArena.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Commission).HasPrecision(18, 2);

        builder.HasIndex(p => p.AuctionId).IsUnique();
        builder.HasIndex(p => p.WinnerId);
        builder.HasIndex(p => new { p.Status, p.DueDate });
    }
}