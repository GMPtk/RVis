using RVis.Base.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RVisUI.Controls.Views
{
  /// <summary>
  /// Interaction logic for SelectSimulationView.xaml
  /// </summary>
  public partial class SelectSimulationView : UserControl
  {
    public SelectSimulationView()
    {
      InitializeComponent();
      _simulations.Loaded += (s, e) =>
      {
        if (_simulations.SelectedIndex.IsFound())
        {
          _simulations.ScrollIntoView(_simulations.SelectedItem);
        }
      };
    }
  }
}
