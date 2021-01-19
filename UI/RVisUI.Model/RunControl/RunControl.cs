using LanguageExt;
using MailKit.Net.Smtp;
using MimeKit;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using static RVis.Base.Check;

namespace RVisUI.Model
{
  public sealed class RunControl : IDisposable
  {
    public RunControl(
      Arr<RunControlJob> runControlJobs,
      IAppSettings appSettings
      )
    {
      _runControlJobs = runControlJobs;
      _appSettings = appSettings;
    }

    public void Dispose()
    {
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }

    public IObservable<(DateTime Timestamp, string Message)> Messages =>
      _messages.AsObservable();
    private readonly ISubject<(DateTime Timestamp, string Message)> _messages =
      new Subject<(DateTime Timestamp, string Message)>();

    public void StartJobs()
    {
      _ctsJobs = new CancellationTokenSource();

      Logger.Log.Info("Run control starting jobs");

      Task.Run(() => DoJobsAsync(_ctsJobs.Token), _ctsJobs.Token);
    }

    private async Task<bool> DoJobAsync(RunControlJob job, SimLibrary simLibrary, CancellationToken cancellationToken)
    {
      _messages.OnNext((DateTime.Now, $"Starting job \"{job.Configuration.Title}\""));

      var maybeSimulation = simLibrary.Simulations.Find(s => s.SimConfig.Title == job.Configuration.Simulation);

      async Task<bool> Some(Simulation simulation)
      {
        var pathToRunControlJob = simulation.GetPrivateDirectory(nameof(RunControl), "Jobs", job.Name);
        if (!Directory.Exists(pathToRunControlJob)) Directory.CreateDirectory(pathToRunControlJob);

        var pathToSpecHash = Path.Combine(pathToRunControlJob, "spechash.bin");
        if (File.Exists(pathToSpecHash))
        {
          var bytes = await File.ReadAllBytesAsync(pathToSpecHash, cancellationToken);

          if (!bytes.SequenceEqual(job.SpecHash))
          {
            _messages.OnNext((DateTime.Now, "Found spec/job mismatch"));

            Directory.Delete(pathToRunControlJob, recursive: true);

            _messages.OnNext((DateTime.Now, $"Remaking {pathToRunControlJob}"));

            Directory.CreateDirectory(pathToRunControlJob);
          }
        }

        if (!File.Exists(pathToSpecHash))
        {
          await File.WriteAllBytesAsync(pathToSpecHash, job.SpecHash, cancellationToken);
        }

        foreach (var task in job.Tasks)
        {
          cancellationToken.ThrowIfCancellationRequested();

          var pathToRunControlTask = Path.Combine(pathToRunControlJob, task.Name.ToValidFileName());
          if (!Directory.Exists(pathToRunControlTask)) Directory.CreateDirectory(pathToRunControlTask);

          await task.RunAsync(job.Configuration, simulation, pathToRunControlTask, _messages, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var pathToDropDirectory = Path.GetDirectoryName(job.File);
        RequireDirectory(pathToDropDirectory);
        var pathToDestination = Path.Combine(pathToDropDirectory, job.Name);
        Directory.Move(pathToRunControlJob, pathToDestination);
        var pathToJobFile = Path.Combine(pathToDestination, Path.GetFileName(job.File));
        File.Move(job.File, pathToJobFile, true);

        _messages.OnNext((DateTime.Now, $"Completed job \"{job.Configuration.Title}\""));

        var canNotify =
          job.Notification is not null &&
          job.Notification.EmailAddress.IsAString() &&
          job.Notification.Password.IsAString() &&
          job.Notification.SmtpAddress.IsAString() &&
          job.Notification.SmtpPort > 0;

        if (canNotify)
        {
          var mailMessage = new MimeMessage();
          mailMessage.From.Add(new MailboxAddress("RVis Run Control", "no-reply@rvis.app"));
          mailMessage.To.Add(new MailboxAddress(job.Notification!.EmailAddress, job.Notification.EmailAddress));
          mailMessage.Subject = $"Job Completed - {job.Configuration.Title}";
          mailMessage.Body = new TextPart("plain")
          {
            Text = $"Completed at {DateTime.Now}"
          };

          try
          {
            using var smtpClient = new SmtpClient();
            await smtpClient.ConnectAsync(
              job.Notification.SmtpAddress,
              job.Notification.SmtpPort,
              job.Notification.SecureSocket,
              cancellationToken
              );
            await smtpClient.AuthenticateAsync(
              job.Notification.EmailAddress,
              job.Notification.Password,
              cancellationToken
              );
            await smtpClient.SendAsync(
              mailMessage,
              cancellationToken
              );
            await smtpClient.DisconnectAsync(
              quit: true,
              cancellationToken
              );
          }
          catch (Exception ex)
          {
            Logger.Log.Error(ex);
          }
        }

        return true;
      }

      Task<bool> None()
      {
        _messages.OnNext((
          DateTime.Now,
          $"Job \"{job.Configuration.Title}\" specifies non-existent simulation \"{job.Configuration.Simulation}\""
          ));

        return Task.FromResult(false);
      }

      try
      {
        return await maybeSimulation.Match(Some, None);
      }
      catch (Exception ex)
      {
        _messages.OnNext((DateTime.Now, $"Error (see logging for details)"));
        Logger.Log.Error(ex);
        return false;
      }
    }

    private async Task DoJobsAsync(CancellationToken cancellationToken)
    {
      var pathToSimLibrary = _appSettings.PathToSimLibrary.ExpandPath();
      if (!Directory.Exists(pathToSimLibrary))
      {
        _messages.OnNext((DateTime.Now, $"Missing simulation library: {pathToSimLibrary}"));
        return;
      }

      var simLibrary = new SimLibrary();
      simLibrary.LoadFrom(pathToSimLibrary);

      _messages.OnNext((DateTime.Now, "Starting jobs"));

      foreach (var job in _runControlJobs)
      {
        cancellationToken.ThrowIfCancellationRequested();
        await DoJobAsync(job, simLibrary, cancellationToken);
      }

      _messages.OnNext((DateTime.Now, "Jobs completed"));
    }

    private void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          _ctsJobs?.Cancel();
          _ctsJobs?.Dispose();
        }

        _disposed = true;
      }
    }

    private readonly Arr<RunControlJob> _runControlJobs;
    private readonly IAppSettings _appSettings;
    private bool _disposed;
    private CancellationTokenSource? _ctsJobs;
  }
}
