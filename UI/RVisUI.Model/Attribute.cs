using System;
using LanguageExt;

namespace RVisUI.Model
{
  [AttributeUsage(AttributeTargets.Class)]
  public class DisplayIconAttribute : Attribute
  {
    public DisplayIconAttribute(string iconName) : base()
    {
      IconName = iconName;
    }

    public string IconName { get; }
  }

  [AttributeUsage(AttributeTargets.Class)]
  public class PurposeAttribute : Attribute
  {
    public PurposeAttribute(ModulePurpose modulePurpose) : base()
    {
      ModulePurpose = modulePurpose;
    }

    public ModulePurpose ModulePurpose { get; }
  }

  [AttributeUsage(AttributeTargets.Class)]
  public class RequiredRPackagesAttribute : Attribute
  {
    public RequiredRPackagesAttribute(params string[] packageNames) : base()
    {
      PackageNames = packageNames;
    }

    public Arr<string> PackageNames { get; }
  }

  [AttributeUsage(AttributeTargets.Class)]
  public class SupportedTasksAttribute : Attribute
  {
    public SupportedTasksAttribute(params string[] taskNames) : base()
    {
      TaskNames = taskNames;
    }

    public Arr<string> TaskNames { get; }
  }
}
