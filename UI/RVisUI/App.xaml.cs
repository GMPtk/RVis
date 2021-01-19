using Ninject;
using RVisUI.Ioc;
using RVisUI.Model;
using System.Windows;

namespace RVisUI
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application
  {
    internal new static App Current => (App)Application.Current;

    internal ViewModelLocator ViewModelLocator => (ViewModelLocator)Resources["Locator"];

    internal IKernel NinjectKernel => ViewModelLocator.NinjectBootstrapper.Kernel;

    internal IAppSettings AppSettings => NinjectKernel.Get<IAppSettings>();

    public IAppState AppState => NinjectKernel.Get<IAppState>();

    internal IAppService AppService => NinjectKernel.Get<IAppService>();

    static App()
    {
    }

    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);
      DoStartup(e);
    }
  }
}
