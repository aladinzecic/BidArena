using BidArena.Application.Common.Exceptions;
using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using MediatR;

namespace BidArena.Application.Questions.Commands.AnswerQuestion;

public class AnswerQuestionCommandHandler : IRequestHandler<AnswerQuestionCommand>
{
    private readonly IAuctionRepository _auctionRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AnswerQuestionCommandHandler(
        IAuctionRepository auctionRepository,
        IItemRepository itemRepository,
        IQuestionRepository questionRepository,
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork)
    {
        _auctionRepository = auctionRepository;
        _itemRepository = itemRepository;
        _questionRepository = questionRepository;
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AnswerQuestionCommand request, CancellationToken cancellationToken)
    {
        var question = await _questionRepository.GetByIdAsync(request.QuestionId, cancellationToken);

        // Pitanje mora pripadati aukciji iz rute (inače 404).
        if (question is null || question.AuctionId != request.AuctionId)
            throw new KeyNotFoundException($"Pitanje {request.QuestionId} ne postoji");

        var auction = await _auctionRepository.GetByIdAsync(question.AuctionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Aukcija {question.AuctionId} ne postoji");

        var item = await _itemRepository.GetByIdAsync(auction.ItemId, cancellationToken)
            ?? throw new KeyNotFoundException($"Predmet {auction.ItemId} ne postoji");

        // Samo onaj ko je postavio aukciju sme da odgovara.
        if (item.SellerId != request.RequesterId)
            throw new ForbiddenAccessException("Na pitanja može odgovarati samo prodavac aukcije");

        if (question.AnswerText is not null)
            throw new InvalidOperationException("Na ovo pitanje je već odgovoreno");

        question.AnswerText = request.Text.Trim();
        question.AnsweredAt = DateTime.UtcNow;

        var message = $"Prodavac je odgovorio na vaše pitanje o aukciji „{item.Title}“.";

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _questionRepository.UpdateAsync(question, cancellationToken);
            await _notificationRepository.AddAsync(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = question.AskedByUserId,
                AuctionId = auction.Id,
                Type = NotificationType.QuestionAnswered,
                Message = message
            }, cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
