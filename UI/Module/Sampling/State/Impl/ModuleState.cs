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

    private class _SamplesStateDTO
    {
      public int? NumberOfSamples { get; set; }
      public int? Seed { get; set; }
      public _LatinHypercubeDesignDTO LatinHypercubeDesign { get; set; }
      public _RankCorrelationDesignDTO RankCorrelationDesign { get; set; }
    }

    private class _OutputsStateDTO
    {
      public string SelectedOutputName { get; set; }
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
      public _SamplesStateDTO SamplesState { get; set; }
      public _OutputsStateDTO OutputsState { get; set; }
      public _ParameterStateDTO[] ParameterStates { get; set; }
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

          SamplesState = new _SamplesStateDTO
          {
            NumberOfSamples = instance.SamplesState.NumberOfSamples,
            Seed = instance.SamplesState.Seed,
            LatinHypercubeDesign = instance.SamplesState.LatinHypercubeDesign.ToDTO(),
            RankCorrelationDesign = instance.SamplesState.RankCorrelationDesign.ToDTO()
          },

          OutputsState = new _OutputsStateDTO
          {
            SelectedOutputName = instance.OutputsState.SelectedOutputName
          },

          ParameterStates = instance.ParameterStates
          .Map(ps => new _ParameterStateDTO
          {
            Name = ps.Name,
            DistributionType = ps.DistributionType.ToString(),
            DistributionStates = Distribution.SerializeDistributions(ps.Distributions),
            IsSelected = ps.IsSelected
          })
          .OrderBy(ps => ps.Name.ToUpperInvariant())
          .ToArray(),

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

      SamplesState.NumberOfSamples = dto.SamplesState?.NumberOfSamples;
      SamplesState.Seed = dto.SamplesState?.Seed;
      SamplesState.LatinHypercubeDesign = dto.SamplesState?.LatinHypercubeDesign.FromDTO() ?? default;
      SamplesState.RankCorrelationDesign = dto.SamplesState?.RankCorrelationDesign.FromDTO() ?? default;

      OutputsState.SelectedOutputName = dto.OutputsState?.SelectedOutputName;

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

      if (dto.SamplingDesign.IsAString())
      {
        try
        {
          var createdOn = dto.SamplingDesign.FromDirectoryName();
          SamplingDesign = samplingDesigns.Load(createdOn);
          Samples = SamplingDesign.Samples;
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
