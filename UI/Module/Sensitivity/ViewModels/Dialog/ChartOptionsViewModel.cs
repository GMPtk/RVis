using LanguageExt;
using ReactiveUI;
using RVis.Base.Extensions;
using RVisUI.Model;
using RVisUI.Model.Extensions;
using System;
using System.ComponentModel;
using System.Windows.Input;
using static RVis.Base.Check;
using static RVis.Base.Extensions.NumExt;
using static RVisUI.Wpf.WpfTools;

namespace Sensitivity
{
  internal sealed class ChartOptionsViewModel : INotifyPropertyChanged
  {
    public ChartOptionsViewModel()
      : this(new RVisUI.AppInf.Design.AppService())
    {
      RequireTrue(IsInDesignMode);

      _windowTitle = "Options Dialog";

      _chartTitle = "(chart title)";

      _showAxesTab = true;

      _horizontalAxisMinimumAuto = false;
      _horizontalAxisMinimum = -123;

      _horizontalAxisMaximumAuto = true;
      _horizontalAxisMaximum = 123;

      _horizontalAxisAbsoluteMinimumAuto = true;
      _horizontalAxisAbsoluteMinimum = -456;

      _horizontalAxisAbsoluteMaximumAuto = false;
      _horizontalAxisAbsoluteMaximum = 456;

      _verticalAxisMinimumAuto = false;
      _verticalAxisMinimum = -234;

      _verticalAxisMaximumAuto = true;
      _verticalAxisMaximum = 234;

      _verticalAxisAbsoluteMinimumAuto = true;
      _verticalAxisAbsoluteMinimum = -567;

      _verticalAxisAbsoluteMaximumAuto = false;
      _verticalAxisAbsoluteMaximum = 567;
    }

    public ChartOptionsViewModel(IAppService appService)
    {
      Loaded = ReactiveCommand.Create(HandleLoaded);
      SelectElement = ReactiveCommand.Create(HandleSelectElement);
      OK = ReactiveCommand.Create(() => DialogResult = true);

      _reactiveSafeInvoke = appService.GetReactiveSafeInvoke();

      this
        .ObservableForProperty(vm => vm.Page)
        .Subscribe(_reactiveSafeInvoke.SuspendAndInvoke<object>(ObservePage));

      this
        .ObservableForProperty(vm => vm.SelectedOxyColor)
        .Subscribe(_reactiveSafeInvoke.SuspendAndInvoke<object>(ObserveSelectedOxyColor));
    }

    internal const int LABELS_PAGE = 0;
    internal const int CHART_PAGE = 1;
    internal const int AXES_PAGE = 2;

    public int Page
    {
      get => _page;
      set => this.RaiseAndSetIfChanged(ref _page, value, PropertyChanged);
    }
    private int _page;

    public bool ShowAxesTab
    {
      get => _showAxesTab;
      set => this.RaiseAndSetIfChanged(ref _showAxesTab, value, PropertyChanged);
    }
    private bool _showAxesTab;

    public string? ChartTitle
    {
      get => _chartTitle;
      set => this.RaiseAndSetIfChanged(ref _chartTitle, value, PropertyChanged);
    }
    private string? _chartTitle;

    public string? YAxisTitle
    {
      get => _yAxisTitle;
      set => this.RaiseAndSetIfChanged(ref _yAxisTitle, value, PropertyChanged);
    }
    private string? _yAxisTitle;

    public string? XAxisTitle
    {
      get => _xAxisTitle;
      set => this.RaiseAndSetIfChanged(ref _xAxisTitle, value, PropertyChanged);
    }
    private string? _xAxisTitle;

    public ICommand OK { get; }

    public bool? DialogResult
    {
      get => _dialogResult;
      set => this.RaiseAndSetIfChanged(ref _dialogResult, value, PropertyChanged);
    }
    private bool? _dialogResult;

