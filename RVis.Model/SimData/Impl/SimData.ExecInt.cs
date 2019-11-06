using LanguageExt;
using Nett;
using RVis.Model.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using static MathNet.Numerics.Statistics.SortedArrayStatistics;
using static RVis.Base.Check;
using static RVis.Model.Logger;
using static System.Convert;

namespace RVis.Model
{
  public sealed partial class SimData
  {
    internal sealed class SimExecutionInterval
    {
      internal SimExecutionInterval(Simulation simulation)
      {
        RequireNotNull(simulation);

        Simulation = simulation;
      }

      internal Simulation Simulation { get; }

      internal Option<(int ms, int n)> GetExecutionInterval()
      {
        UpdateExecutionInterval();
        return _executionInterval;
      }

      internal void Load()
      {
        var pathToExecutionInterval = Simulation.GetPrivatePath(
          new[] { nameof(SimData) },
          EXECUTION_INTERVAL_FILE_NAME
          );

        if (File.Exists(pathToExecutionInterval))
        {
          try
          {
            var tomlTable = Toml.ReadFile(pathToExecutionInterval);
            int ms; int n;
            ms = tomlTable.ContainsKey(nameof(ms)) ? tomlTable[nameof(ms)].Get<int>() : default;
            if (ms != default)
            {
              n = tomlTable.ContainsKey(nameof(n)) ? tomlTable[nameof(n)].Get<int>() : default;
              _executionInterval = (ms, n);
            }
          }
          catch (Exception ex)
          {
            Log.Error(ex, $"{nameof(SimExecutionInterval)}.{nameof(Load)}");
          }
        }
      }

      internal void Save()
      {
        UpdateExecutionInterval();

        _executionInterval.IfSome(ei =>
        {
          if (ei != default)
          {
            var (ms, n) = ei;
            var tomlTable = Toml.Create();
            tomlTable.Add(nameof(ms), ms);
            tomlTable.Add(nameof(n), n);
            var pathToExecutionInterval = Simulation.GetPrivatePath(new[] { nameof(SimData) }, EXECUTION_INTERVAL_FILE_NAME);
            try
            {
              Toml.WriteFile(tomlTable, pathToExecutionInterval);
            }
            catch (Exception ex)
            {
              Log.Error(ex, $"{nameof(SimExecutionInterval)}.{nameof(Save)}");
            }
          }
        });
      }

      internal void AddInterval(long ms) => _executionIntervals.Add(ms);

      private void UpdateExecutionInterval()
      {
        var (_, n) = _executionInterval.IfNone((default, 0));

        if (n < _executionIntervals.Count || _executionIntervals.Count > 30)
        {
          _executionIntervals.Sort();

          var median = Median(_executionIntervals.Select(ToDouble).ToArray());

          RequireTrue(median < int.MaxValue);
          var ms = ToInt32(median);

          _executionInterval = (ms, _executionIntervals.Count);
        }
      }

      private const string EXECUTION_INTERVAL_FILE_NAME = "execution-interval.toml";

      private Option<(int ms, int n)> _executionInterval;
      private readonly List<long> _executionIntervals = new List<long>();
    }
  }
}
