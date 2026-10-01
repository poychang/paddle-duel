using Arcade1972.App.Storage;
using Arcade1972.App.Store;
using Arcade1972.Core;
using Arcade1972.Infrastructure;
using Arcade1972.Infrastructure.Store;
using Microsoft.UI.Xaml;

namespace Arcade1972.App;

public partial class App : Application
{
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
        var stateDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Arcade1972");
        var pendingFulfillment = new PendingFulfillmentCoordinator(
            () => new WindowsStoreGateway(),
            new JsonPendingFulfillmentJournal(stateDirectory));
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