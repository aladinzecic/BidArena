using BidArena.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BidArena.Infrastructure.Persistence.Configurations;

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.HasKey(q => q.Id);
        builder.Property(q => q.QuestionText).IsRequired().HasMaxLength(1000);
        builder.Property(q => q.AnswerText).HasMaxLength(2000);

        builder.HasIndex(q => new { q.AuctionId, q.AskedAt });
    }
}
