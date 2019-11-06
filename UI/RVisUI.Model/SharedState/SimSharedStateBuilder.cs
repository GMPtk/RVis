using LanguageExt;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using RVisUI.Model.Extensions;
using System;
using System.Linq;
using static RVis.Base.Check;
using SCG = System.Collections.Generic;

namespace RVisUI.Model
{
  public sealed class SimSharedStateBuilder : ISimSharedStateBuilder, IDisposable
  {
    public SimSharedStateBuilder(
      ISimSharedState sharedState, 
      Simulation simulation, 
      ISimEvidence evidence, 
      SimSharedStateBuild buildType
      )
    {
      _sharedState = sharedState;
      _simulation = simulation;
      _evidence = evidence;
      BuildType = buildType;
    }

    public SimSharedStateBuild BuildType { get; }

    public void AddParameter(
      string name, 
      double value, 
      double minimum, 
      double maximum, 
      Option<IDistribution> distribution
      )
    {
      RequireFalse(
        _parameterSharedStates.Any(pss => pss.Name == name),
        $"Trying to add existing parameter: {name}"
        );

      var parameters = _simulation.SimConfig.SimInput.SimParameters;
      RequireTrue(
        parameters.ContainsParameter(name),
        $"Trying to add unknown parameter to shared state: {name}"
        );

      _parameterSharedStates.Add((name, value, minimum, maximum, distribution));
    }

    public void AddOutput(string name)
    {
      var values = _simulation.SimConfig.SimOutput.SimValues;
      RequireTrue(
        values.ContainsElement(name),
        $"Trying to add unknown element to shared state: {name}"
        );

      _elementNames.Add(name);
    }

    public void AddObservations(string reference)
    {
      _evidence
        .GetObservations(reference)
        .AssertSome($"Trying to add unknown observations reference: {reference}");

      _observationsReferences.Add(reference);
    }

    public void Dispose() => Dispose(true);

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          Build();
        }

        _disposed = true;
      }
    }

    private void Build()
    {
      var sharedState = RequireInstanceOf<SimSharedState>(_sharedState);

      if (BuildType.IncludesParameters())
      {
        sharedState.SetState(_parameterSharedStates
          .Select(pss => new SimParameterSharedState(
            pss.Name,
            pss.Value,
            pss.Minimum,
            pss.Maximum,
            pss.Distribution
            )
          )
          .ToArr()
        );
      }

      if (BuildType.IncludesOutputs())
      {
        sharedState.SetState(_elementNames
          .Select(n => new SimElementSharedState(n))
          .ToArr()
          );
      }

      if (BuildType.IncludesObservations())
      {
        sharedState.SetState(_observationsReferences
          .Select(r => new SimObservationsSharedState(r))
          .ToArr()
          );
      }
    }

    private readonly ISimSharedState _sharedState;
    private readonly Simulation _simulation;
    private readonly ISimEvidence _evidence;

    private readonly SCG.List<(string Name, double Value, double Minimum, double Maximum, Option<IDistribution> Distribution)> _parameterSharedStates =
      new SCG.List<(string Name, double Value, double Minimum, double Maximum, Option<IDistribution> Distribution)>();
    private readonly SCG.HashSet<string> _elementNames =
      new SCG.HashSet<string>();
    private readonly SCG.HashSet<string> _observationsReferences =
      new SCG.HashSet<string>();

    private bool _disposed = false;
  }
}
