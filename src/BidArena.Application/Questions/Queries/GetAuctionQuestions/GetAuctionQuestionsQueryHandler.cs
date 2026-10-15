using BidArena.Application.Common.Interfaces;
using BidArena.Application.Questions.Dtos;
using MediatR;

namespace BidArena.Application.Questions.Queries.GetAuctionQuestions;

public class GetAuctionQuestionsQueryHandler : IRequestHandler<GetAuctionQuestionsQuery, List<QuestionDto>>
{
    private readonly IAuctionRepository _auctionRepository;
    private readonly IQuestionRepository _questionRepository;

    public GetAuctionQuestionsQueryHandler(
        IAuctionRepository auctionRepository,
        IQuestionRepository questionRepository)
    {
        _auctionRepository = auctionRepository;
        _questionRepository = questionRepository;
    }

    public async Task<List<QuestionDto>> Handle(GetAuctionQuestionsQuery request, CancellationToken cancellationToken)
    {
        if (!await _auctionRepository.ExistsAsync(request.AuctionId, cancellationToken))
            throw new KeyNotFoundException($"Aukcija {request.AuctionId} ne postoji");

        return await _questionRepository.GetByAuctionAsync(request.AuctionId, cancellationToken);
    }
}
