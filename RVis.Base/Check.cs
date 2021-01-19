using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using static RVis.Base.Constant;
using static RVis.Base.Properties.Resources;
using static System.Math;
using static System.String;

namespace RVis.Base
{
  /// <summary>
  /// Code contracts
  /// </summary>
  /// <seealso cref="Debug.Assert"/>
  public static class Check
  {
    #region TrueFalse

    /// <summary>
    /// Throws an InvalidOperationException if the condition is not true.
    /// </summary>
    /// <param name="condition">Something that should be true.</param>
    [DebuggerStepThrough]
    public static void RequireTrue([DoesNotReturnIf(false)] bool condition) =>
      RequireTrue(condition, ERR_NOT_TRUE);

    /// <summary>
    /// Throws an InvalidOperationException if the condition is not true.
    /// </summary>
    /// <param name="condition">Something that should be true.</param>
    /// <param name="message">Message for the exception if the condition is not true.</param>
    [DebuggerStepThrough]
    public static void RequireTrue([DoesNotReturnIf(false)] bool condition, [Localizable(false)] string message)
    {
      if (!condition)
      {
        throw new InvalidOperationException(message);
      }
    }

    /// <summary>
    /// Throws an InvalidOperationException if the condition is not false.
    /// </summary>
    /// <param name="condition">Something that should be false.</param>
    [DebuggerStepThrough]
    public static void RequireFalse([DoesNotReturnIf(true)] bool condition) =>
      RequireFalse(condition, ERR_NOT_FALSE);

    /// <summary>
    /// Throws an InvalidOperationException if the condition is not false.
    /// </summary>
    /// <param name="condition">Something that should be false.</param>
    /// <param name="message">Message for the exception if the condition is not false.</param>
    [DebuggerStepThrough]
    public static void RequireFalse([DoesNotReturnIf(true)] bool condition, [Localizable(false)] string message) =>
      RequireTrue(!condition, message);

    #endregion

    #region Null

    /// <summary>
    /// Throws an ArgumentException if the object is not null.
    /// </summary>
    /// <param name="o">Something that should be null.</param>
    [DebuggerStepThrough]
    public static void RequireNull(object? o) =>
      RequireNull(o, ERR_NOT_NULL);

    /// <summary>
    /// Throws an ArgumentException if the object is not null.
    /// </summary>
    /// <param name="o">Something that should be null.</param>
    /// <param name="message">Message for the exception if the object is not null.</param>
    [DebuggerStepThrough]
    public static void RequireNull(object? o, [Localizable(false)] string message)
    {
      if (o != null)
      {
        throw new ArgumentException(message);
      }
    }

    /// <summary>
    /// Throws an ArgumentNullException if the string is null or empty.
    /// </summary>
    /// <param name="argument">A string should that not be null or empty.</param>
    [DebuggerStepThrough]
    public static void RequireNullEmptyWhiteSpace(string? s) =>
      RequireNullEmptyWhiteSpace(s, ERR_STRING_NOT_NULL_OR_EMPTY);

    /// <summary>
    /// Throws an ArgumentNullException if the string is null or empty.
    /// </summary>
    /// <param name="argument">A string should that not be null or empty.</param>
    /// <param name="message">Message for the exception if the string is null or empty.</param>
    [DebuggerStepThrough]
    public static void RequireNullEmptyWhiteSpace(string? s, [Localizable(false)] string message)
    {
      if (!IsNullOrWhiteSpace(s))
      {
        throw new ArgumentNullException(message);
      }
    }

    #endregion

    #region NotNull

    /// <summary>
    /// Throws an ArgumentNullException if the object is null.
    /// </summary>
    /// <param name="o">Something that should not be null.</param>
    [DebuggerStepThrough]
    public static void RequireNotNull([NotNull] object? o) =>
      RequireNotNull(o, ERR_NULL);

    /// <summary>
    /// Throws an ArgumentNullException if the object is null.
    /// </summary>
    /// <param name="o">Something that should not be null.</param>
    /// <param name="message">Message for the exception if the object is null.</param>
    [DebuggerStepThrough]
    public static void RequireNotNull([NotNull] object? o, [Localizable(false)] string message)
    {
      if (o == null)
      {
        throw new ArgumentNullException(message);
      }
    }

