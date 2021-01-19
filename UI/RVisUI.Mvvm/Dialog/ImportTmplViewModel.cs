using LanguageExt;
using ReactiveUI;
using RVis.Base.Extensions;
using RVis.Model;
using RVisUI.Model;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using static RVis.Base.Check;

namespace RVisUI.Mvvm
{
  public class ImportTmplViewModel : ReactiveObject, IImportTmplViewModel
  {
    public ImportTmplViewModel(ManagedImport managedImport, string fileName, IAppService appService)
    {
      _managedImport = managedImport;
      _appService = appService;

      FileName = fileName;

      var changeParameterDescriptionUnit =
        ReactiveCommand.Create<IParameterCandidateViewModel>(HandleChangeParameterDescriptionUnit);

      ParameterCandidates = managedImport.ParameterCandidates.Map<IParameterCandidateViewModel>(pc =>
        new ParameterCandidateViewModel(
          pc.IsUsed,
          pc.Name,
          pc.Value,
          pc.Unit,
          pc.Description,
          changeParameterDescriptionUnit
          )
        );

      UseAllParameters = ReactiveCommand.Create(HandleUseAllParameters);
      UseNoParameters = ReactiveCommand.Create(HandleUseNoParameters);

      SetIndependentVariable = ReactiveCommand.Create(
        HandleSetIndependentVariable,
        this.ObservableForProperty(vm => vm.SelectedElementCandidate, sec => sec != default)
        );

      var changeElementDescriptionUnit =
        ReactiveCommand.Create<IElementCandidateViewModel>(HandleChangeElementDescriptionUnit);

      _elementCandidates = managedImport.ValueCandidates
        .Bind(vc =>
          vc.ElementCandidates.Map<IElementCandidateViewModel>(ec =>
            new ElementCandidateViewModel(
              ec.IsUsed,
              ec.Name,
              vc.Name,
              false,
              ec.Values,
              ec.Unit,
              ec.Description,
              changeElementDescriptionUnit
              )
            )
        );
      ElementCandidates = new ObservableCollection<IElementCandidateViewModel>(_elementCandidates);

      UseAllOutputs = ReactiveCommand.Create(HandleUseAllOutputs);
      UseNoOutputs = ReactiveCommand.Create(HandleUseNoOutputs);

      _simulationName = managedImport.SimulationName;
      _simulationDescription = managedImport.SimulationDescription;

      OK = ReactiveCommand.Create(HandleOK);
      Cancel = ReactiveCommand.Create(() => DialogResult = false);
    }

    public string FileName { get; }

    public Arr<IParameterCandidateViewModel> ParameterCandidates { get; }

    public ICommand UseAllParameters { get; }

    public ICommand UseNoParameters { get; }

    public IElementCandidateViewModel? IndependentVariable
    {
      get => _independentVariable;
      set => this.RaiseAndSetIfChanged(ref _independentVariable, value);
    }
    private IElementCandidateViewModel? _independentVariable;

    public ICommand SetIndependentVariable { get; }

    public ObservableCollection<IElementCandidateViewModel> ElementCandidates { get; }

    public IElementCandidateViewModel? SelectedElementCandidate
    {
      get => _selectedElementCandidate;
      set => this.RaiseAndSetIfChanged(ref _selectedElementCandidate, value);
    }
    private IElementCandidateViewModel? _selectedElementCandidate;

    public ICommand UseAllOutputs { get; }

    public ICommand UseNoOutputs { get; }

    public string SimulationName
    {
      get => _simulationName;
      set
      {
        this.RaiseAndSetIfChanged(ref _simulationName, value);
        if (_simulationName == default) throw new Exception("Name is required");
      }
    }
    private string _simulationName;

    public string? SimulationDescription
    {
      get => _simulationDescription;
      set => this.RaiseAndSetIfChanged(ref _simulationDescription, value);
    }
    private string? _simulationDescription;

    public ICommand OK { get; }

    public ICommand Cancel { get; }

    public bool? DialogResult
    {
      get => _dialogResult;
      set => this.RaiseAndSetIfChanged(ref _dialogResult, value);
    }
    private bool? _dialogResult;

