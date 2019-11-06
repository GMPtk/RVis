using RVisUI.Model.Extensions;
using System.ComponentModel;
using System.Data;

namespace Sampling
{
  internal class SamplesDesignActivityViewModel : DesignActivityViewModelBase, ISamplesDesignActivityViewModel, INotifyPropertyChanged
  {
    internal SamplesDesignActivityViewModel() : base("AWAITING SAMPLES") { }

    public DataView Samples
    {
      get => _samples;
      set => this.RaiseAndSetIfChanged(ref _samples, value, PropertyChanged);
    }
    private DataView _samples;

    public event PropertyChangedEventHandler PropertyChanged;
  }
}
