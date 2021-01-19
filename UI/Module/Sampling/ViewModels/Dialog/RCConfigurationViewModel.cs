using LanguageExt;
using ReactiveUI;
using RVis.Base.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows.Input;
using static LanguageExt.Prelude;
using static RVis.Base.Check;

namespace Sampling
{
  internal sealed class RCConfigurationViewModel : IRCConfigurationViewModel, INotifyPropertyChanged
  {
    internal RCConfigurationViewModel(IAppState appState, IAppService appService)
    {
      _appService = appService;

      Cancel = ReactiveCommand.Create(HandleCancel);

      IsMc2dInstalled = appState.InstalledRPackages.Exists(p => p.Package == "mc2d");

      _rcSetKeyboardTarget = ReactiveCommand.Create<RCParameterViewModel>(HandleSetKeyboardTarget);

      Disable = ReactiveCommand.Create(HandleDisable);

      OK = ReactiveCommand.Create(
        HandleOK,
        this.ObservableForProperty(vm => vm.CanOK, _ => CanOK)
        );

      UpdateEnable();

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      this
        .ObservableForProperty(
          vm => vm.Correlations
          )
        .Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<object>(
            ObserveCorrelations
          )
        );
    }

    public bool IsMc2dInstalled { get; }

    public Arr<string> ParameterNames
    {
      get => _parameterNames;
      set => this.RaiseAndSetIfChanged(ref _parameterNames, value, PropertyChanged);
    }
    private Arr<string> _parameterNames;

    public IRCParameterViewModel[][]? RCParameterViewModels
    {
      get => _rcParameterViewModels;
      set => this.RaiseAndSetIfChanged(ref _rcParameterViewModels, value, PropertyChanged);
    }
    private IRCParameterViewModel[][]? _rcParameterViewModels;

    public string? TargetParameterV
    {
      get => _targetParameterV;
      set => this.RaiseAndSetIfChanged(ref _targetParameterV, value, PropertyChanged);
    }
    private string? _targetParameterV;

    public string? TargetParameterH
    {
      get => _targetParameterH;
      set => this.RaiseAndSetIfChanged(ref _targetParameterH, value, PropertyChanged);
    }
    private string? _targetParameterH;

    public Arr<double> TargetCorrelations
    {
      get => _targetCorrelations;
      set => this.RaiseAndSetIfChanged(ref _targetCorrelations, value, PropertyChanged);
    }
    private Arr<double> _targetCorrelations;

    public RankCorrelationDesignType RankCorrelationDesignType
    {
      get => _rankCorrelationDesignType;
      set => this.RaiseAndSetIfChanged(ref _rankCorrelationDesignType, value, PropertyChanged);
    }
    private RankCorrelationDesignType _rankCorrelationDesignType;

    public ICommand Disable { get; }

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

    public Arr<(string Parameter, Arr<double> Correlations)> Correlations
    {
      get => _correlations;
      set => this.RaiseAndSetIfChanged(ref _correlations, value, PropertyChanged);
    }
    private Arr<(string Parameter, Arr<double> Correlations)> _correlations;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void HandleCancel() =>
      DialogResult = false;

    private void HandleSetKeyboardTarget(RCParameterViewModel rcParameterViewModel)
    {
      RequireNotNull(RCParameterViewModels);

      var state =
        Range(0, RCParameterViewModels.Length).Map(i =>
          Range(0, RCParameterViewModels[i].Length).Map(j =>
          {
            var element = RCParameterViewModels[i][j];
            var correlation = element.CorrelationN;
            var isEditable = element is RCParameterViewModel;
            var isTarget = ReferenceEquals(rcParameterViewModel, element);
            return (Row: i, Column: j, Correlation: correlation, IsEditable: isEditable, IsTarget: isTarget);
          })
          .ToArr()
        ).ToArr();

      var canCompute = state.ForAll(
        s => s.ForAll(t => t.IsTarget || !t.IsEditable || t.Correlation.HasValue)
        );

      if (!canCompute)
      {
        TargetParameterV = default;
        TargetParameterH = default;
        TargetCorrelations = default;
        return;
      }

      var (row, column) = state
        .Bind(s => s)
        .Filter(s => s.IsTarget)
        .Map(t => (t.Row, t.Column))
        .Single();

      var matrix = state
        .Map(s => s
          .Map(t => t.Correlation ?? 0d)
          .ToArray()
          )
        .ToArray()
        .ToMultidimensional();

      var validCorrelations = Range(-100, 201)
        .Map(i =>
        {
          var correlation = i / 100d;
          matrix[row, column] = correlation;
          matrix[column, row] = correlation;
          return matrix.IsPositiveDefinite() ? Some(correlation) : None;
        })
        .Somes()
        .ToArr();

      TargetParameterV = ParameterNames[row];
      TargetParameterH = ParameterNames[column];
      TargetCorrelations = validCorrelations;
    }

