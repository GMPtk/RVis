using LanguageExt;
using RVisUI.Model;
using System;
using System.Windows.Input;
using static LanguageExt.Prelude;

namespace RVisUI.Mvvm.Design
{
  internal class _CommonConfiguration : ICommonConfiguration
  {
    private static readonly bool?[] _values = new[] { default(bool?), true, false };

    internal _CommonConfiguration(int i)
    {
      var index = i % _values.Length;

      AutoApplyParameterSharedState = _values[++index % _values.Length];
      AutoShareParameterSharedState = _values[++index % _values.Length];
      AutoApplyElementSharedState = _values[++index % _values.Length];
      AutoShareElementSharedState = _values[++index % _values.Length];
      AutoApplyObservationsSharedState = _values[++index % _values.Length];
      AutoShareObservationsSharedState = _values[++index % _values.Length];
    }

    public bool? AutoApplyParameterSharedState { get; set; }
    public bool? AutoShareParameterSharedState { get; set; }
    public bool? AutoApplyElementSharedState { get; set; }
    public bool? AutoShareElementSharedState { get; set; }
    public bool? AutoApplyObservationsSharedState { get; set; }
    public bool? AutoShareObservationsSharedState { get; set; }
  }

  public class CommonConfigurationViewModel : ICommonConfigurationViewModel
  {
    public CommonConfigurationViewModel()
    {
      ModuleViewModels = Range(1, 30)
        .Map(i => new ModuleViewModel($"Module No.{i:0000}", new _CommonConfiguration(i)))
        .ToArr();
    }

    public Arr<ModuleViewModel> ModuleViewModels { get; }

    public ICommand OK => throw new NotImplementedException();

    public ICommand Cancel => throw new NotImplementedException();

    public bool? DialogResult { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  }
}
