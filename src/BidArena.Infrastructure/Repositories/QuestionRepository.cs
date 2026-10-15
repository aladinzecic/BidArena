using BidArena.Application.Common.Interfaces;
using BidArena.Application.Questions.Dtos;
using BidArena.Domain.Entities;
using BidArena.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BidArena.Infrastructure.Repositories;

public class QuestionRepository : IQuestionRepository
{
    private readonly ApplicationDbContext _context;

    public QuestionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Question question, CancellationToken cancellationToken)
    {
        _context.Questions.Add(question);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Question?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Questions.FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(Question question, CancellationToken cancellationToken)
    {
        _context.Questions.Update(question);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<QuestionDto>> GetByAuctionAsync(Guid auctionId, CancellationToken cancellationToken)
    {
        var query =
            from q in _context.Questions.AsNoTracking()
            join u in _context.Users.AsNoTracking() on q.AskedByUserId equals u.Id
            where q.AuctionId == auctionId
            orderby q.AskedAt
            select new QuestionDto
            {
                Id = q.Id,
                AuctionId = q.AuctionId,
                AskedByUserId = q.AskedByUserId,
                AskedByName = u.DisplayName,
                QuestionText = q.QuestionText,
                AnswerText = q.AnswerText,
                AskedAt = q.AskedAt,
                AnsweredAt = q.AnsweredAt
            };

        return await query.ToListAsync(cancellationToken);
    }
}
