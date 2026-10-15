using MediatR;

namespace BidArena.Application.Questions.Commands.AskQuestion;

public record AskQuestionCommand(Guid AuctionId, Guid UserId, string Text) : IRequest<Guid>;
