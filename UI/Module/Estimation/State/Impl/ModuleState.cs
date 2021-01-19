using LanguageExt;
using RVis.Base;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.AppInf;
using RVisUI.Model;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Estimation
{
  internal sealed partial class ModuleState
  {
    private class _PriorsStateDTO
    {
      public string? SelectedPrior { get; set; }
    }

    private class _LikelihoodStateDTO
    {
      public string? SelectedOutput { get; set; }
    }

    private class _DesignStateDTO
    {
      public int? Iterations { get; set; }
      public int? BurnIn { get; set; }
      public int? Chains { get; set; }
    }

    private class _SimulationStateDTO
    {
      public string? SelectedParameter { get; set; }
    }

    private class _PriorStateDTO
    {
      public string? Name { get; set; }
      public string? DistributionType { get; set; }
      public string[]? DistributionStates { get; set; }
      public bool IsSelected { get; set; }
    }

    private class _OutputStateDTO
    {
      public string? Name { get; set; }
      public string? ErrorModelType { get; set; }
      public string[]? ErrorModelStates { get; set; }
      public bool IsSelected { get; set; }
    }

    private class _ModuleStateDTO
    {
      public _PriorsStateDTO? PriorsState { get; set; }
      public _LikelihoodStateDTO? LikelihoodState { get; set; }
      public _DesignStateDTO? DesignState { get; set; }
      public _SimulationStateDTO? SimulationState { get; set; }
      public _PriorStateDTO[]? PriorStates { get; set; }
      public _OutputStateDTO[]? OutputStates { get; set; }
      public string[]? SelectedObservationsReferences { get; set; }
      public string? EstimationDesign { get; set; }
      public string? RootExportDirectory { get; set; }
      public bool OpenAfterExport { get; set; }
      public bool? AutoApplyParameterSharedState { get; set; } = false;
      public bool? AutoShareParameterSharedState { get; set; } = false;
      public bool? AutoApplyElementSharedState { get; set; }
      public bool? AutoShareElementSharedState { get; set; }
      public bool? AutoApplyObservationsSharedState { get; set; }
      public bool? AutoShareObservationsSharedState { get; set; }
    }

    internal static void Save(ModuleState instance, Simulation simulation, ISimEvidence evidence)
    {
      simulation.SavePrivateData(
        new _ModuleStateDTO
        {
          PriorsState = new _PriorsStateDTO
          {
            SelectedPrior = instance.PriorsState.SelectedPrior
          },

          LikelihoodState = new _LikelihoodStateDTO
          {
            SelectedOutput = instance.LikelihoodState.SelectedOutput
          },

          DesignState = new _DesignStateDTO
          {
            Iterations = instance.DesignState.Iterations,
            BurnIn = instance.DesignState.BurnIn,
            Chains = instance.DesignState.Chains
          },

          SimulationState = new _SimulationStateDTO
          {
            SelectedParameter = instance.SimulationState.SelectedParameter
          },

          PriorStates = instance.PriorStates
          .Map(ps => new _PriorStateDTO
          {
            Name = ps.Name,
            DistributionType = ps.DistributionType.ToString(),
            DistributionStates = Distribution.SerializeDistributions(ps.Distributions),
            IsSelected = ps.IsSelected
          })
          .ToArray(),

          OutputStates = instance.OutputStates
          .Map(os => new _OutputStateDTO
          {
            Name = os.Name,
            ErrorModelType = os.ErrorModelType.ToString(),
            ErrorModelStates = ErrorModel.SerializeErrorModels(os.ErrorModels),
            IsSelected = os.IsSelected
          })
          .ToArray(),

          SelectedObservationsReferences = instance.SelectedObservations
            .Map(o => evidence.GetReference(o))
            .ToArray(),

          EstimationDesign = instance.EstimationDesign?.CreatedOn.ToDirectoryName(),

          RootExportDirectory = instance.RootExportDirectory,
          OpenAfterExport = instance.OpenAfterExport,

          AutoApplyParameterSharedState = instance.AutoApplyParameterSharedState,
          AutoShareParameterSharedState = instance.AutoShareParameterSharedState,
          AutoApplyElementSharedState = instance.AutoApplyElementSharedState,
          AutoShareElementSharedState = instance.AutoShareElementSharedState,
          AutoApplyObservationsSharedState = instance.AutoApplyObservationsSharedState,
          AutoShareObservationsSharedState = instance.AutoShareObservationsSharedState
        },
        nameof(Estimation),
        nameof(ViewModel),
        nameof(ModuleState)
        );
    }

    internal static ModuleState LoadOrCreate(
      Simulation simulation,
      ISimEvidence evidence,
      IAppService appService,
      EstimationDesigns estimationDesigns
      )
    {
      var maybeDTO = simulation.LoadPrivateData<_ModuleStateDTO>(
        nameof(Estimation),
        nameof(ViewModel),
        nameof(ModuleState)
        );

      return maybeDTO.Match(
        dto => new ModuleState(dto, evidence, appService, estimationDesigns),
        () => new ModuleState(evidence, appService, estimationDesigns)
        );
    }

    private ModuleState(
      _ModuleStateDTO dto,
      ISimEvidence evidence,
      IAppService appService,
      EstimationDesigns estimationDesigns
      )
    {
      _estimationDesigns = estimationDesigns;

      PriorsState.SelectedPrior = dto.PriorsState?.SelectedPrior;

      LikelihoodState.SelectedOutput = dto.LikelihoodState?.SelectedOutput;

      DesignState.Iterations = dto.DesignState?.Iterations;
      DesignState.BurnIn = dto.DesignState?.BurnIn;
      DesignState.Chains = dto.DesignState?.Chains;

      SimulationState.SelectedParameter = dto.SimulationState?.SelectedParameter;

      if (!dto.PriorStates.IsNullOrEmpty())
      {
        _priorStates = dto.PriorStates
          .Select(ps =>
          {
            var name = ps.Name.AssertNotNull();
            var distributionType = Enum.TryParse(ps.DistributionType, out DistributionType dt) ? dt : DistributionType.None;
            var distributionStates = Distribution.DeserializeDistributions(ps.DistributionStates.AssertNotNull());
            var isSelected = ps.IsSelected;
            return new ParameterState(name, distributionType, distributionStates, isSelected);
          })
          .ToArr();
      }

      if (!dto.OutputStates.IsNullOrEmpty())
      {
        _outputStates = dto.OutputStates
          .Select(os =>
          {
            var name = os.Name.AssertNotNull();
            var errorModelType = Enum.TryParse(os.ErrorModelType, out ErrorModelType emt) ? emt : ErrorModelType.None;
            var errorModelStates = ErrorModel.DeserializeErrorModels(os.ErrorModelStates.AssertNotNull());
            var isSelected = os.IsSelected;
            return new OutputState(name, errorModelType, errorModelStates, isSelected);
          })
          .ToArr();
      }

      _selectedObservations = dto.SelectedObservationsReferences.IsNullOrEmpty()
        ? default
        : dto.SelectedObservationsReferences
            .Select(r => evidence.GetObservations(r))
            .Somes()
            .ToArr();

      if (dto.EstimationDesign.IsAString())
      {
        try
        {
          var createdOn = dto.EstimationDesign.FromDirectoryName();
          EstimationDesign = estimationDesigns.Load(createdOn);
          var pathToEstimationDesign = estimationDesigns.GetPathToEstimationDesign(EstimationDesign);
          ChainStates = ChainState.Load(pathToEstimationDesign);
          PosteriorState = PosteriorState.Load(pathToEstimationDesign);
        }
        catch (Exception) { /* logged elsewhere */ }
      }

      RootExportDirectory = dto.RootExportDirectory;
      OpenAfterExport = dto.OpenAfterExport;

      _autoApplyParameterSharedState = dto.AutoApplyParameterSharedState;
      _autoShareParameterSharedState = dto.AutoShareParameterSharedState;
      _autoApplyElementSharedState = dto.AutoApplyElementSharedState;
      _autoShareElementSharedState = dto.AutoShareElementSharedState;
      _autoApplyObservationsSharedState = dto.AutoApplyObservationsSharedState;
      _autoShareObservationsSharedState = dto.AutoShareObservationsSharedState;

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      _subscriptions = new CompositeDisposable(

        evidence.ObservationsChanges.Subscribe(
          _reactiveSafeInvoke.SuspendAndInvoke<(Arr<SimObservations> Observations, ObservableQualifier ObservableQualifier)>(
            ObserveEvidenceObservationsChanges
          )
        )

      );
    }

    private ModuleState(ISimEvidence evidence, IAppService appService, EstimationDesigns estimationDesigns)
      : this(new _ModuleStateDTO(), evidence, appService, estimationDesigns)
    {
    }
  }
}
