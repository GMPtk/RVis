using MahApps.Metro.Controls;
using RVisUI.Mvvm;
using System.Windows.Controls;

namespace RVisUI.Controls.Dialogs
{
  /// <summary>
  /// Interaction logic for ImportExecDialog.xaml
  /// </summary>
  public partial class ImportExecDialog : MetroWindow
  {
    public ImportExecDialog()
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
