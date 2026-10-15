using BidArena.Application.Items.Commands.CreateItem;
using BidArena.Domain.Entities;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BidArena.Tests.Validators;

public class CreateItemCommandValidatorTests
{
    private readonly CreateItemCommandValidator _validator = new();

    private static CreateItemCommand Valid() =>
        new(Guid.NewGuid(), "Ručni sat", "Opis predmeta", "Satovi", "Novo", "Novi Pazar");

    public static IEnumerable<object[]> ConditionNames =>
        Enum.GetNames<ItemCondition>().Select(name => new object[] { name });

    [Fact]
    public void Vazeci_unos_prolazi()
    {
        var result = _validator.TestValidate(Valid());

        result.ShouldNotHaveAnyValidationErrors();
    }

    // Title

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Naslov_je_obavezan(string? title)
    {
        var result = _validator.TestValidate(Valid() with { Title = title! });

        result.ShouldHaveValidationErrorFor(x => x.Title).WithErrorMessage("Naslov je obavezan");
    }

    [Fact]
    public void Naslov_kraci_od_3_karaktera_nije_validan()
    {
        var result = _validator.TestValidate(Valid() with { Title = "ab" });

        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Naslov mora imati između 3 i 200 karaktera");
    }

    [Fact]
    public void Naslov_duzi_od_200_karaktera_nije_validan()
    {
        var result = _validator.TestValidate(Valid() with { Title = new string('a', 201) });

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(200)]
    public void Naslov_na_granicama_je_validan(int length)
    {
        var result = _validator.TestValidate(Valid() with { Title = new string('a', length) });

        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    // DescriptionMarkdown

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Opis_je_obavezan(string? description)
    {
        var result = _validator.TestValidate(Valid() with { DescriptionMarkdown = description! });

        result.ShouldHaveValidationErrorFor(x => x.DescriptionMarkdown).WithErrorMessage("Opis je obavezan");
    }

    [Fact]
    public void Opis_duzi_od_10000_karaktera_nije_validan()
    {
        var result = _validator.TestValidate(Valid() with { DescriptionMarkdown = new string('a', 10001) });

        result.ShouldHaveValidationErrorFor(x => x.DescriptionMarkdown);
    }

    [Fact]
    public void Opis_od_tacno_10000_karaktera_je_validan()
    {
        var result = _validator.TestValidate(Valid() with { DescriptionMarkdown = new string('a', 10000) });

        result.ShouldNotHaveValidationErrorFor(x => x.DescriptionMarkdown);
    }

    // Category

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Kategorija_je_obavezna(string? category)
    {
        var result = _validator.TestValidate(Valid() with { Category = category! });

        result.ShouldHaveValidationErrorFor(x => x.Category).WithErrorMessage("Kategorija je obavezna");
    }

    public static IEnumerable<object[]> CategoryNames =>
        Enum.GetNames<ItemCategory>().Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(CategoryNames))]
    public void Svaka_vrednost_ItemCategory_je_validna(string category)
    {
        var result = _validator.TestValidate(Valid() with { Category = category });

        result.ShouldNotHaveValidationErrorFor(x => x.Category);
    }

    [Theory]
    [InlineData("Nepostojeca")]
    [InlineData("99")]
    public void Nepoznata_kategorija_nije_validna(string category)
    {
        var result = _validator.TestValidate(Valid() with { Category = category });

        result.ShouldHaveValidationErrorFor(x => x.Category);
    }

    // Condition

    [Theory]
    [MemberData(nameof(ConditionNames))]
    public void Svaka_vrednost_ItemCondition_je_validna(string condition)
    {
        var result = _validator.TestValidate(Valid() with { Condition = condition });

        result.ShouldNotHaveValidationErrorFor(x => x.Condition);
    }

    [Theory]
    [InlineData("novo")]
    [InlineData("KAONOVO")]
    public void Stanje_se_poredi_bez_razlike_u_velicini_slova(string condition)
    {
        var result = _validator.TestValidate(Valid() with { Condition = condition });

        result.ShouldNotHaveValidationErrorFor(x => x.Condition);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Stanje_je_obavezno(string? condition)
    {
        var result = _validator.TestValidate(Valid() with { Condition = condition! });

        result.ShouldHaveValidationErrorFor(x => x.Condition).WithErrorMessage("Stanje predmeta je obavezno");
    }

    [Theory]
    [InlineData("Polovno")]
    [InlineData("Novo ")]
    [InlineData("99")]
    [InlineData("1")]
    public void Nepoznato_stanje_nije_validno(string condition)
    {
        var result = _validator.TestValidate(Valid() with { Condition = condition });

        result.ShouldHaveValidationErrorFor(x => x.Condition)
            .WithErrorMessage("Nepoznato stanje predmeta, dozvoljeno: Novo, KaoNovo, Koriscen, ZaPopravku");
    }

    // Location

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Lokacija_je_obavezna(string? location)
    {
        var result = _validator.TestValidate(Valid() with { Location = location! });

        result.ShouldHaveValidationErrorFor(x => x.Location).WithErrorMessage("Lokacija je obavezna");
    }

    [Fact]
    public void Lokacija_duza_od_200_karaktera_nije_validna()
    {
        var result = _validator.TestValidate(Valid() with { Location = new string('a', 201) });

        result.ShouldHaveValidationErrorFor(x => x.Location);
    }

    [Fact]
    public void Lokacija_od_tacno_200_karaktera_je_validna()
    {
        var result = _validator.TestValidate(Valid() with { Location = new string('a', 200) });

        result.ShouldNotHaveValidationErrorFor(x => x.Location);
    }

    [Fact]
    public void Greska_se_javlja_samo_na_polju_koje_je_nevazece()
    {
        var result = _validator.TestValidate(Valid() with { Title = "ab" });

        result.ShouldHaveValidationErrorFor(x => x.Title);
        result.ShouldNotHaveValidationErrorFor(x => x.DescriptionMarkdown);
        result.ShouldNotHaveValidationErrorFor(x => x.Category);
        result.ShouldNotHaveValidationErrorFor(x => x.Condition);
        result.ShouldNotHaveValidationErrorFor(x => x.Location);
    }
}