    private void HandleChangeParameterDescriptionUnit(IParameterCandidateViewModel parameterCandidateViewModel)
    {
      var parameterCandidate = _managedImport.ParameterCandidates.Find(
        pc => pc.Name == parameterCandidateViewModel.Name
        ).AssertSome();

      var changeDescriptionUnitViewModel = new ChangeDescriptionUnitViewModel(
        parameterCandidate.Name,
        parameterCandidate.Description,
        parameterCandidate.Unit,
        _managedImport.SymbolInfos
        );

      var didChange = _appService.ShowDialog(changeDescriptionUnitViewModel, this);
      if (didChange)
      {
        parameterCandidate.Description = changeDescriptionUnitViewModel.Description;
        parameterCandidate.Unit = changeDescriptionUnitViewModel.Unit;

        parameterCandidateViewModel.Description = parameterCandidate.Description;
        parameterCandidateViewModel.Unit = parameterCandidate.Unit;
      }
    }

    private void HandleSetIndependentVariable()
    {
      RequireNotNull(SelectedElementCandidate);

      if (IndependentVariable != default)
      {
        var insertAt = _elementCandidates.IndexOf(IndependentVariable);
        ElementCandidates.Insert(insertAt, IndependentVariable);
      }
      var independentVariable = SelectedElementCandidate;
      SelectedElementCandidate = default;
      var removeAt = ElementCandidates.IndexOf(independentVariable);
      ElementCandidates.RemoveAt(removeAt);
      IndependentVariable = independentVariable;
    }

    private void HandleChangeElementDescriptionUnit(IElementCandidateViewModel elementCandidateViewModel)
    {
      var valueCandidate = _managedImport.ValueCandidates.Find(
        vc => vc.Name == elementCandidateViewModel.ValueName
        ).AssertSome();

      var elementCandidate = valueCandidate.ElementCandidates.Find(
        ec => ec.Name == elementCandidateViewModel.Name
        ).AssertSome();

      var changeDescriptionUnitViewModel = new ChangeDescriptionUnitViewModel(
        elementCandidate.Name,
        elementCandidate.Description,
        elementCandidate.Unit,
        _managedImport.SymbolInfos
        );

      var didChange = _appService.ShowDialog(changeDescriptionUnitViewModel, this);
      if (didChange)
      {
        elementCandidate.Description = changeDescriptionUnitViewModel.Description;
        elementCandidate.Unit = changeDescriptionUnitViewModel.Unit;

        elementCandidateViewModel.Description = elementCandidate.Description;
        elementCandidateViewModel.Unit = elementCandidate.Unit;
      }
    }

    private void HandleUseAllParameters() => ParameterCandidates.Iter(pc => pc.IsUsed = true);

    private void HandleUseNoParameters() => ParameterCandidates.Iter(pc => pc.IsUsed = false);

    private void HandleUseAllOutputs() => ElementCandidates.Iter(ec => ec.IsUsed = true);

    private void HandleUseNoOutputs() => ElementCandidates.Iter(ec => ec.IsUsed = false);

    private void HandleOK()
    {
      if (ParameterCandidates.ForAll(pc => !pc.IsUsed))
      {
        _appService.Notify(NotificationType.Error, nameof(ImportExecViewModel), nameof(ParameterCandidates), "Use at least one parameter", this);
        return;
      }

      if (IndependentVariable == default)
      {
        _appService.Notify(NotificationType.Error, nameof(ImportExecViewModel), nameof(IndependentVariable), "Set the independent variable", this);
        return;
      }

      if (ElementCandidates.ForAll(ec => !ec.IsUsed))
      {
        _appService.Notify(NotificationType.Error, nameof(ImportExecViewModel), nameof(ElementCandidates), "Use at least one output", this);
        return;
      }

      var duplicateNames = ElementCandidates
        .Filter(ec => ec.IsUsed)
        .GroupBy(ec => ec.Name)
        .Filter(g => g.Count() > 1)
        .Map(g => g.First().Name);

      if (duplicateNames.Any())
      {
        var duplicated = string.Join(", ", duplicateNames);
        _appService.Notify(
          NotificationType.Error,
          nameof(ImportExecViewModel),
          nameof(ElementCandidates),
          $"Duplicate output names found: {duplicated}",
          this
          );
        return;
      }

      if (SimulationName.IsntAString())
      {
        _appService.Notify(
          NotificationType.Error,
          nameof(ImportExecViewModel),
          nameof(SimulationName),
          "Provide a name for the simulation",
          this
          );
        return;
      }

      DialogResult = true;
    }

    private readonly ManagedImport _managedImport;
    private readonly IAppService _appService;
    private readonly Arr<IElementCandidateViewModel> _elementCandidates;
  }
}
