using LanguageExt;
using ReactiveUI;
using RVis.Base;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.AppInf;
using RVisUI.AppInf.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using static LanguageExt.Prelude;
using static MathNet.Numerics.Statistics.Correlation;
using static MathNet.Numerics.Statistics.SortedArrayStatistics;
using static RVis.Base.Check;
using static RVis.Base.Extensions.LangExt;
using static RVis.Base.Extensions.NumExt;
using stats = MathNet.Numerics.Statistics.Statistics;

namespace Sampling
{
  internal sealed class SamplesViewModel : ViewModelBase, ISamplesViewModel
  {
    internal SamplesViewModel(
      IAppState appState,
      IAppService appService,
      IAppSettings appSettings,
      ModuleState moduleState
      )
    {
      _appState = appState;
      _appService = appService;
      _appSettings = appSettings;
      _moduleState = moduleState;

      _simulation = appState.Target.AssertSome();

      ShareParameters = ReactiveCommand.Create(
        HandleShareParameters,
        this.WhenAny(
          vm => vm.Distributions,
          _ => _moduleState.FilterConfig.IsEnabled &&
               !_moduleState.FilterConfig.Filters.IsEmpty &&
               !_moduleState.Outputs.IsEmpty &&
               Distributions.Count > 0)
        );

      ConfigureLHS = ReactiveCommand.Create(
        HandleConfigureLHS,
        this.WhenAny(vm => vm.CanConfigureLHS, _ => CanConfigureLHS)
        );

      ConfigureRC = ReactiveCommand.Create(
        HandleConfigureRC,
        this.WhenAny(vm => vm.CanConfigureRC, _ => CanConfigureRC)
        );

      GenerateSamples = ReactiveCommand.Create(
        HandleGenerateSamplesAsync,
        this.WhenAny(vm => vm.CanGenerateSamples, _ => CanGenerateSamples)
        );

      ViewCorrelation = ReactiveCommand.Create(
        HandleViewCorrelation,
        this.WhenAny(vm => vm.CanViewCorrelation, _ => CanViewCorrelation)
        );

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(

        _moduleState.ParameterStateChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(Arr<ParameterState>, ObservableQualifier)>(
            ObserveParameterStateChange
            )
          ),

        _moduleState.SamplesState
          .ObservableForProperty(ms => ms.LatinHypercubeDesign)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveSamplesStateLatinHypercubeDesign
              )
            ),

