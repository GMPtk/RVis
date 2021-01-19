using LanguageExt;
using RVis.Base.Extensions;
using System;
using static LanguageExt.Prelude;
using static LanguageExt.Seq;
using static RVis.Base.Check;
using static RVis.Base.Extensions.StrExt;
using static System.String;
using static System.Globalization.CultureInfo;

namespace RVis.Base
{
  public enum OptArgType
  {
    ShortOpt,
    LongOpt,
    ArgOnly
  }

  public enum OptArgUsage
  {
    None,
    Optional,
    Required
  }

  public sealed class CliOptSpec : Record<CliOptSpec>
  {
    public CliOptSpec(string? option, OptArgType optArgType, OptArgUsage optArgUsage)
    {
      RequireTrue(optArgType == OptArgType.ArgOnly || option.IsAString());

      Option = option?.PascalToHyphenated().ToLowerInvariant();
      OptArgType = optArgType;
      OptArgUsage = optArgUsage;
    }

    public static readonly CliOptSpec ArgOnly = new CliOptSpec(null, OptArgType.ArgOnly, OptArgUsage.Required);

    public readonly string? Option;
    public readonly OptArgType OptArgType;
    public readonly OptArgUsage OptArgUsage;
  }

  public sealed class CliOpt : Record<CliOpt>
  {
    public CliOpt(CliOptSpec cliOptSpec, string? argument)
    {
      RequireTrue(
        cliOptSpec.OptArgUsage != OptArgUsage.Required || argument.IsAString(),
        $"Option {cliOptSpec.Option} requires an argument"
        );

      CliOptSpec = cliOptSpec;
      Argument = null == argument ? None : Some(argument);
    }

    public CliOpt(CliOptSpec cliOptSpec) : this(cliOptSpec, null) { }

    public readonly CliOptSpec CliOptSpec;
    public readonly Option<string> Argument;
  }

  public static class CliOptExt
  {
    public static Seq<CliOptSpec> ToCliOptSpecs(this Seq<string> optSpecs)
    {
      static CliOptSpec StringToCliOptSpec(string optSpec)
      {
        var optArgType = 1 < optSpec.Length ? OptArgType.LongOpt : OptArgType.ShortOpt;

        var optArgUsage = optSpec.EndsWith("=?", StringComparison.InvariantCulture) ?
          OptArgUsage.Optional : optSpec.EndsWith("=", StringComparison.InvariantCulture) ?
            OptArgUsage.Required : OptArgUsage.None;

        var option = optSpec.TrimEnd('=', '?');

        return new CliOptSpec(option, optArgType, optArgUsage);
      }

      return map(optSpecs, StringToCliOptSpec);
    }

    public static Seq<CliOpt> ToCliOpts(this string[] args, Seq<CliOptSpec> cliOptSpecs) =>
      ToCliOpts(args.ToSeq(), cliOptSpecs);

    public static Seq<CliOpt> ToCliOpts(this Seq<string> args, Seq<CliOptSpec> cliOptSpecs)
    {
      CliOpt ArgToCliOpt(string arg)
      {
        var optArgType = arg.StartsWith("--", StringComparison.InvariantCulture) ?
          OptArgType.LongOpt : arg.StartsWith("-", StringComparison.InvariantCulture) ?
            OptArgType.ShortOpt : OptArgType.ArgOnly;

        if (optArgType == OptArgType.ArgOnly) return new CliOpt(CliOptSpec.ArgOnly, arg);

        arg = arg.TrimStart('-');

        string option;
        string? argument;

        if (optArgType == OptArgType.LongOpt)
        {
          var index = arg.IndexOf('=');
          if (index.IsntFound())
          {
            option = arg;
            argument = null;
          }
          else
          {
            option = arg.Substring(0, index);
            argument = arg[(index + 1)..];
          }
        }
        else
        {
          option = arg.Substring(0, 1);
          argument = arg.Length > 1 ? arg[1..] : null;
        }

        if (argument.IsntAString()) argument = null;

        var cliOptSpec = cliOptSpecs.Find(cos => cos.Option == option);

        var cliOpt = cliOptSpec.Match(
          Some: cos => new CliOpt(cos, argument),
          None: () => throw new ArgumentOutOfRangeException(nameof(arg), option, $"Unknown option: {option}")
          );

        return cliOpt;
      }

      return map(args, ArgToCliOpt);
    }

    public static Option<CliOpt> GetOpt<T>(this Seq<CliOpt> cliOpts, params T[] ts)
    {
      var options = ts.Map(t => Convert.ToString(t,InvariantCulture)!.PascalToHyphenated().ToLowerInvariant()).ToSeq();
      return GetOptImpl(cliOpts, options);
    }

    public static Option<CliOpt> GetOpt(this Seq<CliOpt> cliOpts, params string[] options) =>
      GetOptImpl(cliOpts, options.Map(s => s.PascalToHyphenated().ToLowerInvariant()).ToSeq());

    public static Either<string, Option<U>> GetOpt<T, U>(this Seq<CliOpt> cliOpts, params T[] ts) =>
      GetOptImpl<U>(cliOpts, ts.Map(t => Convert.ToString(t, InvariantCulture)!.PascalToHyphenated().ToLowerInvariant()).ToSeq());

    public static Either<string, Option<T>> GetOpt<T>(this Seq<CliOpt> cliOpts, params string[] options) =>
      GetOptImpl<T>(cliOpts, options.ToSeq());

    private static Option<CliOpt> GetOptImpl(this Seq<CliOpt> cliOpts, Seq<string> options) =>
      cliOpts.Find(co => options.Exists(o => co.CliOptSpec.Option == o));

    public static Either<string, Option<T>> GetOptImpl<T>(this Seq<CliOpt> cliOpts, Seq<string> options)
    {
      var cliOptOrNotFound = GetOptImpl(cliOpts, options).ToEither($"Option not found: {Join(", ", options)}");

      Either<string, Option<T>> AttemptConvertArgument(CliOpt co)
      {
        if (co.CliOptSpec.OptArgUsage == OptArgUsage.Optional && co.Argument.IsNone)
        {
          return Right<string, Option<T>>(None);
        }

        var t = from text in co.Argument
                from arg in convert<T>(text)
                select arg;

        if (t.IsNone)
        {
          return $"Option {Join(", ", options)} requires a valid {typeof(T).Name}";
        }

        return t;
      }

      return cliOptOrNotFound.Bind(AttemptConvertArgument);
    }

    public static Seq<string> GetArgs(this Seq<CliOpt> cliOpts) =>
      Seq(
        somes(
          map(
            filter(cliOpts, co => co.CliOptSpec.OptArgType == OptArgType.ArgOnly),
            co => co.Argument
            )
          )
        );
  }
}
