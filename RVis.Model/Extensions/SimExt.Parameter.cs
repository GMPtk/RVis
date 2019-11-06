using LanguageExt;
using RVis.Base.Extensions;
using static RVis.Base.Check;
using static System.Double;
using static System.Globalization.CultureInfo;
using static System.String;

namespace RVis.Model.Extensions
{
  public static partial class SimExt
  {
    public static string GetRValue(this SimParameter parameter) =>
      IsPositiveInfinity(parameter.Scalar)
        ? "Inf"
        : IsNegativeInfinity(parameter.Scalar)
          ? "-Inf"
          : IsNaN(parameter.Scalar)
            ? "NaN"
            : parameter.Scalar.ToString(InvariantCulture);

    public static Arr<SimParameter> GetEdits(this SimInput @default, SimInput edited) =>
      @default.SimParameters
        .Map(p => edited.SimParameters.Find(q => q.Name == p.Name && q.Value != p.Value))
        .Somes()
        .ToArr();

    public static string ToAssignment(this SimParameter parameter) =>
      $"{parameter.Name}={parameter.Value}{parameter.Unit ?? Empty}";

    public static string ToAssignment(this SimParameter parameter, string valueFormat) =>
      $"{parameter.Name}=" +
      $"{(IsNaN(parameter.Scalar) ? parameter.Value : parameter.Scalar.ToString(valueFormat, InvariantCulture))}" +
      $"{parameter.Unit ?? Empty}";

    public static string ToAssignments(this Arr<SimParameter> edits) =>
      Join(";", edits.Map(e => e.ToAssignment()));

    public static string ToAssignments(this Arr<SimParameter> edits, string valueFormat) =>
      Join(";", edits.Map(e => e.ToAssignment(valueFormat)));

    public static Arr<SimParameter> GetParameters(this SimInput @default, string assignments)
    {
      if (assignments.IsntAString()) return Arr<SimParameter>.Empty;

      SimParameter AssignmentToEdit(string assignment)
      {
        var parts = assignment.Split('=');
        RequireTrue(parts.Length == 2, $"Invalid parameter assignment {assignment}");
        var name = parts[0].Trim();
        var parameter = @default.SimParameters.Find(p => p.Name == name).AssertSome($"Unknown parameter name {name}");
        var value = parts[1].Trim();
        if (parameter.Unit.IsAString() && value.EndsWith(parameter.Unit, System.StringComparison.InvariantCulture))
        {
          value = value.Substring(0, value.Length - parameter.Unit.Length).TrimEnd();
        }
        return parameter.With(value);
      }

      return assignments.Split(';').Map(AssignmentToEdit).ToArr();
    }

    public static bool ContainsParameter(this Arr<SimParameter> parameters, string name) =>
      parameters.Exists(p => p.Name == name);

    public static Option<SimParameter> FindParameter(this Arr<SimParameter> parameters, string name) =>
      parameters.Find(p => p.Name == name);

    public static Option<SimParameter> FindParameter(this Arr<SimParameter> parameters, SimParameter parameter) =>
      FindParameter(parameters, parameter.Name);

    public static SimParameter GetParameter(this Arr<SimParameter> parameters, string name) =>
      parameters.Find(p => p.Name == name).AssertSome($"Unknown parameter: {name}");
  }
}
