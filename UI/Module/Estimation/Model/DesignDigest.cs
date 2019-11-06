using System;

namespace Estimation
{
  internal struct DesignDigest
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
