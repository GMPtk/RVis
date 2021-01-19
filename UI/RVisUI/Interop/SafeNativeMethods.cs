using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Interop;

namespace RVisUI.Interop
{
  [SuppressUnmanagedCodeSecurity]
  internal static class SafeNativeMethods
  {
    [DllImport("user32.dll")]
    static extern IntPtr GetActiveWindow();

    internal static Window? GetActiveApplicationWindow()
    {
      IntPtr active = GetActiveWindow();

      return App.Current.Windows
        .OfType<Window>()
        .SingleOrDefault(window => new WindowInteropHelper(window).Handle == active);
    }
  }
}
