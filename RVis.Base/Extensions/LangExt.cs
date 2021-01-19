using LanguageExt;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using static LanguageExt.Prelude;

namespace RVis.Base.Extensions
{
  public static class LangExt
  {
    public static T AssertNotNull<T>([NotNull] this T? t, string message)
    {
      if (t is null) throw new ArgumentNullException(nameof(t), message);
      return t;
    }

    public static T AssertNotNull<T>([NotNull] this T? t) =>
      AssertNotNull(t, $"Null {typeof(T).Name} passed");

    public static T AssertSome<T>(this Option<T> optionT, string failMessage) =>
      optionT.IfNone(() => throw new ValueIsNoneException(failMessage));

    public static T AssertSome<T>(this Option<T> optionT) =>
      optionT.IfNone(() => throw ValueIsNoneException.Default);

    public static R AssertRight<L, R>(this Either<L, R> eitherT, string failMessage) =>
      eitherT.IfLeft(() => throw new EitherIsNotRightException(failMessage));

    public static R AssertRight<L, R>(this Either<L, R> eitherT) =>
      eitherT.IfLeft(() => throw new EitherIsNotRightException());

    public static Option<U> LookUp<T, U>(this Arr<(T, U)> arr, T t) => arr
      .Find(p => EqualityComparer<T>.Default.Equals(p.Item1, t))
      .Match(q => Some(q.Item2), () => None);

    public static bool ContainsNone<T>(this IEnumerable<Option<T>> options) =>
      options.Exists(o => o.IsNone);

    public static Option<T> NoneOf<T>() =>
      None;
  }
}
