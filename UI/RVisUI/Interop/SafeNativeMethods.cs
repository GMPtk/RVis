using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Interop;

namespace RVisUI.Interop
{
  [SuppressUnmanagedCodeSecurity]
  internal static class SafeNativeMethods
  {
    [DllImport("user32.dll")]
    static extern IntPtr GetActiveWindow();

    internal static System.Windows.Window GetActiveApplicationWindow()
    {
      IntPtr active = GetActiveWindow();

      return App.Current.Windows.OfType<System.Windows.Window>()
          .SingleOrDefault(window => new WindowInteropHelper(window).Handle == active);
    }

    internal enum TaskDialogResult
    {
      Ok = 1,
      Cancel = 2,
      Retry = 4,
      Yes = 6,
      No = 7,
      Close = 8
    }

    [Flags]
    internal enum TaskDialogButtons
    {
      Ok = 0x0001,
      Yes = 0x0002,
      No = 0x0004,
      Cancel = 0x0008,
      Retry = 0x0010,
      Close = 0x0020
    }

    internal enum TaskDialogIcon
    {
      Warning = 65535,
      Error = 65534,
      Information = 65533,
      Shield = 65532
    }

    //[DllImport("comctl32.dll", CharSet = CharSet.Unicode, EntryPoint = "TaskDialog")]
    //internal static extern int TaskDialog(
    //  IntPtr hWndParent,
    //  IntPtr hInstance,
    //  String pszWindowTitle,
    //  String pszMainInstruction,
    //  String pszContent,
    //  int dwCommonButtons,
    //  IntPtr pszIcon,
    //  out int pnButton
    //  );
  }
}
