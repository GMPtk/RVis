using System.Windows;

namespace RVisUI.Wpf
{
  public class DataPipeCollection : FreezableCollection<DataPipe>
  {
    protected override Freezable CreateInstanceCore()
    {
      return new DataPipeCollection();
    }
  }
}
