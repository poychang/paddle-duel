using System.Diagnostics;
using PaddleDuel.App.Storage;
using PaddleDuel.App.Store;
using PaddleDuel.Core;
using PaddleDuel.Infrastructure.Store;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using VirtualKey = Windows.System.VirtualKey;
using Rect = Windows.Foundation.Rect;
using static PaddleDuel.Infrastructure.Store.StorePlayCatalog;

namespace PaddleDuel.App;

public sealed partial class MainWindow : Window
{
    private readonly DailyFreePlayQuota freePlayQuota;
    private readonly PendingFulfillmentCoordinator pendingFulfillment;
    private readonly PlayEntitlementService playEntitlementService;
    private readonly WindowStateStore windowStateStore = new();
    private readonly Classic1972Rules rules = new();
    private readonly ClassicGameSimulation simulation;
    private ClassicAiController aiController;
    private readonly DispatcherQueueTimer gameTimer;
    private readonly Stopwatch frameClock = new();
    private FixedStepGameLoop gameLoop;
    private bool leftUpPressed;
    private bool leftDownPressed;
    private bool rightUpPressed;
    private bool rightDownPressed;
    private bool isOnePlayer;
    private bool resumeGameAfterInformation;
    private bool resumeGameAfterSettings;
    private bool isMatchPaused;
    private bool isCompletingMatch;
    private bool isStartingMatch;
    private bool isWindowActive;
    private PlayEntitlementSession? activePlaySession;
    private ClassicAiDifficulty aiDifficulty = ClassicAiDifficulty.Medium;
    private IStoreGateway? storeGateway;
    private StorePurchaseCoordinator? purchaseCoordinator;
    private bool storeProductsLoaded;

