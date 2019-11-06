using System;

namespace Sensitivity
{
  internal class DesignDigestViewModel : IDesignDigestViewModel
  {
    internal DesignDigestViewModel(DateTime createdOn, string description)
    {
      CreatedOn = createdOn;
      Description = description;
    }

    public DateTime CreatedOn { get; }

    public string Description { get; }
  }
}
