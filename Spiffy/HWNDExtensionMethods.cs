using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32.Foundation;
using Windows.Win32;
using System.Runtime.InteropServices;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;
using System.ComponentModel;
using System.Diagnostics;
using Windows.Win32.System.Threading;

namespace Spiffy;
internal static class HWNDExtensionMethods
{
    public const int WM_GETICON = 0x7F;
    public const int ICON_SMALL = 0;
    public const int ICON_BIG = 1;
    public const int ICON_SMALL2 = 2;
    public static unsafe string GetWindowTitle(this HWND hWnd, int maxLength = 256)
    {
        Span<char> text = stackalloc char[maxLength];
        fixed (char* textPointer = text)
        {
            var length = PInvoke.GetWindowText(hWnd, textPointer, maxLength);
            return text[..length].ToString();
        }
    }


    public static unsafe string GetClassName(this HWND hWnd, int maxLength = 256)
    {
        Span<char> text = stackalloc char[maxLength];
        fixed (char* textPointer = text)
        {
            var length = PInvoke.GetClassName(hWnd, textPointer, maxLength);
            return text[..length].ToString();
        }
    }

    public static unsafe uint IsCloaked(this HWND hWnd)
    {

        var valuePointer = Marshal.AllocHGlobal(sizeof(uint));
        try
        {
            PInvoke.DwmGetWindowAttribute(
                hWnd,
                DWMWINDOWATTRIBUTE.DWMWA_CLOAKED,
                valuePointer.ToPointer(),
                (uint)sizeof(uint)).ThrowOnFailure();
            return Marshal.PtrToStructure<uint>(valuePointer);
        }
        finally
        {
            Marshal.FreeHGlobal(valuePointer);
        }
    }

    public static unsafe (uint ProcessId, uint ThreadId) GetProcessAndThreadId(this HWND hWnd)
    {
        uint value = 0;

        uint* ptr1 = &value;
        var threadId = PInvoke.GetWindowThreadProcessId(hWnd, ptr1);

        return (value, threadId);
    }

    public static WINDOWINFO GetWindowInfo(this HWND hWnd)
    {
        WINDOWINFO info = new WINDOWINFO();
        info.cbSize = (uint)Marshal.SizeOf(info);
        if (!PInvoke.GetWindowInfo(hWnd, ref info))
        {
            throw new InvalidOperationException("Failed to get window information");
        }
        return info;
    }


    public static void ActivateWindow(this HWND hWnd)
    {
        //Todo: does not work if window is minimized
        var result = PInvoke.SetForegroundWindow(hWnd);
        if (result.Value == 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public static IntPtr GetWindowIcon(this HWND hWnd)
    {
        var iconHandle = PInvoke.SendMessage(hWnd, WM_GETICON, ICON_BIG, 0);
        if (iconHandle == IntPtr.Zero)
        {
            iconHandle = PInvoke.SendMessage(hWnd, WM_GETICON, ICON_SMALL, 0);
        }
        if (iconHandle == IntPtr.Zero)
        {
            iconHandle = PInvoke.SendMessage(hWnd, WM_GETICON, ICON_SMALL2, 0);
        }

        return iconHandle;
    }

    //Todo: Refactor this it weird that its on a uint!!
    public static unsafe string GetExecutablePath(this uint processId)
    {
        uint length = 1024;
        using var processHandle = PInvoke.OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
        if (processHandle.IsInvalid)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        Span<char> text = stackalloc char[(int)length];
        fixed (char* textPointer = text)
        {
            PInvoke.QueryFullProcessImageName(processHandle, PROCESS_NAME_FORMAT.PROCESS_NAME_WIN32, textPointer, ref length);
            return text[..(int)length].ToString();
        }
    }

    public static unsafe string? GetPackageFullName(this uint processId)
    {
        uint length = 1024;
        using var processHandle = PInvoke.OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
        if (processHandle.IsInvalid)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        Span<char> text = stackalloc char[(int)length];
        fixed (char* textPointer = text)
        {
            var error = PInvoke.GetPackageFullName(processHandle, ref length, textPointer);
            if (error == WIN32_ERROR.APPMODEL_ERROR_NO_PACKAGE)
            {
                return null;
            }
            if (error != WIN32_ERROR.NO_ERROR)
            {
                throw new Win32Exception((int)error);
            }
            return text[..(int)length].ToString();
        }
    }
}

