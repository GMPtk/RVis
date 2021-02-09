using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using static System.Char;
using static System.Convert;
using static System.Environment;
using static System.Globalization.CultureInfo;
using static System.String;
using static RVis.Base.Check;

namespace RVis.Base.Extensions
{
  public static class StrExt
  {
    public static string? CheckParseValue<T>(this string? s)
    {
      if (s.IsAString())
      {
        T _ = default;
        var typeT = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        try
        {
          _ = (T)ChangeType(s, typeT, InvariantCulture);
        }
        catch (Exception)
        {
          throw new ArgumentException($"Valid {typeT.Name.ToLowerInvariant()} required");
        }
      }

      return s;
    }

    public static bool IsGreaterThan(this string lhs, string rhs) =>
      Compare(lhs, rhs, StringComparison.InvariantCulture) > 0;

    public static bool IsGreaterThanOrEqualTo(this string lhs, string rhs) =>
      Compare(lhs, rhs, StringComparison.InvariantCulture) >= 0;

    public static bool IsLessThan(this string lhs, string rhs) =>
      Compare(lhs, rhs, StringComparison.InvariantCulture) < 0;

    public static bool IsLessThanOrEqualTo(this string lhs, string rhs) =>
      Compare(lhs, rhs, StringComparison.InvariantCulture) <= 0;

    public static string? RejectEmpty(this string? s) =>
      IsNullOrWhiteSpace(s) ? null : s;

    public static string ToValidFileName(this string s, string replaceWith = "_") =>
      _invalid.Replace(s, replaceWith);

    public static string PascalToHyphenated(this string s) =>
      Regex.Replace(s, @"(\S)([A-Z])", @"$1-$2");

    public static string ToKey(this string s) =>
      new string(s.Where(c => IsLetterOrDigit(c)).ToArray()).ToLowerInvariant();

    public static string Elide(this string s, int maxLen, string? suffix = "...") =>
      s.Length <= maxLen ?
        s :
        s.Substring(0, maxLen - (suffix ?? Empty).Length) + suffix;

    public static string ExpandPath(this string s) =>
      s.StartsWith(_docsPrefix, StringComparison.InvariantCulture) == true 
        ? Path.Combine(
          GetFolderPath(SpecialFolder.MyDocuments),
          s[_docsPrefix.Length..]
          )
        : s;

    public static string ContractPath(this string s)
    {
      var myDocuments = GetFolderPath(SpecialFolder.MyDocuments);
      RequireDirectory(myDocuments);
      if (s.StartsWith(myDocuments, StringComparison.InvariantCultureIgnoreCase) == true)
      {
        s = s[myDocuments.Length..];
        s = s.TrimStart(Path.DirectorySeparatorChar);
        s = _docsPrefix + s;
      }
      return s;
    }

    public static byte[] ToHashBytes(this string s) =>
      _sha1.ComputeHash(Encoding.UTF8.GetBytes(s));

    public static string ToHash(this string s)
    {
      if (_sha1HashMemo.TryGetValue(s, out string? hash)) return hash;
      hash = BitConverter.ToString(s.ToHashBytes()).Replace("-", Empty);
      _sha1HashMemo.TryAdd(s, hash);
      return hash;
    }

    public static string Replace(this string s, char[] toReplace, string newValue) =>
      Join(newValue, s.Split(toReplace, StringSplitOptions.RemoveEmptyEntries));

    public static bool EqualsCI(this string? s, string? t) =>
      string.Equals(s, t, StringComparison.InvariantCultureIgnoreCase);

    public static bool DoesNotEqualCI(this string? s, string? t) =>
      !EqualsCI(s, t);

    public static bool IsAString([NotNullWhen(true)] this string? s) =>
      !IsNullOrWhiteSpace(s);

    public static bool IsntAString([NotNullWhen(false)] this string? s) =>
      !IsAString(s);

    public static string[] Tokenize(this string s, char[]? separators = null) =>
      s.Split(separators ?? _separators, StringSplitOptions.RemoveEmptyEntries);

    public static bool ContainsWhiteSpace([NotNullWhen(true)] this string? s) =>
      s != default && s.Any(IsWhiteSpace);

    public static string ToCsvQuoted(this string s) =>
      s.ContainsWhiteSpace() ? "\"" + s + "\"" : s;

    private static Regex GetInvalid()
    {
      var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
      var invalidRegStr = Format(InvariantCulture, @"([{0}]*\.+$)|([{0}]+)", invalidChars);
      return new Regex(invalidRegStr);
    }

    private static readonly char[] _separators = new[] { ';', ',' };
    private const string _docsPrefix = "~/";
    private static readonly Regex _invalid = GetInvalid();
    private static readonly SHA1CryptoServiceProvider _sha1 = new SHA1CryptoServiceProvider();
    private static readonly ConcurrentDictionary<string, string> _sha1HashMemo = new ConcurrentDictionary<string, string>();
  }
}
