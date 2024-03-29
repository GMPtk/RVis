﻿using LanguageExt;
using RVis.Base.Extensions;
using System;
using System.IO;
using System.Threading.Tasks;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVis.Model.Logger;
using static System.Globalization.CultureInfo;

namespace RVis.Model
{
  public class SimLibrary
  {
    public SimLibrary()
    {

    }

    public string? Location { get; private set; }

    public Arr<Simulation> Simulations { get; private set; } = Arr<Simulation>.Empty;

    public (int Successes, int Failures) LoadFrom(string location)
    {
      RequireDirectory(location);

      var (simulations, failures) = Load(location);

      Location = location;
      Simulations = simulations;

      OnLoaded();

      return (simulations.Count, failures);
    }
    private void OnLoaded() =>
      Loaded?.Invoke(this, EventArgs.Empty);
    public event EventHandler<EventArgs> Loaded = default!;

    private void OnDeleted(Simulation simulation) =>
      Deleted?.Invoke(this, new SimulationDeletedEventArgs(simulation));
    public event EventHandler<SimulationDeletedEventArgs> Deleted = default!;

    public (int Successes, int Failures) Refresh()
    {
      RequireDirectory(Location);

      var (simulations, failures) = Load(Location);

      Simulations = simulations;

      OnLoaded();

      return (simulations.Count, failures);
    }

    public string ImportRSimulation(string location, string libraryDirectoryName) => 
      ImportSimulation(location, libraryDirectoryName, "R");

    public string ImportExeSimulation(string location, string libraryDirectoryName) => 
      ImportSimulation(location, libraryDirectoryName, "exe");

    private string ImportSimulation(string location, string libraryDirectoryName, string ext)
    {
      RequireDirectory(Location);
      RequireDirectory(location);

      var diSimulation = new DirectoryInfo(location);
      var files = diSimulation.GetFiles($"*.{ext}");
      RequireEqual(files.Length, 1);

      libraryDirectoryName = GetImportDirectoryName(libraryDirectoryName, Location);
      var pathToSimulation = Path.Combine(Location, libraryDirectoryName);

      Directory.Move(location, pathToSimulation);

      return libraryDirectoryName;
    }

    public void Delete(Simulation simulation)
    {
      var containingDirectoryName = DateTime.UtcNow.ToString("o", InvariantCulture).ToValidFileName();
      var pathToContainingDirectory = Path.Combine(Path.GetTempPath(), containingDirectoryName);
      Directory.Move(simulation.PathToSimulation, pathToContainingDirectory);
      Task.Run(() => Directory.Delete(pathToContainingDirectory, true));
      try
      {
        Directory.Delete(simulation.PathToSimulation);
      }
      catch (Exception)
      {
        // sim gone from lib so swallow
      }
      Simulations = Simulations.Remove(simulation);
      OnDeleted(simulation);
    }

    private static (Arr<Simulation> Simulations, int Failures) Load(string location)
    {
      var directory = new DirectoryInfo(location);

      var factory = fun(
        (DirectoryInfo di) => Try(() => Simulation.LoadFrom(di.FullName))
        );

      var attempts = directory
        .GetDirectories()
        .ToSeq()
        .Filter(di => !di.Name.StartsWith(".", StringComparison.InvariantCulture))
        .Map(factory);

      var (exceptions, simulations) = partition(attempts);

      exceptions.Iter(Log.Error);

      return (simulations.ToArr(), exceptions.Length());
    }

    private static string GetImportDirectoryName(string directoryName, string location)
    {
      var pathToImport = Path.Combine(location, directoryName);
      if (!Directory.Exists(pathToImport)) return directoryName;

      var counter = 0;
      string mangled;
      do
      {
        ++counter;
        mangled = $"{directoryName} ({counter:000})";
        pathToImport = Path.Combine(location, mangled);
      }
      while (Directory.Exists(pathToImport));

      return mangled;
    }
  }
}
