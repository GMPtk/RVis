using MahApps.Metro.Controls;
using RVis.Base.Extensions;
using RVisUI.Ioc.Mvvm;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using static RVis.Base.Check;
using static RVisUI.Model.ModuleInfo;

namespace RVisUI.Controls.Dialogs
{
  /// <summary>
  /// Interaction logic for ConfigureModulesDialog.xaml
  /// </summary>
  public partial class ConfigureModulesDialog : MetroWindow
  {
    public ConfigureModulesDialog()
    {
      InitializeComponent();

      _moveDown.IsEnabled = false;
      _moveUp.IsEnabled = false;
      _toggleEnable.IsEnabled = false;

      var services = App.Current.AppState.GetServices(rebind: true);
      var moduleInfos = GetModuleInfos(services);
      moduleInfos = SortAndEnable(moduleInfos, App.Current.AppSettings.ModuleConfiguration);

      var viewModels = moduleInfos.Select(mi => new ModuleConfigViewModel(mi));
      _moduleConfigViewModels = new ObservableCollection<ModuleConfigViewModel>(viewModels);
      _listView.ItemsSource = _moduleConfigViewModels;
    }

    private void UpdateMoveEnable()
    {
      var enable = _listView.SelectedIndex.IsFound();
      var isAtStart = 0 == _listView.SelectedIndex;
      var isAtEnd = _moduleConfigViewModels.Count == _listView.SelectedIndex + 1;
      _moveDown.IsEnabled = enable && !isAtEnd;
      _moveUp.IsEnabled = enable && !isAtStart;
    }

    private void UpdateToggleEnableEnable()
    {
      var enable = _listView.SelectedIndex.IsFound();
      _toggleEnable.IsEnabled = enable;
      if (enable) _toggleEnable.IsChecked = ((ModuleConfigViewModel)_listView.SelectedItem).IsEnabled;
    }

    private void HandleSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      UpdateMoveEnable();
      UpdateToggleEnableEnable();
    }

    private void HandleMoveDown(object sender, RoutedEventArgs e)
    {
      var selectedIndex = _listView.SelectedIndex;
      _moduleConfigViewModels.Move(selectedIndex, selectedIndex + 1);
      UpdateMoveEnable();
    }

    private void HandleMoveUp(object sender, RoutedEventArgs e)
    {
      var selectedIndex = _listView.SelectedIndex;
      _moduleConfigViewModels.Move(selectedIndex, selectedIndex - 1);
      UpdateMoveEnable();
    }

    private void HandleToggleEnable(object sender, RoutedEventArgs e)
    {
      var isEnabled = _toggleEnable.IsChecked == true;
      var moduleConfigViewModel = RequireInstanceOf<ModuleConfigViewModel>(_listView.SelectedItem);
      moduleConfigViewModel.IsEnabled = isEnabled;
      moduleConfigViewModel.ModuleInfo.IsEnabled = isEnabled;
    }

    private void HandleOK(object sender, RoutedEventArgs e)
    {
      var moduleInfos = _moduleConfigViewModels.Select(vm => vm.ModuleInfo).ToArr();
      var moduleConfiguration = GetModuleConfiguration(moduleInfos);
      App.Current.AppSettings.ModuleConfiguration = moduleConfiguration;
      DialogResult = true;
      Close();
    }

    private readonly ObservableCollection<ModuleConfigViewModel> _moduleConfigViewModels;
  }
}
