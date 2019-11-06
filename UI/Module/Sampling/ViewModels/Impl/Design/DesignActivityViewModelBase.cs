using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.Reactive.Disposables;

namespace Sampling
{
  internal abstract class DesignActivityViewModelBase
  {
    protected DesignActivityViewModelBase(string noActivityMessage) =>
      NoActivityMessage = noActivityMessage;

    protected DesignActivityViewModelBase(IAppService appService, IAppSettings appSettings, string noActivityMessage)
      : this(noActivityMessage)
    {
      _appService = appService;
      _appSettings = appSettings;

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(

        _appSettings
          .GetWhenPropertyChanged()
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<string>(ObserveAppSettingsPropertyChange)
            )

        );
    }

    public string NoActivityMessage { get; }

    public void Dispose() => Dispose(true);

    protected virtual void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          _subscriptions.Dispose();
        }

        _disposed = true;
      }
    }

    protected virtual void ObserveThemeChange()
    {

    }

    private void ObserveAppSettingsPropertyChange(string propertyName)
    {
      if (propertyName.IsThemeProperty()) ObserveThemeChange();
    }

    private protected readonly IAppService _appService;
    private protected readonly IAppSettings _appSettings;
    private protected readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private bool _disposed = false;
  }
}
