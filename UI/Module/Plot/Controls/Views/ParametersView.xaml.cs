using RVis.Base.Extensions;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Plot.Controls.Views
{
  /// <summary>
  /// Interaction logic for ParametersView.xaml
  /// </summary>
  public partial class ParametersView : UserControl
  {
    public ParametersView()
    {
      InitializeComponent();

      _cvsUnselected = (CollectionViewSource)Resources["_cvsUnselected"];

      _txtSearch.TextChanged += HandleSearchTextChanged;

      _btnClearSearch.Click += HandleClearSearchClicked;
    }

    private void HandleSearchTextChanged(object sender, TextChangedEventArgs e)
    {
      if (_txtSearch.Text.IsAString())
      {
        if (null == _cvsUnselected.View.Filter)
        {
          _cvsUnselected.View.Filter = Predicate;
        }
      }
      else
      {
        _cvsUnselected.View.Filter = null;
      }

      _cvsUnselected.View.Refresh();
    }

    private bool Predicate(object o)
    {
      if (o is not IParameterViewModel parameterViewModel)
      {
        throw new InvalidOperationException($"Expecting {nameof(IParameterViewModel)} instance");
      }

      var filter = _txtSearch.Text.ToUpperInvariant();

      return filter.Length == 1 
        ? parameterViewModel.SortKey.StartsWith(filter, StringComparison.InvariantCulture) 
        : parameterViewModel.SortKey.Contains(filter);
    }

    private void HandleClearSearchClicked(object sender, RoutedEventArgs e) => 
      _txtSearch.Text = string.Empty;

    private readonly CollectionViewSource _cvsUnselected;
  }
}