    public int SelectedElement
    {
      get => _selectedElement;
      set => this.RaiseAndSetIfChanged(ref _selectedElement, value, PropertyChanged);
    }
    private int _selectedElement;

    public ICommand SelectElement { get; }

    public int SelectedOxyColor
    {
      get
      {
        if (-1 == _selectedOxyColor && !_elementColorIndices.IsNullOrEmpty())
        {
          _selectedOxyColor = _elementColorIndices[SelectedElement];
          SelectedOxyColorView = !SelectedOxyColorView;
        }
        return _selectedOxyColor;
      }
      set => this.RaiseAndSetIfChanged(ref _selectedOxyColor, value, PropertyChanged);
    }
    private int _selectedOxyColor = NOT_FOUND;

    public Arr<string> ElementNames
    {
      get => _elementNames;
      set => this.RaiseAndSetIfChanged(ref _elementNames, value, PropertyChanged);
    }
    private Arr<string> _elementNames;

    public int[]? ElementColorIndices
    {
      get => _elementColorIndices;
      set => this.RaiseAndSetIfChanged(ref _elementColorIndices, value, PropertyChanged);
    }
    private int[]? _elementColorIndices;

    public bool SelectedOxyColorView
    {
      get => _selectedOxyColorView;
      set => this.RaiseAndSetIfChanged(ref _selectedOxyColorView, value, PropertyChanged);
    }
    private bool _selectedOxyColorView;

    public bool HorizontalAxisMinimumAuto
    {
      get => _horizontalAxisMinimumAuto;
      set => this.RaiseAndSetIfChanged(ref _horizontalAxisMinimumAuto, value, PropertyChanged);
    }
    private bool _horizontalAxisMinimumAuto;

    public double HorizontalAxisMinimum
    {
      get => _horizontalAxisMinimum;
      set => this.RaiseAndSetIfChanged(ref _horizontalAxisMinimum, value, PropertyChanged);
    }
    private double _horizontalAxisMinimum;

    public bool HorizontalAxisMaximumAuto
    {
      get => _horizontalAxisMaximumAuto;
      set => this.RaiseAndSetIfChanged(ref _horizontalAxisMaximumAuto, value, PropertyChanged);
    }
    private bool _horizontalAxisMaximumAuto;

    public double HorizontalAxisMaximum
    {
      get => _horizontalAxisMaximum;
      set => this.RaiseAndSetIfChanged(ref _horizontalAxisMaximum, value, PropertyChanged);
    }
    private double _horizontalAxisMaximum;

    public bool HorizontalAxisAbsoluteMinimumAuto
    {
      get => _horizontalAxisAbsoluteMinimumAuto;
      set => this.RaiseAndSetIfChanged(ref _horizontalAxisAbsoluteMinimumAuto, value, PropertyChanged);
    }
    private bool _horizontalAxisAbsoluteMinimumAuto;

    public double HorizontalAxisAbsoluteMinimum
    {
      get => _horizontalAxisAbsoluteMinimum;
      set => this.RaiseAndSetIfChanged(ref _horizontalAxisAbsoluteMinimum, value, PropertyChanged);
    }
    private double _horizontalAxisAbsoluteMinimum;

    public bool HorizontalAxisAbsoluteMaximumAuto
    {
      get => _horizontalAxisAbsoluteMaximumAuto;
      set => this.RaiseAndSetIfChanged(ref _horizontalAxisAbsoluteMaximumAuto, value, PropertyChanged);
    }
    private bool _horizontalAxisAbsoluteMaximumAuto;

    public double HorizontalAxisAbsoluteMaximum
    {
      get => _horizontalAxisAbsoluteMaximum;
      set => this.RaiseAndSetIfChanged(ref _horizontalAxisAbsoluteMaximum, value, PropertyChanged);
    }
    private double _horizontalAxisAbsoluteMaximum;

    public bool VerticalAxisMinimumAuto
    {
      get => _verticalAxisMinimumAuto;
      set => this.RaiseAndSetIfChanged(ref _verticalAxisMinimumAuto, value, PropertyChanged);
    }
    private bool _verticalAxisMinimumAuto;

