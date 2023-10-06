using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Spiffy.Activation;
using Spiffy.Contracts.Services;
using Spiffy.Views;

namespace Spiffy.Services;

public class ActivationService : IActivationService
{
    private readonly ActivationHandler<LaunchActivatedEventArgs> _defaultHandler;
    private readonly IEnumerable<IActivationHandler> _activationHandlers;
    private readonly IThemeSelectorService _themeSelectorService;
    private UIElement? _shell = null;

    public ActivationService(ActivationHandler<LaunchActivatedEventArgs> defaultHandler, IEnumerable<IActivationHandler> activationHandlers, IThemeSelectorService themeSelectorService)
    {
        _defaultHandler = defaultHandler;
        _activationHandlers = activationHandlers;
        _themeSelectorService = themeSelectorService;
    }

    public async Task ActivateAsync(object activationArgs)
    {
        // Execute tasks before activation.
        await InitializeAsync();

        // Set the MainWindow Content.
        if (App.MainWindow.Content == null)
        {
            App.MainWindow.Content = _shell ?? new Frame();
        }

        // Handle activation via ActivationHandlers.
        await HandleActivationAsync(activationArgs);

        // Set window as borderless
        if (App.MainWindow.AppWindow.Presenter is OverlappedPresenter p)
        {
            p.SetBorderAndTitleBar(false, false);
            p.IsResizable = false;
        }

        // Center window on the screen where the current active window is
        var activeWindow = SpiffyWindow.GetForegroundWindow();
        var activeDisplayArea = activeWindow.GetDisplayArea();

        App.MainWindow.AppWindow.Resize(new Windows.Graphics.SizeInt32((int)(activeDisplayArea.WorkArea.Width * 0.75), (int)(activeDisplayArea.WorkArea.Height * 0.75)));
        var centeredPosition = App.MainWindow.AppWindow.Position;
        centeredPosition.X = activeDisplayArea.WorkArea.X + ((activeDisplayArea.WorkArea.Width - App.MainWindow.AppWindow.Size.Width) / 2);
        centeredPosition.Y = activeDisplayArea.WorkArea.Y + ((activeDisplayArea.WorkArea.Height - App.MainWindow.AppWindow.Size.Height) / 2);
        App.MainWindow.AppWindow.Move(centeredPosition);

        // Activate the MainWindow.
        App.MainWindow.Activate();

        // Execute tasks after activation.
        await StartupAsync();
    }

    private async Task HandleActivationAsync(object activationArgs)
    {
        var activationHandler = _activationHandlers.FirstOrDefault(h => h.CanHandle(activationArgs));

        if (activationHandler != null)
        {
            await activationHandler.HandleAsync(activationArgs);
        }

        if (_defaultHandler.CanHandle(activationArgs))
        {
            await _defaultHandler.HandleAsync(activationArgs);
        }
    }

    private async Task InitializeAsync()
    {
        await _themeSelectorService.InitializeAsync().ConfigureAwait(false);
    }

    private async Task StartupAsync()
    {
        await _themeSelectorService.SetRequestedThemeAsync();
    }
}