    /// <summary>
    /// Throws an ArgumentNullException if the string is null or empty.
    /// </summary>
    /// <param name="argument">A string should that not be null or empty.</param>
    [DebuggerStepThrough]
    public static void RequireNotNullEmptyWhiteSpace([NotNull] string? s) =>
      RequireNotNullEmptyWhiteSpace(s, ERR_STRING_NULL_OR_EMPTY);

    /// <summary>
    /// Throws an ArgumentNullException if the string is null or empty.
    /// </summary>
    /// <param name="argument">A string should that not be null or empty.</param>
    /// <param name="message">Message for the exception if the string is null or empty.</param>
    [DebuggerStepThrough]
    public static void RequireNotNullEmptyWhiteSpace([NotNull] string? s, [Localizable(false)] string message)
    {
      if (IsNullOrWhiteSpace(s))
      {
        throw new ArgumentNullException(message);
      }
    }

    #endregion

    #region EqualNotEqual

    /// <summary>
    /// Throws an ArgumentException if the objects are not equal.
    /// </summary>
    /// <param name="o1">An object.</param>
    /// <param name="o2">Another object that should be equal to the first.</param>
    [DebuggerStepThrough]
    public static void RequireEqual(object o1, object? o2) =>
      RequireEqual(o1, o2, ERR_NOT_EQUAL);

    /// <summary>
    /// Throws an ArgumentException if the objects are not equal.
    /// </summary>
    /// <param name="o1">An object.</param>
    /// <param name="o2">Another object that should be equal to the first.</param>
    /// <param name="message">Message for the exception if the objects are not equal.</param>
    [DebuggerStepThrough]
    public static void RequireEqual(object o1, object? o2, [Localizable(false)] string message)
    {
      if (!o1.Equals(o2))
      {
        throw new ArgumentException(message);
      }
    }

    /// <summary>
    /// Throws an ArgumentException if the objects are equal.
    /// </summary>
    /// <param name="o1">An object.</param>
    /// <param name="o2">Another object that shouldn't be equal to the first.</param>
    [DebuggerStepThrough]
    public static void RequireNotEqual(object o1, object? o2) =>
      RequireNotEqual(o1, o2, ERR_EQUAL);

    /// <summary>
    /// Throws an ArgumentException if the objects are equal.
    /// </summary>
    /// <param name="o1">An object.</param>
    /// <param name="o2">Another object that shouldn't be equal to the first.</param>
    /// <param name="message">Message for the exception if the objects are equal.</param>
    [DebuggerStepThrough]
    public static void RequireNotEqual(object o1, object? o2, [Localizable(false)] string message)
    {
      if (o1.Equals(o2))
      {
        throw new ArgumentException(message);
      }
    }

    /// <summary>
    /// Throws an ArgumentException if two numbers are not equal, within a specified tolerance (default = 1.5e-8).
    /// </summary>
    /// <param name="x">A number.</param>
    /// <param name="y">Another number that should be more or less equal to the first.</param>
    [DebuggerStepThrough]
    public static void RequireAlmostEqual(double x, double y) =>
      RequireAlmostEqual(x, y, TOLERANCE, ERR_NOT_ALMOST_EQUAL);

    /// <summary>
    /// Throws an ArgumentException if two numbers are not equal, within a specified tolerance (default = 1.5e-8).
    /// </summary>
    /// <param name="x">A number.</param>
    /// <param name="y">Another number that should be more or less equal to the first.</param>
    /// <param name="message">Message for the exception if the two numbers are not equal.</param>
    [DebuggerStepThrough]
    public static void RequireAlmostEqual(double x, double y, [Localizable(false)] string message) =>
      RequireAlmostEqual(x, y, TOLERANCE, message);

    /// <summary>
    /// Throws an ArgumentException if two numbers are not equal, within a specified tolerance (default = 1.5e-8).
    /// </summary>
    /// <param name="x">A number.</param>
    /// <param name="y">Another number that should be more or less equal to the first.</param>
    /// <param name="eps">Tolerance for equality.</param>
    [DebuggerStepThrough]
    public static void RequireAlmostEqual(double x, double y, double eps) =>
      RequireAlmostEqual(x, y, eps, ERR_NOT_ALMOST_EQUAL);

