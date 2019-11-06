using System;

namespace RVis.Base
{
  [Flags]
  public enum ObservableQualifier
  {
    Add = 0b0000_0000_0000_0001,
    Remove = 0b0000_0000_0000_0010,
    Change = 0b0000_0000_0000_0100
  }
}
