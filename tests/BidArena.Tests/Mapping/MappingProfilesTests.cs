using AutoMapper;
using BidArena.Application.Auctions.Mappings;
using BidArena.Application.Common.Interfaces;
using BidArena.Application.Disputes.Dtos;
using BidArena.Application.Disputes.Queries.GetDisputes;
using BidArena.Application.Disputes.Queries.GetMyDisputes;
using BidArena.Application.Notifications.Queries.GetMyNotifications;
using BidArena.Application.Payments.Queries.GetMyPayments;
using BidArena.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace BidArena.Tests.Mapping;

public class MappingProfilesTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly IMapper _mapper;

    public MappingProfilesTests()
    {
        // Isti način registracije kao u Program.cs: svi profili iz Application assembly-ja.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => { }, typeof(AuctionMappingProfile));
        _mapper = services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    // Payment

    [Fact]
    public void Payment_se_mapira_u_PaymentDto_sa_statusom_kao_tekstom()
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            AuctionId = Guid.NewGuid(),
            WinnerId = UserId,
            Amount = 150m,
            Commission = 7.50m,
            Status = PaymentStatus.Paid,
            DueDate = DateTime.UtcNow.AddDays(2),
            PaidAt = DateTime.UtcNow
        };

        var dto = _mapper.Map<PaymentDto>(payment);

        dto.Id.Should().Be(payment.Id);
        dto.AuctionId.Should().Be(payment.AuctionId);
        dto.Amount.Should().Be(150m);
        dto.Commission.Should().Be(7.50m);
        dto.Status.Should().Be("Paid");
        dto.DueDate.Should().Be(payment.DueDate);
        dto.PaidAt.Should().Be(payment.PaidAt);
    }

    [Fact]
    public async Task GetMyPayments_vraca_DTO_objekte_istim_redosledom()
    {
        var first = new Payment { Id = Guid.NewGuid(), AuctionId = Guid.NewGuid(), WinnerId = UserId, Amount = 10m, Status = PaymentStatus.Pending };
        var second = new Payment { Id = Guid.NewGuid(), AuctionId = Guid.NewGuid(), WinnerId = UserId, Amount = 20m, Status = PaymentStatus.Overdue };

        var repo = new Mock<IPaymentRepository>();
        repo.Setup(r => r.GetByWinnerIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment> { first, second });

        var handler = new GetMyPaymentsQueryHandler(repo.Object, _mapper);

        var result = await handler.Handle(new GetMyPaymentsQuery(UserId), default);

        result.Select(p => p.Id).Should().Equal(first.Id, second.Id);
        result.Select(p => p.Status).Should().Equal("Pending", "Overdue");
        result.Select(p => p.PaidAt).Should().OnlyContain(p => p == null);
    }

    // Dispute

    [Fact]
    public void Dispute_se_mapira_u_DisputeDto_sa_statusom_kao_tekstom()
    {
        var dispute = new Dispute
        {
            Id = Guid.NewGuid(),
            AuctionId = Guid.NewGuid(),
            OpenedByUserId = UserId,
            Reason = "Predmet nije stigao",
            Status = DisputeStatus.Resolved,
            Resolution = "Povraćaj novca",
            ResolvedByAdminId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var dto = _mapper.Map<DisputeDto>(dispute);

        dto.Id.Should().Be(dispute.Id);
        dto.AuctionId.Should().Be(dispute.AuctionId);
        dto.OpenedByUserId.Should().Be(UserId);
        dto.Reason.Should().Be("Predmet nije stigao");
        dto.Status.Should().Be("Resolved");
        dto.Resolution.Should().Be("Povraćaj novca");
        dto.ResolvedByAdminId.Should().Be(dispute.ResolvedByAdminId);
        dto.CreatedAt.Should().Be(dispute.CreatedAt);
    }

    [Fact]
    public void Otvoren_spor_bez_resenja_zadrzava_null_vrednosti()
    {
        var dispute = new Dispute { Id = Guid.NewGuid(), AuctionId = Guid.NewGuid(), OpenedByUserId = UserId, Reason = "Razlog" };

        var dto = _mapper.Map<DisputeDto>(dispute);

        dto.Status.Should().Be("Open");
        dto.Resolution.Should().BeNull();
        dto.ResolvedByAdminId.Should().BeNull();
    }

    [Fact]
    public void Slike_spora_se_mapiraju_u_DTO_poredjane_po_OrderIndex()
    {
        var dispute = new Dispute { Id = Guid.NewGuid(), AuctionId = Guid.NewGuid(), OpenedByUserId = UserId, Reason = "Razlog" };
        dispute.Photos.Add(new DisputePhoto { Id = Guid.NewGuid(), DisputeId = dispute.Id, Url = "/uploads/b.png", OrderIndex = 1 });
        dispute.Photos.Add(new DisputePhoto { Id = Guid.NewGuid(), DisputeId = dispute.Id, Url = "/uploads/a.png", OrderIndex = 0 });

        var dto = _mapper.Map<DisputeDto>(dispute);

        dto.Photos.Select(p => p.Url).Should().Equal("/uploads/a.png", "/uploads/b.png");
    }

    [Fact]
    public void Spor_bez_slika_ima_praznu_listu_slika()
    {
        var dispute = new Dispute { Id = Guid.NewGuid(), AuctionId = Guid.NewGuid(), OpenedByUserId = UserId, Reason = "Razlog" };

        var dto = _mapper.Map<DisputeDto>(dispute);

        dto.Photos.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDisputes_i_GetMyDisputes_vracaju_DTO_objekte()
    {
        var dispute = new Dispute { Id = Guid.NewGuid(), AuctionId = Guid.NewGuid(), OpenedByUserId = UserId, Reason = "Razlog", Status = DisputeStatus.UnderReview };

        var repo = new Mock<IDisputeRepository>();
        repo.Setup(r => r.GetByStatusAsync(DisputeStatus.UnderReview, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dispute> { dispute });
        repo.Setup(r => r.GetForUserAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Dispute> { dispute });

        var all = await new GetDisputesQueryHandler(repo.Object, _mapper)
            .Handle(new GetDisputesQuery(DisputeStatus.UnderReview), default);
        var mine = await new GetMyDisputesQueryHandler(repo.Object, _mapper)
            .Handle(new GetMyDisputesQuery(UserId), default);

        all.Should().ContainSingle().Which.Status.Should().Be("UnderReview");
        mine.Should().ContainSingle().Which.Id.Should().Be(dispute.Id);
    }

    // Notification

    [Fact]
    public void Notification_se_mapira_u_NotificationDto_sa_tipom_kao_tekstom()
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            Type = NotificationType.AuctionWon,
            Message = "Pobedili ste na aukciji",
            IsRead = true,
            CreatedAt = DateTime.UtcNow.AddHours(-3)
        };

        var dto = _mapper.Map<NotificationDto>(notification);

        dto.Id.Should().Be(notification.Id);
        dto.Type.Should().Be("AuctionWon");
        dto.Message.Should().Be("Pobedili ste na aukciji");
        dto.IsRead.Should().BeTrue();
        dto.CreatedAt.Should().Be(notification.CreatedAt);
    }

    [Fact]
    public async Task GetMyNotifications_vraca_DTO_objekte()
    {
        var notification = new Notification { Id = Guid.NewGuid(), UserId = UserId, Type = NotificationType.Outbid, Message = "Nadmašeni ste" };

        var repo = new Mock<INotificationRepository>();
        repo.Setup(r => r.GetByUserAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Notification> { notification });

        var handler = new GetMyNotificationsQueryHandler(repo.Object, _mapper);

        var result = await handler.Handle(new GetMyNotificationsQuery(UserId), default);

        result.Should().ContainSingle().Which.Type.Should().Be("Outbid");
    }
}