    private void HandleDisable() =>
      DialogResult = true;

    private void HandleOK()
    {
      RequireNotNull(RCParameterViewModels);

      var correlations = RCParameterViewModels
        .Select((a, i) =>
        {
          var parameterName = ParameterNames[i];
          var correlations = a
            .OfType<RCParameterViewModel>()
            .Select(vm => vm.CorrelationN ?? throw new NullReferenceException($"{vm.Name} has null corr"))
            .ToArr();
          return (parameterName, correlations);
        })
        .ToArr();

      try
      {
        if (!correlations.IsPositiveDefinite())
        {
          _appService.Notify(
            NotificationType.Error,
            nameof(RCConfigurationViewModel),
            nameof(HandleOK),
            "Rank correlation requires a positive-definite matrix"
            );

          return;
        }
      }
      catch (Exception ex)
      {
        _appService.Notify(
          NotificationType.Error,
          nameof(RCConfigurationViewModel),
          nameof(HandleOK),
          ex.Message
          );

        return;
      }

      Correlations = correlations;
      RankCorrelationDesignType = RankCorrelationDesignType.ImanConn;
      DialogResult = true;
    }

    private void ObserveCorrelations(object _) =>
      Populate();

    private void ObserveCorrelation(object _) =>
      UpdateEnable();

    private void Populate()
    {
      _rcParameterViewModelSubscriptions?.Dispose();
      _rcParameterViewModels = default;

      if (Correlations.IsEmpty)
      {
        UpdateEnable();
        return;
      }

      ParameterNames = Correlations.Map(c => c.Parameter);

      var nParameters = ParameterNames.Count;

      RequireTrue(nParameters > 1);

      var rcParameterViewModels = new IRCParameterViewModel[nParameters][];

      Range(0, nParameters).Iter(i =>
      {
        rcParameterViewModels[i] = new IRCParameterViewModel[nParameters];

        var correlations = Correlations[i].Correlations;

        Range(0, nParameters).Iter(j =>
        {
          if (j < i)
          {
            rcParameterViewModels[i][j] = new RCParameterMirrorViewModel(
              rcParameterViewModels[j][i]
              );
          }
          else if (j == i)
          {
            rcParameterViewModels[i][j] = new RCParameterDiagonalViewModel(
              ParameterNames[i],
              1d
              );
          }
          else
          {
            rcParameterViewModels[i][j] = new RCParameterViewModel(
              ParameterNames[i],
              _rcSetKeyboardTarget
              )
            {
              CorrelationN = correlations[j - i - 1]
            };
          }
        });
      });

      var editable = rcParameterViewModels
        .Select(a => a.OfType<RCParameterViewModel>())
        .SelectMany(vm => vm)
        .ToArray();

      _rcParameterViewModelSubscriptions = new CompositeDisposable(
        editable.Select(rcpvm => rcpvm
          .ObservableForProperty(vm => vm.CorrelationN)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveCorrelation
              )
            )
          )
        );

      RCParameterViewModels = rcParameterViewModels;

      UpdateEnable();
    }

    private void UpdateEnable()
    {
      if (_rcParameterViewModels == default)
      {
        CanOK = false;
        return;
      }

      var editable = _rcParameterViewModels
        .Select(a => a.OfType<RCParameterViewModel>())
        .SelectMany(vm => vm)
        .ToArray();

      var allPopulated = editable.All(vm => vm.CorrelationN.HasValue);

      CanOK = allPopulated;
    }

    private readonly IAppService _appService;
    private readonly ICommand _rcSetKeyboardTarget;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private IDisposable? _rcParameterViewModelSubscriptions;
  }
}
