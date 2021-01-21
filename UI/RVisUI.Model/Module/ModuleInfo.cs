using LanguageExt;
using RVis.Base.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using static RVis.Base.Check;
using static System.Globalization.CultureInfo;
using static System.String;

namespace RVisUI.Model
{
  public sealed class ModuleInfo
  {
    internal ModuleInfo(
      string id,
      string displayName,
      string? displayIcon,
      string description,
      Arr<string> requiredRPackageNames,
      ModulePurpose modulePurpose,
      Arr<string> supportedTaskNames,
      IRVisExtensibility service,
      string assemblyVersion,
      string key
      )
    {
      ID = id;
      DisplayName = displayName;
      DisplayIcon = displayIcon;
      Description = description;
      RequiredRPackageNames = requiredRPackageNames;
      ModulePurpose = modulePurpose;
      SupportedTaskNames = supportedTaskNames;
      Service = service;
      AssemblyVersion = assemblyVersion;
      Key = key;
    }

    public string ID { get; }
    public string DisplayName { get; }
    public string? DisplayIcon { get; }
    public string Description { get; }
    public Arr<string> RequiredRPackageNames { get; }
    public ModulePurpose ModulePurpose { get; }
    public Arr<string> SupportedTaskNames { get; }
    public bool IsEnabled { get; set; }
    public IRVisExtensibility Service { get; }
    public string AssemblyVersion { get; }
    internal string Key { get; }

    public static Arr<ModuleInfo> GetModuleInfos(Arr<IRVisExtensibility> services)
    {
      var list = new List<ModuleInfo>();

      foreach (var service in services)
      {
        var type = service.GetType();
        var id = type.GUID.ToString("N", InvariantCulture).ToLowerInvariant();
        var displayName = type.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? type.Name;
        var displayIcon = type.GetCustomAttribute<DisplayIconAttribute>()?.IconName;
        var description = type.GetCustomAttribute<DescriptionAttribute>()?.Description ?? type.Name;
        var modulePurpose = type.GetCustomAttribute<PurposeAttribute>()?.ModulePurpose ?? ModulePurpose.None;
        var requiredRPackageNames = type.GetCustomAttribute<RequiredRPackagesAttribute>()?.PackageNames ?? default;
        var supportedTaskNames = type.GetCustomAttribute<SupportedTasksAttribute>()?.TaskNames ?? default;
        var assemblyVersion = type.Assembly.GetName().Version!.AsMmbrString();

        var moduleInfo = new ModuleInfo(
          id,
          displayName,
          displayIcon,
          description,
          requiredRPackageNames,
          modulePurpose,
          supportedTaskNames,
          service,
          assemblyVersion,
          displayName.ToKey()
        );

        var dropModuleInfo = list.Any(
          mi => mi.Key == moduleInfo.Key &&
          0 <= Compare(mi.AssemblyVersion, moduleInfo.AssemblyVersion, StringComparison.InvariantCulture)
          );

        if (!dropModuleInfo) list.Add(moduleInfo);
      }

      RequireUniqueElements(list, mi => mi.ID, "Found modules with duplicate IDs");
      RequireUniqueElements(list, mi => mi.Key, "Found modules with duplicate keys");

      var allTaskNames = list.SelectMany(l => l.SupportedTaskNames);
      RequireUniqueElements(allTaskNames, tn => tn, "Found duplicate task names among modules");

      return list.ToArr();
    }

    public static Arr<ModuleInfo> SortAndEnable(Arr<ModuleInfo> moduleInfos, string moduleConfiguration)
    {
      RequireUniqueElements(moduleInfos, mi => mi.ID);

      Arr<(string ID, bool IsEnabled, int Index)> moduleConfigs = default;

      if (moduleConfiguration.IsAString())
      {
        var perModuleInfoParts = moduleConfiguration.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        (string, bool, int) ToModConf(int ordinal, string entry)
        {
          var parts = entry.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
          if (2 != parts.Length) return default;
          var id = parts[0];
          if (id.IsntAString()) return default;
          if (!moduleInfos.Exists(mi => mi.ID == id)) return default;
          if (!bool.TryParse(parts[1], out bool isEnabled)) return default;
          return (id, isEnabled, ordinal);
        }

        moduleConfigs = perModuleInfoParts
          .Map(ToModConf)
          .Filter(a => a != default)
          .ToArr();
      }

      if (moduleConfigs.IsEmpty)
      {
        moduleInfos.Iter(mi => mi.IsEnabled = true);
        return moduleInfos.OrderBy(mi => mi.ModulePurpose).ToArr();
      }

      var indexOffset = moduleConfigs.Count;

      (ModuleInfo ModuleInfo, int Index) Index(ModuleInfo mi)
      {
        var index = moduleConfigs.FindIndex(a => a.ID == mi.ID);

        if (index.IsFound())
        {
          mi.IsEnabled = moduleConfigs[index].IsEnabled;
        }
        else
        {
          mi.IsEnabled = false;
          index = indexOffset + (int)mi.ModulePurpose;
        }

        return (mi, index);
      }

      return moduleInfos
        .Map(Index)
        .OrderBy(a => a.Index)
        .Map(a => a.ModuleInfo)
        .ToArr();
    }

    public static string GetModuleConfiguration(Arr<ModuleInfo> moduleInfos) =>
      Join(";", moduleInfos.Map(mi => $"{mi.ID}:{mi.IsEnabled}"));
  }
}
