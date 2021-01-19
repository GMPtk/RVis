using LanguageExt;
using ReactiveUI;
using RVis.Base.Extensions;
using RVisUI.Model.Extensions;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using static System.Globalization.CultureInfo;

namespace Sensitivity
{
  internal sealed class RankingViewModel : IRankingViewModel, INotifyPropertyChanged
  {
    internal RankingViewModel(
      double? from,
      double? to,
      string? xUnits,
      Arr<(string Name, bool IsSelected)> outputs,
      Arr<string> selectedParameters,
      IScorer scorer
      )
    {
      FromText = from?.ToString(InvariantCulture);
      ToText = to?.ToString(InvariantCulture);
      XUnits = xUnits;
      _scorer = scorer;

      OutputViewModels = outputs
        .Map(o => new OutputViewModel(o.Name) { IsSelected = o.IsSelected })
        .ToArr<IOutputViewModel>();

      Populate();
      RankedParameterViewModels.Iter(
        vm => vm.IsSelected = selectedParameters.Contains(vm.Name)
        );
      UpdateEnable();

      OK = ReactiveCommand.Create(
        HandleOK,
        this.WhenAny(vm => vm.CanOK, _ => CanOK)
        );

      Cancel = ReactiveCommand.Create(HandleCancel);

      this.ObservableForProperty(vm => vm.FromText).Subscribe(ObserveFromText);
      this.ObservableForProperty(vm => vm.ToText).Subscribe(ObserveToText);
      OutputViewModels.Iter(ovm => ovm
        .ObservableForProperty(vm => vm.IsSelected)
        .Subscribe(ObserveOutputIsSelected)
        );
    }

    public string? FromText
    {
      get => _fromText;
      set => this.RaiseAndSetIfChanged(ref _fromText, value.CheckParseValue<double>(), PropertyChanged);
    }
    private string? _fromText;

    public double? From =>
      double.TryParse(_fromText, out double d) ? d : default(double?);

    public string? ToText
    {
      get => _toText;
      set => this.RaiseAndSetIfChanged(ref _toText, value.CheckParseValue<double>(), PropertyChanged);
    }
    private string? _toText;

    public double? To =>
      double.TryParse(_toText, out double d) ? d : default(double?);

    public string? XUnits { get; }

    public Arr<IOutputViewModel> OutputViewModels { get; }

    public Arr<IRankedParameterViewModel> RankedParameterViewModels
    {
      get => _rankedParameterViewModels;
      set => this.RaiseAndSetIfChanged(ref _rankedParameterViewModels, value, PropertyChanged);
    }
    private Arr<IRankedParameterViewModel> _rankedParameterViewModels;

    public bool CanOK
    {
      get => _canOK;
      set => this.RaiseAndSetIfChanged(ref _canOK, value, PropertyChanged);
    }
    private bool _canOK;

    public ICommand OK { get; }

    public ICommand Cancel { get; }

    public bool? DialogResult
    {
      get => _dialogResult;
      set => this.RaiseAndSetIfChanged(ref _dialogResult, value, PropertyChanged);
    }
    private bool? _dialogResult;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void HandleOK() => DialogResult = true;

    private void HandleCancel() => DialogResult = false;

    private void ObserveFromText(IObservedChange<RankingViewModel, string?> _)
    {
      Populate();
      UpdateEnable();
    }

    private void ObserveToText(IObservedChange<RankingViewModel, string?> _)
    {
      Populate();
      UpdateEnable();
    }

    private void ObserveOutputIsSelected(IObservedChange<IOutputViewModel, bool> _)
    {
      Populate();
      UpdateEnable();
    }

    private void ObserveParameterIsSelected(IObservedChange<IRankedParameterViewModel, bool> _)
    {
      UpdateEnable();
    }

    private void Populate()
    {
      _parameterIsSelectedSubscription?.Dispose();

      var outputs = OutputViewModels.Filter(vm => vm.IsSelected).Map(vm => vm.Name);

      var canPopulate = From.HasValue && To.HasValue && !outputs.IsEmpty;

      if (!canPopulate)
      {
        RankedParameterViewModels = default;
        return;
      }

      var scores = _scorer.GetScores(From!.Value, To!.Value, outputs);

      var selectedParameters = RankedParameterViewModels
        .Filter(vm => vm.IsSelected)
        .Map(vm => vm.Name);

      RankedParameterViewModels = scores
        .Map(s => new RankedParameterViewModel(s.ParameterName, s.Score)
        {
          IsSelected = selectedParameters.Contains(s.ParameterName)
        })
        .ToArr<IRankedParameterViewModel>();

      _parameterIsSelectedSubscription = Observable
        .Merge(
          RankedParameterViewModels.Map(
            rpvm => rpvm.ObservableForProperty(vm => vm.IsSelected)
            )
          )
        .Subscribe(ObserveParameterIsSelected);
    }

    private void UpdateEnable()
    {
      CanOK = RankedParameterViewModels.Exists(vm => vm.IsSelected);
    }

    private readonly IScorer _scorer;
    private IDisposable? _parameterIsSelectedSubscription;
  }
}
