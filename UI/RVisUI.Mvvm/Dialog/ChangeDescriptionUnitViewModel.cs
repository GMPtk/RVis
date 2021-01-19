using LanguageExt;
using ReactiveUI;
using RVis.Base.Extensions;
using RVis.Model;
using System.Linq;
using System.Windows.Input;
using static System.Globalization.CultureInfo;

namespace RVisUI.Mvvm
{
  public class ChangeDescriptionUnitViewModel : ReactiveObject, IChangeDescriptionUnitViewModel
  {
    public ChangeDescriptionUnitViewModel(string name, string? description, string? unit, Arr<ISymbolInfo> symbolInfos)
    {
      _targetSymbol = name;
      _description = description;
      _unit = unit;

      var choices = symbolInfos
        .Filter(si => si.Symbol.IsAString() && (si.Comment.IsAString() || si.Unit.IsAString()))
        .OrderBy(si => si.Symbol);

      LineSymDescUnit = choices
        .Select(si => new object?[]
        {
          0 == si.LineNo ? string.Empty : si.LineNo.ToString(InvariantCulture),
          si.Symbol,
          si.Comment,
          si.Unit
        })
        .ToArray();

      OK = ReactiveCommand.Create(() => DialogResult = true);
      Cancel = ReactiveCommand.Create(() => DialogResult = false);
    }

    public string TargetSymbol
    {
      get => _targetSymbol;
      set => this.RaiseAndSetIfChanged(ref _targetSymbol, value);
    }
    private string _targetSymbol;

    public string? Description
    {
      get => _description;
      set => this.RaiseAndSetIfChanged(ref _description, value);
    }
    private string? _description;

    public string? Unit
    {
      get => _unit;
      set => this.RaiseAndSetIfChanged(ref _unit, value);
    }
    private string? _unit;

    public object?[][] LineSymDescUnit { get; }

    public ICommand OK { get; }

    public ICommand Cancel { get; }

    public bool? DialogResult
    {
      get => _dialogResult;
      set => this.RaiseAndSetIfChanged(ref _dialogResult, value);
    }

    private bool? _dialogResult;
  }
}
