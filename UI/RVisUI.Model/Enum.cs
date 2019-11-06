using System;

namespace RVisUI.Model
{
  public enum NotificationType
  {
    Information,
    Warning,
    Error
  }

  public enum StartUpOption
  {
    None,
    ExtraModulePath, e,
    LoadSim, l,
    Name, n,
  }

  [Flags]
  public enum SimSharedStateBuild
  {
    Parameters = 0b_0000_0001,
    Outputs = 0b_0000_0010,
    Observations = 0b_0000_0100,
    All = Parameters | Outputs | Observations
  }

  [Flags]
  public enum SimSharedStateApply
  {
    Parameters = 0b_0000_0000_0000_0001,
    Outputs = 0b_0000_0000_0000_0010,
    Observations = 0b_0000_0000_0000_0100,

    Single = 0b_0000_0001_0000_0000,
    Set = 0b_0000_0010_0000_0000,

    All = Parameters | Outputs | Observations
  }

  [Flags]
  public enum ModulePurpose
  {
    None = 0,

    // Activity
    Parameter       = 0b_0000_0000_0000_0000_0000_0001,
    Output          = 0b_0000_0000_0000_0000_0000_0010,
    Preliminary     = 0b_0000_0000_0000_0000_0000_0100,
    Case            = 0b_0000_0000_0000_0000_0000_1000,

    // Subcategory
    Sensitivity     = 0b_0000_0000_0000_0001_0000_0000,
    Sampling        = 0b_0000_0000_0000_0010_0000_0000,
    Estimation      = 0b_0000_0000_0000_0100_0000_0000,

    // Category
    Inspection      = 0b_0000_0001_0000_0000_0000_0000,
    Uncertainty     = 0b_0000_0010_0000_0000_0000_0000,
    Calibration     = 0b_0000_0100_0000_0000_0000_0000,
    Administration  = 0b_0000_1000_0000_0000_0000_0000,
    //Configuration,
  }
}
