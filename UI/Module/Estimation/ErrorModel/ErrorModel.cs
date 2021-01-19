using LanguageExt;
using RVis.Base.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static RVis.Base.Extensions.EnumExt;
using static Estimation.Logger;
using static System.Globalization.CultureInfo;
using static System.String;

namespace Estimation
{
  internal static class ErrorModel
  {
    internal const double DEFAULT_SIGMA_STEP_INITIALIZER = 0.04;
    internal const double DEFAULT_DELTA_STEP_INITIALIZER = 0.01;

    internal static Arr<ErrorModelType> GetErrorModelTypes(ErrorModelType flags) =>
      _errorModelTypes.Filter(dt => flags.HasFlag(dt));

    internal static string[] SerializeErrorModels(Arr<IErrorModel> errorModels)
    {
      RequireTrue(_errorModelTypes.ForAll(
        dt => errorModels.Exists(ds => ds.ErrorModelType == dt)
        ));

      return errorModels.Map(ds => ds.ToString()!).ToArray();
    }

    internal static Arr<IErrorModel> DeserializeErrorModels(string[] serialized)
    {
      var errorModels = serialized
        .Select(DeserializeErrorModel)
        .Somes()
        .ToArr();

      RequireUniqueElements(errorModels, s => s.ErrorModelType);

      return _errorModelTypes
        .Map(dt => errorModels
          .Find(s => s.ErrorModelType == dt)
          .Match(s => s, () => GetDefault(dt))
          )
        .ToArr();
    }

    internal static Option<IErrorModel> DeserializeErrorModel(string serialized)
    {
      if (serialized.IsntAString()) return None;

      var parts = serialized
        .Split(new[] { PROP_SEP }, StringSplitOptions.RemoveEmptyEntries);

      if (!Enum.TryParse(parts[0], out ErrorModelType errorModelType))
      {
        Log.Error($"Unrecognised error model type: {parts[0]}");
        return None;
      }

      var parseMethod = _errorModelParserMap[errorModelType];

      var maybeErrorModel = (Option<IErrorModel>)parseMethod
        .Invoke(
          null,
          new object[] { parts.Skip(1).ToArr() }
        )
        .AssertNotNull();

      if (maybeErrorModel.IsNone)
      {
        Log.Error($"Failed to error model distribution: {serialized}");
      }

      return maybeErrorModel;
    }

    internal static string SerializeErrorModel(ErrorModelType errorModelType, Arr<object> state) =>
      $"{errorModelType}{PROP_SEP}{Join(PROP_SEP, state.Map(o => Convert.ToString(o, InvariantCulture)))}";

    internal static IErrorModel GetDefault(ErrorModelType errorModelType)
    {
      RequireTrue(_errorModelTypeMap.ContainsKey(errorModelType));
      return _errorModelDefaultMap[errorModelType];
    }

    internal static Arr<IErrorModel> GetDefaults() =>
      _errorModelTypes.Map(dt => _errorModelDefaultMap[dt]);

    static ErrorModel()
    {
      _errorModelTypes = GetFlags<ErrorModelType>();

      _errorModelTypeMap = _errorModelTypes.ToDictionary(
        dt => dt,
        dt => typeof(ErrorModel)
          .Assembly
          .GetType($"{nameof(Estimation)}.{dt}{nameof(ErrorModel)}")
          .AssertNotNull()
        );

      _errorModelDefaultMap = _errorModelTypeMap
        .ToDictionary(
          kvp => kvp.Key,
          kvp =>
          {
            var defaultProperty = kvp.Value.GetField(nameof(NormalErrorModel.Default), BindingFlags.NonPublic | BindingFlags.Static);
            RequireNotNull(defaultProperty, $"Default property not found on type {kvp.Value.Name}");
            return (IErrorModel)defaultProperty.GetValue(null).AssertNotNull();
          });

      _errorModelParserMap = _errorModelTypeMap
        .ToDictionary(
          kvp => kvp.Key,
          kvp =>
          {
            var parseMethod = kvp.Value.GetMethod(nameof(NormalErrorModel.Parse), BindingFlags.NonPublic | BindingFlags.Static);
            RequireNotNull(parseMethod, $"Parse method not found on type {kvp.Value.Name}");
            return parseMethod;
          });
    }

    private const string PROP_SEP = "$";
    private static readonly Arr<ErrorModelType> _errorModelTypes;
    private static readonly IDictionary<ErrorModelType, Type> _errorModelTypeMap;
    private static readonly IDictionary<ErrorModelType, IErrorModel> _errorModelDefaultMap;
    private static readonly IDictionary<ErrorModelType, MethodInfo> _errorModelParserMap;
  }
}
