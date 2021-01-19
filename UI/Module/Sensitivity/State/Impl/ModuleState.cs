using LanguageExt;
using OxyPlot;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.AppInf;
using System;
using System.Linq;
using System.Reactive.Linq;
using static System.Double;

namespace Sensitivity
{
  internal sealed partial class ModuleState
  {
    private class _ParametersStateDTO
    {
      public string? SelectedParameter { get; set; }
    }

    private class _DesignStateDTO
    {
      public string? SensitivityMethod { get; set; }
      public int? NoOfRuns { get; set; }
      public int? NoOfSamples { get; set; }
      public string? SelectedElementName { get; set; }
    }

    private class _TraceStateDTO
    {
      public double? ViewHeight { get; set; }
      public string? ChartTitle { get; set; }
      public string? YAxisTitle { get; set; }
      public string? XAxisTitle { get; set; }
      public string? MarkerFill { get; set; }
      public string? SeriesColor { get; set; }
      public double? HorizontalAxisMinimum { get; set; }
      public double? HorizontalAxisMaximum { get; set; }
      public double? HorizontalAxisAbsoluteMinimum { get; set; }
      public double? HorizontalAxisAbsoluteMaximum { get; set; }
      public double? VerticalAxisMinimum { get; set; }
      public double? VerticalAxisMaximum { get; set; }
      public double? VerticalAxisAbsoluteMinimum { get; set; }
      public double? VerticalAxisAbsoluteMaximum { get; set; }
    }

    private class _LowryStateDTO
    {
      public string? ChartTitle { get; set; }
      public string? YAxisTitle { get; set; }
      public string? XAxisTitle { get; set; }
      public string? InteractionsFillColor { get; set; }
      public string? MainEffectsFillColor { get; set; }
      public string? SmokeFill { get; set; }
    }

    private class _MeasuresStateDTO
    {
      public string? SelectedOutputName { get; set; }
    }

    private class _ParameterStateDTO
    {
      public string? Name { get; set; }
      public string? DistributionType { get; set; }
      public string[]? DistributionStates { get; set; }
      public bool IsSelected { get; set; }
    }

    private class _ModuleStateDTO
    {
      public _ParametersStateDTO? ParametersState { get; set; }
      public _DesignStateDTO? DesignState { get; set; }
      public _TraceStateDTO? TraceState { get; set; }
      public _LowryStateDTO? LowryState { get; set; }
      public _MeasuresStateDTO? MeasuresState { get; set; }
      public _ParameterStateDTO[]? ParameterStates { get; set; }
      public string? SensitivityDesign { get; set; }
      public string? RootExportDirectory { get; set; }
      public bool OpenAfterExport { get; set; }
      public bool? AutoApplyParameterSharedState { get; set; } = false;
      public bool? AutoShareParameterSharedState { get; set; } = false;
      public bool? AutoApplyElementSharedState { get; set; }
      public bool? AutoShareElementSharedState { get; set; }
      public bool? AutoApplyObservationsSharedState { get; set; }
      public bool? AutoShareObservationsSharedState { get; set; }
    }

    private static double? ToDTOAxisValue(double d) =>
      IsNaN(d) || d == MaxValue || d == MinValue || IsInfinity(d) ? default(double?) : d;

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
            SensitivityMethod = instance.DesignState.SensitivityMethod?.ToString(),
            NoOfRuns = instance.DesignState.NoOfRuns,
            NoOfSamples = instance.DesignState.NoOfSamples,
            SelectedElementName = instance.DesignState.SelectedElementName
          },

          TraceState = new _TraceStateDTO
          {
            ViewHeight = instance.TraceState.ViewHeight,
            MarkerFill = instance.TraceState.MarkerFill?.ToByteString(),
            SeriesColor = instance.TraceState.SeriesColor?.ToByteString(),
            HorizontalAxisMinimum = ToDTOAxisValue(instance.TraceState.HorizontalAxisMinimum),
            HorizontalAxisMaximum = ToDTOAxisValue(instance.TraceState.HorizontalAxisMaximum),
            HorizontalAxisAbsoluteMinimum = ToDTOAxisValue(instance.TraceState.HorizontalAxisAbsoluteMinimum),
            HorizontalAxisAbsoluteMaximum = ToDTOAxisValue(instance.TraceState.HorizontalAxisAbsoluteMaximum),
            VerticalAxisMinimum = ToDTOAxisValue(instance.TraceState.VerticalAxisMinimum),
            VerticalAxisMaximum = ToDTOAxisValue(instance.TraceState.VerticalAxisMaximum),
            VerticalAxisAbsoluteMinimum = ToDTOAxisValue(instance.TraceState.VerticalAxisAbsoluteMinimum),
            VerticalAxisAbsoluteMaximum = ToDTOAxisValue(instance.TraceState.VerticalAxisAbsoluteMaximum)
          },

          LowryState = new _LowryStateDTO
          {
            ChartTitle = instance.LowryState.ChartTitle,
            XAxisTitle = instance.LowryState.XAxisTitle,
            YAxisTitle = instance.LowryState.YAxisTitle,
            InteractionsFillColor = instance.LowryState.InteractionsFillColor?.ToByteString(),
            MainEffectsFillColor = instance.LowryState.MainEffectsFillColor?.ToByteString(),
            SmokeFill = instance.LowryState.SmokeFill?.ToByteString()
          },

