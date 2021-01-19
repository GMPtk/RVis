using LanguageExt;
using System;
using System.Collections.Generic;
using static RVis.Base.Check;

namespace Estimation
{
  internal struct OutputState : IEquatable<OutputState>
  {
    internal static OutputState Create(string name)
    {
      var errorModels = ErrorModel.GetDefaults();

      var outputState = new OutputState(
        name,
        ErrorModelType.LogNormal,
        errorModels,
        true
        );

      return outputState;
    }

    internal OutputState(
      string name,
      ErrorModelType errorModelType,
      Arr<IErrorModel> errorModels,
      bool isSelected
      )
    {
      RequireNotNullEmptyWhiteSpace(name);
      RequireFalse(errorModelType == ErrorModelType.None);
      RequireTrue(errorModels.Exists(d => d.ErrorModelType == errorModelType));

      Name = name;
      ErrorModelType = errorModelType;
      ErrorModels = errorModels;
      IsSelected = isSelected;
    }

    public string Name { get; }

    public ErrorModelType ErrorModelType { get; }

    public Arr<IErrorModel> ErrorModels { get; }

    public bool IsSelected { get; }

    public override bool Equals(object? obj)
    {
      if (obj is OutputState rhs) return Equals(rhs);
      return false;
    }

    public bool Equals(OutputState rhs) =>
      Name == rhs.Name &&
      ErrorModelType == rhs.ErrorModelType &&
      ErrorModels == rhs.ErrorModels &&
      IsSelected == rhs.IsSelected;

    public static bool operator ==(OutputState left, OutputState right) =>
      left.Equals(right);

    public static bool operator !=(OutputState left, OutputState right) =>
      !(left == right);

    public override int GetHashCode()
    {
      var hashCode = -2079731689;
      hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
      hashCode = hashCode * -1521134295 + ErrorModelType.GetHashCode();
      hashCode = hashCode * -1521134295 + EqualityComparer<Arr<IErrorModel>>.Default.GetHashCode(ErrorModels);
      hashCode = hashCode * -1521134295 + IsSelected.GetHashCode();
      return hashCode;
    }
  }
}
