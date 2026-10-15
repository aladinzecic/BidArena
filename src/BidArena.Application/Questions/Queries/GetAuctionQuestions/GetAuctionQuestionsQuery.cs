using BidArena.Application.Questions.Dtos;
using MediatR;

namespace BidArena.Application.Questions.Queries.GetAuctionQuestions;

public record GetAuctionQuestionsQuery(Guid AuctionId) : IRequest<List<QuestionDto>>;
