using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using MediatR;

namespace BidArena.Application.Disputes.Commands.TakeDispute;

public class TakeDisputeCommandHandler : IRequestHandler<TakeDisputeCommand>
{
    private readonly IDisputeRepository _disputeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TakeDisputeCommandHandler(IDisputeRepository disputeRepository, IUnitOfWork unitOfWork)
    {
        _disputeRepository = disputeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(TakeDisputeCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var dispute = await _disputeRepository.GetByIdForUpdateAsync(request.DisputeId, cancellationToken)
                ?? throw new KeyNotFoundException($"Spor {request.DisputeId} ne postoji");

            if (dispute.Status != DisputeStatus.Open)
                throw new InvalidOperationException(
                    $"Spor se može preuzeti samo iz statusa Open, trenutni status je {dispute.Status}");

            dispute.Status = DisputeStatus.UnderReview;
            await _disputeRepository.UpdateAsync(dispute, cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