    /// <summary>
    /// Throws an ArgumentException if two numbers are not equal, within a specified tolerance (default = 1.5e-8).
    /// </summary>
    /// <param name="x">A number.</param>
    /// <param name="y">Another number that should be more or less equal to the first.</param>
    /// <param name="eps">Tolerance for equality.</param>
    /// <param name="message">Message for the exception if the two numbers are not equal.</param>
    [DebuggerStepThrough]
    public static void RequireAlmostEqual(double x, double y, double eps, [Localizable(false)] string message)
    {
      if (Abs(x - y) >= eps)
      {
        throw new ArgumentException(message);
      }
    }

    #endregion

    #region IsInRange

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if a T is not in a specified range.
    /// </summary>
    /// <param name="x">A T that should be in range.</param>
    /// <param name="lowerBound">The lower bound of the range.</param>
    /// <param name="upperBound">The upper bound of the range.</param>
    [DebuggerStepThrough]
    public static void RequireInRange<T>(T x, T lowerBound, T upperBound) where T : struct, IComparable =>
      RequireInRange(x, lowerBound, upperBound, ERR_BELOW_RANGE, ERR_ABOVE_RANGE);

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if a T is not in a specified range.
    /// </summary>
    /// <param name="x">A T that should be in range.</param>
    /// <param name="lowerBound">The lower bound of the range.</param>
    /// <param name="upperBound">The upper bound of the range.</param>
    /// <param name="messageLower">Message for the exception if the T is too low.</param>
    /// <param name="messageUpper">Message for the exception if the T is too high.</param>
    [DebuggerStepThrough]
    public static void RequireInRange<T>(T x, T lowerBound, T upperBound, string messageLower, string messageUpper) where T : struct, IComparable
    {
      if (x.CompareTo(lowerBound) < 0)
      {
        throw new ArgumentOutOfRangeException(messageLower);
      }
      if (x.CompareTo(upperBound) > 0)
      {
        throw new ArgumentOutOfRangeException(messageUpper);
      }
    }

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if a number is not strictly in a specified range.
    /// </summary>
    /// <param name="x">A number that should be in range.</param>
    /// <param name="lowerBound">The lower bound of the range.</param>
    /// <param name="upperBound">The upper bound of the range.</param>
    [DebuggerStepThrough]
    public static void RequireInRange(double x, double lowerBound, double upperBound) =>
      RequireInRange(x, lowerBound, upperBound, DefaultLowerBoundStrictness, DefaultUpperBoundStrictness);

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if a number is not strictly in a specified range.
    /// </summary>
    /// <param name="x">A number that should be in range.</param>
    /// <param name="lowerBound">The lower bound of the range.</param>
    /// <param name="upperBound">The upper bound of the range.</param>
    /// <param name="messageNaN">Message for the exception if the number is NaN.</param>
    /// <param name="messageLower">Message for the exception if the number is too low.</param>
    /// <param name="messageUpper">Message for the exception if the number is too high.</param>
    [DebuggerStepThrough]
    public static void RequireInRange(double x, double lowerBound, double upperBound, string messageNaN, string messageLower, string messageUpper) =>
      RequireInRange(x, lowerBound, upperBound, DefaultLowerBoundStrictness, DefaultUpperBoundStrictness, messageNaN, messageLower, messageUpper);

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if a number is not in a specified range.
    /// </summary>
    /// <param name="x">A number that should be in range.</param>
    /// <param name="lowerBound">The lower bound of the range.</param>
    /// <param name="upperBound">The upper bound of the range.</param>
    /// <param name="isLowerBoundStrict">Whether or not the lower bound is strict (not included in the range).</param>
    /// <param name="isUpperBoundStrict">Whether or not the upper bound is strict (not included in the range).</param>
    [DebuggerStepThrough]
    public static void RequireInRange(double x, double lowerBound, double upperBound, bool isLowerBoundStrict, bool isUpperBoundStrict) =>
      RequireInRange(x, lowerBound, upperBound, isLowerBoundStrict, isUpperBoundStrict, ERR_NAN, ERR_BELOW_RANGE, ERR_ABOVE_RANGE);

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if a number is not in a specified range.
    /// </summary>
    /// <param name="x">A number that should be in range.</param>
    /// <param name="lowerBound">The lower bound of the range.</param>
    /// <param name="upperBound">The upper bound of the range.</param>
    /// <param name="isLowerBoundStrict">Whether or not the lower bound is strict (not included in the range).</param>
    /// <param name="isUpperBoundStrict">Whether or not the upper bound is strict (not included in the range).</param>
    /// <param name="messageNaN">Message for the exception if the number is NaN.</param>
    /// <param name="messageLower">Message for the exception if the number is too low.</param>
    /// <param name="messageUpper">Message for the exception if the number is too high.</param>
    [DebuggerStepThrough]
    public static void RequireInRange(double x, double lowerBound, double upperBound, bool isLowerBoundStrict, bool isUpperBoundStrict, string messageNaN, string messageLower, string messageUpper)
    {
      if (double.IsNaN(x))
      {
        throw new ArgumentOutOfRangeException(messageNaN);
      }
      if (x < lowerBound || (isLowerBoundStrict && x == lowerBound))
      {
        throw new ArgumentOutOfRangeException(messageLower);
      }
      if (x > upperBound || (isUpperBoundStrict && x == upperBound))
      {
        throw new ArgumentOutOfRangeException(messageUpper);
      }
    }

