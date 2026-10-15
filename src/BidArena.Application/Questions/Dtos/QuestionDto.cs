namespace BidArena.Application.Questions.Dtos;

public class QuestionDto
{
    public Guid Id { get; set; }
    public Guid AuctionId { get; set; }
    public Guid AskedByUserId { get; set; }
    public string AskedByName { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public string? AnswerText { get; set; }
    public DateTime AskedAt { get; set; }
    public DateTime? AnsweredAt { get; set; }
}
