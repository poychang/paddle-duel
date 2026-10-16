namespace PaddleDuel.Infrastructure.Store;

public static class StorePlayCatalog
{
    public const string OnePlayStoreId = "9NGSHZT4WSR1";
    public const string TenPlayStoreId = "9MZ6KPM0339W";
    // Partner Center calls this ManagedConsumable; Windows.Services.Store uses Consumable.
    public const string ProductKind = "Consumable";

    public static uint GetUnitsPerPurchase(string storeId) => storeId switch
    {
        OnePlayStoreId => 1,
        TenPlayStoreId => 10,
        _ => throw new ArgumentOutOfRangeException(nameof(storeId), storeId, "Unknown play product."),
    };
}
