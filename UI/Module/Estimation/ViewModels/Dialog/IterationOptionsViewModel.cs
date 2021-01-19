using ReactiveUI;
using RVis.Base.Extensions;
using RVisUI.Model.Extensions;
using System.ComponentModel;
using System.Windows.Input;
using static System.Globalization.CultureInfo;

namespace Estimation
{
  internal class IterationOptionsViewModel : IIterationOptionsViewModel, INotifyPropertyChanged
  {
    internal IterationOptionsViewModel(double? targetAcceptRate, bool useApproximation)
    {
      TargetAcceptRateText = targetAcceptRate?.ToString(InvariantCulture);
      UseApproximation = useApproximation;

      OK = ReactiveCommand.Create(
        HandleOK,
        this.WhenAny(vm => vm.IterationsToAdd, vm => vm.TargetAcceptRateText, CanHandleOK)
        );
      Cancel = ReactiveCommand.Create(HandleCancel);
    }

    public string? IterationsToAddText
    {
      get => _iterationsToAddText;
      set => this.RaiseAndSetIfChanged(ref _iterationsToAddText, value.CheckParseValue<int>(), PropertyChanged);
    }
    private string? _iterationsToAddText;

    public int? IterationsToAdd => 
      int.TryParse(_iterationsToAddText, out int i) ? i : default(int?);

    public string? TargetAcceptRateText
    {
      get => _targetAcceptRateText;
      set => this.RaiseAndSetIfChanged(ref _targetAcceptRateText, value.CheckParseValue<double>(), PropertyChanged);
    }
    private string? _targetAcceptRateText;

    public double? TargetAcceptRate => 
      double.TryParse(_targetAcceptRateText, out double d) ? d : default(double?);

    public bool UseApproximation
    {
      get => _useApproximation;
      set => this.RaiseAndSetIfChanged(ref _useApproximation, value, PropertyChanged);
    }
    private bool _useApproximation;

    public ICommand OK { get; }

    public ICommand Cancel { get; }

    public bool? DialogResult
    {
      get => _dialogResult;
      set => this.RaiseAndSetIfChanged(ref _dialogResult, value, PropertyChanged);
    }
    private bool? _dialogResult;

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool CanHandleOK(object _, object __) =>
      (!IterationsToAdd.HasValue || IterationsToAdd.Value > 0) &&
      (!TargetAcceptRate.HasValue || (TargetAcceptRate.Value > 0d && TargetAcceptRate.Value < 1d));

    private void HandleOK() => DialogResult = true;

    private void HandleCancel() => DialogResult = false;
  }
}
