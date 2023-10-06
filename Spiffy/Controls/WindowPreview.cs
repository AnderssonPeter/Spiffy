using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Spiffy.Views;
using Windows.Foundation;
using Windows.Win32;

namespace Spiffy.Controls;
internal class WindowPreview : Control
{
    private static uint counter;
    private uint id = Interlocked.Increment(ref counter);
    private nint thumbnailId;

    private static readonly DependencyProperty WindowProperty = DependencyProperty.Register(nameof(Window), typeof(SpiffyWindow), typeof(WindowPreview), new PropertyMetadata(null, new PropertyChangedCallback(OnWindowChangedCallback)));

    public SpiffyWindow Window
    {
        get => (SpiffyWindow)GetValue(WindowProperty);
        set => SetValue(WindowProperty, value);
    }

    public WindowPreview()
    {
        DefaultStyleKey = typeof(WindowPreview);
        SizeChanged += WindowPreview_SizeChanged;
        EffectiveViewportChanged += WindowPreview_EffectiveViewportChanged;
        this.Unloaded += WindowPreview_Unloaded;
        this.Loaded += WindowPreview_Loaded;
    }

    private void WindowPreview_Loaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine(id + " WindowPreview_Unloaded");
        if (Window != null && thumbnailId == 0)
        {
            SetupDisplay();
        }
    }

    private void WindowPreview_Unloaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine(id + " WindowPreview_Unloaded");
        DestroyDisplay();
    }

    private static void OnWindowChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var windowPreview = (WindowPreview)sender;
        windowPreview.OnWindowChanged();
    }

    private void OnWindowChanged()
    {
        Debug.WriteLine(id + " OnWindowChanged");
        DestroyDisplay();
        SetupDisplay();
        UpdateDisplay();
    }

    private void SetupDisplay()
    {
        Debug.WriteLine(id + " SetupDisplay");
        if (thumbnailId != 0)
        {
            throw new UnreachableException();
        }
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        PInvoke.DwmRegisterThumbnail(new Windows.Win32.Foundation.HWND(handle), Window.Handle, out thumbnailId).ThrowOnFailure();
    }

    private void UpdateDisplay()
    {
        if (thumbnailId == 0)
        {
            throw new UnreachableException();
        }
        try
        {
            var transform = this.TransformToVisual(App.MainWindow.Content);
            var coordinate = transform.TransformPoint(new Point(0, 0));
            Debug.WriteLine(id + $" UpdateDisplay({thumbnailId}, {coordinate.X}, {coordinate.Y})");
            //todo: keep the aspect ratio
            //todo: when negative values cut part of the source region or outside of the window
            PInvoke.DwmUpdateThumbnailProperties(thumbnailId, new Windows.Win32.Graphics.Dwm.DWM_THUMBNAIL_PROPERTIES
            {
                dwFlags = (uint)DWM_FLAGS.DWM_TNP_VISIBLE | (uint)DWM_FLAGS.DWM_TNP_RECTDESTINATION,
                fVisible = true,
                rcDestination = new Windows.Win32.Foundation.RECT
                {
                    left = (int)coordinate.X,
                    top = (int)coordinate.Y,
                    right = (int)coordinate.X + (int)ActualWidth,
                    bottom = (int)coordinate.Y + (int)ActualHeight
                }
            });
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private void DestroyDisplay()
    {
        Debug.WriteLine(id + " DestroyDisplay");
        if (thumbnailId != 0)
        {
            PInvoke.DwmUnregisterThumbnail(thumbnailId).ThrowOnFailure();
            thumbnailId = 0;
        }
    }

    private void WindowPreview_EffectiveViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
    {
        Debug.WriteLine(id + " EffectiveViewportChanged");
        if (thumbnailId != 0)
        {
            UpdateDisplay();
        }
    }

    private void WindowPreview_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        Debug.WriteLine(id + " SizeChanged");
        if (thumbnailId != 0)
        {
            UpdateDisplay();
        }
    }

    [Flags]
    enum DWM_FLAGS : uint
    {
        /// <summary>
        /// A value for the rcDestination member has been specified.
        /// </summary>
        DWM_TNP_RECTDESTINATION = 0x00000001,
        /// <summary>
        /// A value for the rcSource member has been specified
        /// </summary>
        DWM_TNP_RECTSOURCE = 0x00000002,
        /// <summary>
        /// A value for the opacity member has been specified.
        /// </summary>
        DWM_TNP_OPACITY = 0x00000004,
        /// <summary>
        /// A value for the fVisible member has been specified.
        /// </summary>
        DWM_TNP_VISIBLE = 0x00000008,
        /// <summary>
        /// A value for the fSourceClientAreaOnly member has been specified.
        /// </summary>
        DWM_TNP_SOURCECLIENTAREAONLY = 0x00000010
    }
}
