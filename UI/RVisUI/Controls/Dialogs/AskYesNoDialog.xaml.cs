using MahApps.Metro.Controls;
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
using System.Windows.Shapes;

namespace RVisUI.Controls.Dialogs
{
  /// <summary>
  /// Interaction logic for AskYesNoDialog.xaml
  /// </summary>
  public partial class AskYesNoDialog : MetroWindow
  {
    public AskYesNoDialog(
      string question,
      string? prompt = default,
      string about = "Confirm"
      )
    {
      InitializeComponent();

      Title = about;

      if (prompt.IsAString())
      {
        _tbQuestion.Inlines.Add(new Run(prompt));
        _tbQuestion.Inlines.Add(new LineBreak());
        _tbQuestion.Inlines.Add(new LineBreak());
      }
      _tbQuestion.Inlines.Add(new Run(question));
    }

    private void HandleOK(object _, RoutedEventArgs __)
    {
      DialogResult = true;
      Close();
    }
  }
}
