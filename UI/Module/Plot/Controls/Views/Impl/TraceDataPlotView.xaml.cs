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

namespace Plot.Controls.Views
{
  /// <summary>
  /// Interaction logic for TraceDataPlotView.xaml
  /// </summary>
  public partial class TraceDataPlotView : UserControl
  {
    public TraceDataPlotView()
    {
      InitializeComponent();
    }

    private void CloseCommandHandler(object sender, ExecutedRoutedEventArgs e)
    {
      _depVarConfigPopup.IsOpen = false;
    }
  }
}
