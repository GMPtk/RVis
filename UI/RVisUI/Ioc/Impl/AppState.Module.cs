using LanguageExt;
using Ninject;
using Ninject.Extensions.Conventions;
using ReactiveUI;
using RVis.Base.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.IO;
using System.Linq;
using static System.Array;
using static System.IO.Path;
using static System.Reflection.Assembly;

namespace RVisUI.Ioc
{
  public partial class AppState
  {
    public string? ExtraModulePath
    {
      get => _extraModulePath;
      set => this.RaiseAndSetIfChanged(ref _extraModulePath, value, PropertyChanged);
    }
    private string? _extraModulePath;

    public Arr<IRVisExtensibility> GetServices(bool rebind)
    {
      if (!rebind)
      {
        var services = App.Current.NinjectKernel.GetAll<IRVisExtensibility>().ToArr();
        if (!services.IsEmpty) return services;
      }

      App.Current.NinjectKernel.Unbind<IRVisExtensibility>();

      var pathToApp = GetExecutingAssembly().GetDirectory();
      var pathToModules = Combine(pathToApp, "module");
      var directories = Directory.Exists(pathToModules) ?
        Directory.GetDirectories(pathToModules) :
        Empty<string>();

      if (!directories.Any()) App.Current.Log.Info($"No modules found in {pathToModules}");

      //AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
      //{
      //  try
      //  {
      //    var assemblyName = new System.Reflection.AssemblyName(e.Name);
      //    var assemblyPath = Combine(pathToApp, "bin", $"{assemblyName.Name}.dll");
      //    return File.Exists(assemblyPath) ? System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath) : default;
      //  }
      //  catch
      //  {
      //    App.Current.Log.Error($"{e.RequestingAssembly?.FullName} wants {e.Name}");
      //  }
      //  return default;
      //};

      foreach (var directory in directories) DoDirectoryBind(directory);

      if (ExtraModulePath.IsAString())
      {
        var extraModulePaths = ExtraModulePath
          .Split(';')
          .Select(emp => emp.Trim());

        foreach (var extraModulePath in extraModulePaths)
        {
          if (File.Exists(extraModulePath))
          {
            DoFileBind(extraModulePath);
          }
          else if (Directory.Exists(extraModulePath))
          {
            DoDirectoryBind(extraModulePath);
          }
        }
      }

      return App.Current.NinjectKernel.GetAll<IRVisExtensibility>().ToArr();
    }

    private static void DoFileBind(string file)
    {
      try
      {
        App.Current.NinjectKernel.Bind(x => x
          .From(file)
          .SelectAllClasses()
          .InheritedFrom<IRVisExtensibility>()
          .BindAllInterfaces()
          .Configure(b => b.InTransientScope())
          );
      }
      catch (Exception ex)
      {
        App.Current.Log.Error(ex, $"Failed to load module from {file}");
      }
    }

    private static void DoDirectoryBind(string directory)
    {
      try
      {
        App.Current.NinjectKernel.Bind(x => x
          .FromAssembliesInPath(directory)
          .SelectAllClasses()
          .InheritedFrom<IRVisExtensibility>()
          .BindAllInterfaces()
          .Configure(b => b.InTransientScope())
          );
      }
      catch (Exception ex)
      {
        App.Current.Log.Error(ex, $"Failed to load module in {directory}");
      }
    }

    private void ObserveModuleConfiguration(object _)
    {
      CreateUIComponents(ActiveViewModel);
    }
  }
}