    public MainWindow(
        DailyFreePlayQuota freePlayQuota,
        IPendingFulfillmentJournal fulfillmentJournal)
    {
        this.freePlayQuota = freePlayQuota;
        pendingFulfillment = new PendingFulfillmentCoordinator(GetStoreGateway, fulfillmentJournal);
        playEntitlementService = new(
            freePlayQuota,
            GetStoreGateway,
            OnePlayStoreId,
            TenPlayStoreId,
            pendingFulfillment);
        InitializeComponent();
        WindowRoot.AddHandler(UIElement.KeyUpEvent, new KeyEventHandler(WindowRoot_KeyUp), true);
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "PaddleDuel.ico"));

        simulation = new ClassicGameSimulation(rules);
        aiController = new ClassicAiController(rules, aiDifficulty);
        gameLoop = new FixedStepGameLoop(simulation, simulation.CreateInitialState());
        gameTimer = DispatcherQueue.CreateTimer();
        gameTimer.Interval = TimeSpan.FromMilliseconds(8);
        gameTimer.Tick += GameTimer_Tick;

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        RestoreWindowState();

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            AppWindow.TitleBar.ButtonForegroundColor = Colors.White;
            AppWindow.TitleBar.ButtonInactiveForegroundColor = Colors.Gray;
        }

        Activated += MainWindow_Activated;
        Closed += MainWindow_Closed;
    }

    private IStoreGateway GetStoreGateway() =>
        storeGateway ??= new WindowsStoreGateway(WinRT.Interop.WindowNative.GetWindowHandle(this));

    private void FullScreenButton_Click(object sender, RoutedEventArgs e)
    {
        SaveWindowState();
        var presenter = AppWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen
            ? AppWindowPresenterKind.Overlapped
            : AppWindowPresenterKind.FullScreen;

        AppWindow.SetPresenter(presenter);
        SaveWindowState();
    }

    private void AppTitleBar_Loaded(object sender, RoutedEventArgs e)
    {
        SetTitleBarInteractiveRegion();
    }

    private void AppTitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        SetTitleBarInteractiveRegion();
    }

    private void SetTitleBarInteractiveRegion()
    {
        if (!ExtendsContentIntoTitleBar || AppTitleBar.XamlRoot is null)
        {
            return;
        }

        var scale = AppTitleBar.XamlRoot.RasterizationScale;
        var transform = TitleBarActions.TransformToVisual(null);
        var bounds = transform.TransformBounds(
            new Rect(0, 0, TitleBarActions.ActualWidth, TitleBarActions.ActualHeight));
        var region = new RectInt32(
            (int)Math.Round(bounds.X * scale),
            (int)Math.Round(bounds.Y * scale),
            (int)Math.Round(bounds.Width * scale),
            (int)Math.Round(bounds.Height * scale));

        var inputSource = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);
        inputSource.SetRegionRects(NonClientRegionKind.Passthrough, [region]);
    }

    private void InformationButton_Click(object sender, RoutedEventArgs e)
    {
        ShowInformation();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        ShowSettings();
    }

    private void CloseInformationButton_Click(object sender, RoutedEventArgs e)
    {
        HideInformation();
    }

    private void PurchaseEntryButton_Click(object sender, RoutedEventArgs e)
    {
        if (isStartingMatch || isCompletingMatch || activePlaySession is not null)
        {
            return;
        }

        PurchasePlaceholderOverlay.Visibility = Visibility.Visible;
        ClosePurchasePlaceholderButton.Focus(FocusState.Programmatic);
        _ = LoadStoreProductsAsync();
    }

    private void ClosePurchasePlaceholderButton_Click(object sender, RoutedEventArgs e)
    {
        HidePurchasePlaceholder();
    }

    private void HidePurchasePlaceholder()
    {
        PurchasePlaceholderOverlay.Visibility = Visibility.Collapsed;
        PurchaseEntryButton.Focus(FocusState.Programmatic);
    }

    private async Task LoadStoreProductsAsync()
    {
        storeProductsLoaded = false;
        BuyOnePlayButton.IsEnabled = false;
        BuyTenPlayButton.IsEnabled = false;
        StorePurchaseStatusText.Text = AppStrings.Get("StoreConnecting");
        StoreProductFallbackText.Visibility = Visibility.Collapsed;

        try
        {
            var gateway = GetStoreGateway();
            purchaseCoordinator ??= new StorePurchaseCoordinator(gateway);
            var result = await gateway.GetProductsAsync(new[] { OnePlayStoreId, TenPlayStoreId });
            if (result.Status != StoreOperationStatus.Succeeded)
            {
                ShowStoreUnavailable();
                return;
            }

            var products = result.Products.ToDictionary(product => product.StoreId, StringComparer.Ordinal);
            if (!products.TryGetValue(OnePlayStoreId, out var onePlay)
                || !products.TryGetValue(TenPlayStoreId, out var tenPlay))
            {
                ShowStoreUnavailable();
                return;
            }

            OnePlayProductText.Text = $"{onePlay.DisplayName}  {onePlay.FormattedPrice}";
            TenPlayProductText.Text = $"{tenPlay.DisplayName}  {tenPlay.FormattedPrice}";
            StorePurchaseStatusText.Text = AppStrings.Get("StoreSelectPurchase");
            storeProductsLoaded = true;
            BuyOnePlayButton.IsEnabled = true;
            BuyTenPlayButton.IsEnabled = true;
        }
        catch (Exception error)
        {
            Debug.WriteLine($"Unable to load Store products: {error}");
            ShowStoreUnavailable();
        }
    }

    private void ShowStoreUnavailable()
    {
        StorePurchaseStatusText.Text = AppStrings.Get("StoreUnavailable");
        StoreProductFallbackText.Visibility = Visibility.Visible;
    }

    private async void BuyOnePlayButton_Click(object sender, RoutedEventArgs e)
    {
        await PurchaseProductAsync(OnePlayStoreId);
    }

    private async void BuyTenPlayButton_Click(object sender, RoutedEventArgs e)
    {
        await PurchaseProductAsync(TenPlayStoreId);
    }

    private async Task PurchaseProductAsync(string storeId)
    {
        if (!storeProductsLoaded || purchaseCoordinator is null)
        {
            return;
        }

        BuyOnePlayButton.IsEnabled = false;
        BuyTenPlayButton.IsEnabled = false;
        StorePurchaseStatusText.Text = AppStrings.Get("StoreOpening");
        try
        {
            var result = await purchaseCoordinator.RequestPurchaseAsync(storeId);
            StorePurchaseStatusText.Text = result.Status switch
            {
                StorePurchaseUiStatus.Purchased => AppStrings.Get("PurchaseComplete"),
                StorePurchaseUiStatus.Cancelled => AppStrings.Get("PurchaseCancelled"),
                StorePurchaseUiStatus.NetworkError => AppStrings.Get("PurchaseNetworkError"),
                StorePurchaseUiStatus.ServerError => AppStrings.Get("PurchaseServerError"),
                StorePurchaseUiStatus.NotSignedIn => AppStrings.Get("PurchaseSignIn"),
                StorePurchaseUiStatus.AlreadyInProgress => AppStrings.Get("PurchaseInProgress"),
                _ => AppStrings.Get("ProductUnavailable"),
            };
            if (result.Status == StorePurchaseUiStatus.Purchased)
            {
                await RefreshQuotaDisplayAsync();
            }
        }
        catch (Exception error)
        {
            Debug.WriteLine($"Unable to complete Store purchase: {error}");
            ShowStoreUnavailable();
        }
        finally
        {
            BuyOnePlayButton.IsEnabled = storeProductsLoaded;
            BuyTenPlayButton.IsEnabled = storeProductsLoaded;
        }
    }

    private void ShowInformation()
    {
        if (InformationOverlay.Visibility == Visibility.Visible
            || SettingsOverlay.Visibility == Visibility.Visible)
        {
            return;
        }

        resumeGameAfterInformation = gameTimer.IsRunning;
        gameTimer.Stop();
        GameRoot.IsTabStop = false;
        ClearInput();
        InformationOverlay.Visibility = Visibility.Visible;
        CloseInformationButton.Focus(FocusState.Programmatic);
    }

    private void HideInformation()
    {
        InformationOverlay.Visibility = Visibility.Collapsed;

        if (resumeGameAfterInformation)
        {
            frameClock.Restart();
            gameTimer.Start();
            FocusGameSurface();
        }
        else if (isMatchPaused)
        {
            ShowPauseOverlay();
        }
        else
        {
            StartButton.Focus(FocusState.Programmatic);
        }

        resumeGameAfterInformation = false;
    }

    private void ShowSettings()
    {
        if (SettingsOverlay.Visibility == Visibility.Visible
            || InformationOverlay.Visibility == Visibility.Visible)
        {
            return;
        }

        resumeGameAfterSettings = gameTimer.IsRunning;
        gameTimer.Stop();
        GameRoot.IsTabStop = false;
        ClearInput();
        SettingsDisplayModeText.Text = AppWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen
            ? AppStrings.Get("DisplayFullScreen")
            : AppStrings.Get("DisplayWindowed");
        SettingsOverlay.Visibility = Visibility.Visible;
        CloseSettingsButton.Focus(FocusState.Programmatic);
    }

    private void CloseSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        HideSettings();
    }

    private void HideSettings()
    {
        SettingsOverlay.Visibility = Visibility.Collapsed;
        if (resumeGameAfterSettings)
        {
            frameClock.Restart();
            gameTimer.Start();
            FocusGameSurface();
        }
        else if (isMatchPaused)
        {
            ShowPauseOverlay();
        }
        else
        {
            SettingsButton.Focus(FocusState.Programmatic);
        }

        resumeGameAfterSettings = false;
    }

    private void SettingsWindowedButton_Click(object sender, RoutedEventArgs e)
    {
        if (AppWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen)
        {
            AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
        }

        SettingsDisplayModeText.Text = AppStrings.Get("DisplayWindowed");
        SaveWindowState();
    }

    private void SettingsFullScreenButton_Click(object sender, RoutedEventArgs e)
    {
        AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
        SettingsDisplayModeText.Text = AppStrings.Get("DisplayFullScreen");
        SaveWindowState();
    }

    private void SettingsEasyDifficultyButton_Click(object sender, RoutedEventArgs e)
    {
        SetAiDifficulty(ClassicAiDifficulty.Easy);
    }

    private void SettingsMediumDifficultyButton_Click(object sender, RoutedEventArgs e)
    {
        SetAiDifficulty(ClassicAiDifficulty.Medium);
    }

    private void SettingsHardDifficultyButton_Click(object sender, RoutedEventArgs e)
    {
        SetAiDifficulty(ClassicAiDifficulty.Hard);
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        await TryStartMatchAsync(onePlayer: false);
    }

    private async void StartOnePlayerButton_Click(object sender, RoutedEventArgs e)
    {
        await TryStartMatchAsync(onePlayer: true);
    }

    private void EasyDifficultyButton_Click(object sender, RoutedEventArgs e)
    {
        SetAiDifficulty(ClassicAiDifficulty.Easy);
    }

    private void MediumDifficultyButton_Click(object sender, RoutedEventArgs e)
    {
        SetAiDifficulty(ClassicAiDifficulty.Medium);
    }

    private void HardDifficultyButton_Click(object sender, RoutedEventArgs e)
    {
        SetAiDifficulty(ClassicAiDifficulty.Hard);
    }

    private void SetAiDifficulty(ClassicAiDifficulty difficulty)
    {
        aiDifficulty = difficulty;
        aiController = new ClassicAiController(rules, difficulty);
        var label = AppStrings.Get($"Difficulty{difficulty}");
        AiDifficultyText.Text = AppStrings.Format("AiDifficulty", label);
        SettingsAiDifficultyText.Text = AppStrings.Format("AiDifficulty", label);
        UpdateDifficultyButtonStyles();
    }

    private void UpdateDifficultyButtonStyles()
    {
        SetDifficultyButtonStyle(EasyDifficultyButton, aiDifficulty == ClassicAiDifficulty.Easy);
        SetDifficultyButtonStyle(MediumDifficultyButton, aiDifficulty == ClassicAiDifficulty.Medium);
        SetDifficultyButtonStyle(HardDifficultyButton, aiDifficulty == ClassicAiDifficulty.Hard);
        SetDifficultyButtonStyle(SettingsEasyDifficultyButton, aiDifficulty == ClassicAiDifficulty.Easy);
        SetDifficultyButtonStyle(SettingsMediumDifficultyButton, aiDifficulty == ClassicAiDifficulty.Medium);
        SetDifficultyButtonStyle(SettingsHardDifficultyButton, aiDifficulty == ClassicAiDifficulty.Hard);
    }

    private static void SetDifficultyButtonStyle(Button button, bool isSelected)
    {
        button.Background = isSelected
            ? new SolidColorBrush(Colors.WhiteSmoke)
            : new SolidColorBrush(Colors.Transparent);
        button.BorderThickness = isSelected ? new Thickness(0) : new Thickness(1);
        button.BorderBrush = new SolidColorBrush(Colors.LightGray);
        button.Foreground = new SolidColorBrush(isSelected ? Colors.Black : Colors.White);
    }

    private async Task TryStartMatchAsync(bool onePlayer)
    {
        if (isStartingMatch || isCompletingMatch || activePlaySession is not null)
        {
            return;
        }

        isStartingMatch = true;
        SetStartButtonsEnabled(false);
        try
        {
            var startResult = await playEntitlementService.TryStartMatchAsync();
            if (startResult.Status != EntitlementStartStatus.Allowed)
            {
                MenuHeading.Text = startResult.Status switch
                {
                    EntitlementStartStatus.NoEntitlement => AppStrings.Get("NoPlaysAvailable"),
                    EntitlementStartStatus.FulfillmentPending => AppStrings.Get("PaymentPending"),
                    _ => AppStrings.Get("EntitlementStoreUnavailable"),
                };
                return;
            }

            activePlaySession = startResult.Session;
            isOnePlayer = onePlayer;
            isMatchPaused = false;
            PauseOverlay.Visibility = Visibility.Collapsed;
            if (onePlayer)
            {
                aiController = new ClassicAiController(rules, aiDifficulty);
            }
            aiController.Reset();
            gameLoop = new FixedStepGameLoop(simulation, simulation.CreateInitialState());
            MenuOverlay.Visibility = Visibility.Collapsed;
            ClearInput();
            RenderGame();
            frameClock.Restart();
            gameTimer.Start();
            if (!isWindowActive || InformationOverlay.Visibility == Visibility.Visible
                || SettingsOverlay.Visibility == Visibility.Visible
                || PurchasePlaceholderOverlay.Visibility == Visibility.Visible)
            {
                PauseMatch();
            }
            else
            {
                FocusGameSurface();
            }
        }
        catch (Exception error)
        {
            Debug.WriteLine($"Unable to start match: {error}");
            MenuHeading.Text = AppStrings.Get("QuotaUnavailable");
            SetStartButtonsEnabled(false);
            PurchaseEntryButton.Visibility = Visibility.Collapsed;
        }
        finally
        {
            isStartingMatch = false;
            if (MenuOverlay.Visibility == Visibility.Visible)
            {
                await RefreshQuotaDisplayAsync();
            }
        }
    }

    private async void GameTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        if (isCompletingMatch)
        {
            return;
        }

        var elapsed = frameClock.Elapsed;
        frameClock.Restart();

        var input = new PaddleInput(
            Axis(leftUpPressed, leftDownPressed),
            isOnePlayer
                ? aiController.Update(gameLoop.State, elapsed.TotalSeconds)
                : Axis(rightUpPressed, rightDownPressed));
        gameLoop.Advance(elapsed, input);
        RenderGame();

        if (gameLoop.State.Phase == MatchPhase.Finished)
        {
            gameTimer.Stop();
            ClearInput();
            isCompletingMatch = true;
            await CompleteFinishedMatchAsync();
        }
    }

    private async Task CompleteFinishedMatchAsync()
    {
        try
        {
            if (activePlaySession is null)
            {
                throw new InvalidOperationException("The finished match has no entitlement session.");
            }

            var completion = await playEntitlementService.CompleteMatchAsync(activePlaySession);
            if (completion == EntitlementCompletionStatus.Pending)
            {
                ShowMenu(AppStrings.Get("PaymentPending"), allowNewMatch: false);
                return;
            }
            if (completion == EntitlementCompletionStatus.Failed)
            {
                ShowMenu(AppStrings.Get("QuotaSaveFailed"), allowNewMatch: false);
                return;
            }

            activePlaySession = null;
            var heading = gameLoop.State.Winner == PlayerSide.Left
                ? AppStrings.Get("LeftPlayerWins")
                : AppStrings.Get("RightPlayerWins");
            ShowMenu(heading, allowNewMatch: true);
        }
        catch (Exception error)
        {
            Debug.WriteLine($"Unable to persist completed match: {error}");
            ShowMenu(AppStrings.Get("QuotaSaveFailed"), allowNewMatch: false);
        }
        finally
        {
            isCompletingMatch = false;
            await RefreshQuotaDisplayAsync();
        }
    }

    private void ShowMenu(string heading, bool allowNewMatch)
    {
        GameRoot.IsTabStop = false;
        MenuHeading.Text = heading;
        StartButton.Content = AppStrings.Get("PlayAgain");
        SetStartButtonsEnabled(allowNewMatch);
        MenuOverlay.Visibility = Visibility.Visible;
        if (allowNewMatch)
        {
            StartButton.Focus(FocusState.Programmatic);
        }
    }

    private void SetStartButtonsEnabled(bool isEnabled)
    {
        var canStart = isEnabled && !isStartingMatch && !isCompletingMatch && activePlaySession is null;
        OnePlayerButton.IsEnabled = canStart;
        StartButton.IsEnabled = canStart;
    }

    private async void MenuOverlay_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await RefreshQuotaDisplayAsync();
        }
        finally
        {
            StartupOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        isWindowActive = args.WindowActivationState != WindowActivationState.Deactivated;
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            PauseMatch();
            return;
        }

        try
        {
            await pendingFulfillment.RetryPendingAsync();
            if (activePlaySession is not null && gameLoop.State.Phase == MatchPhase.Finished
                && !isCompletingMatch)
            {
                isCompletingMatch = true;
                await CompleteFinishedMatchAsync();
            }
            else if (MenuOverlay.Visibility == Visibility.Visible)
            {
                await RefreshQuotaDisplayAsync();
            }
        }
        catch (Exception error)
        {
            Debug.WriteLine($"Unable to retry pending fulfillment: {error}");
            MenuHeading.Text = AppStrings.Get("PaymentPending");
            SetStartButtonsEnabled(false);
        }

        if (isMatchPaused && PauseOverlay.Visibility == Visibility.Visible)
        {
            ResumeMatchButton.Focus(FocusState.Programmatic);
        }
    }

    private async Task RefreshQuotaDisplayAsync()
    {
        try
        {
            UpdateQuotaDisplay(await freePlayQuota.GetAvailabilityAsync());
        }
        catch (Exception error)
        {
            Debug.WriteLine($"Unable to read daily quota: {error}");
            QuotaStatusText.Text = AppStrings.Format("FreePlaysUnavailable", freePlayQuota.DailyLimit);
            QuotaResetText.Text = AppStrings.Get("ResetUnavailable");
            SetStartButtonsEnabled(false);
            PurchaseEntryButton.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateQuotaDisplay(FreePlayAvailability availability)
    {
        var resetDate = availability.QuotaDate.AddDays(1);
        QuotaStatusText.Text =
            AppStrings.Format("FreePlays", availability.RemainingPlays, freePlayQuota.DailyLimit);
        QuotaResetText.Text = AppStrings.Format("QuotaReset", resetDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        var hasFreePlays = availability.RemainingPlays > 0;
        // Starting a match checks Store balances after free plays are exhausted.
        SetStartButtonsEnabled(true);
        PurchaseEntryButton.Visibility = hasFreePlays
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void RenderGame()
    {
        if (GameCanvas.ActualWidth <= 0 || GameCanvas.ActualHeight <= 0)
        {
            return;
        }

        var scale = Math.Min(
            GameCanvas.ActualWidth / rules.FieldWidth,
            GameCanvas.ActualHeight / rules.FieldHeight);
        var offsetX = (GameCanvas.ActualWidth - (rules.FieldWidth * scale)) / 2;
        var offsetY = (GameCanvas.ActualHeight - (rules.FieldHeight * scale)) / 2;
        var state = gameLoop.State;

        PositionRectangle(
            LeftPaddle,
            offsetX + (rules.PaddleInset * scale),
            offsetY + (state.LeftPaddle.Y * scale),
            rules.PaddleWidth * scale,
            rules.PaddleHeight * scale);
        PositionRectangle(
            RightPaddle,
            offsetX + ((rules.FieldWidth - rules.PaddleInset - rules.PaddleWidth) * scale),
            offsetY + (state.RightPaddle.Y * scale),
            rules.PaddleWidth * scale,
            rules.PaddleHeight * scale);
        PositionRectangle(
            Ball,
            offsetX + ((state.Ball.X - (rules.BallSize / 2)) * scale),
            offsetY + ((state.Ball.Y - (rules.BallSize / 2)) * scale),
            rules.BallSize * scale,
            rules.BallSize * scale);
        PositionRectangle(
            CenterLine,
            offsetX + ((rules.FieldWidth / 2) * scale),
            offsetY,
            Math.Max(1, scale),
            rules.FieldHeight * scale);

        LeftScore.Text = state.LeftScore.ToString();
        RightScore.Text = state.RightScore.ToString();
        Canvas.SetLeft(LeftScore, offsetX + (rules.FieldWidth * scale * 0.36));
        Canvas.SetTop(LeftScore, offsetY + (12 * scale));
        Canvas.SetLeft(RightScore, offsetX + (rules.FieldWidth * scale * 0.60));
        Canvas.SetTop(RightScore, offsetY + (12 * scale));
    }

    private static void PositionRectangle(
        FrameworkElement rectangle,
        double left,
        double top,
        double width,
        double height)
    {
        rectangle.Width = width;
        rectangle.Height = height;
        Canvas.SetLeft(rectangle, left);
        Canvas.SetTop(rectangle, top);
    }

    private static double Axis(bool upPressed, bool downPressed)
    {
        return (downPressed ? 1 : 0) - (upPressed ? 1 : 0);
    }

    private void WindowRoot_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (!gameTimer.IsRunning)
        {
            return;
        }

        if (e.Key == VirtualKey.Escape)
        {
            PauseMatch();
        }
        else if (e.Key == VirtualKey.I)
        {
            ShowInformation();
        }
        else if (e.Key is VirtualKey.W or VirtualKey.S or VirtualKey.Up or VirtualKey.Down)
        {
            SetKeyState(e.Key, true);
        }
        else
        {
            return;
        }

        e.Handled = true;
    }

    private void PauseMatch()
    {
        ClearInput();
        if (activePlaySession is null || isCompletingMatch
            || (!gameTimer.IsRunning && !resumeGameAfterInformation && !resumeGameAfterSettings))
        {
            return;
        }

        gameTimer.Stop();
        GameRoot.IsTabStop = false;
        isMatchPaused = true;
        resumeGameAfterInformation = false;
        resumeGameAfterSettings = false;
        if (InformationOverlay.Visibility != Visibility.Visible
            && SettingsOverlay.Visibility != Visibility.Visible)
        {
            ShowPauseOverlay();
        }
    }

    private void ShowPauseOverlay()
    {
        PauseOverlay.Visibility = Visibility.Visible;
        ResumeMatchButton.Focus(FocusState.Programmatic);
    }

    private void ResumeMatchButton_Click(object sender, RoutedEventArgs e)
    {
        ResumeMatch();
    }

    private void ResumeMatch()
    {
        if (!isMatchPaused || activePlaySession is null)
        {
            return;
        }

        PauseOverlay.Visibility = Visibility.Collapsed;
        isMatchPaused = false;
        ClearInput();
        frameClock.Restart();
        gameTimer.Start();
        FocusGameSurface();
    }

    private async void AbandonMatchButton_Click(object sender, RoutedEventArgs e)
    {
        if (!isMatchPaused || activePlaySession is null)
        {
            return;
        }

        gameTimer.Stop();
        ClearInput();
        activePlaySession = null;
        isMatchPaused = false;
        PauseOverlay.Visibility = Visibility.Collapsed;
        ShowMenu(AppStrings.Get("MatchAbandoned"), allowNewMatch: false);
        await RefreshQuotaDisplayAsync();
    }

    private void PauseOverlay_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            ResumeMatch();
            e.Handled = true;
        }
    }

    private void WindowRoot_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is VirtualKey.W or VirtualKey.S or VirtualKey.Up or VirtualKey.Down)
        {
            SetKeyState(e.Key, false);
            if (gameTimer.IsRunning)
            {
                e.Handled = true;
            }
        }
    }

    private void SetKeyState(VirtualKey key, bool isPressed)
    {
        switch (key)
        {
            case VirtualKey.W:
                leftUpPressed = isPressed;
                break;
            case VirtualKey.S:
                leftDownPressed = isPressed;
                break;
            case VirtualKey.Up:
                rightUpPressed = isPressed;
                break;
            case VirtualKey.Down:
                rightDownPressed = isPressed;
                break;
        }
    }

    private void GameRoot_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (gameTimer.IsRunning)
        {
            FocusGameSurface();
        }
    }

    private void FocusGameSurface()
    {
        GameRoot.IsTabStop = true;
        if (!GameRoot.Focus(FocusState.Programmatic))
        {
            Debug.WriteLine("Unable to focus the game surface; pausing to avoid uncontrolled play.");
            PauseMatch();
        }
    }

    private void GameCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        RenderGame();
    }

    private void InformationOverlay_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is VirtualKey.Escape or VirtualKey.I)
        {
            HideInformation();
            e.Handled = true;
        }
    }

    private void SettingsOverlay_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is VirtualKey.Escape or VirtualKey.I)
        {
            HideSettings();
            e.Handled = true;
        }
    }

    private void PurchasePlaceholderOverlay_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            HidePurchasePlaceholder();
            e.Handled = true;
        }
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        SaveWindowState();
        gameTimer.Stop();
        gameTimer.Tick -= GameTimer_Tick;
        Activated -= MainWindow_Activated;
    }

    private void RestoreWindowState()
    {
        var state = windowStateStore.Load();
        if (state is null)
        {
            AppWindow.Resize(new SizeInt32(1024, 720));
            return;
        }

        var displayArea = DisplayArea.GetFromWindowId(
            AppWindow.Id,
            DisplayAreaFallback.Nearest);
        var workArea = displayArea.WorkArea;
        var width = Math.Clamp(state.Width, 480, workArea.Width);
        var height = Math.Clamp(state.Height, 360, workArea.Height);
        var x = Math.Clamp(state.X, workArea.X, workArea.X + workArea.Width - width);
        var y = Math.Clamp(state.Y, workArea.Y, workArea.Y + workArea.Height - height);

        AppWindow.Resize(new SizeInt32(width, height));
        AppWindow.Move(new PointInt32(x, y));
        if (state.IsFullScreen)
        {
            AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
        }
    }

    private void SaveWindowState()
    {
        try
        {
            var isFullScreen = AppWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen;
            var size = AppWindow.Size;
            var position = AppWindow.Position;
            windowStateStore.Save(new WindowStateSnapshot(
                position.X,
                position.Y,
                size.Width,
                size.Height,
                isFullScreen));
        }
        catch (IOException)
        {
        }
    }

    private void ClearInput()
    {
        leftUpPressed = false;
        leftDownPressed = false;
        rightUpPressed = false;
        rightDownPressed = false;
    }
}