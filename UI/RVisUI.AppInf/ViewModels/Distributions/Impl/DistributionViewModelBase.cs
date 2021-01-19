using OxyPlot;
using OxyPlot.Annotations;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.Reactive.Disposables;
using static System.Double;

namespace RVisUI.AppInf
{
  public abstract class DistributionViewModelBase
  {
    internal DistributionViewModelBase(IAppService appService, IAppSettings appSettings)
    {
      _appService = appService;
      _appSettings = appSettings;

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(

        _appSettings
          .GetWhenPropertyChanged()
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<string?>(ObserveAppSettingsPropertyChange)
            )

        );
    }

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

    protected private static void AddLineAnnotation(double? bound, string text, PlotModel plotModel)
    {
      if (bound.HasValue)
      {
        var lineAnnotation = new LineAnnotation
        {
          Type = LineAnnotationType.Vertical,
          X = bound.Value,
          Color = OxyColors.DimGray,
          Text = text,
          FontSize = 8,
          LineStyle = LineStyle.Dash,
          StrokeThickness = 1.6
        };

        plotModel.Annotations.Add(lineAnnotation);
      }
    }

    private void ObserveAppSettingsPropertyChange(string? propertyName)
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
