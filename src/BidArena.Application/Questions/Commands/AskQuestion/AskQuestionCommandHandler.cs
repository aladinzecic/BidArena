using BidArena.Application.Common.Exceptions;
using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using MediatR;

namespace BidArena.Application.Questions.Commands.AskQuestion;

public class AskQuestionCommandHandler : IRequestHandler<AskQuestionCommand, Guid>
{
    private readonly IAuctionRepository _auctionRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AskQuestionCommandHandler(
        IAuctionRepository auctionRepository,
        IQuestionRepository questionRepository,
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork)
    {
        _auctionRepository = auctionRepository;
        _questionRepository = questionRepository;
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(AskQuestionCommand request, CancellationToken cancellationToken)
    {
        var data = await _auctionRepository.GetWithItemAsync(request.AuctionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Aukcija {request.AuctionId} ne postoji");

        if (data.Item.SellerId == request.UserId)
            throw new ForbiddenAccessException("Ne možete postavljati pitanja na sopstvenu aukciju");

        if (data.Auction.Status is not (AuctionStatus.Scheduled or AuctionStatus.Active))
            throw new InvalidOperationException(
                $"Pitanja se mogu postavljati samo dok je aukcija zakazana ili aktivna, trenutni status je {data.Auction.Status}");

        var question = new Question
        {
            Id = Guid.NewGuid(),
            AuctionId = request.AuctionId,
            AskedByUserId = request.UserId,
            QuestionText = request.Text.Trim(),
            AskedAt = DateTime.UtcNow
        };

        var message = $"Novo pitanje za aukciju „{data.Item.Title}“. Odgovorite na stranici aukcije.";

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _questionRepository.AddAsync(question, cancellationToken);
            await _notificationRepository.AddAsync(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = data.Item.SellerId,
                AuctionId = request.AuctionId,
                Type = NotificationType.QuestionAsked,
                Message = message
            }, cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }

        return question.Id;
    }
}
