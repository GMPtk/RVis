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

    public bool IsBaseDark { get => false; set => throw new NotImplementedException(); }
    public string PrimaryColorName { get => "deep purple"; set => throw new NotImplementedException(); }
    public int PrimaryColorHue { get => 5; set => throw new NotImplementedException(); }
    public string SecondaryColorName { get => "lime"; set => throw new NotImplementedException(); }
    public int SecondaryColorHue { get => 5; set => throw new NotImplementedException(); }
    public string PrimaryForegroundColorName { get => "deep purple"; set => throw new NotImplementedException(); }
    public int PrimaryForegroundColorHue { get => 5; set => throw new NotImplementedException(); }
    public string SecondaryForegroundColorName { get => "lime"; set => throw new NotImplementedException(); }
    public int SecondaryForegroundColorHue { get => 5; set => throw new NotImplementedException(); }

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
