using LanguageExt;
using MathNet.Numerics.LinearAlgebra;
using ReactiveUI;
using RVisUI.Model.Extensions;
using System.ComponentModel;
using System.Windows.Input;
using static LanguageExt.Prelude;
using static RVis.Base.Check;
using static System.Math;

namespace Sampling
{
  internal sealed class ViewCorrelationViewModel : IViewCorrelationViewModel, INotifyPropertyChanged
  {
    internal ViewCorrelationViewModel(Arr<string> parameterNames, Matrix<double> correlations)
    {
      ParameterNames = parameterNames;

      var nParameters = ParameterNames.Count;

      RequireTrue(
        nParameters > 1 &&
        nParameters == correlations.RowCount &&
        nParameters == correlations.ColumnCount
        );

      var rcParameterViewModels = new IRCParameterViewModel[nParameters][];

      Range(0, nParameters).Iter(i =>
      {
        rcParameterViewModels[i] = new IRCParameterViewModel[nParameters];

        var row = correlations.Row(i);

        Range(0, nParameters).Iter(j =>
        {
          if (j == i)
          {
            rcParameterViewModels[i][j] = new RCParameterDiagonalViewModel(
              ParameterNames[i],
              1d
              );
          }
          else
          {
            rcParameterViewModels[i][j] = new RCParameterViewModel(
              ParameterNames[i],
              default
              )
            {
              CorrelationN = Round(row[j], 2)
            };
          }
        });
      });

      _rcParameterViewModels = rcParameterViewModels;

      Close = ReactiveCommand.Create(HandleClose);
    }

    public Arr<string> ParameterNames
    {
      get => _parameterNames;
      set => this.RaiseAndSetIfChanged(ref _parameterNames, value, PropertyChanged);
    }
    private Arr<string> _parameterNames;

    public IRCParameterViewModel[][] RCParameterViewModels
    {
      get => _rcParameterViewModels;
      set => this.RaiseAndSetIfChanged(ref _rcParameterViewModels, value, PropertyChanged);
    }
    private IRCParameterViewModel[][] _rcParameterViewModels;

    public ICommand Close { get; }

    public bool? DialogResult
    {
      get => _dialogResult;
      set => this.RaiseAndSetIfChanged(ref _dialogResult, value, PropertyChanged);
    }
    private bool? _dialogResult;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void HandleClose() =>
      DialogResult = false;
  }
}
