using MahApps.Metro.Controls;
using RVisUI.Model;
using System.Windows;

namespace RVisUI.Controls.Dialogs
{
  /// <summary>
  /// Interaction logic for NotifyDialog.xaml
  /// </summary>
  public partial class NotifyDialog : MetroWindow
  {
    public NotifyDialog(NotificationType notificationType, string about, string notification)
    {
      InitializeComponent();

      switch (notificationType)
      {
        case NotificationType.Error:
          _piError.Visibility = Visibility.Visible;
          break;

        case NotificationType.Warning:
          _piWarning.Visibility = Visibility.Visible;
          break;

        default:
          _piInformation.Visibility = Visibility.Visible;
          break;
      }

      Title = about;

      _tbNotification.Text = notification;
    }
  }
}
