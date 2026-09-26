using ShopSphere.Domain.Abstractions;

namespace ShopSphere.Domain.Shared;

public sealed record Money
{
    public const string DefaultCurrency = "USD";

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Money Zero(string currency = DefaultCurrency) => new(0m, currency);

    public static Result<Money> Create(decimal amount, string currency = DefaultCurrency)
    {
        if (amount < 0)
        {
            return MoneyErrors.NegativeAmount;
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
        {
            return MoneyErrors.InvalidCurrency;
        }

        return new Money(decimal.Round(amount, 2, MidpointRounding.AwayFromZero), currency.ToUpperInvariant());
    }

    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new InvalidOperationException("Cannot add money values with different currencies.");
        }

        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public Money Multiply(int quantity) => new(Amount * quantity, Currency);

    public override string ToString() => $"{Amount:0.00} {Currency}";
}

public static class MoneyErrors
{
    public static readonly Error NegativeAmount =
        Error.Validation("Money.NegativeAmount", "Money amount cannot be negative.");

    public static readonly Error InvalidCurrency =
        Error.Validation("Money.InvalidCurrency", "Currency must be a 3-letter ISO 4217 code.");
}
