using MahApps.Metro.Controls;
using RVisUI.Mvvm;
using System.Windows.Controls;

namespace RVisUI.Controls.Dialogs
{
  /// <summary>
  /// Interaction logic for ImportTmplDialog.xaml
  /// </summary>
  public partial class ImportTmplDialog : MetroWindow
  {
    public ImportTmplDialog()
    {
      InitializeComponent();
    }

    private void HandleParametersMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      var dataGrid = (DataGrid)sender;
      if (dataGrid.SelectedItem is IParameterCandidateViewModel vm && vm.IsUsed)
      {
        vm.ChangeUnitDescription.Execute(vm);
      }
    }

    private void HandleOutputMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      var dataGrid = (DataGrid)sender;
      if (dataGrid.SelectedItem is IElementCandidateViewModel vm && vm.IsUsed)
      {
        vm.ChangeUnitDescription.Execute(vm);
      }
    }
  }
}
