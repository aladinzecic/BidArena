using BidArena.Application.Common.Interfaces;
using BidArena.Application.Items.Commands.CreateItem;
using BidArena.Domain.Entities;
using FluentAssertions;
using Moq;

namespace BidArena.Tests.Items;

public class CreateItemCommandHandlerTests
{
    private static readonly Guid Seller = Guid.NewGuid();

    private readonly Mock<IItemRepository> _itemRepo = new();
    private readonly List<Item> _savedItems = new();
    private readonly CreateItemCommandHandler _handler;

    public CreateItemCommandHandlerTests()
    {
        _itemRepo
            .Setup(r => r.AddAsync(It.IsAny<Item>(), It.IsAny<CancellationToken>()))
            .Callback<Item, CancellationToken>((i, _) => _savedItems.Add(i))
            .ReturnsAsync((Item i, CancellationToken _) => i);

        _handler = new CreateItemCommandHandler(_itemRepo.Object);
    }

    private static CreateItemCommand Command(string condition = "Novo") =>
        new(Seller, "Ručni sat", "Opis **sata**", "Satovi", condition, "Novi Pazar");

    [Fact]
    public async Task Predmet_se_pravi_za_prodavca_iz_komande()
    {
        var id = await _handler.Handle(Command(), default);

        var item = _savedItems.Should().ContainSingle().Subject;
        item.Id.Should().Be(id);
        item.SellerId.Should().Be(Seller);
        item.Title.Should().Be("Ručni sat");
        item.DescriptionMarkdown.Should().Be("Opis **sata**");
        item.Category.Should().Be(ItemCategory.Satovi);
        item.Location.Should().Be("Novi Pazar");
        item.Condition.Should().Be(ItemCondition.Novo);
    }

    [Theory]
    [InlineData("KaoNovo", ItemCondition.KaoNovo)]
    [InlineData("kaonovo", ItemCondition.KaoNovo)]
    [InlineData("ZAPOPRAVKU", ItemCondition.ZaPopravku)]
    public async Task Stanje_se_parsira_bez_razlike_u_velicini_slova(string input, ItemCondition expected)
    {
        await _handler.Handle(Command(input), default);

        _savedItems.Single().Condition.Should().Be(expected);
    }

    [Fact]
    public async Task Nepoznato_stanje_baca_ArgumentException_i_ne_snima()
    {
        Func<Task> act = () => _handler.Handle(Command("Polovno"), default);

        await act.Should().ThrowAsync<ArgumentException>();
        _itemRepo.Verify(r => r.AddAsync(It.IsAny<Item>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
