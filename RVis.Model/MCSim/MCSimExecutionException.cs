using LanguageExt;
using System;
using static System.Environment;
using static System.String;

namespace RVis.Model
{
  public sealed class MCSimExecutionException : Exception
  {
    public MCSimExecutionException(string message, int exitCode, Arr<string> diagnostics) : base(message)
    {
      ExitCode = exitCode;
      Diagnostics = diagnostics;
    }

    public int ExitCode { get; }
    
    public Arr<string> Diagnostics { get; }

    public override string ToString() => 
      $"{Message}{NewLine}{NewLine}" +
      $"Diagnostics:{NewLine}{NewLine}" +
      $"{Join(NewLine, Diagnostics)}{NewLine}{NewLine}" +
      $"{StackTrace?.ToString()}";
  }
}
