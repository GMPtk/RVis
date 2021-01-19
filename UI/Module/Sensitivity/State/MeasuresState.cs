using LanguageExt;
using RVisUI.Model.Extensions;
using System.ComponentModel;
using DataTable = System.Data.DataTable;

namespace Sensitivity
{
  internal sealed class MeasuresState : INotifyPropertyChanged
  {
    internal string? SelectedOutputName
    {
      get => _selectedOutputName;
      set => this.RaiseAndSetIfChanged(ref _selectedOutputName, value, PropertyChanged);
    }
    private string? _selectedOutputName;

    internal Map<string, (DataTable Mu, DataTable MuStar, DataTable Sigma)> MorrisOutputMeasures
    {
      get => _morrisOutputMeasures;
      set => this.RaiseAndSetIfChanged(ref _morrisOutputMeasures, value, PropertyChanged);
    }
    private Map<string, (DataTable Mu, DataTable MuStar, DataTable Sigma)> _morrisOutputMeasures;

    internal Map<string, (DataTable FirstOrder, DataTable TotalOrder, DataTable Variance)> Fast99OutputMeasures
    {
      get => _fast99OutputMeasures;
      set => this.RaiseAndSetIfChanged(ref _fast99OutputMeasures, value, PropertyChanged);
    }
    private Map<string, (DataTable FirstOrder, DataTable TotalOrder, DataTable Variance)> _fast99OutputMeasures;

    public event PropertyChangedEventHandler? PropertyChanged;
  }
}
