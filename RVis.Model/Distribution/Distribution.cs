using LanguageExt;
using RVis.Base.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVis.Base.Extensions.EnumExt;
using static RVis.Model.Logger;
using static System.Globalization.CultureInfo;
using static System.String;

namespace RVis.Model
{
  public static class Distribution
  {
    public static Arr<DistributionType> GetDistributionTypes(DistributionType flags) =>
      _distributionTypes.Filter(dt => flags.HasFlag(dt));

    public static (string Variable, IDistribution Distribution) ParseRelation(string representation)
    {
      var match = _reRelation.Match(representation);
      
      RequireTrue(match.Success, $"Failed to parse relation: {representation}");

      var variable = match.Groups[1].Value;
      
      RequireTrue(
        Enum.TryParse(match.Groups[2].Value, out DistributionType distributionType),
        $"Unrecognised distribution type: {match.Groups[2].Value}"
        );

      var parts = match.Groups
        .Cast<Group>()
        .Skip(3)
        .Where(g => g.Success)
        .Select(g => g.Value)
        .ToArr();

      var parseMethod = _distributionParserMap[distributionType];

      var maybeDistribution = (Option<IDistribution>)parseMethod.Invoke(
        null,
        new object[] { parts }
        )!;

      var distribution = maybeDistribution.IfNone(
        () => throw new InvalidOperationException(
          $"{distributionType} distribution incorrectly specified: {representation}"
          )
        );

      return (variable, distribution);
    }

    public static string[] SerializeDistributions(Arr<IDistribution> distributions)
    {
      RequireTrue(_distributionTypes.ForAll(
        dt => distributions.Exists(ds => ds.DistributionType == dt)
        ));

      return distributions.Map(ds => ds.ToString()).ToArray();
    }

    public static Arr<IDistribution> DeserializeDistributions(string[] serialized)
    {
      var distributions = serialized
        .Select(DeserializeDistribution)
        .Somes()
        .ToArr();

      RequireUniqueElements(distributions, s => s.DistributionType);

      return _distributionTypes
        .Map(dt => distributions
          .Find(s => s.DistributionType == dt)
          .Match(s => s, () => GetDefault(dt))
          )
        .ToArr();
    }

    public static Option<IDistribution> DeserializeDistribution(string? serialized)
    {
      if (serialized.IsntAString()) return None;

      var parts = serialized
        .Split(new[] { PROP_SEP }, StringSplitOptions.RemoveEmptyEntries);

      if (!Enum.TryParse(parts[0], out DistributionType distributionType))
      {
        Log.Error($"Unrecognised distribution type: {parts[0]}");
        return None;
      }

      var parseMethod = _distributionParserMap[distributionType];

      var maybeDistribution = (Option<IDistribution>)parseMethod.Invoke(
        null,
        new object[] { parts.Skip(1).ToArr() }
        )!;

      if (maybeDistribution.IsNone)
      {
        Log.Error($"Failed to deserialize distribution: {serialized}");
      }

      return maybeDistribution;
    }

    public static string SerializeDistribution(DistributionType distributionType, Arr<object> state) =>
      $"{distributionType}{PROP_SEP}{Join(PROP_SEP, state.Map(o => Convert.ToString(o, InvariantCulture)))}";

    public static IDistribution GetDefault(DistributionType distributionType)
    {
      RequireTrue(_distributionTypeMap.ContainsKey(distributionType));
      return _distributionDefaultMap[distributionType];
    }

    public static Arr<IDistribution> GetDefaults() =>
      _distributionTypes.Map(dt => _distributionDefaultMap[dt]);

    static Distribution()
    {
      _distributionTypes = GetFlags<DistributionType>();

      _distributionTypeMap = _distributionTypes.ToDictionary(
        dt => dt,
        dt => typeof(Distribution)
          .Assembly
          .GetType($"{nameof(RVis)}.{nameof(Model)}.{dt}{nameof(Distribution)}")!
        );

      _distributionDefaultMap = _distributionTypeMap
        .ToDictionary(
          kvp => kvp.Key,
          kvp =>
          {
            var defaultProperty = kvp.Value.GetField(nameof(NormalDistribution.Default), BindingFlags.Public | BindingFlags.Static);
            RequireNotNull(defaultProperty, $"Default property not found on type {kvp.Value.Name}");
            return (IDistribution)defaultProperty.GetValue(null)!;
          });

      _distributionParserMap = _distributionTypeMap
        .ToDictionary(
          kvp => kvp.Key,
          kvp =>
          {
            var parseMethod = kvp.Value.GetMethod(nameof(NormalDistribution.Parse), BindingFlags.NonPublic | BindingFlags.Static);
            RequireNotNull(parseMethod, $"Parse method not found on type {kvp.Value.Name}");
            return parseMethod;
          });
    }

    private const string PROP_SEP = "$";
    private static readonly Arr<DistributionType> _distributionTypes;
    private static readonly IDictionary<DistributionType, Type> _distributionTypeMap;
    private static readonly IDictionary<DistributionType, IDistribution> _distributionDefaultMap;
    private static readonly IDictionary<DistributionType, MethodInfo> _distributionParserMap;
    private static readonly Regex _reRelation = 
      new(@"(\w+)\s*~\s*(\w+)\s*\(\s*([\d-+.eE]+)(?:\s*,\s*([\d-+.eE]+))*\)(?:\s*\[\s*([\d-+.eE]+)\s*,\s*([\d-+.eE]+)\s*\])?");
  }
}
