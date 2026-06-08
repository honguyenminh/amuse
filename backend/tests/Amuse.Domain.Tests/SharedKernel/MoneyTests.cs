using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Tests.SharedKernel;

public sealed class MoneyTests
{
    [Fact]
    public void Create_accepts_valid_iso_currency()
    {
        var result = Money.Create(1999, "usd");

        Assert.True(result.IsSuccess);
        Assert.Equal(1999, result.Value!.AmountMinor);
        Assert.Equal("USD", result.Value.Currency);
        Assert.False(result.Value.IsZero);
    }

    [Fact]
    public void Create_rejects_negative_amount()
    {
        var result = Money.Create(-1, "USD");

        Assert.False(result.IsSuccess);
        Assert.Equal(MoneyErrors.InvalidAmount, result.Error);
    }

    [Fact]
    public void Create_rejects_invalid_currency()
    {
        var result = Money.Create(100, "US");

        Assert.False(result.IsSuccess);
        Assert.Equal(MoneyErrors.InvalidCurrency, result.Error);
    }

    [Fact]
    public void Zero_check()
    {
        var result = Money.Create(0, "VND");
        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsZero);
    }

    [Fact]
    public void Add_requires_same_currency()
    {
        var left = Money.Create(100, "USD").Value!;
        var right = Money.Create(50, "USD").Value!;

        var sum = left.Add(right);

        Assert.Equal(150, sum.AmountMinor);
    }
}
