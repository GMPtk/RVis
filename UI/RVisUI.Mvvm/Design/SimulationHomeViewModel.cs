using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

#nullable disable

namespace RVisUI.Mvvm.Design
{
  public class SimulationHomeViewModel : ISimulationHomeViewModel
  {
    public SimulationHomeViewModel()
    {
      var assembly = typeof(ObservableCollection<>).Assembly;
      var names = (from t in assembly.GetTypes()
                  where t.IsClass && t.Namespace == "System.Collections.ObjectModel"
                  select t.Name).ToArray();
      var random = new Random();
      var sc = SynchronizationContext.Current;
      Task.Run(async () =>
      {
        while (1000 > BusyMessages.Count)
        {
          await Task.Delay(random.Next(2000));
          var name = names[random.Next(names.Length)];
          sc.Send((_) => {
            BusyMessages.Add(name);
            BusyMessages.Add("xxx");
          }, null);
        }
      });

    }

    public string Name { get => "A PBPK Model..."; set => throw new NotImplementedException(); }

    public ICommand ChangeCommonConfiguration => throw new NotImplementedException();

    public ICommand Export => throw new NotImplementedException();

    public ICommand Close => throw new NotImplementedException();

    public bool IsBusy { get => false; set => throw new NotImplementedException(); }
    public string BusyWith { get => "The Main Activity. Some extra text. Some extra text. Some extra text. Some extra text."; set => throw new NotImplementedException(); }

    public ObservableCollection<string> BusyMessages { get; } = new ObservableCollection<string>();

    public bool EnableBusyCancel { get => true; set => throw new NotImplementedException(); }

    public ICommand BusyCancel => throw new NotImplementedException();

    public ISharedStateViewModel SharedStateViewModel => new SharedStateViewModel();

    public int UIComponentIndex { get => 0; set => throw new NotImplementedException(); }

    public string ActiveUIComponentName { get => "My module"; set => throw new NotImplementedException(); }
  }
}
