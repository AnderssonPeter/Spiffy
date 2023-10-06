using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.Graphics.Dwm;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32.System.Threading;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI;
using System.Drawing;
using System.Drawing.Imaging;
using ABI.Windows.ApplicationModel.Activation;
using System.Threading;
using Windows.Win32.Storage.Packaging.Appx;
using Windows.Management.Deployment;
using Windows.ApplicationModel;
using System.Xml;
using System.Linq;
using Windows.Networking.Sockets;
using WinRT;
using System.Text.RegularExpressions;
using ColorCode.Compilation.Languages;
using static System.Net.Mime.MediaTypeNames;
using System.Xml.Linq;

namespace Spiffy;

public class LightSpiffyWindow
{
    public HWND Handle { get; }
    public bool IsAltTabWindow { get; }
    public uint ProcessId { get; }
    public uint ThreadId { get; }
    public LightSpiffyWindow(HWND handle)
    {
        Handle = handle;
        //todo: for some reason we get the incorrect ProcessId some store apps, this in turn gives us a broken icon (Example is the store app and Microsoft To Do)
        (ProcessId, ThreadId) = Handle.GetProcessAndThreadId();
        IsAltTabWindow = GetIsAltTabWindow();
    }

    public LightSpiffyWindow(LightSpiffyWindow source)
    {
        Handle = source.Handle;
        ProcessId = source.ProcessId;
        IsAltTabWindow = source.IsAltTabWindow;
    }

    public DisplayArea GetDisplayArea()
    {
        var activeWindowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(Handle);
        return DisplayArea.GetFromWindowId(activeWindowId, DisplayAreaFallback.Nearest);
    }

    public static LightSpiffyWindow GetForegroundWindow()
    {
        return new LightSpiffyWindow(PInvoke.GetForegroundWindow());
    }

    private bool GetIsAltTabWindow()
    {
        if (!PInvoke.IsWindowVisible(Handle))
        {
            return false;
        }
        var windowInformation = Handle.GetWindowInfo();
        if ((windowInformation.dwExStyle & WINDOW_EX_STYLE.WS_EX_TOOLWINDOW) != 0)
        {
            return false;
        }

        if (Handle.IsCloaked() != 0)
        {
            return false;
        }
        if (PInvoke.GetAncestor(Handle, GET_ANCESTOR_FLAGS.GA_ROOT) != Handle)
        {
            return false;
        }
        if (this.ProcessId == Environment.ProcessId)
        {
            return false;
        }
        var walkHandle = PInvoke.GetAncestor(Handle, GET_ANCESTOR_FLAGS.GA_ROOTOWNER);
        HWND tryPointer;
        while ((tryPointer = PInvoke.GetLastActivePopup(walkHandle)) != tryPointer)
        {
            if (PInvoke.IsWindowVisible(tryPointer))
            {
                break;
            }
            walkHandle = tryPointer;
        }

        return walkHandle == Handle;
    }

}
public class SpiffyWindow : LightSpiffyWindow
{
    public string Title { get; }
    public string ClassName { get; }
    public BitmapImage Icon { get; }
    public string? ProcessPath { get; }
    public string? ProcessName { get; }

    public SpiffyWindow(LightSpiffyWindow lightSpiffyWindow) : base(lightSpiffyWindow)
    {
        try
        {
            ProcessPath = ProcessId.GetExecutablePath();
            ProcessName = Path.GetFileName(ProcessPath);

        }
        catch (Win32Exception)
        {
            //We failed to get ProcessPath!
        }
        Title = Handle.GetWindowTitle();
        ClassName = Handle.GetClassName();
        Icon = GetIcon();
    }

    public static List<SpiffyWindow> GetAll()
    {
        List<SpiffyWindow> windows = new List<SpiffyWindow>();
        var lShellWindow = PInvoke.GetShellWindow();
        PInvoke.EnumWindows((hWnd, lParam) =>
        {
            if (hWnd == lShellWindow)
                return true;

            var lightSpíffyWindow = new LightSpiffyWindow(hWnd);
            if (lightSpíffyWindow.IsAltTabWindow)
            {
                windows.Add(new SpiffyWindow(lightSpíffyWindow));
            }
            return true;
        }, 0);

        return windows;
    }

