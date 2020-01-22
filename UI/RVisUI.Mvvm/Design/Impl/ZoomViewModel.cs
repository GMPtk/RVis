using System;
using System.Windows.Input;
using static System.Globalization.CultureInfo;

namespace RVisUI.Mvvm.Design
{
  public sealed class ZoomViewModel : IZoomViewModel
  {
    public ICommand Open => throw new NotImplementedException();

    public bool IsOpen => true;

    public ICommand Shrink => throw new NotImplementedException();

    public bool CanShrink => false;

    public ICommand Enlarge => throw new NotImplementedException();

    public bool CanEnlarge => false;

    public double MinZoom => 0.3;

    public double MaxZoom => 3d;

    public double Zoom { get => 0.66; set => throw new NotImplementedException(); }

    public string PercentZoom => Zoom.ToString("P0", InvariantCulture);

    public ICommand Reset => throw new NotImplementedException();
  }
}
