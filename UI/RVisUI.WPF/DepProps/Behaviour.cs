using RVis.Base.Extensions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using static RVis.Base.ProcessHelper;

namespace RVisUI.Wpf
{
  public static class Behaviour
  {
    public static bool GetSelectedListItemView(DependencyObject o) =>
      (bool)o.GetValue(SelectedListItemViewProperty);

    public static void SetSelectedListItemView(DependencyObject o, bool value) =>
      o.SetValue(SelectedListItemViewProperty, value);

    public static readonly DependencyProperty SelectedListItemViewProperty =
      DependencyProperty.RegisterAttached(
        "SelectedListItemView",
        typeof(bool),
        typeof(Behaviour),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.None, OnSelectedListItemViewChanged)
        );

    private static void OnSelectedListItemViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is ListBox listBox && null != listBox.SelectedItem)
      {
        listBox.Dispatcher.InvokeAsync(
          () => listBox.ScrollIntoView(listBox.SelectedItem),
          DispatcherPriority.ApplicationIdle
          );
      }
    }

    public static void SetUpdateOnEnter(DependencyObject d, DependencyProperty value) =>
      d.SetValue(UpdateOnEnterProperty, value);

    public static DependencyProperty GetUpdateOnEnter(DependencyObject d) =>
      (DependencyProperty)d.GetValue(UpdateOnEnterProperty);

    public static readonly DependencyProperty UpdateOnEnterProperty =
      DependencyProperty.RegisterAttached(
        "UpdateOnEnter",
        typeof(DependencyProperty),
        typeof(Behaviour),
        new PropertyMetadata(null, HandleUpdateOnEnterPropertyChanged)
        );

    private static void HandleUpdateOnEnterPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is not UIElement element) return;

      if (e.OldValue != default)
      {
        element.PreviewKeyDown -= HandlePreviewKeyDown;
      }

      if (e.NewValue != default)
      {
        element.PreviewKeyDown += HandlePreviewKeyDown;
      }
    }

    static void HandlePreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter) DoUpdateSource(e.Source);
    }

    static void DoUpdateSource(object source)
    {
      if (source is not DependencyObject dependencyObject) return;

      var dependencyProperty = GetUpdateOnEnter(dependencyObject);

      if (dependencyProperty == default) return;

      var binding = BindingOperations.GetBindingExpression(dependencyObject, dependencyProperty);

      if (binding != default)
      {
        binding.UpdateSource();
      }
    }

    public static bool? GetDialogResult(DependencyObject o) =>
      (bool?)o.GetValue(DialogResultProperty);

    public static void SetDialogResult(DependencyObject o, bool? value) =>
      o.SetValue(DialogResultProperty, value);

    public static readonly DependencyProperty DialogResultProperty =
      DependencyProperty.RegisterAttached(
        "DialogResult",
        typeof(bool?),
        typeof(Behaviour),
        new PropertyMetadata(default(bool?), HandleDialogResultChanged));

    private static void HandleDialogResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is Window window) window.DialogResult = e.NewValue as bool?;
    }

    public static string? DocRoot { get; set; }

    public static bool? GetOpenInBrowser(DependencyObject o) =>
      (bool?)o.GetValue(OpenInBrowserProperty);

    public static void SetOpenInBrowser(DependencyObject o, bool? value) => o.SetValue(OpenInBrowserProperty, value);

    public static readonly DependencyProperty OpenInBrowserProperty =
      DependencyProperty.RegisterAttached(
        "OpenInBrowser",
        typeof(bool?),
        typeof(Behaviour),
        new UIPropertyMetadata(default(bool?), HandleOpenInBrowserChanged)
        );

    private static void HandleOpenInBrowserChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (!d.Resolve(out Hyperlink? hyperlink)) return;

      var openInBrowser = (bool?)e.NewValue == true && (bool?)e.OldValue != true;
      var desist = (bool?)e.NewValue != true && (bool?)e.OldValue == true;

      if (openInBrowser)
      {
        hyperlink.RequestNavigate += HandleRequestNavigate;
      }
      else if (desist)
      {
        hyperlink.RequestNavigate -= HandleRequestNavigate;
      }
    }

    private static void HandleRequestNavigate(object _, RequestNavigateEventArgs e) =>
      OpenUrl(e.Uri.ToString());

    public static bool? GetShowDoc(DependencyObject o) =>
      (bool?)o.GetValue(ShowDocProperty);

    public static void SetShowDoc(DependencyObject o, bool? value) => o.SetValue(ShowDocProperty, value);

    public static readonly DependencyProperty ShowDocProperty =
      DependencyProperty.RegisterAttached(
        "ShowDoc",
        typeof(bool?),
        typeof(Behaviour),
        new UIPropertyMetadata(default(bool?), HandleShowDocChanged)
        );

    private static void HandleShowDocChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (!d.Resolve(out Button? button)) return;

      var showDoc = (bool?)e.NewValue == true && (bool?)e.OldValue != true;
      var desist = (bool?)e.NewValue != true && (bool?)e.OldValue == true;

      if (showDoc)
      {
        button.CommandBindings.Add(_showDocBinding);
        button.Command = _showDoc;
      }
      else if (desist)
      {
        button.CommandBindings.Remove(_showDocBinding);
        button.Command = default;
      }
    }

    private static void ExecuteShowDoc(object sender, ExecutedRoutedEventArgs e)
    {
      var button = (FrameworkElement)e.Source;
      var relUrl = button.Tag as string;
      var url = DocRoot + relUrl;
      if (url.IsAString()) OpenUrl(url);

      e.Handled = true;
    }

    // from https://mike-ward.net/2013/09/23/keyboard-handling-in-wpf-popup-class/

    public static readonly DependencyProperty IsKeyboardInputEnabledProperty =
      DependencyProperty.RegisterAttached(
        "IsKeyboardInputEnabled",
        typeof(bool),
        typeof(Behaviour),
        new PropertyMetadata(default(bool), HandleIsKeyboardInputEnabledChanged));

    public static bool GetIsKeyboardInputEnabled(DependencyObject d) =>
      (bool)d.GetValue(IsKeyboardInputEnabledProperty);

    public static void SetIsKeyboardInputEnabled(DependencyObject d, bool value) =>
      d.SetValue(IsKeyboardInputEnabledProperty, value);

    private static void HandleIsKeyboardInputEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs ea)
    {
      if (sender is not Popup popup) return;
      if (ea.NewValue is not bool enable) return;

      EnableKeyboardInput(popup, enable);
    }

    private static void EnableKeyboardInput(Popup popup, bool enable)
    {
      if (enable)
      {
        IInputElement? restoreFocusToOnClose = null;

        popup.Loaded += (_, __) =>
        {
          popup.Child.Focusable = true;
          popup.Child.IsVisibleChanged += (___, ____) =>
          {
            if (popup.Child.IsVisible)
            {
              restoreFocusToOnClose = Keyboard.FocusedElement;
              Keyboard.Focus(popup.Child);
            }
          };
        };

        popup.Closed += (_, __) => Keyboard.Focus(restoreFocusToOnClose);
      }
    }

    private static readonly ICommand _showDoc =
      new RoutedCommand();

    private static readonly CommandBinding _showDocBinding =
      new CommandBinding(_showDoc, ExecuteShowDoc);
  }
}