    public void Activate()
    {
        if (PInvoke.IsIconic(Handle).Value != 0)
        {
            PInvoke.ShowWindow(Handle, SHOW_WINDOW_CMD.SW_RESTORE);
        }
        Handle.ActivateWindow();
    }

    public override string ToString() => Title;

    private BitmapImage GetIcon()
    {
        BitmapImage FromIconToBitmap(Icon icon)
        {
            using (var bitmap = icon.ToBitmap())
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png);
                stream.Seek(0, SeekOrigin.Begin);
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.SetSource(stream.AsRandomAccessStream());
                return bitmapImage;
            }
        }
        var iconHandle = Handle.GetWindowIcon();
        if (iconHandle != IntPtr.Zero)
        {
            using (var icon = System.Drawing.Icon.FromHandle(iconHandle))
            {
                return FromIconToBitmap(icon);
            }
            //return Microsoft.UI.Xaml.Media.Imaging.BitmapImage.FromAbi(iconHandle);            
        }

        //Check if its a appx package
        var packageFullName = ProcessId.GetPackageFullName();
        if (!string.IsNullOrEmpty(packageFullName))
        {
            const uint PACKAGE_FILTER_HEAD = 0x00000010;
            unsafe
            {
                _PACKAGE_INFO_REFERENCE* pointer;
                var error = PInvoke.OpenPackageInfoByFullName(packageFullName, out pointer);
                if (error != WIN32_ERROR.NO_ERROR)
                {
                    throw new Win32Exception((int)error);
                }

                uint length = (uint)sizeof(PACKAGE_INFO) * 16;
                uint count = 0;

                Span<PACKAGE_INFO> buffer = stackalloc PACKAGE_INFO[(int)length];
                fixed (PACKAGE_INFO* bufferPointer = buffer)
                {
                    error = PInvoke.GetPackageInfo(pointer, PACKAGE_FILTER_HEAD, &length, (byte*)bufferPointer, &count);
                    if (error != WIN32_ERROR.NO_ERROR)
                    {
                        throw new Win32Exception((int)error);
                    }
                }
                for (var i = 0; i < count; i++)
                {
                    var path = Marshal.PtrToStringUni(buffer[i].path);
                    var manifestPath = System.IO.Path.Combine(path, "AppXManifest.xml");
                    var document = new XmlDocument();
                    document.Load(manifestPath);
                    var names = new XmlNamespaceManager(document.NameTable);
                    names.AddNamespace("x", document.DocumentElement.NamespaceURI);

                    var node = document.SelectSingleNode(@"/x:Package/x:Properties/x:Logo", names);
                    if (node != null)
                    {
                        var name = System.IO.Path.GetFileNameWithoutExtension(node.InnerText);
                        var extension = System.IO.Path.GetExtension(node.InnerText);
                        var file = Directory.EnumerateFiles(Path.Combine(path, Path.GetDirectoryName(node.InnerText)), name + ".scale-*" + extension)
                            .OrderByDescending(f => int.Parse(Regex.Match(f, @"scale-(\d+)").Groups[1].Value))
                            .FirstOrDefault();
                        if (file != null)
                        {
                            using (var stream = File.OpenRead(file))
                            {
                                BitmapImage bitmapImage = new BitmapImage();
                                bitmapImage.SetSource(stream.AsRandomAccessStream());
                                return bitmapImage;
                            }
                        }
                    }
                }

            }
        }

        using (var icon = System.Drawing.Icon.ExtractAssociatedIcon(ProcessPath))
        {
            return FromIconToBitmap(icon);
        }
    }

}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct PACKAGE_INFO
{
    public int reserved;
    public int flags;
    public IntPtr path;
    public IntPtr packageFullName;
    public IntPtr packageFamilyName;
    public PACKAGE_ID packageId;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct PACKAGE_ID
{
    public int reserved;
    public AppxPackageArchitecture processorArchitecture;
    public ushort VersionRevision;
    public ushort VersionBuild;
    public ushort VersionMinor;
    public ushort VersionMajor;
    public IntPtr name;
    public IntPtr publisher;
    public IntPtr resourceId;
    public IntPtr publisherId;
}

internal enum AppxPackageArchitecture
{
    x86 = 0,
    Arm = 5,
    x64 = 9,
    Neutral = 11,
    Arm64 = 12
}