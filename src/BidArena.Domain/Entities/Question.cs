namespace BidArena.Domain.Entities;

public class Question
{
    public Guid Id { get; set; }
    public Guid AuctionId { get; set; }
    public Guid AskedByUserId { get; set; }

    public string QuestionText { get; set; } = string.Empty;
    public string? AnswerText { get; set; }
    public DateTime AskedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AnsweredAt { get; set; }
}