          MeasuresState = new _MeasuresStateDTO
          {
            SelectedOutputName = instance.MeasuresState.SelectedOutputName
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

          SensitivityDesign = instance.SensitivityDesign?.CreatedOn.ToDirectoryName(),

          RootExportDirectory = instance.RootExportDirectory,
          OpenAfterExport = instance.OpenAfterExport,

          AutoApplyParameterSharedState = instance.AutoApplyParameterSharedState,
          AutoShareParameterSharedState = instance.AutoShareParameterSharedState,
          AutoApplyElementSharedState = instance.AutoApplyElementSharedState,
          AutoShareElementSharedState = instance.AutoShareElementSharedState,
          AutoApplyObservationsSharedState = instance.AutoApplyObservationsSharedState,
          AutoShareObservationsSharedState = instance.AutoShareObservationsSharedState
        },
        nameof(Sensitivity),
        nameof(ViewModel),
        nameof(ModuleState)
        );
    }

    internal static ModuleState LoadOrCreate(Simulation simulation, SensitivityDesigns sensitivityDesigns)
    {
      var maybeDTO = simulation.LoadPrivateData<_ModuleStateDTO>(
        nameof(Sensitivity),
        nameof(ViewModel),
        nameof(ModuleState)
        );

      return maybeDTO.Match(
        dto => new ModuleState(dto, sensitivityDesigns),
        () => new ModuleState(sensitivityDesigns)
        );
    }

    private ModuleState(_ModuleStateDTO dto, SensitivityDesigns sensitivityDesigns)
    {
      ParametersState.SelectedParameter = dto.ParametersState?.SelectedParameter;

      DesignState.SensitivityMethod = 
        Enum.TryParse(dto.DesignState?.SensitivityMethod, out SensitivityMethod sensitivityMethod) 
        ? sensitivityMethod 
        : default(SensitivityMethod?);

      DesignState.NoOfRuns = dto.DesignState?.NoOfRuns;
      DesignState.NoOfSamples = dto.DesignState?.NoOfSamples;
      
      DesignState.SelectedElementName = dto.DesignState?.SelectedElementName;

      if (dto.TraceState == default)
      {
        TraceState.VerticalAxisMinimum = 0d;
        TraceState.VerticalAxisAbsoluteMinimum = 0d;
      }
      else
      {
        TraceState.ViewHeight = dto.TraceState.ViewHeight;
        TraceState.ChartTitle = dto.TraceState.ChartTitle;
        TraceState.XAxisTitle = dto.TraceState.XAxisTitle;
        TraceState.YAxisTitle = dto.TraceState.YAxisTitle;
        if (dto.TraceState?.MarkerFill.IsAString() == true)
        {
          TraceState.MarkerFill = OxyColor.Parse(dto.TraceState.MarkerFill);
        }
        if (dto.TraceState?.SeriesColor.IsAString() == true)
        {
          TraceState.SeriesColor = OxyColor.Parse(dto.TraceState.SeriesColor);
        }
        TraceState.HorizontalAxisMinimum = dto.TraceState?.HorizontalAxisMinimum ?? TraceState.HorizontalAxisMinimum;
        TraceState.HorizontalAxisMaximum = dto.TraceState?.HorizontalAxisMaximum ?? TraceState.HorizontalAxisMaximum;
        TraceState.HorizontalAxisAbsoluteMinimum = dto.TraceState?.HorizontalAxisAbsoluteMinimum ?? TraceState.HorizontalAxisAbsoluteMinimum;
        TraceState.HorizontalAxisAbsoluteMaximum = dto.TraceState?.HorizontalAxisAbsoluteMaximum ?? TraceState.HorizontalAxisAbsoluteMaximum;
        TraceState.VerticalAxisMinimum = dto.TraceState?.VerticalAxisMinimum ?? TraceState.VerticalAxisMinimum;
        TraceState.VerticalAxisMaximum = dto.TraceState?.VerticalAxisMaximum ?? TraceState.VerticalAxisMaximum;
        TraceState.VerticalAxisAbsoluteMinimum = dto.TraceState?.VerticalAxisAbsoluteMinimum ?? TraceState.VerticalAxisAbsoluteMinimum;
        TraceState.VerticalAxisAbsoluteMaximum = dto.TraceState?.VerticalAxisAbsoluteMaximum ?? TraceState.VerticalAxisAbsoluteMaximum;
      }

      if (dto.LowryState == default)
      {
        LowryState.YAxisTitle = "Total (= Main Effect + Interaction)";
      }
      else
      {
        LowryState.ChartTitle = dto.LowryState.ChartTitle;
        LowryState.XAxisTitle = dto.LowryState.XAxisTitle;
        LowryState.YAxisTitle = dto.LowryState.YAxisTitle;
        if (dto.LowryState.InteractionsFillColor?.IsAString() == true)
        {
          LowryState.InteractionsFillColor = OxyColor.Parse(dto.LowryState.InteractionsFillColor);
        }
        if (dto.LowryState.MainEffectsFillColor?.IsAString() == true)
        {
          LowryState.MainEffectsFillColor = OxyColor.Parse(dto.LowryState.MainEffectsFillColor);
        }
        if (dto.LowryState.SmokeFill?.IsAString() == true)
        {
          LowryState.SmokeFill = OxyColor.Parse(dto.LowryState.SmokeFill);
        }
      }

      MeasuresState.SelectedOutputName = dto.MeasuresState?.SelectedOutputName;

      if (!dto.ParameterStates.IsNullOrEmpty())
      {
        _parameterStates = dto.ParameterStates
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

      if (dto.SensitivityDesign.IsAString())
      {
        try
        {
          var createdOn = dto.SensitivityDesign.FromDirectoryName();
          SensitivityDesign = sensitivityDesigns.Load(createdOn);
          Trace = sensitivityDesigns.LoadTrace(SensitivityDesign);
          Ranking = sensitivityDesigns.LoadRanking(SensitivityDesign);
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

    private ModuleState(SensitivityDesigns sensitivityDesigns) : this(new _ModuleStateDTO(), sensitivityDesigns)
    {
    }
  }
}
