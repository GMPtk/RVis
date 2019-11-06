using LanguageExt;
using RVis.Base.Extensions;
using RVis.Model;
using RVis.Model.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using static LanguageExt.Prelude;
using static Plot.Logger;
using static RVis.Base.Check;
using static System.Double;
using static System.Globalization.CultureInfo;
using static System.String;

namespace Plot
{
  public class OutputGroup
  {
    internal static Option<OutputGroup> Parse(string line, SimInput defaultInput)
    {
      try
      {
        var parts = line.Split(
          new[] { SEPARATOR },
          StringSplitOptions.RemoveEmptyEntries
          );

        var name = parts[0];
        var description = parts[1];
        var createdOn = DateTime.Parse(parts[2], InvariantCulture);
        var inputAssignments = defaultInput.GetParameters(parts[3]);
        var serieInputHashes = parts[4].Split(';');

        return new OutputGroup(
          name,
          description,
          createdOn,
          inputAssignments,
          serieInputHashes
          );
      }
      catch (Exception)
      {
        Log.Warn($"Failed to parse output group: {line}");
        return None;
      }
    }

    internal static OutputGroup Create(string name, SimDataItem<OutputRequest> simDataItem)
    {
      var serieInputs = simDataItem.Item.SerieInputs.AssertRight("No serie inputs");
      RequireTrue(serieInputs.Count > 1);

      var parameterValues = new SortedDictionary<string, List<double>>();
      var defaultInput = simDataItem.Simulation.SimConfig.SimInput;

      serieInputs.Iter(i =>
      {
        var edits = defaultInput.GetEdits(i);

        edits.Filter(p => !IsNaN(p.Scalar)).Iter(p =>
        {
          if (!parameterValues.ContainsKey(p.Name))
          {
            parameterValues.Add(p.Name, new List<double>(serieInputs.Count));
          }

          parameterValues[p.Name].Add(p.Scalar);
        });
      });

      var serieInputHashes = serieInputs.Map(i => i.Hash);

      return Create(name, parameterValues, serieInputHashes, defaultInput);
    }

    internal static OutputGroup Create(string name, IEnumerable<SimDataLogEntry> logEntries, SimInput defaultInput)
    {
      var parameterValues = new SortedDictionary<string, List<double>>();
      var nLogEntries = logEntries.Count();

      var logEntryEdits = logEntries
        .Select(le =>
        {
          var edits = defaultInput.GetParameters(le.ParameterAssignments);
          edits.Iter(e =>
          {
            if (!parameterValues.ContainsKey(e.Name))
            {
              parameterValues.Add(e.Name, new List<double>(nLogEntries));
            }
          });
          return edits;
        })
        .ToArr();

      var parameterNames = parameterValues.Keys.ToArr();
      var defaultParameterValues = parameterNames.ToDictionary(
        pn => pn,
        pn => defaultInput.SimParameters.GetParameter(pn).Scalar
        );

      logEntryEdits.Iter(lee =>
      {
        parameterNames.Iter(pn =>
        {
          var edit = lee.Find(p => p.Name == pn);
          edit.Match(
            e => parameterValues[pn].Add(e.Scalar),
            () => parameterValues[pn].Add(defaultParameterValues[pn])
          );
        });
      });

      var serieInputHashes = logEntries.Select(le => le.SerieInputHash).ToArr();

      return Create(name, parameterValues, serieInputHashes, defaultInput);
    }

    private static OutputGroup Create(
      string name,
      SortedDictionary<string, List<double>> parameterValues,
      Arr<string> serieInputHashes,
      SimInput defaultInput
      )
    {
      var includeDefaultValues = serieInputHashes.Exists(h => h == defaultInput.Hash);

      var ranges = parameterValues.Keys
        .OrderBy(k => k)
        .Select(k =>
        {
          var defaultParameter = defaultInput.SimParameters.GetParameter(k);
          var list = parameterValues[k];
          if (includeDefaultValues && !IsNaN(defaultParameter.Scalar))
          {
            list.Add(defaultParameter.Scalar);
          }
          list.Sort();
          var values = list.Distinct().ToArray();

          var rangeDescription = k + "=";
          var unit = defaultParameter.Unit ?? string.Empty;

          if (values.Length == 1)
          {
            rangeDescription += $"{values[0]:G4}{unit}";
          }
          else if (values.Length < 6)
          {
            rangeDescription += Join(",", values.Select(v => $"{v:G4}{unit}"));
          }
          else
          {
            var min = values.Min();
            var max = values.Max();

            rangeDescription += $"{min:G4}{unit}->{max:G4}{unit}";
          }

          var rAssignmentRhs = values.Length > 1
            ? $"c({Join(", ", values.Select(v => v.ToString(InvariantCulture)))})"
            : values[0].ToString(InvariantCulture);

          var rangeParameter = defaultParameter.With(rAssignmentRhs);

          return (Description: rangeDescription, Parameter: rangeParameter);
        })
        .ToArray();

      var description = Join(", ", ranges.Select(r => r.Description));
      var createdOn = DateTime.Now;
      var inputAssignments = ranges.Select(r => r.Parameter).ToArr();

      return new OutputGroup(name, description, createdOn, inputAssignments, serieInputHashes);
    }

    internal OutputGroup(string name, string description, DateTime createdOn, Arr<SimParameter> inputAssignments, Arr<string> serieInputHashes)
    {
      Name = name;
      Description = description;
      CreatedOn = createdOn;
      InputAssignments = inputAssignments;
      SerieInputHashes = serieInputHashes;
    }

    public string Name { get; }
    public string Description { get; }
    public DateTime CreatedOn { get; }
    public Arr<SimParameter> InputAssignments { get; }
    public Arr<string> SerieInputHashes { get; }

    public override string ToString() =>
      $"{Name}{SEPARATOR}" +
      $"{Description}{SEPARATOR}" +
      $"{CreatedOn:o}{SEPARATOR}" +
      $"{InputAssignments.ToAssignments()}{SEPARATOR}" +
      $"{Join(";", SerieInputHashes)}";

    private const string SEPARATOR = "$$";
  }
}
