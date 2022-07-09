using RVisUI.Model;
using System;
using System.ComponentModel;

#nullable disable
#pragma warning disable 0067

namespace RVisUI.AppInf.Design
{
  public sealed class AppSettings : IAppSettings
  {
    public bool RestoreWindow { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public string PrimaryColorName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public string SecondaryColorName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public bool IsBaseDark { get => true; set => throw new NotImplementedException(); }
    public bool IsColorAdjusted { get => true; set => throw new NotImplementedException(); }
    public float DesiredContrastRatio { get => 0.5f; set => throw new NotImplementedException(); }
    public int ContrastValue { get => 1; set => throw new NotImplementedException(); }
    public int ColorSelectionValue { get => 1; set => throw new NotImplementedException(); }
    public string ModuleConfiguration { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public int RThrottlingUseCores { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public string PathToSimLibrary { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public string PathToRunControlDrop { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public double Zoom { get => 0.5; set => throw new NotImplementedException(); }

    public event PropertyChangedEventHandler PropertyChanged;

    public T Get<T>(string name)
    {
      throw new NotImplementedException();
    }

    public void Set<T>(string name, T value)
    {
      throw new NotImplementedException();
    }
  }
}
