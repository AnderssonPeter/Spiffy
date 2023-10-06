using Microsoft.UI.Xaml;
using Spiffy.Helpers;

using Windows.UI.ViewManagement;

namespace Spiffy;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.Title = "Spiffy";
    }
}
