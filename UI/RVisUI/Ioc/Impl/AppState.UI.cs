using LanguageExt;
using ReactiveUI;
using RVis.Base.Extensions;
using RVisUI.Controls.Views;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using RVisUI.Mvvm;
using System;
using System.Linq;
using static LanguageExt.Prelude;
using static RVis.Base.Extensions.NumExt;
using static RVisUI.Model.ModuleInfo;

namespace RVisUI.Ioc
{
  public partial class AppState
  {
    public object? ActiveViewModel
    {
      get => _activeViewModel;
      set => this.RaiseAndSetIfChanged(ref _activeViewModel, value, PropertyChanged);
    }
    private object? _activeViewModel;

    public Arr<(string ID, string DisplayName, string? DisplayIcon, object View, object ViewModel)> UIComponents
    {
      get => _uiComponents;
      set => this.RaiseAndSetIfChanged(ref _uiComponents, value, PropertyChanged);
    }
    private Arr<(string ID, string DisplayName, string? DisplayIcon, object View, object ViewModel)> _uiComponents;

    public (string ID, string DisplayName, string? DisplayIcon, object View, object ViewModel) ActiveUIComponent
    {
      get => _activeUIComponent;
      set => this.RaiseAndSetIfChanged(ref _activeUIComponent, value, PropertyChanged);
    }
    private (string ID, string DisplayName, string? DisplayIcon, object View, object ViewModel) _activeUIComponent;

    private Either<Exception, (string ID, string DisplayName, string? DisplayIcon, object View, object ViewModel)> CreateUIComponent(ModuleInfo mi)
    {
      var missingPackages = mi.RequiredRPackageNames.Filter(n => !InstalledRPackages.Exists(p => p.Package == n));

      if (missingPackages.IsEmpty)
      {
        try
        {
          return (
            mi.ID,
            mi.DisplayName,
            mi.DisplayIcon,
            View: mi.Service.GetView(),
            ViewModel: mi.Service.GetViewModel()
            );
        }
        catch (Exception ex)
        {
          return ex;
        }
      }

      return (
        mi.ID,
        mi.DisplayName,
        mi.DisplayIcon,
        View: new ModuleNotSupportedView(),
        ViewModel: new ModuleNotSupportedViewModel(mi.DisplayName, missingPackages)
        );
    }

    private void CreateUIComponents(object? viewModel)
    {
      var existingUIComponents = UIComponents;
      Arr<(string ID, string DisplayName, string? DisplayIcon, object View, object ViewModel)> updatedUIComponents = default;

      if (viewModel is ISimulationHomeViewModel)
      {
        var services = GetServices(rebind: true);
        var moduleInfos = GetModuleInfos(services);
        moduleInfos = SortAndEnable(moduleInfos, _appSettings.ModuleConfiguration);
        moduleInfos = moduleInfos.Filter(mi => mi.IsEnabled);

        var updatedUIComponentTries = moduleInfos
          .Map(
            mi => existingUIComponents
              .Find(c => c.ID == mi.ID)
              .Match(c => c, CreateUIComponent(mi))
          );

        var errors = lefts(updatedUIComponentTries).ToArr();
        var components = rights(updatedUIComponentTries).ToArr();

        if (!errors.IsEmpty)
        {
          components
            .Map(c => c.ViewModel)
            .OfType<IDisposable>()
            .Iter(d => d.Dispose());

          throw new AggregateException("One or more modules failed to load UI components", errors);
        }

        updatedUIComponents = components;
      }

      var activeID = ActiveUIComponent.ID;
      ActiveUIComponent = default;

      existingUIComponents
        .Filter(c => !updatedUIComponents.Exists(d => d.ID == c.ID))
        .OfType<IDisposable>()
        .Iter(d => d.Dispose());

      UIComponents = updatedUIComponents;

      var uiComponentIndex = updatedUIComponents.FindIndex(uic => uic.ID == activeID);
      ActiveUIComponent = uiComponentIndex.IsFound()
        ? updatedUIComponents[uiComponentIndex]
        : !updatedUIComponents.IsEmpty
          ? updatedUIComponents.Head()
          : default;
    }

    private void DisposeUIComponents()
    {
      var uiComponents = UIComponents;
      UIComponents = default;
      uiComponents.Map(c => c.ViewModel)
        .OfType<IDisposable>()
        .Iter(d => d.Dispose());
    }
  }
}
