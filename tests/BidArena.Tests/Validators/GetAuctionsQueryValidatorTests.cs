using BidArena.Application.Auctions.Queries.GetAuctions;
using BidArena.Domain.Entities;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BidArena.Tests.Validators;

public class GetAuctionsQueryValidatorTests
{
    private readonly GetAuctionsQueryValidator _validator = new();

    private static GetAuctionsQuery Valid() => new(AuctionStatus.Active, "Satovi", "sat", 1, 20);

    public static IEnumerable<object[]> PublicStatuses =>
        Enum.GetValues<AuctionStatus>()
            .Where(s => s != AuctionStatus.Draft)
            .Select(s => new object[] { s });

    [Fact]
    public void Vazeci_unos_prolazi()
    {
        var result = _validator.TestValidate(Valid());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Upit_bez_filtera_sa_podrazumevanim_vrednostima_prolazi()
    {
        var result = _validator.TestValidate(new GetAuctionsQuery(null, null, null));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Strana_mora_biti_najmanje_1(int page)
    {
        var result = _validator.TestValidate(Valid() with { Page = page });

        result.ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    [InlineData(-5)]
    public void Velicina_strane_van_opsega_1_do_50_nije_validna(int pageSize)
    {
        var result = _validator.TestValidate(Valid() with { PageSize = pageSize });

        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    public void Velicina_strane_na_granicama_je_validna(int pageSize)
    {
        var result = _validator.TestValidate(Valid() with { PageSize = pageSize });

        result.ShouldNotHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Draft_status_nije_dozvoljen()
    {
        var result = _validator.TestValidate(Valid() with { Status = AuctionStatus.Draft });

        result.ShouldHaveValidationErrorFor(x => x.Status).WithErrorMessage("Draft aukcije nisu javne");
    }

    [Theory]
    [MemberData(nameof(PublicStatuses))]
    public void Javni_statusi_su_dozvoljeni(AuctionStatus status)
    {
        var result = _validator.TestValidate(Valid() with { Status = status });

        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void Pretraga_duza_od_100_karaktera_nije_validna()
    {
        var result = _validator.TestValidate(Valid() with { Search = new string('a', 101) });

        result.ShouldHaveValidationErrorFor(x => x.Search);
    }

    [Fact]
    public void Pretraga_od_tacno_100_karaktera_je_validna()
    {
        var result = _validator.TestValidate(Valid() with { Search = new string('a', 100) });

        result.ShouldNotHaveValidationErrorFor(x => x.Search);
    }

    [Theory]
    [InlineData("Nepostojeca")]
    [InlineData("99")]
    public void Nepoznata_kategorija_nije_validna(string category)
    {
        var result = _validator.TestValidate(Valid() with { Category = category });

        result.ShouldHaveValidationErrorFor(x => x.Category);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("satovi")]
    public void Prazna_ili_poznata_kategorija_je_validna(string? category)
    {
        var result = _validator.TestValidate(Valid() with { Category = category });

        result.ShouldNotHaveValidationErrorFor(x => x.Category);
    }
}
