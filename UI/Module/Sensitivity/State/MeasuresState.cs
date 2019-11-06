using LanguageExt;
using RVisUI.Model.Extensions;
using System.ComponentModel;
using DataTable = System.Data.DataTable;

namespace Sensitivity
{
  internal sealed class MeasuresState : INotifyPropertyChanged
  {
    internal string SelectedOutputName
    {
      get => _selectedOutputName;
      set => this.RaiseAndSetIfChanged(ref _selectedOutputName, value, PropertyChanged);
    }
    private string _selectedOutputName;

    internal Map<string, (DataTable FirstOrder, DataTable TotalOrder, DataTable Variance)> OutputMeasures
    {
      get => _outputMeasures;
      set => this.RaiseAndSetIfChanged(ref _outputMeasures, value, PropertyChanged);
    }
    private Map<string, (DataTable FirstOrder, DataTable TotalOrder, DataTable Variance)> _outputMeasures;

    public event PropertyChangedEventHandler PropertyChanged;
  }
}
