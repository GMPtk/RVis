using MahApps.Metro.Controls;
using RVis.Base.Extensions;
using RVisUI.Ioc.Mvvm;
using RVisUI.Model;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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

      var moduleInfos = App.Current.AppState.LoadModules();
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
      if (enable) _toggleEnable.IsChecked = (_listView.SelectedItem as ModuleConfigViewModel).IsEnabled;
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
      (_listView.SelectedItem as ModuleConfigViewModel).IsEnabled = isEnabled;
      (_listView.SelectedItem as ModuleConfigViewModel).ModuleInfo.IsEnabled = isEnabled;
    }

    private void HandleOK(object sender, RoutedEventArgs e)
    {
      var moduleInfos = _moduleConfigViewModels.Select(vm => vm.ModuleInfo).ToArray();
      var moduleConfiguration = ModuleInfo.GetModuleConfiguration(moduleInfos);
      App.Current.AppSettings.ModuleConfiguration = moduleConfiguration;
      DialogResult = true;
      Close();
    }

    private readonly ObservableCollection<ModuleConfigViewModel> _moduleConfigViewModels;
  }
}