    #endregion

    #region IsFraction

    /// <summary>
    /// Throws an error if a number is not strictly between zero and one.
    /// </summary>
    /// <param name="x">A number that should be in range.</param>
    [DebuggerStepThrough]
    public static void RequireFraction(double x) =>
      RequireFraction(x, DefaultLowerBoundStrictness, DefaultUpperBoundStrictness, ERR_NAN, ERR_BELOW_RANGE, ERR_ABOVE_RANGE);

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if a number is not strictly between zero and one.
    /// </summary>
    /// <param name="x">A number that should be in range.</param>
    /// <param name="messageNaN">Message for the exception if the number is NaN.</param>
    /// <param name="messageLower">Message for the exception if the number is too low.</param>
    /// <param name="messageUpper">Message for the exception if the number is too high.</param>
    [DebuggerStepThrough]
    public static void RequireFraction(double x, string messageNaN, string messageLower, string messageUpper) =>
      RequireFraction(x, DefaultLowerBoundStrictness, DefaultUpperBoundStrictness, messageNaN, messageLower, messageUpper);

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if a number is not between zero and one.
    /// </summary>
    /// <param name="x">A number that should be in range.</param>
    /// <param name="isLowerBoundStrict">Whether or not the lower bound is strict (not included in the range).</param>
    /// <param name="isUpperBoundStrict">Whether or not the upper bound is strict (not included in the range).</param>
    [DebuggerStepThrough]
    public static void RequireFraction(double x, bool isLowerBoundStrict, bool isUpperBoundStrict) =>
      RequireFraction(x, isLowerBoundStrict, isUpperBoundStrict, ERR_NAN, ERR_BELOW_RANGE, ERR_ABOVE_RANGE);

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if a number is not between zero and one.
    /// </summary>
    /// <param name="x">A number that should be in range.</param>
    /// <param name="isLowerBoundStrict">Whether or not the lower bound is strict (not included in the range).</param>
    /// <param name="isUpperBoundStrict">Whether or not the upper bound is strict (not included in the range).</param>
    /// <param name="messageNaN">Message for the exception if the number is NaN.</param>
    /// <param name="messageLower">Message for the exception if the number is too low.</param>
    /// <param name="messageUpper">Message for the exception if the number is too high.</param>
    [DebuggerStepThrough]
    public static void RequireFraction(double x, bool isLowerBoundStrict, bool isUpperBoundStrict, string messageNaN, string messageLower, string messageUpper) =>
      RequireInRange(x, 0.0, 1.0, isLowerBoundStrict, isUpperBoundStrict, messageNaN, messageLower, messageUpper);

    #endregion

