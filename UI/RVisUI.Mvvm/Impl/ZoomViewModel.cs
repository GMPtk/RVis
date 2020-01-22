using ReactiveUI;
using RVisUI.Model;
using System;
using System.Reactive.Disposables;
using System.Windows.Input;
using static System.Globalization.CultureInfo;

namespace RVisUI.Mvvm
{
  public sealed class ZoomViewModel : ReactiveObject, IZoomViewModel, IDisposable
  {
    public ZoomViewModel(IAppService appService, IAppSettings appSettings)
    {
      _appSettings = appSettings;

      Open = ReactiveCommand.Create(HandleOpen);
      Shrink = ReactiveCommand.Create(
        HandleShrink,
        this.ObservableForProperty(vm => vm.CanShrink, _ => CanShrink)
        );
      Enlarge = ReactiveCommand.Create(
        HandleEnlarge,
        this.ObservableForProperty(vm => vm.CanEnlarge, _ => CanEnlarge)
        );
      Reset = ReactiveCommand.Create(HandleReset);

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(

        _appSettings
          .ObservableForProperty(@as => @as.Zoom)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveAppSettingsZoom
              )
            ),

        this
          .ObservableForProperty(vm => vm.Zoom)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveZoom
              )
            )

        );

      _zoom = _appSettings.Zoom;
      _percentZoom = _zoom.ToString("P0", InvariantCulture);

      PopulatePercentZoom();
      UpdateEnable();
    }

    public ICommand Open { get; }

    public bool IsOpen
    {
      get => _isOpen;
      set => this.RaiseAndSetIfChanged(ref _isOpen, value);
    }
    private bool _isOpen;

    public ICommand Shrink { get; }

    public bool CanShrink
    {
      get => _canShrink;
      set => this.RaiseAndSetIfChanged(ref _canShrink, value);
    }
    private bool _canShrink;

    public ICommand Enlarge { get; }

    public bool CanEnlarge
    {
      get => _canEnlarge;
      set => this.RaiseAndSetIfChanged(ref _canEnlarge, value);
    }
    private bool _canEnlarge;

    public double MinZoom => 0.3;

    public double MaxZoom => 2d;

    public double Zoom
    {
      get => _zoom;
      set => this.RaiseAndSetIfChanged(ref _zoom, value);
    }
    private double _zoom;

    public string PercentZoom
    {
      get => _percentZoom;
      set => this.RaiseAndSetIfChanged(ref _percentZoom, value);
    }
    private string _percentZoom;

    public ICommand Reset { get; }

    public void Dispose() =>
      Dispose(true);

    private void HandleOpen()
    {
      IsOpen = !IsOpen;
    }

    private void HandleShrink()
    {
      Zoom -= 0.1;
    }

    private void HandleEnlarge()
    {
      Zoom += 0.1;
    }

    private void HandleReset()
    {
      Zoom = 1d;
    }

    private void ObserveAppSettingsZoom(object _)
    {
      Zoom = _appSettings.Zoom;
      PopulatePercentZoom();
      UpdateEnable();
    }

    private void ObserveZoom(object _)
    {
      _appSettings.Zoom = Zoom;
      PopulatePercentZoom();
      UpdateEnable();
    }

    private void PopulatePercentZoom()
    {
      PercentZoom = _zoom.ToString("P0", InvariantCulture);
    }

    private void UpdateEnable()
    {
      CanShrink = Zoom > MinZoom;
      CanEnlarge = Zoom < MaxZoom;
    }

    void Dispose(bool disposing)
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

    private readonly IAppSettings _appSettings;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
    private bool _disposed = false;
  }
}
