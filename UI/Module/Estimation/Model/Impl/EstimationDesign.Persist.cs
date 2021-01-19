using LanguageExt;
using Nett;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using System;
using System.Data;
using System.IO;
using System.Linq;
using static Estimation.Logger;
using static RVis.Base.Check;
using static System.IO.Path;

namespace Estimation
{
  internal sealed partial class EstimationDesign
  {
    private class _PriorDTO
    {
      public string? Name { get; set; }
      public string? Distribution { get; set; }
    }

    private class _OutputDTO
    {
      public string? Name { get; set; }
      public string? ErrorModel { get; set; }
    }

    private class _EstimationDesignDTO
    {
      public string? CreatedOn { get; set; }
      public _PriorDTO[]? Priors { get; set; }
      public _OutputDTO[]? Outputs { get; set; }
      public string[]? ObservationsReferences { get; set; }
      public int Iterations { get; set; }
      public int BurnIn { get; set; }
      public int Chains { get; set; }
      public double? TargetAcceptRate { get; set; }
      public bool UseApproximation { get; set; } = true;
    }

    private const string DESIGN_FILE_NAME = "design.toml";

    internal static string GetPathToEstimationDesign(EstimationDesign instance, string pathToEstimationDesignsDirectory) =>
      GetPathToEstimationDesign(instance.CreatedOn, pathToEstimationDesignsDirectory);

    internal static string GetPathToEstimationDesign(DateTime createdOn, string pathToEstimationDesignsDirectory) =>
      GetPathToEstimationDesign(createdOn.ToDirectoryName(), pathToEstimationDesignsDirectory);

    internal static string GetPathToEstimationDesign(string estimationDesignDirectory, string pathToEstimationDesignsDirectory) =>
      Combine(pathToEstimationDesignsDirectory, estimationDesignDirectory);

    internal static void RemoveEstimationDesign(string pathToEstimationDesignsDirectory, DateTime createdOn)
    {
      var pathToEstimationDesignDirectory = GetPathToEstimationDesign(createdOn, pathToEstimationDesignsDirectory);

      try
      {
        Directory.Delete(pathToEstimationDesignDirectory, true);
      }
      catch (Exception ex)
      {
        Log.Error(ex, $"Failed to remove estimation design from {pathToEstimationDesignDirectory}");
      }
    }

    internal static EstimationDesign LoadEstimationDesign(string pathToEstimationDesignsDirectory, DateTime createdOn, ISimEvidence evidence)
    {
      var pathToEstimationDesignDirectory = GetPathToEstimationDesign(createdOn, pathToEstimationDesignsDirectory);

      var pathToDesign = Combine(pathToEstimationDesignDirectory, DESIGN_FILE_NAME);

      try
      {
        var dto = Toml.ReadFile<_EstimationDesignDTO>(pathToDesign);
        RequireNotNull(dto.Priors);
        var priors = dto.Priors
          .Select(dp => new ModelParameter(
            dp.Name.AssertNotNull(),
            Distribution.DeserializeDistribution(dp.Distribution).AssertSome()
            )
          )
          .ToArr();
        RequireNotNull(dto.Outputs);
        var outputs = dto.Outputs
          .Select(@do => new ModelOutput(
            @do.Name.AssertNotNull(),
            ErrorModel.DeserializeErrorModel(@do.ErrorModel.AssertNotNull()).AssertSome()
            )
          )
          .ToArr();
        var observations = dto.ObservationsReferences.IsNullOrEmpty()
          ? default
          : dto.ObservationsReferences
              .Select(r => evidence.GetObservations(r))
              .Somes()
              .ToArr();

        return new EstimationDesign(
          createdOn,
          priors,
          outputs,
          observations,
          dto.Iterations,
          dto.BurnIn,
          dto.Chains,
          dto.TargetAcceptRate,
          dto.UseApproximation
          );
      }
      catch (Exception ex)
      {
        var message = $"Failed to load estimation design from {pathToDesign}";
        Log.Error(ex, message);
        throw new Exception(message);
      }
    }

    internal static void SaveEstimationDesign(EstimationDesign instance, string pathToEstimationDesignsDirectory, ISimEvidence evidence)
    {
      RequireDirectory(pathToEstimationDesignsDirectory);

      var pathToEstimationDesignDirectory = GetPathToEstimationDesign(instance, pathToEstimationDesignsDirectory);

      RequireFalse(Directory.Exists(pathToEstimationDesignDirectory));
      Directory.CreateDirectory(pathToEstimationDesignDirectory);

      SaveDesign(
        instance.CreatedOn,
        instance.Priors,
        instance.Outputs,
        instance.Observations,
        instance.Iterations,
        instance.BurnIn,
        instance.Chains,
        instance.TargetAcceptRate,
        instance.UseApproximation,
        pathToEstimationDesignDirectory,
        evidence
        );
    }

    internal static void UpdateEstimationDesign(EstimationDesign instance, string pathToEstimationDesignsDirectory, ISimEvidence evidence)
    {
      RequireDirectory(pathToEstimationDesignsDirectory);

      var pathToEstimationDesignDirectory = GetPathToEstimationDesign(instance, pathToEstimationDesignsDirectory);

      RequireDirectory(pathToEstimationDesignDirectory);

      SaveDesign(
        instance.CreatedOn,
        instance.Priors,
        instance.Outputs,
        instance.Observations,
        instance.Iterations,
        instance.BurnIn,
        instance.Chains,
        instance.TargetAcceptRate,
        instance.UseApproximation,
        pathToEstimationDesignDirectory,
        evidence
        );
    }

    private static void SaveDesign(
      DateTime createdOn,
      Arr<ModelParameter> priors,
      Arr<ModelOutput> outputs,
      Arr<SimObservations> observations,
      int iterations,
      int burnIn,
      int chains,
      double? targetAcceptRate,
      bool useApproximation,
      string pathToEstimationDesignDirectory,
      ISimEvidence evidence
      )
    {
      RequireDirectory(pathToEstimationDesignDirectory);

      var dto = new _EstimationDesignDTO
      {
        CreatedOn = createdOn.ToDirectoryName(),

        Priors = priors
          .Map(dp => new _PriorDTO { Name = dp.Name, Distribution = dp.Distribution.ToString() })
          .ToArray(),
        Outputs = outputs
          .Map(@do => new _OutputDTO { Name = @do.Name, ErrorModel = @do.ErrorModel.ToString() })
          .ToArray(),
        ObservationsReferences = observations
          .Map(o => evidence.GetReference(o))
          .ToArray(),

        Iterations = iterations,
        BurnIn = burnIn,
        Chains = chains,
        TargetAcceptRate = targetAcceptRate,
        UseApproximation = useApproximation
      };

      var pathToDesign = Combine(pathToEstimationDesignDirectory, DESIGN_FILE_NAME);

      try
      {
        Toml.WriteFile(dto, pathToDesign);
      }
      catch (Exception ex)
      {
        var message = $"Failed to save estimation design to {pathToDesign}";
        Log.Error(ex, message);
        throw new Exception(message);
      }
    }
  }
}
