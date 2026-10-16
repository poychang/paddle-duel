using PaddleDuel.Infrastructure.Store;

namespace PaddleDuel.Tests;

public sealed class StorePlayCatalogTests
{
    [Theory]
    [InlineData("9NGSHZT4WSR1", 1u)]
    [InlineData("9MZ6KPM0339W", 10u)]
    public void OfficialManagedConsumablesHaveExpectedPackSizes(string storeId, uint units)
    {
        Assert.Equal("Consumable", StorePlayCatalog.ProductKind);
        Assert.Equal(units, StorePlayCatalog.GetUnitsPerPurchase(storeId));
    }

    [Theory]
    [InlineData("arcade1972.play.1")]
    [InlineData("9NGSHZT4WSR1/0010")]
    [InlineData("")]
    public void UnknownOrSkuIdsCannotBeUsedAsPlayProducts(string storeId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => StorePlayCatalog.GetUnitsPerPurchase(storeId));
    }
}
