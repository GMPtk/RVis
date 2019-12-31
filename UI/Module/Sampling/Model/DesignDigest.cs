using System;

namespace Sampling
{
  internal readonly struct DesignDigest
  {
    internal DesignDigest(DateTime createdOn, string description)
    {
      CreatedOn = createdOn;
      Description = description;
    }

    internal DateTime CreatedOn { get; }
    internal string Description { get; }
  }
}