        _moduleState
          .ObservableForProperty(ms => ms.FilterConfig)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveModuleStateFilterConfig
              )
            ),

        _moduleState
          .ObservableForProperty(ms => ms.OutputFilters)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveModuleStateOutputFilters
              )
            ),

        _moduleState
          .ObservableForProperty(ms => ms.SamplesState.RankCorrelationDesign)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveSamplesStateRankCorrelationDesign
              )
            ),

        _moduleState
          .ObservableForProperty(vm => vm.SamplingDesign)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveModuleStateSamplingDesign
              )
            ),

        _appSettings
          .GetWhenPropertyChanged()
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<string?>(
              ObserveAppSettingsPropertyChange
              )
            ),

        Observable
          .Merge(
            this.ObservableForProperty(vm => vm.NSamples),
            this.ObservableForProperty(vm => vm.Seed)
            )
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveInputs
              )
            ),

        this
          .ObservableForProperty(vm => vm.IsReadOnly)
          .Subscribe(
            _reactiveSafeInvoke.SuspendAndInvoke<object>(
              ObserveIsReadOnly
              )
            )

        );

      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        if (_moduleState.SamplingDesign == default)
        {
          Populate(_moduleState.SamplesState, _moduleState.ParameterStates);
        }
        else
        {
          Populate(_moduleState.SamplingDesign);
        }

        UpdateEnable();
      }
    }

    public bool IsReadOnly
    {
      get => _isReadOnly;
      set => this.RaiseAndSetIfChanged(ref _isReadOnly, value);
    }
    private bool _isReadOnly;

    public Arr<string> Distributions
    {
      get => _distributions;
      set => this.RaiseAndSetIfChanged(ref _distributions, value);
    }
    private Arr<string> _distributions;

    public ICommand ShareParameters { get; }

    public Arr<string> Invariants
    {
      get => _invariants;
      set => this.RaiseAndSetIfChanged(ref _invariants, value);
    }
    private Arr<string> _invariants;

    public int? NSamples
    {
      get => _nSamples;
      set => this.RaiseAndSetIfChanged(ref _nSamples, value);
    }
    private int? _nSamples;

    public int? Seed
    {
      get => _seed;
      set => this.RaiseAndSetIfChanged(ref _seed, value);
    }
    private int? _seed;

    public LatinHypercubeDesignType LatinHypercubeDesignType
    {
      get => _latinHypercubeDesignType;
      set => this.RaiseAndSetIfChanged(ref _latinHypercubeDesignType, value);
    }
    private LatinHypercubeDesignType _latinHypercubeDesignType;

    public ICommand ConfigureLHS { get; }

    public bool CanConfigureLHS
    {
      get => _canConfigureLHS;
      set => this.RaiseAndSetIfChanged(ref _canConfigureLHS, value);
    }
    private bool _canConfigureLHS;

    public RankCorrelationDesignType RankCorrelationDesignType
    {
      get => _rankCorrelationDesignType;
      set => this.RaiseAndSetIfChanged(ref _rankCorrelationDesignType, value);
    }
    private RankCorrelationDesignType _rankCorrelationDesignType;

    public ICommand ConfigureRC { get; }

    public bool CanConfigureRC
    {
      get => _canConfigureRC;
      set => this.RaiseAndSetIfChanged(ref _canConfigureRC, value);
    }
    private bool _canConfigureRC;

    public ICommand GenerateSamples { get; }

    public bool CanGenerateSamples
    {
      get => _canGenerateSamples;
      set => this.RaiseAndSetIfChanged(ref _canGenerateSamples, value);
    }
    private bool _canGenerateSamples;

    public DataView? Samples
    {
      get => _samples;
      set => this.RaiseAndSetIfChanged(ref _samples, value);
    }
    private DataView? _samples;

    public string[][]? Statistics
    {
      get => _statistics;
      set => this.RaiseAndSetIfChanged(ref _statistics, value);
    }
    private string[][]? _statistics;

    public ICommand ViewCorrelation { get; }

    public bool CanViewCorrelation
    {
      get => _canViewCorrelation;
      set => this.RaiseAndSetIfChanged(ref _canViewCorrelation, value);
    }
    private bool _canViewCorrelation;

    public ObservableCollection<IParameterSamplingViewModel> ParameterSamplingViewModels { get; }
      = new ObservableCollection<IParameterSamplingViewModel>();

    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        _subscriptions.Dispose();
      }

      base.Dispose(disposing);
    }

    private void HandleShareParameters()
    {
      RequireNotNull(_moduleState.SamplingDesign);
      RequireNotNull(Samples?.Table);

      var designParameters = _moduleState.SamplingDesign.DesignParameters;

      var columnNames = Samples.Table.Columns.OfType<DataColumn>().Select(dc => dc.ColumnName);
      var rowIndices = Range(0, Samples.Table.Rows.Count);
      var columnData = columnNames.Select(cn => (
        Name: cn,
        Data: rowIndices.Select(ri => (double)Samples.Table.Rows[ri][cn]).ToArray()
        ));
      var states = columnData
        .Select(cd =>
        {
          var designParameter = designParameters
            .Find(dp => dp.Name == cd.Name)
            .AssertSome($"Unexpected sample column {cd.Name}");

          var isInvariant = designParameter.Distribution.DistributionType == DistributionType.Invariant;

          if (isInvariant)
          {
            return (
              cd.Name,
              Value: designParameter.Distribution.Mean,
              Minimum: designParameter.Distribution.Mean.GetPreviousOrderOfMagnitude(),
              Maximum: designParameter.Distribution.Mean.GetNextOrderOfMagnitude(),
              Distribution: Some(designParameter.Distribution)
              );
          }

          var average = cd.Data.Average();
          var minimum = cd.Data.Min();
          var maximum = cd.Data.Max();
          var distribution = designParameter.Distribution.WithLowerUpper(minimum, maximum);

          return (
            cd.Name,
            Value: average,
            Minimum: minimum,
            Maximum: maximum,
            Distribution: Some(distribution)
            );
        })
        .ToArr();

      _appState.SimSharedState.ShareParameterState(states);
    }

    private void HandleConfigureLHS()
    {
      var view = new LHSConfigurationDialog();

      var viewModel = new LHSConfigurationViewModel(_appState, _appService)
      {
        LatinHypercubeDesign = _moduleState.SamplesState.LatinHypercubeDesign
      };

      if (viewModel.LatinHypercubeDesignType == LatinHypercubeDesignType.None)
      {
        viewModel.LatinHypercubeDesignType = LatinHypercubeDesignType.Randomized;
      }

      var didOK = _appService.ShowDialog(view, viewModel, default);

      if (didOK)
      {
        _moduleState.SamplesState.LatinHypercubeDesign = viewModel.LatinHypercubeDesign;
      }
    }

    private void HandleConfigureRC()
    {
      RequireOrdered(_moduleState.ParameterStates, ps => ps.Name.ToUpperInvariant());

      var view = new RCConfigurationDialog();

      var selectedParameters = _moduleState.ParameterStates
        .Filter(ps => ps.IsSelected && ps.DistributionType != DistributionType.Invariant)
        .Map(ps => ps.Name)
        .ToArr();

      var correlations = _moduleState.SamplesState.RankCorrelationDesign.Correlations.CorrelationsFor(
        selectedParameters
        );

      var viewModel = new RCConfigurationViewModel(_appState, _appService)
      {
        Correlations = correlations
      };

      var didOK = _appService.ShowDialog(view, viewModel, default);

      if (didOK)
      {
        if (viewModel.RankCorrelationDesignType == RankCorrelationDesignType.None)
        {
          _moduleState.SamplesState.RankCorrelationDesign = _moduleState.SamplesState.RankCorrelationDesign.With(
            RankCorrelationDesignType.None
            );
        }
        else
        {
          correlations = _moduleState.SamplesState.RankCorrelationDesign.Correlations.UpdateCorrelations(
            viewModel.Correlations
            );

          _moduleState.SamplesState.RankCorrelationDesign = new RankCorrelationDesign(
            RankCorrelationDesignType.ImanConn,
            correlations
            );
        }
      }
    }

    private async Task HandleGenerateSamplesAsync()
    {
      RequireTrue(NSamples > 0);
      RequireTrue(_moduleState.ParameterStates.Exists(ps => ps.IsSelected));
      RequireOrdered(_moduleState.ParameterStates, ps => ps.Name.ToUpperInvariant());

      if (_moduleState.SamplesState.RankCorrelationDesign.RankCorrelationDesignType != RankCorrelationDesignType.None)
      {
        var selectedNonInvariants = _moduleState.ParameterStates
          .Filter(ps => ps.IsSelected && ps.DistributionType != DistributionType.Invariant);

        var rcMissingParameters = selectedNonInvariants
          .Filter(ps => !_moduleState.SamplesState.RankCorrelationDesign.Correlations.Exists(c => c.Parameter == ps.Name));

        if (!rcMissingParameters.IsEmpty)
        {
          _appService.Notify(
            NotificationType.Error,
            nameof(SamplesViewModel),
            nameof(HandleGenerateSamplesAsync),
            "There is one or more missing correlations. Either disable rank correlation or update the matrix."
            );

          return;
        }

        var correlations = _moduleState.SamplesState.RankCorrelationDesign.Correlations.CorrelationsFor(
          selectedNonInvariants.Map(ps => ps.Name)
          );

        if (!correlations.IsPositiveDefinite())
        {
          _appService.Notify(
            NotificationType.Error,
            nameof(SamplesViewModel),
            nameof(HandleGenerateSamplesAsync),
            "Correlation matrix is not positive-definite. Either disable rank correlation or update the matrix."
            );

          return;
        }
      }

      TaskName = "Generating Samples";
      IsRunningTask = true;

      try
      {
        var isLHSDesign = _moduleState.SamplesState.LatinHypercubeDesign.LatinHypercubeDesignType != LatinHypercubeDesignType.None;

        var sampleGenerator = isLHSDesign
          ? (ISampleGenerator)new HypercubeSampleGenerator(
              _moduleState.ParameterStates,
              _moduleState.SamplesState.LatinHypercubeDesign,
              _moduleState.SamplesState.RankCorrelationDesign,
              NSamples.Value,
              Seed,
              _appService.RVisServerPool
            )
          : new DistributionSampleGenerator(
              _moduleState.ParameterStates,
              _moduleState.SamplesState.RankCorrelationDesign,
              NSamples.Value,
              Seed,
              _appService.RVisServerPool
            );

        var samples = await sampleGenerator.GetSamplesAsync();

        using (_reactiveSafeInvoke.SuspendedReactivity)
        {
          _moduleState.Samples = samples;
          PopulateSamples(samples);
          PopulateHistograms();
          UpdateEnable();
        }
      }
      catch (Exception ex)
      {
        _appService.Notify(
          NotificationType.Error,
          nameof(SamplesViewModel),
          nameof(HandleGenerateSamplesAsync),
          ex.Message
          );
      }

      IsRunningTask = false;
    }

    private void HandleViewCorrelation()
    {
      var view = new ViewCorrelationDialog();

      var parameterNames = _moduleState.ParameterStates
        .Filter(ps => ps.IsSelected && ps.DistributionType != DistributionType.Invariant)
        .Map(ps => ps.Name);

      RequireTrue(parameterNames.Count > 1);
      RequireNotNull(Samples?.Table);

      var vectors = parameterNames.Map(
        pn => Range(0, Samples.Table.Rows.Count)
          .Map(i => Samples.Table.Rows[i].Field<double>(pn))
          .ToArray()
        );

      var correlations = SpearmanMatrix(vectors);

      var viewModel = new ViewCorrelationViewModel(parameterNames, correlations);

      _appService.ShowDialog(view, viewModel, default);
    }

    private void ObserveParameterStateChange((Arr<ParameterState> ParameterStates, ObservableQualifier ObservableQualifier) change)
    {
      PopulateDistributions(_moduleState.ParameterStates);
      PopulateInvariants(_moduleState.ParameterStates);
      Samples = default;
      Statistics = default;
      PopulateHistograms();

      UpdateEnable();
    }

    private void ObserveSamplesStateLatinHypercubeDesign(object _)
    {
      LatinHypercubeDesignType = _moduleState.SamplesState.LatinHypercubeDesign.LatinHypercubeDesignType;
      Samples = default;
      Statistics = default;
      PopulateHistograms();
      UpdateEnable();
    }

    private void ObserveModuleStateFilterConfig(object _)
    {
      RequireNotNull(_moduleState.SamplingDesign);

      PopulateSamples(
        _moduleState.SamplingDesign.Samples
        );
      PopulateDistributions(_moduleState.SamplingDesign.DesignParameters);
      PopulateHistograms();
    }

    private void ObserveModuleStateOutputFilters(object _)
    {
      RequireNotNull(_moduleState.SamplingDesign);

      PopulateSamples(
        _moduleState.SamplingDesign.Samples
        );
      PopulateDistributions(_moduleState.SamplingDesign.DesignParameters);
      PopulateHistograms();
    }

    private void ObserveSamplesStateRankCorrelationDesign(object _)
    {
      RankCorrelationDesignType = _moduleState.SamplesState.RankCorrelationDesign.RankCorrelationDesignType;
      Samples = default;
      Statistics = default;
      PopulateHistograms();
      UpdateEnable();
    }

    private void ObserveModuleStateSamplingDesign(object _)
    {
      if (_moduleState.SamplingDesign == default)
      {
        Populate(_moduleState.SamplesState, _moduleState.ParameterStates);
      }
      else
      {
        Populate(_moduleState.SamplingDesign);
      }

      UpdateEnable();
    }

    private void ObserveAppSettingsPropertyChange(string? propertyName)
    {
      if (!propertyName.IsThemeProperty()) return;

      foreach (var parameterSamplingViewModel in ParameterSamplingViewModels)
      {
        parameterSamplingViewModel.Histogram.ApplyThemeToPlotModelAndAxes();
        parameterSamplingViewModel.Histogram.InvalidatePlot(false);
      }
    }

    private void ObserveInputs(object _)
    {
      _moduleState.SamplesState.NumberOfSamples = NSamples;
      _moduleState.SamplesState.Seed = Seed;
      Samples = default;
      Statistics = default;
      PopulateHistograms();

      UpdateEnable();
    }
    private void ObserveIsReadOnly(object _) =>
      UpdateEnable();

    private void UpdateEnable()
    {
      var nSelectedParameters = _moduleState.ParameterStates.Count(
        ps => ps.IsSelected
        );
      var nSelectedNonInvariants = _moduleState.ParameterStates.Count(
        ps => ps.IsSelected && ps.DistributionType != DistributionType.Invariant
        );

      CanConfigureLHS = !IsReadOnly && nSelectedNonInvariants > 0;
      CanConfigureRC = !IsReadOnly && nSelectedNonInvariants > 1;
      CanGenerateSamples = !IsReadOnly && nSelectedParameters > 0 && NSamples > 0;
      CanViewCorrelation = nSelectedNonInvariants > 1 && Samples != default;
    }

    private void PopulateDistributions(Arr<ParameterState> parameterStates) =>
      Distributions = parameterStates
        .Filter(ps => ps.IsSelected)
        .Map(ps => (ps.Name, Distribution: ps.GetDistribution()))
        .Filter(t => t.Distribution.DistributionType != DistributionType.Invariant)
        .Select(t => t.Distribution.ToString(t.Name))
        .ToArr();

    private void PopulateDistributions(Arr<DesignParameter> designParameters)
    {
      var isFiltered =
        _moduleState.FilterConfig.IsEnabled &&
        !_moduleState.FilterConfig.Filters.IsEmpty &&
        !_moduleState.Outputs.IsEmpty;

      if (!isFiltered)
      {
        Distributions = designParameters
          .Filter(dp => dp.Distribution.DistributionType != DistributionType.Invariant)
          .Select(dp => dp.Distribution.ToString(dp.Name))
          .ToArr();
        return;
      }

      if (Samples?.Table is null || Samples.Table.Rows.Count == 0)
      {
        Distributions = default;
        return;
      }

      var columnNames = Samples.Table.Columns.OfType<DataColumn>().Select(dc => dc.ColumnName);
      var rowIndices = Range(0, Samples.Table.Rows.Count);
      var columnData = columnNames.Select(cn => (
        Name: cn,
        Data: rowIndices.Select(ri => (double)Samples.Table.Rows[ri][cn]).ToArray()
        ));
      Distributions = columnData.Select(cd => $"{cd.Name} = {cd.Data.Average():G4} [{cd.Data.Min():G4}, {cd.Data.Max():G4}]").ToArr();
    }

    private void PopulateInvariants(Arr<ParameterState> parameterStates) =>
      Invariants = parameterStates
        .Filter(ps => ps.IsSelected)
        .Map(ps => (ps.Name, Distribution: ps.GetDistribution()))
        .Filter(t => t.Distribution.DistributionType == DistributionType.Invariant)
        .Select(t => t.Distribution.ToString(t.Name))
        .ToArr();

    private void PopulateInvariants(Arr<DesignParameter> designParameters) =>
      Invariants = designParameters
        .Filter(dp => dp.Distribution.DistributionType == DistributionType.Invariant)
        .Select(dp => dp.Distribution.ToString(dp.Name))
        .ToArr();

    private void Populate(SamplesState samplesState, Arr<ParameterState> parameterStates)
    {
      PopulateDistributions(parameterStates);
      PopulateInvariants(parameterStates);

      NSamples = samplesState.NumberOfSamples ?? 100;
      Seed = samplesState.Seed;
      LatinHypercubeDesignType = samplesState.LatinHypercubeDesign.LatinHypercubeDesignType;
      RankCorrelationDesignType = samplesState.RankCorrelationDesign.RankCorrelationDesignType;
      Samples = default;
      Statistics = default;

      PopulateHistograms();
    }

    private void Populate(SamplingDesign samplingDesign)
    {
      NSamples = samplingDesign.Samples.Rows.Count;
      Seed = samplingDesign.Seed;
      LatinHypercubeDesignType = samplingDesign.LatinHypercubeDesign.LatinHypercubeDesignType;
      RankCorrelationDesignType = samplingDesign.RankCorrelationDesign.RankCorrelationDesignType;

      PopulateSamples(samplingDesign.Samples);
      PopulateHistograms();

      PopulateDistributions(samplingDesign.DesignParameters);
      PopulateInvariants(samplingDesign.DesignParameters);
    }

    private void PopulateSamples(DataTable? samples)
    {
      if (samples is null)
      {
        Samples = default;
        Statistics = default;
        return;
      }

      DataTable dataTable;

      if (!_moduleState.FilterConfig.IsEnabled || _moduleState.OutputFilters.IsEmpty)
      {
        dataTable = samples;
      }
      else
      {
        dataTable = samples.Clone();
        _moduleState.OutputFilters.Filter(f => f.IsInFilteredSet).Iter(f =>
        {
          dataTable.ImportRow(samples.Rows[f.Index]);
        });
      }

      Samples = dataTable.DefaultView;

      var columnNames = dataTable.Columns.OfType<DataColumn>().Select(dc => dc.ColumnName);
      var rowIndices = Range(0, dataTable.Rows.Count);
      var columnData = columnNames
        .Select(cn => (
          Name: cn,
          Data: rowIndices.Select(ri => (double)dataTable.Rows[ri][cn]).OrderBy(d => d).ToArray()
        ))
        .Select(t => (
          t.Name,
          t.Data,
          Median: Median(t.Data),
          Mean: stats.Mean(t.Data),
          SD: stats.StandardDeviation(t.Data)
        ));

      Statistics = columnData.Select(cd => new[] {
        cd.Name,
        cd.Median.ToString("G4"),
        cd.Mean.ToString("G4"),
        $"{(cd.Mean - cd.SD):G4} - {(cd.Mean + cd.SD):G4}",
        $"{(cd.Mean - 2d * cd.SD):G4} - {(cd.Mean + 2d * cd.SD):G4}",
        $"{(cd.Mean - 3d * cd.SD):G4} - {(cd.Mean + 3d * cd.SD):G4}"
      }).ToArray();

      Distributions = columnData.Select(cd => $"{cd.Name} = {cd.Data.Average():G4} [{cd.Data.Min():G4}, {cd.Data.Max():G4}]").ToArr();
    }

    private void PopulateHistograms()
    {
      var sampledParameters = _moduleState.ParameterStates
        .Filter(ps => ps.IsSelected && ps.DistributionType != DistributionType.Invariant)
        .Map(ps => (
          ps.Name,
          ParameterState: ps,
          Parameter: _simulation.SimConfig.SimInput.SimParameters.GetParameter(ps.Name)
          ));

      var toRemove = ParameterSamplingViewModels.Where(vm => !sampledParameters.Exists(sp => sp.Name == vm.Parameter.Name)).ToArr();
      toRemove.Iter(vm => ParameterSamplingViewModels.Remove(vm));

      sampledParameters.Iter(sp =>
      {
        var parameterSamplingViewModel = ParameterSamplingViewModels.SingleOrDefault(vm => vm.Parameter.Name == sp.Name);

        if (parameterSamplingViewModel == default)
        {
          parameterSamplingViewModel = new ParameterSamplingViewModel(sp.Parameter, _moduleState);
          parameterSamplingViewModel.Histogram.ApplyThemeToPlotModelAndAxes();
          ParameterSamplingViewModels.InsertInOrdered(parameterSamplingViewModel, vm => vm.SortKey);
        }

        parameterSamplingViewModel.Distribution = sp.ParameterState.GetDistribution();

        if (Samples != default)
        {
          RequireNotNull(Samples.Table);
          var columnIndex = Samples.Table.Columns.IndexOf(sp.Name);
          RequireTrue(columnIndex.IsFound());
          var samples = Samples.Table
            .AsEnumerable()
            .Select(dr => dr.Field<double>(columnIndex))
            .ToArr();
          parameterSamplingViewModel.Samples = samples;
        }
        else
        {
          parameterSamplingViewModel.Samples = default;
        }
      });
    }

    private readonly IAppState _appState;
    private readonly IAppService _appService;
    private readonly IAppSettings _appSettings;
    private readonly ModuleState _moduleState;
    private readonly Simulation _simulation;
    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private readonly IDisposable _subscriptions;
  }
}
