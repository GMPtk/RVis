using LanguageExt;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.AppInf;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace Sampling
{
  internal sealed partial class ModuleState
  {
    private class _ParametersStateDTO
    {
      public string SelectedParameter { get; set; }
    }

    private class _DesignStateDTO
    {
      public int? NumberOfSamples { get; set; }
      public int? Seed { get; set; }
      public string SelectedElementName { get; set; }
    }

    private class _ParameterStateDTO
    {
      public string Name { get; set; }
      public string DistributionType { get; set; }
      public string[] DistributionStates { get; set; }
      public bool IsSelected { get; set; }
    }

    private class _ModuleStateDTO
    {
      public _ParametersStateDTO ParametersState { get; set; }
      public _DesignStateDTO DesignState { get; set; }
      public _ParameterStateDTO[] ParameterStates { get; set; }
      public _LatinHypercubeDesignDTO LatinHypercubeDesign { get; set; }
      public string SamplingDesign { get; set; }
      public string RootExportDirectory { get; set; }
      public bool OpenAfterExport { get; set; }
      public bool? AutoApplyParameterSharedState { get; set; } = false;
      public bool? AutoShareParameterSharedState { get; set; } = false;
      public bool? AutoApplyElementSharedState { get; set; }
      public bool? AutoShareElementSharedState { get; set; }
      public bool? AutoApplyObservationsSharedState { get; set; }
      public bool? AutoShareObservationsSharedState { get; set; }
    }

    internal static void Save(ModuleState instance, Simulation simulation)
    {
      simulation.SavePrivateData(
        new _ModuleStateDTO
        {
          ParametersState = new _ParametersStateDTO
          {
            SelectedParameter = instance.ParametersState.SelectedParameter
          },

          DesignState = new _DesignStateDTO
          {
            NumberOfSamples = instance.DesignState.NumberOfSamples,
            Seed = instance.DesignState.Seed,
            SelectedElementName = instance.DesignState.SelectedElementName
          },

          ParameterStates = instance.ParameterStates
          .Map(ps => new _ParameterStateDTO
          {
            Name = ps.Name,
            DistributionType = ps.DistributionType.ToString(),
            DistributionStates = Distribution.SerializeDistributions(ps.Distributions),
            IsSelected = ps.IsSelected
          })
          .ToArray(),

          LatinHypercubeDesign = instance.LatinHypercubeDesign.ToDTO(),

          SamplingDesign = instance.SamplingDesign?.CreatedOn.ToDirectoryName(),

          RootExportDirectory = instance.RootExportDirectory,
          OpenAfterExport = instance.OpenAfterExport,

          AutoApplyParameterSharedState = instance.AutoApplyParameterSharedState,
          AutoShareParameterSharedState = instance.AutoShareParameterSharedState,
          AutoApplyElementSharedState = instance.AutoApplyElementSharedState,
          AutoShareElementSharedState = instance.AutoShareElementSharedState,
          AutoApplyObservationsSharedState = instance.AutoApplyObservationsSharedState,
          AutoShareObservationsSharedState = instance.AutoShareObservationsSharedState
        },
        nameof(Sampling),
        nameof(ViewModel),
        nameof(ModuleState)
        );
    }

    internal static ModuleState LoadOrCreate(Simulation simulation, SamplingDesigns samplingDesigns)
    {
      var maybeDTO = simulation.LoadPrivateData<_ModuleStateDTO>(
        nameof(Sampling),
        nameof(ViewModel),
        nameof(ModuleState)
        );

      return maybeDTO.Match(
        dto => new ModuleState(dto, samplingDesigns),
        () => new ModuleState(samplingDesigns)
        );
    }

    private ModuleState(_ModuleStateDTO dto, SamplingDesigns samplingDesigns)
    {
      ParametersState.SelectedParameter = dto.ParametersState?.SelectedParameter;

      DesignState.NumberOfSamples = dto.DesignState?.NumberOfSamples;
      DesignState.Seed = dto.DesignState?.Seed;
      DesignState.SelectedElementName = dto.DesignState?.SelectedElementName;

      if (!dto.ParameterStates.IsNullOrEmpty())
      {
        _parameterStates = dto.ParameterStates
          .Select(ps =>
          {
            var name = ps.Name;
            var distributionType = Enum.TryParse(ps.DistributionType, out DistributionType dt) ? dt : DistributionType.None;
            var distributionStates = Distribution.DeserializeDistributions(ps.DistributionStates);
            var isSelected = ps.IsSelected;
            return new ParameterState(name, distributionType, distributionStates, isSelected);
          })
          .ToArr();
      }

      LatinHypercubeDesign = dto.LatinHypercubeDesign.FromDTO();

      if (dto.SamplingDesign.IsAString())
      {
        try
        {
          var createdOn = dto.SamplingDesign.FromDirectoryName();
          SamplingDesign = samplingDesigns.Load(createdOn);
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
    }

    private ModuleState(SamplingDesigns samplingDesigns) : this(new _ModuleStateDTO(), samplingDesigns)
    {
    }
  }
}
