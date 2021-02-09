using LanguageExt;
using MailKit.Security;
using Nett;
using RVis.Base.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static System.IO.Path;

namespace RVisUI.Model
{
  public static class RunControlSpec
  {
    public static Arr<(string Name, string File, TomlTable Spec)> LoadJobSpecs(IAppSettings appSettings)
    {
      var pathToDropDirectory = appSettings.PathToRunControlDrop.ExpandPath();

      if (!Directory.Exists(pathToDropDirectory)) return default;

      const string jobExtension = ".job.toml";

      var jobFiles = Directory.GetFiles(pathToDropDirectory, $"*{jobExtension}");

      Option<(string Name, string File, TomlTable Spec)> LoadJobSpec(string jobFile)
      {
        var jobFileName = GetFileName(jobFile);
        var jobName = jobFileName[0..(jobFileName.Length - jobExtension.Length)];
        var pathToOutputDirectory = Combine(pathToDropDirectory, jobName);

        if (!Directory.Exists(pathToOutputDirectory))
        {
          try
          {
            var jobSpec = Toml.ReadFile(jobFile);
            return Some((jobName, jobFile, jobSpec));
          }
          catch (Exception ex)
          {
            Logger.Log.Error(ex);
          }
        }

        return None;
      }

      var jobSpecs = jobFiles
        .Select(LoadJobSpec)
        .Somes()
        .ToArr();

      return jobSpecs;
    }

    public static RunControl? ToRunControl(
      Arr<(string Name, string File, TomlTable Spec)> jobSpecs,
      Arr<ModuleInfo> moduleInfos,
      IAppSettings appSettings
      )
    {
      try
      {
        var runControlJobs = jobSpecs
          .Map(js => ToRunControlJob(js.Name, js.File, js.Spec, moduleInfos))
          .ToArr();

        return new RunControl(runControlJobs, appSettings);
      }
      catch (Exception ex)
      {
        Logger.Log.Error(ex);
      }

      return null;
    }

    private static RunControlJob ToRunControlJob(
      string name,
      string file,
      TomlTable spec,
      Arr<ModuleInfo> moduleInfos
      )
    {
      var taskSpecs = RequireInstanceOf<TomlTableArray>(spec["task"], "Missing or invalid task(s)");

      var tasks = taskSpecs.Items
        .Select(tt => ToRunControlTask(tt, moduleInfos))
        .ToArr();

      RequireUniqueElements(
        tasks,
        t => t.Name,
        $"Found tasks with duplicate name in {file}"
        );

      var configuration = ToConfiguration(
        RequireInstanceOf<TomlTable>(spec["configuration"], "Missing or invalid configuration")
        );

      var notification = spec.ContainsKey("notification")
        ? ToNotification(RequireInstanceOf<TomlTable>(spec["notification"], "Invalid notification"))
        : null;

      var specHash = spec.ToString().ToHashBytes();

      return new RunControlJob(name, file, configuration, notification, tasks, specHash);
    }

    private static RunControlNotification? ToNotification(TomlTable tomlTable)
    {
      try
      {
        var emailAddress = tomlTable["email_address"].Get<string>();
        var password = tomlTable["password"].Get<string>();
        var smtpAddress = tomlTable["smtp_address"].Get<string>();
        var smtpPort = tomlTable["smtp_port"].Get<int>();
        var secureSocket = tomlTable["secure_socket"].Get<SecureSocketOptions>();

        return new RunControlNotification(emailAddress, password, smtpAddress, smtpPort, secureSocket);
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException($"Invalid notification ({ex.Message})", ex);
      }
    }

    private static RunControlConfiguration ToConfiguration(TomlTable tomlTable)
    {
      try
      {
        var title = tomlTable["title"].Get<string>();
        var simulation = tomlTable["simulation"].Get<string>();
        var maxCores = tomlTable["max_cores"].Get<int>();

        return new RunControlConfiguration(title, simulation, maxCores);
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException($"Invalid configuration ({ex.Message})", ex);
      }
    }

    private static IRunControlTask ToRunControlTask(
      TomlTable taskSpec,
      Arr<ModuleInfo> moduleInfos
      )
    {
      RequireTrue(taskSpec.ContainsKey("name"), "Task spec missing name");
      RequireTrue(taskSpec.ContainsKey("type"), "Task spec missing type");

      var type = taskSpec["type"].Get<string>();

      RequireNotNullEmptyWhiteSpace(type, "Task missing type");

      var moduleInfo = moduleInfos
        .Find(mi => mi.SupportedTaskNames.Contains(type, StringComparer.InvariantCultureIgnoreCase))
        .IfNone(() => throw new InvalidOperationException($"Unsupported task type: {type}"));

      return moduleInfo.Service.GetRunControlTask(type, taskSpec);
    }
  }
}
