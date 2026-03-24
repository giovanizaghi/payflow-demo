using PayFlow.PointsApi.Domain;

namespace PayFlow.Unit.Tests.Domain;

public class PointsAccountTests
{
    [Fact]
    public void Create_NewAccount_HasZeroBalance()
    {
        var account = PointsAccount.Create(Guid.NewGuid());

        Assert.Equal(0, account.Balance);
        Assert.Equal(0, account.EarnedThisMonth);
    }

    [Fact]
    public void Earn_ReturnsAwardedPoints()
    {
        var account = PointsAccount.Create(Guid.NewGuid());

        var awarded = account.Earn(100);

        Assert.Equal(100, awarded);
        Assert.Equal(100, account.Balance);
    }

    [Fact]
    public void Earn_RespectsMonthlyCapExactly()
    {
        var account = PointsAccount.Create(Guid.NewGuid());
        account.Earn(500);

        var second = account.Earn(1);

        Assert.Equal(0, second);
        Assert.Equal(500, account.Balance);
    }

    [Fact]
    public void Earn_PartialAward_WhenNearCap()
    {
        var account = PointsAccount.Create(Guid.NewGuid());
        account.Earn(450);

        var second = account.Earn(100);

        Assert.Equal(50, second);
        Assert.Equal(500, account.Balance);
    }

    [Fact]
    public void Earn_AtCapAlready_ReturnsZero()
    {
        var account = PointsAccount.Create(Guid.NewGuid());
        account.Earn(500);

        var result = account.Earn(50);

        Assert.Equal(0, result);
    }

    [Fact]
    public void TryRedeem_WithSufficientBalance_ReturnsTrue()
    {
        var account = PointsAccount.Create(Guid.NewGuid());
        account.Earn(200);

        var result = account.TryRedeem(150);

        Assert.True(result);
        Assert.Equal(50, account.Balance);
    }

    [Fact]
    public void TryRedeem_WithInsufficientBalance_ReturnsFalse()
    {
        var account = PointsAccount.Create(Guid.NewGuid());
        account.Earn(100);

        var result = account.TryRedeem(200);

        Assert.False(result);
        Assert.Equal(100, account.Balance);
    }

    [Fact]
    public void TryRedeem_ExactBalance_ReturnsTrue()
    {
        var account = PointsAccount.Create(Guid.NewGuid());
        account.Earn(100);

        var result = account.TryRedeem(100);

        Assert.True(result);
        Assert.Equal(0, account.Balance);
    }

    [Fact]
    public void TryRedeem_ZeroBalance_ReturnsFalse()
    {
        var account = PointsAccount.Create(Guid.NewGuid());

        var result = account.TryRedeem(1);

        Assert.False(result);
    }
}
