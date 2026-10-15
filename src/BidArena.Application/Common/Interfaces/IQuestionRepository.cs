using BidArena.Application.Questions.Dtos;
using BidArena.Domain.Entities;

namespace BidArena.Application.Common.Interfaces;

public interface IQuestionRepository
{
    Task AddAsync(Question question, CancellationToken cancellationToken);
    Task<Question?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateAsync(Question question, CancellationToken cancellationToken);
    // Pitanja aukcije (najstarija prvo) zajedno sa imenom korisnika koji je pitao.
    Task<List<QuestionDto>> GetByAuctionAsync(Guid auctionId, CancellationToken cancellationToken);
}
