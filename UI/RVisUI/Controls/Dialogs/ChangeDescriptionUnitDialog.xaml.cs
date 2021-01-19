using MahApps.Metro.Controls;
using System.Windows.Controls;

namespace RVisUI.Controls.Dialogs
{
  /// <summary>
  /// Interaction logic for ChangeDescriptionUnitDialog.xaml
  /// </summary>
  public partial class ChangeDescriptionUnitDialog : MetroWindow
  {
    public ChangeDescriptionUnitDialog()
    {
      InitializeComponent();
    }

    private void HandleSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      var lineSymDescUnit = (object[])_dataGrid.SelectedItem;
      _txtDesc.Text = lineSymDescUnit[2] as string;
      _txtUnit.Text = lineSymDescUnit[3] as string;
    }

    private void HandleClearDesc(object sender, System.Windows.RoutedEventArgs e) => _txtDesc.Text = string.Empty;

    private void HandleClearUnit(object sender, System.Windows.RoutedEventArgs e) => _txtUnit.Text = string.Empty;
  }
}
