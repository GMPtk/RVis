using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace RVisUI.Mvvm.Design
{
  public sealed class RunControlViewModel : IRunControlViewModel
  {
    public RunControlViewModel()
    {
      var sc = System.Threading.SynchronizationContext.Current;

      if (sc is not null)
      {
        Task.Run(async () =>
        {
          var no = 1;

          while (no < 101)
          {
            sc.Post(o => { Messages.Add(Tuple.Create(DateTime.Now, $"Message {no:000}")); }, default);
            await Task.Delay(200);
            ++no;
          }
        });
      }
    }

    public bool IsVisible => false;

    public ObservableCollection<Tuple<DateTime, string>> Messages { get; } = new();
  }
}
