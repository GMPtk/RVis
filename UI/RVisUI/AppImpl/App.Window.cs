using RVisUI.Interop;
using System.Linq;
using System.Windows;

namespace RVisUI
{
  public partial class App
  {
    public void RunDialog<T>(object? viewModel = null) where T : Window, new()
    {
      var dialog = new T();
      if (null != viewModel)
      {
        dialog.DataContext = viewModel;
      }
      dialog.Owner = MainWindow;
      dialog.ShowDialog();
    }

    public Window? GetActiveWindow()
    {
      var window = SafeNativeMethods.GetActiveApplicationWindow();
      if (null == window)
      {
        window = Windows
          .OfType<Window>()
          .SingleOrDefault(x => x.IsActive);
      }
      return window;
    }
  }
}
