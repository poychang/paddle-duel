using PaddleDuel.App.Storage;
using PaddleDuel.App.Store;
using PaddleDuel.Core;
using PaddleDuel.Infrastructure;
using PaddleDuel.Infrastructure.Store;
using Microsoft.UI.Xaml;

namespace PaddleDuel.App;

public partial class App : Application
{
    // Keep the legacy lock so older development builds cannot open the same player data concurrently.
    private const string SingleInstanceMutexName = "Local\\Arcade1972.SingleInstance";
    private Mutex? singleInstanceMutex;
    private Window? window;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        singleInstanceMutex = new Mutex(
            initiallyOwned: true,
            name: SingleInstanceMutexName,
            createdNew: out var createdNew);
        if (!createdNew)
        {
            singleInstanceMutex.Dispose();
            singleInstanceMutex = null;
            Environment.Exit(0);
            return;
        }

        var freePlayQuota = new DailyFreePlayQuota(
            new SystemUtcClock(),
            new LocalStateFreePlayQuotaStore());
        var pendingFulfillment = new PendingFulfillmentCoordinator(
            () => new WindowsStoreGateway(),
            new JsonPendingFulfillmentJournal(LocalStateDirectory.GetPath()));
        window = new MainWindow(freePlayQuota, pendingFulfillment);
        window.Closed += Window_Closed;
        window.Activate();
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        singleInstanceMutex?.ReleaseMutex();
        singleInstanceMutex?.Dispose();
        singleInstanceMutex = null;
    }
}