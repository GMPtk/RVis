using LanguageExt;
using ReactiveUI;
using RVis.Base.Extensions;
using RVis.Model;
using RVisUI.Model;
using System;
using System.Linq;
using System.Windows.Input;
using static RVis.Base.Check;

namespace RVisUI.Mvvm
{
  public class ImportExecViewModel : ReactiveObject, IImportExecViewModel
  {
    public ImportExecViewModel(ManagedImport managedImport, IAppService appService)
    {
      RequireNotNull(managedImport.ExecutorFunction);
      RequireNotNull(managedImport.ExecutorFormal);
      RequireNotNull(managedImport.ExecutorIndependentVariable);

      _managedImport = managedImport;
      _appService = appService;

      ExecInvocation = $"output <- {managedImport.ExecutorFunction.Symbol}({managedImport.ExecutorFormal.Symbol})";

      var changeParameterDescriptionUnit = 
        ReactiveCommand.Create<IParameterCandidateViewModel>(HandleChangeParameterDescriptionUnit);

      ParameterCandidates = managedImport.ExecutorParameterCandidates.Map<IParameterCandidateViewModel>(pc =>
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

      var changeElementDescriptionUnit = 
        ReactiveCommand.Create<IElementCandidateViewModel>(HandleChangeElementDescriptionUnit);

      var ivValueCandidate = managedImport.ExecutorIndependentVariable;
      var ivElementCandidate = ivValueCandidate.ElementCandidates[0];

      IndependentVariable = new ElementCandidateViewModel(
        ivElementCandidate.IsUsed,
        ivElementCandidate.Name,
        ivValueCandidate.Name,
        true,
        ivElementCandidate.Values,
        ivElementCandidate.Unit,
        ivElementCandidate.Description,
        changeElementDescriptionUnit
        );

      ElementCandidates = managedImport.ExecutorValueCandidates
        .Bind(vc =>
          vc.ElementCandidates.Map(ec =>
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
        )
        .ToArr<IElementCandidateViewModel>();

      UseAllOutputs = ReactiveCommand.Create(HandleUseAllOutputs);
      UseNoOutputs = ReactiveCommand.Create(HandleUseNoOutputs);

      _simulationName = managedImport.SimulationName;
      _simulationDescription = managedImport.SimulationDescription;

      OK = ReactiveCommand.Create(HandleOK);
      Cancel = ReactiveCommand.Create(() => DialogResult = false);
    }

    public string ExecInvocation { get; }

    public Arr<IParameterCandidateViewModel> ParameterCandidates { get; }

    public ICommand UseAllParameters { get; }

    public ICommand UseNoParameters { get; }

    public IElementCandidateViewModel IndependentVariable { get; }

    public Arr<IElementCandidateViewModel> ElementCandidates { get; }

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
      var executorParameterCandidate = _managedImport.ExecutorParameterCandidates.Find(
        pc => pc.Name == parameterCandidateViewModel.Name
        ).AssertSome();

      var changeDescriptionUnitViewModel = new ChangeDescriptionUnitViewModel(
        executorParameterCandidate.Name,
        executorParameterCandidate.Description,
        executorParameterCandidate.Unit,
        _managedImport.SymbolInfos
        );

      var didChange = _appService.ShowDialog(changeDescriptionUnitViewModel, this);
      if (didChange)
      {
        executorParameterCandidate.Description = changeDescriptionUnitViewModel.Description;
        executorParameterCandidate.Unit = changeDescriptionUnitViewModel.Unit;

        parameterCandidateViewModel.Description = executorParameterCandidate.Description;
        parameterCandidateViewModel.Unit = executorParameterCandidate.Unit;
      }
    }

    private void HandleChangeElementDescriptionUnit(IElementCandidateViewModel elementCandidateViewModel)
    {
      var executorValueCandidate = ReferenceEquals(elementCandidateViewModel, IndependentVariable) ?
        _managedImport.ExecutorIndependentVariable :
        _managedImport.ExecutorValueCandidates.Find(
        pc => pc.Name == elementCandidateViewModel.Name
        ).AssertSome();

      RequireNotNull(executorValueCandidate);

      var executorElementCandidate = executorValueCandidate.ElementCandidates[0];

      var changeDescriptionUnitViewModel = new ChangeDescriptionUnitViewModel(
        executorElementCandidate.Name,
        executorElementCandidate.Description,
        executorElementCandidate.Unit,
        _managedImport.SymbolInfos
        );

      var didChange = _appService.ShowDialog(changeDescriptionUnitViewModel, this);
      if (didChange)
      {
        executorElementCandidate.Description = changeDescriptionUnitViewModel.Description;
        executorElementCandidate.Unit = changeDescriptionUnitViewModel.Unit;

        elementCandidateViewModel.Description = executorElementCandidate.Description;
        elementCandidateViewModel.Unit = executorElementCandidate.Unit;
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
        _appService.Notify(
          NotificationType.Error, 
          nameof(ImportExecViewModel), 
          nameof(ParameterCandidates), 
          "Use at least one parameter", 
          this
          );
        return;
      }

      if (ElementCandidates.ForAll(ec => !ec.IsUsed))
      {
        _appService.Notify(
          NotificationType.Error, 
          nameof(ImportExecViewModel), 
          nameof(ElementCandidates), 
          "Use at least one output", 
          this
          );
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
  }
}
