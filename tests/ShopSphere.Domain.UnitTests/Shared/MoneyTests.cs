using ShopSphere.Domain.Shared;

namespace ShopSphere.Domain.UnitTests.Shared;

public class MoneyTests
{
    [Fact]
    public void Create_Should_Fail_When_Amount_Is_Negative()
    {
        var result = Money.Create(-1m);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(MoneyErrors.NegativeAmount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("DOLLAR")]
    public void Create_Should_Fail_When_Currency_Is_Invalid(string currency)
    {
        var result = Money.Create(10m, currency);

        result.Error.ShouldBe(MoneyErrors.InvalidCurrency);
    }

    [Fact]
    public void Create_Should_Round_To_Two_Decimals_And_Normalize_Currency()
    {
        var money = Money.Create(10.555m, "eur").Value;

        money.Amount.ShouldBe(10.56m);
        money.Currency.ShouldBe("EUR");
    }

    [Fact]
    public void Addition_Should_Throw_When_Currencies_Differ()
    {
        var usd = Money.Create(1m, "USD").Value;
        var eur = Money.Create(1m, "EUR").Value;

        Should.Throw<InvalidOperationException>(() => usd + eur);
    }

    [Fact]
    public void Values_With_Same_Amount_And_Currency_Should_Be_Equal()
    {
        Money.Create(5m).Value.ShouldBe(Money.Create(5m).Value);
    }
}
