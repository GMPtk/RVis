using RVisUI.Model;

namespace RVisUI.Mvvm
{
  public sealed class ModuleViewModel
  {
    public ModuleViewModel(string displayName, ICommonConfiguration commonConfiguration)
    {
      _commonConfiguration = commonConfiguration;

      DisplayName = displayName;

      AutoApplyParameterSharedState = commonConfiguration.AutoApplyParameterSharedState;
      AutoShareParameterSharedState = commonConfiguration.AutoShareParameterSharedState;

      AutoApplyElementSharedState = commonConfiguration.AutoApplyElementSharedState;
      AutoShareElementSharedState = commonConfiguration.AutoShareElementSharedState;

      AutoApplyObservationsSharedState = commonConfiguration.AutoApplyObservationsSharedState;
      AutoShareObservationsSharedState = commonConfiguration.AutoShareObservationsSharedState;
    }

    public string DisplayName { get; }

    public bool? AutoApplyParameterSharedState { get; set; }
    public bool? AutoShareParameterSharedState { get; set; }

    public bool? AutoApplyElementSharedState { get; set; }
    public bool? AutoShareElementSharedState { get; set; }

    public bool? AutoApplyObservationsSharedState { get; set; }
    public bool? AutoShareObservationsSharedState { get; set; }

    internal void ApplyConfiguration()
    {
      _commonConfiguration.AutoApplyParameterSharedState = AutoApplyParameterSharedState;
      _commonConfiguration.AutoShareParameterSharedState = AutoShareParameterSharedState;

      _commonConfiguration.AutoApplyElementSharedState = AutoApplyElementSharedState;
      _commonConfiguration.AutoShareElementSharedState = AutoShareElementSharedState;

      _commonConfiguration.AutoApplyObservationsSharedState = AutoApplyObservationsSharedState;
      _commonConfiguration.AutoShareObservationsSharedState = AutoShareObservationsSharedState;
    }

    private readonly ICommonConfiguration _commonConfiguration;
  }
}