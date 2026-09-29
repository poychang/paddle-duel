using System.Diagnostics;
using Arcade1972.App.Storage;
using Arcade1972.Core;
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

namespace Arcade1972.App;

public sealed partial class MainWindow : Window
{
    private readonly DailyFreePlayQuota freePlayQuota;
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
    private bool isMatchPaused;
    private bool isCompletingMatch;
    private FreePlayMatchSession? activeFreePlaySession;
    private ClassicAiDifficulty aiDifficulty = ClassicAiDifficulty.Medium;

    public MainWindow(DailyFreePlayQuota freePlayQuota)
    {
        this.freePlayQuota = freePlayQuota;
        InitializeComponent();

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

    private void CloseInformationButton_Click(object sender, RoutedEventArgs e)
    {
        HideInformation();
    }

    private void PurchaseEntryButton_Click(object sender, RoutedEventArgs e)
    {
        PurchasePlaceholderOverlay.Visibility = Visibility.Visible;
        ClosePurchasePlaceholderButton.Focus(FocusState.Programmatic);
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

    private void ShowInformation()
    {
        if (InformationOverlay.Visibility == Visibility.Visible)
        {
            return;
        }

        resumeGameAfterInformation = gameTimer.IsRunning;
        gameTimer.Stop();
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
            InputSink.Focus(FocusState.Programmatic);
        }
        else
        {
            StartButton.Focus(FocusState.Programmatic);
        }

        resumeGameAfterInformation = false;
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
        AiDifficultyText.Text = $"AI DIFFICULTY  {difficulty.ToString().ToUpperInvariant()}";
        UpdateDifficultyButtonStyles();
    }

    private void UpdateDifficultyButtonStyles()
    {
        SetDifficultyButtonStyle(EasyDifficultyButton, aiDifficulty == ClassicAiDifficulty.Easy);
        SetDifficultyButtonStyle(MediumDifficultyButton, aiDifficulty == ClassicAiDifficulty.Medium);
        SetDifficultyButtonStyle(HardDifficultyButton, aiDifficulty == ClassicAiDifficulty.Hard);
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
        SetStartButtonsEnabled(false);
        try
        {
            var startResult = await freePlayQuota.TryStartMatchAsync();
            if (!startResult.IsAllowed)
            {
                MenuHeading.Text = "NO FREE PLAYS";
                UpdateQuotaDisplay(startResult.Availability);
                return;
            }

            activeFreePlaySession = startResult.Session;
        }
        catch (Exception)
        {
            MenuHeading.Text = "QUOTA UNAVAILABLE";
            SetStartButtonsEnabled(false);
            PurchaseEntryButton.Visibility = Visibility.Collapsed;
            return;
        }

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
        InputSink.Focus(FocusState.Programmatic);
        frameClock.Restart();
        gameTimer.Start();
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
            if (activeFreePlaySession is null)
            {
                throw new InvalidOperationException("The finished match has no quota session.");
            }

            var completion = await freePlayQuota.CompleteMatchAsync(activeFreePlaySession);
            if (completion == FreePlayCompletionStatus.QuotaExhausted)
            {
                ShowMenu("FREE PLAY LIMIT REACHED", allowNewMatch: false);
                await RefreshQuotaDisplayAsync();
                return;
            }

            activeFreePlaySession = null;
            var heading = gameLoop.State.Winner == PlayerSide.Left
                ? "LEFT PLAYER WINS"
                : "RIGHT PLAYER WINS";
            ShowMenu(heading, allowNewMatch: true);
            await RefreshQuotaDisplayAsync();
        }
        catch (Exception)
        {
            ShowMenu("QUOTA SAVE FAILED", allowNewMatch: false);
        }
        finally
        {
            isCompletingMatch = false;
        }
    }

    private void ShowMenu(string heading, bool allowNewMatch)
    {
        MenuHeading.Text = heading;
        StartButton.Content = "PLAY AGAIN";
        SetStartButtonsEnabled(allowNewMatch);
        MenuOverlay.Visibility = Visibility.Visible;
        if (allowNewMatch)
        {
            StartButton.Focus(FocusState.Programmatic);
        }
    }

    private void SetStartButtonsEnabled(bool isEnabled)
    {
        OnePlayerButton.IsEnabled = isEnabled;
        StartButton.IsEnabled = isEnabled;
    }

    private async void MenuOverlay_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshQuotaDisplayAsync();
    }

    private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            PauseMatch();
            return;
        }

        if (MenuOverlay.Visibility == Visibility.Visible)
        {
            await RefreshQuotaDisplayAsync();
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
        catch (Exception)
        {
            QuotaStatusText.Text = $"FREE PLAYS  -- / {freePlayQuota.DailyLimit}";
            QuotaResetText.Text = "RESET TIME UNAVAILABLE";
            SetStartButtonsEnabled(false);
            PurchaseEntryButton.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateQuotaDisplay(FreePlayAvailability availability)
    {
        var resetDate = availability.QuotaDate.AddDays(1);
        QuotaStatusText.Text =
            $"FREE PLAYS  {availability.RemainingPlays} / {freePlayQuota.DailyLimit}";
        QuotaResetText.Text = $"RESET  {resetDate:yyyy-MM-dd}  00:00 UTC";
        var hasFreePlays = availability.RemainingPlays > 0;
        SetStartButtonsEnabled(hasFreePlays);
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

    private void InputSink_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            PauseMatch();
        }
        else if (e.Key == VirtualKey.I)
        {
            ShowInformation();
        }
        else
        {
            SetKeyState(e.Key, true);
        }

        e.Handled = true;
    }

    private void PauseMatch()
    {
        if (!gameTimer.IsRunning || activeFreePlaySession is null || isCompletingMatch)
        {
            return;
        }

        gameTimer.Stop();
        isMatchPaused = true;
        ClearInput();
        PauseOverlay.Visibility = Visibility.Visible;
        ResumeMatchButton.Focus(FocusState.Programmatic);
    }

    private void ResumeMatchButton_Click(object sender, RoutedEventArgs e)
    {
        ResumeMatch();
    }

    private void ResumeMatch()
    {
        if (!isMatchPaused || activeFreePlaySession is null)
        {
            return;
        }

        PauseOverlay.Visibility = Visibility.Collapsed;
        isMatchPaused = false;
        ClearInput();
        frameClock.Restart();
        InputSink.Focus(FocusState.Programmatic);
        gameTimer.Start();
    }

    private async void AbandonMatchButton_Click(object sender, RoutedEventArgs e)
    {
        if (!isMatchPaused || activeFreePlaySession is null)
        {
            return;
        }

        gameTimer.Stop();
        ClearInput();
        activeFreePlaySession = null;
        isMatchPaused = false;
        PauseOverlay.Visibility = Visibility.Collapsed;
        ShowMenu("MATCH ABANDONED", allowNewMatch: false);
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

    private void InputSink_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        SetKeyState(e.Key, false);
        e.Handled = true;
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
        if (MenuOverlay.Visibility == Visibility.Collapsed)
        {
            InputSink.Focus(FocusState.Programmatic);
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