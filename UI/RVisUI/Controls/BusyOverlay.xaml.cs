using RVis.Base.Extensions;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace RVisUI.Controls
{
  /// <summary>
  /// Interaction logic for BusyOverlay.xaml
  /// </summary>
  public partial class BusyOverlay : UserControl
  {
    public BusyOverlay()
    {
      InitializeComponent();
    }

    public string BusyWith
    {
      get => (string)GetValue(BusyWithProperty);
      set => SetValue(BusyWithProperty, value);
    }

    public static readonly DependencyProperty BusyWithProperty =
        DependencyProperty.Register(
          "BusyWith",
          typeof(string),
          typeof(BusyOverlay),
          new PropertyMetadata(null)
          );

    public bool EnableCancel
    {
      get => (bool)GetValue(EnableCancelProperty);
      set => SetValue(EnableCancelProperty, value);
    }

    public static readonly DependencyProperty EnableCancelProperty =
        DependencyProperty.Register(
          "EnableCancel",
          typeof(bool),
          typeof(BusyOverlay),
          new PropertyMetadata(false)
          );

    public ICommand Cancel
    {
      get => (ICommand)GetValue(CancelProperty);
      set => SetValue(CancelProperty, value);
    }

    public static readonly DependencyProperty CancelProperty =
        DependencyProperty.Register(
          "Cancel",
          typeof(ICommand),
          typeof(BusyOverlay),
          new PropertyMetadata(null)
          );

    public ObservableCollection<string> Messages
    {
      get => (ObservableCollection<string>)GetValue(MessagesProperty);
      set => SetValue(MessagesProperty, value);
    }

    public static readonly DependencyProperty MessagesProperty =
        DependencyProperty.Register(
          "Messages",
          typeof(ObservableCollection<string>),
          typeof(BusyOverlay),
          new PropertyMetadata(null, HandleMessagesPropertyChanged)
          );

    private static void HandleMessagesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (!d.Resolve(out BusyOverlay? busyOverlay)) return;

      if (e.OldValue is ObservableCollection<string> existing)
      {
        existing.CollectionChanged -= busyOverlay.HandleMessagesCollectionChanged;
      }

      if (!e.NewValue.Resolve(out ObservableCollection<string>? messages)) return;

      messages.CollectionChanged += busyOverlay.HandleMessagesCollectionChanged;
    }

    private void HandleMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
      if (e.Action == NotifyCollectionChangedAction.Add)
      {
        if (e.NewItems is not null)
        {
          foreach (string message in e.NewItems)
          {
            var run = new Run(message);
            _tbMessages.Inlines.Add(run);
            _tbMessages.Inlines.Add(new LineBreak());
          }
          _svMessages.ScrollToBottom();
        }
      }
      else if(e.Action == NotifyCollectionChangedAction.Reset)
      {
        _tbMessages.Inlines.Clear();
      }
    }
  }
}
