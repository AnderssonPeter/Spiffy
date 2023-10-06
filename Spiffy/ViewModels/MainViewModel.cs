using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Spiffy.Messages;

namespace Spiffy.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    public AdvancedCollectionView Windows { get; }

    [ObservableProperty]
    private string searchText = string.Empty;
    private readonly IMessenger messenger;

    public MainViewModel(IMessenger messenger)
    {
        var windows = SpiffyWindow.GetAll();
        Windows = new AdvancedCollectionView(windows, true);
        Windows.Filter = w => Filter((SpiffyWindow)w, SearchText);
        //SelectedWindow = windows.FirstOrDefault();
        //todo: sort by last active but place current active last
        Windows.CurrentItem = windows.FirstOrDefault();
        Windows.CurrentChanged += Windows_CurrentChanged;
        this.messenger = messenger;
    }

    private void Windows_CurrentChanged(object? sender, object e)
    {
        Debug.WriteLine("Windows_CurrentChanged " + ((SpiffyWindow?)Windows.CurrentItem)?.Title);
    }

    private static bool Filter(SpiffyWindow spiffyWindow, string text) =>
        CompareString(text, spiffyWindow.Title) ||
        CompareString(text, spiffyWindow.ProcessName);

    static bool CompareString(string part, string? full) => 
        full != null && full.Contains(part, StringComparison.OrdinalIgnoreCase);

    partial void OnSearchTextChanged(string value)
    {
        Debug.WriteLine(nameof(OnSearchTextChanged));
        Windows.RefreshFilter();
        Windows.CurrentItem = Windows.FirstOrDefault();
    }

    public bool CanActivateWindow(SpiffyWindow? spiffyWindow)
    {
        return spiffyWindow != null;
    }

    [RelayCommand(CanExecute = nameof(CanActivateWindow))]
    public void ActivateWindow(SpiffyWindow window)
    {
        window.Activate();
        messenger.Send<HideMainWindowMessage>();
    }

    [RelayCommand]
    public void ShowSettings()
    {
        messenger.Send<ShowSettingsMessage>();
    }
}
