using MediatR;

namespace BidArena.Application.Questions.Commands.AnswerQuestion;

// Odgovoriti može isključivo prodavac aukcije na koju se pitanje odnosi.
public record AnswerQuestionCommand(Guid AuctionId, Guid QuestionId, Guid RequesterId, string Text) : IRequest;