    #region IsPercentage

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if a number is not strictly between zero and one hundred.
    /// </summary>
    /// <param name="x">A number that should be in range.</param>
    [DebuggerStepThrough]
    public static void RequirePercentage(double x) =>
      RequirePercentage(x, DefaultLowerBoundStrictness, DefaultUpperBoundStrictness, ERR_NAN, ERR_BELOW_RANGE, ERR_ABOVE_RANGE);

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if a number is not strictly between zero and one hundred.
    /// </summary>
    /// <param name="x">A number that should be in range.</param>
    /// <param name="messageNaN">Message for the exception if the number is NaN.</param>
    /// <param name="messageLower">Message for the exception if the number is too low.</param>
    /// <param name="messageUpper">Message for the exception if the number is too high.</param>
    [DebuggerStepThrough]
    public static void RequirePercentage(double x, string messageNaN, string messageLower, string messageUpper) =>
      RequirePercentage(x, DefaultLowerBoundStrictness, DefaultUpperBoundStrictness, messageNaN, messageLower, messageUpper);

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if a number is not between zero and one hundred.
    /// </summary>
    /// <param name="x">A number that should be in range.</param>
    /// <param name="isLowerBoundStrict">Whether or not the lower bound is strict (not included in the range).</param>
    /// <param name="isUpperBoundStrict">Whether or not the upper bound is strict (not included in the range).</param>
    [DebuggerStepThrough]
    public static void RequirePercentage(double x, bool isLowerBoundStrict, bool isUpperBoundStrict) =>
      RequirePercentage(x, isLowerBoundStrict, isUpperBoundStrict, ERR_NAN, ERR_BELOW_RANGE, ERR_ABOVE_RANGE);

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if a number is not between zero and one hundred.
    /// </summary>
    /// <param name="x">A number that should be in range.</param>
    /// <param name="isLowerBoundStrict">Whether or not the lower bound is strict (not included in the range).</param>
    /// <param name="isUpperBoundStrict">Whether or not the upper bound is strict (not included in the range).</param>
    /// <param name="messageNaN">Message for the exception if the number is NaN.</param>
    /// <param name="messageLower">Message for the exception if the number is too low.</param>
    /// <param name="messageUpper">Message for the exception if the number is too high.</param>
    [DebuggerStepThrough]
    public static void RequirePercentage(double x, bool isLowerBoundStrict, bool isUpperBoundStrict, string messageNaN, string messageLower, string messageUpper) =>
      RequireInRange(x, 0.0, 100.0, isLowerBoundStrict, isUpperBoundStrict, messageNaN, messageLower, messageUpper);

    #endregion

    #region IsPositive

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if a number is not positive.
    /// </summary>
    /// <param name="x">A number that should be in range.</param>
    /// <param name="isInfinityAllowed">Whether or not infinity is allowed.</param>
    [DebuggerStepThrough]
    public static void RequirePositive(double x) =>
      RequirePositive(x, DefaultIsInfinityAllowed, ERR_NAN, ERR_BELOW_RANGE, ERR_INFINITE);

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if a number is not positive.
    /// </summary>
    /// <param name="x">A number that should be in range.</param>
    /// <param name="isInfinityAllowed">Whether or not infinity is allowed.</param>
    [DebuggerStepThrough]
    public static void RequirePositive(double x, bool isInfinityAllowed) =>
      RequirePositive(x, isInfinityAllowed, ERR_NAN, ERR_BELOW_RANGE, ERR_INFINITE);

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if a number is not positive.
    /// </summary>
    /// <param name="x">A number that should be in range.</param>
    /// <param name="isInfinityAllowed">Whether or not infinity is allowed.</param>
    /// <param name="messageNaN">Message for the exception if the number is NaN.</param>
    /// <param name="messageLower">Message for the exception if the number is too low.</param>
    /// <param name="messageUpper">Message for the exception if the number is too high.</param>
    [DebuggerStepThrough]
    public static void RequirePositive(double x, string messageNaN, string messageLower, string messageUpper) =>
      RequirePositive(x, DefaultIsInfinityAllowed, messageNaN, messageLower, messageUpper);

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if a number is not positive.
    /// </summary>
    /// <param name="x">A number that should be in range.</param>
    /// <param name="isInfinityAllowed">Whether or not infinity is allowed.</param>
    /// <param name="messageNaN">Message for the exception if the number is NaN.</param>
    /// <param name="messageLower">Message for the exception if the number is too low.</param>
    /// <param name="messageUpper">Message for the exception if the number is too high.</param>
    [DebuggerStepThrough]
    public static void RequirePositive(double x, bool isInfinityAllowed, string messageNaN, string messageLower, string messageUpper) =>
      RequireInRange(x, 0.0, double.PositiveInfinity, !isInfinityAllowed, !isInfinityAllowed, messageNaN, messageLower, messageUpper);

    #endregion

