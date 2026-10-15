using BidArena.Application.Questions.Commands.AnswerQuestion;
using BidArena.Application.Questions.Commands.AskQuestion;
using FluentAssertions;

namespace BidArena.Tests.Validators;

public class QuestionValidatorsTests
{
    private readonly AskQuestionCommandValidator _ask = new();
    private readonly AnswerQuestionCommandValidator _answer = new();

    [Fact]
    public void Ask_Valid_Passes() =>
        _ask.Validate(new AskQuestionCommand(Guid.NewGuid(), Guid.NewGuid(), "Da li radi?")).IsValid.Should().BeTrue();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    public void Ask_TooShortOrEmpty_Fails(string text) =>
        _ask.Validate(new AskQuestionCommand(Guid.NewGuid(), Guid.NewGuid(), text)).IsValid.Should().BeFalse();

    [Fact]
    public void Ask_TooLong_Fails() =>
        _ask.Validate(new AskQuestionCommand(Guid.NewGuid(), Guid.NewGuid(), new string('a', 1001))).IsValid.Should().BeFalse();

    [Fact]
    public void Answer_Valid_Passes() =>
        _answer.Validate(new AnswerQuestionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Da.")).IsValid.Should().BeTrue();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Answer_Empty_Fails(string text) =>
        _answer.Validate(new AnswerQuestionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), text)).IsValid.Should().BeFalse();

    [Fact]
    public void Answer_TooLong_Fails() =>
        _answer.Validate(new AnswerQuestionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new string('a', 2001))).IsValid.Should().BeFalse();
}
