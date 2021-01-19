using Nett;
using RVisUI.Model;
using System;

namespace Sampling
{
  internal static class SamplingTasks
  {
    internal static IRunControlTask Create(
      string type, 
      TomlTable taskSpec, 
      IAppState appState, 
      IAppService appService
      )
    {
      return $"{type}Task" switch
      {
        nameof(DistributionSamplingTask) => DistributionSamplingTask.Create(taskSpec, appState, appService),
        nameof(FileSamplingTask) => FileSamplingTask.Create(taskSpec, appState, appService),

        _ => throw new ArgumentOutOfRangeException(nameof(type), "Unsupported sampling task")
      };
    }
  }
}