    #region IsNonNegative

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if a number is not positive or zero.
    /// </summary>
    /// <param name="x">A number that should be in range.</param>
    [DebuggerStepThrough]
    public static void RequireNonNegative(double x) =>
      RequireNonNegative(x, DefaultIsInfinityAllowed, ERR_NAN, ERR_BELOW_RANGE, ERR_INFINITE);

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if a number is not positive or zero.
    /// </summary>
    /// <param name="x">A number that should be in range.</param>
    /// <param name="isInfinityAllowed">Whether or not infinity is allowed.</param>
    [DebuggerStepThrough]
    public static void RequireNonNegative(double x, bool isInfinityAllowed) =>
      RequireNonNegative(x, isInfinityAllowed, ERR_NAN, ERR_BELOW_RANGE, ERR_INFINITE);

    /// <summary>
    /// Throws an ArgumentOutOfRangeException if a number is not positive or zero.
    /// </summary>
    /// <param name="x">A number that should be in range.</param>
    /// <param name="isInfinityAllowed">Whether or not infinity is allowed.</param>
    /// <param name="messageNaN">Message for the exception if the number is NaN.</param>
    /// <param name="messageLower">Message for the exception if the number is too low.</param>
    /// <param name="messageUpper">Message for the exception if the number is too high.</param>
    [DebuggerStepThrough]
    public static void RequireNonNegative(double x, bool isInfinityAllowed, string messageNaN, string messageLower, string messageUpper) =>
      RequireInRange(x, 0.0, double.PositiveInfinity, false, !isInfinityAllowed, messageNaN, messageLower, messageUpper);

    #endregion

    #region Exists

    /// <summary>
    /// Throws an DirectoryNotFoundException if directory does not exist.
    /// </summary>
    /// <param name="path">A directory that must exist</param>
    /// <param name="message">Context for the error</param>
    [DebuggerStepThrough]
    public static void RequireDirectory([NotNull] string? path, string? message = default)
    {
      RequireNotNull(path);

      if (!Directory.Exists(path))
      {
        throw new DirectoryNotFoundException(message ?? $"Does not exist: {path}");
      }
    }

    /// <summary>
    /// Throws an FileNotFoundException if file does not exist.
    /// </summary>
    /// <param name="path">A file that must exist</param>
    /// <param name="message">Context for the error</param>
    [DebuggerStepThrough]
    public static void RequireFile([NotNull] string? path, string? message = default)
    {
      RequireNotNull(path);

      if (!File.Exists(path))
      {
        throw new FileNotFoundException(message ?? $"Does not exist: {path}");
      }
    }

    #endregion

    #region Type

    [DebuggerStepThrough]
    public static T RequireInstanceOf<T>([NotNull] object? implementation) =>
      RequireInstanceOf<T>(implementation, $"Expecting implementation of type {typeof(T).Name}. Received instance of {implementation?.GetType().Name}");

    [DebuggerStepThrough]
    public static T RequireInstanceOf<T>([NotNull] object? implementation, [Localizable(false)] string message)
    {
      if (implementation is T t) return t;

      throw new ArgumentException(message, nameof(implementation));
    }

    #endregion

    #region Collections

    [DebuggerStepThrough]
    public static void RequireUniqueElements<T, U>(IEnumerable<T> enumerable, Func<T, U> keySelector) =>
      RequireUniqueElements(enumerable, keySelector, $"Expecting unique instances of {typeof(T).Name}");

    [DebuggerStepThrough]
    public static void RequireUniqueElements<T, U>(IEnumerable<T> enumerable, Func<T, U> keySelector, [Localizable(false)] string message)
    {
      if (enumerable.GroupBy(keySelector).All(g => g.Count() == 1)) return;

      throw new ArgumentException(message, nameof(enumerable));
    }

    [DebuggerStepThrough]
    public static void RequireOrdered<T, U>(IEnumerable<T> enumerable, Func<T, U> keySelector) =>
      RequireOrdered(enumerable, keySelector, $"Expecting ordered sequence of {typeof(T).Name}");

    [DebuggerStepThrough]
    public static void RequireOrdered<T, U>(IEnumerable<T> enumerable, Func<T, U> keySelector, [Localizable(false)] string message)
    {
      var ordered = enumerable.OrderBy(keySelector);
      
      if (enumerable.SequenceEqual(ordered)) return;

      throw new ArgumentException(message, nameof(enumerable));
    }

    #endregion

    #region Defaults

    private const bool DefaultLowerBoundStrictness = true;
    private const bool DefaultUpperBoundStrictness = true;
    private const bool DefaultIsInfinityAllowed = false;

    #endregion
  }
}