    public double VerticalAxisMinimum
    {
      get => _verticalAxisMinimum;
      set => this.RaiseAndSetIfChanged(ref _verticalAxisMinimum, value, PropertyChanged);
    }
    private double _verticalAxisMinimum;

    public bool VerticalAxisMaximumAuto
    {
      get => _verticalAxisMaximumAuto;
      set => this.RaiseAndSetIfChanged(ref _verticalAxisMaximumAuto, value, PropertyChanged);
    }
    private bool _verticalAxisMaximumAuto;

    public double VerticalAxisMaximum
    {
      get => _verticalAxisMaximum;
      set => this.RaiseAndSetIfChanged(ref _verticalAxisMaximum, value, PropertyChanged);
    }
    private double _verticalAxisMaximum;

    public bool VerticalAxisAbsoluteMinimumAuto
    {
      get => _verticalAxisAbsoluteMinimumAuto;
      set => this.RaiseAndSetIfChanged(ref _verticalAxisAbsoluteMinimumAuto, value, PropertyChanged);
    }
    private bool _verticalAxisAbsoluteMinimumAuto;

    public double VerticalAxisAbsoluteMinimum
    {
      get => _verticalAxisAbsoluteMinimum;
      set => this.RaiseAndSetIfChanged(ref _verticalAxisAbsoluteMinimum, value, PropertyChanged);
    }
    private double _verticalAxisAbsoluteMinimum;

    public bool VerticalAxisAbsoluteMaximumAuto
    {
      get => _verticalAxisAbsoluteMaximumAuto;
      set => this.RaiseAndSetIfChanged(ref _verticalAxisAbsoluteMaximumAuto, value, PropertyChanged);
    }
    private bool _verticalAxisAbsoluteMaximumAuto;

    public double VerticalAxisAbsoluteMaximum
    {
      get => _verticalAxisAbsoluteMaximum;
      set => this.RaiseAndSetIfChanged(ref _verticalAxisAbsoluteMaximum, value, PropertyChanged);
    }
    private double _verticalAxisAbsoluteMaximum;

    public string? WindowTitle
    {
      get => _windowTitle;
      set => this.RaiseAndSetIfChanged(ref _windowTitle, value, PropertyChanged);
    }
    private string? _windowTitle;

    public ICommand Loaded { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void ObservePage(object _)
    {
      if (_isLoaded && _page == CHART_PAGE && !_isChartPageInitialized)
      {
        SelectedOxyColorView = !SelectedOxyColorView;
        _isChartPageInitialized = true;
      }
    }

    private void ObserveSelectedOxyColor(object _)
    {
      RequireTrue(SelectedElement.IsFound());
      RequireTrue(SelectedElement < _elementColorIndices?.Length);

      _elementColorIndices[SelectedElement] = _selectedOxyColor;
    }

    private void HandleLoaded()
    {
      RequireTrue(_reactiveSafeInvoke.React);
      RequireFalse(_elementNames.IsEmpty);
      RequireFalse(_elementColorIndices.IsNullOrEmpty());

      if (_page == CHART_PAGE && !_isChartPageInitialized)
      {
        using (_reactiveSafeInvoke.SuspendedReactivity)
        {
          SelectedOxyColorView = !SelectedOxyColorView;
        }

        _isChartPageInitialized = true;
      }

      _isLoaded = true;
    }

    private void HandleSelectElement()
    {
      using (_reactiveSafeInvoke.SuspendedReactivity)
      {
        RequireNotNull(_elementColorIndices);

        SelectedOxyColor = _elementColorIndices[SelectedElement];
        SelectedOxyColorView = !SelectedOxyColorView;
      }
    }

    private readonly IReactiveSafeInvoke _reactiveSafeInvoke;
    private bool _isLoaded;
    private bool _isChartPageInitialized;
  }
}
