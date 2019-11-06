using RVisUI.Interop;
using System.Linq;
using System.Windows.Interop;

namespace RVisUI
{
  public partial class App
  {
    public void RunDialog<T>(object viewModel = null) where T : System.Windows.Window, new()
    {
      var dialog = new T();
      if (null != viewModel)
      {
        dialog.DataContext = viewModel;
      }
      dialog.Owner = MainWindow;
      dialog.ShowDialog();
    }

    public System.Windows.Window GetActiveWindow()
    {
      var window = SafeNativeMethods.GetActiveApplicationWindow();
      if (null == window)
      {
        window = this.Windows.OfType<System.Windows.Window>().SingleOrDefault(x => x.IsActive);
      }
      return window;
    }
  }
}
