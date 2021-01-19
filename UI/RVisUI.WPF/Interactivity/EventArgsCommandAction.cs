using Microsoft.Xaml.Behaviors;
using RVis.Base.Extensions;
using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace RVisUI.Wpf
{
  public sealed class EventArgsCommandAction : TriggerAction<DependencyObject>
  {
    public static readonly DependencyProperty CommandProperty =
      DependencyProperty.Register("Command", typeof(ICommand), typeof(EventArgsCommandAction), null);

    public ICommand Command
    {
      get => (ICommand)GetValue(CommandProperty);
      set => SetValue(CommandProperty, value);
    }

    public static readonly DependencyProperty CommandParameterProperty =
      DependencyProperty.Register("CommandParameter", typeof(object), typeof(EventArgsCommandAction), null);

    public object CommandParameter
    {
      get => GetValue(CommandParameterProperty);
      set => SetValue(CommandParameterProperty, value);
    }

    public static readonly DependencyProperty EventArgsConverterProperty =
      DependencyProperty.Register("EventArgsConverter", typeof(IValueConverter), typeof(EventArgsCommandAction), null);

    public IValueConverter EventArgsConverter
    {
      get => (IValueConverter)GetValue(EventArgsConverterProperty);
      set => SetValue(EventArgsConverterProperty, value);
    }

    public string? CommandName
    {
      get
      {
        ReadPreamble();
        return _commandName;
      }
      set
      {
        if (CommandName != value)
        {
          WritePreamble();
          _commandName = value;
          WritePostscript();
        }
      }
    }

    protected override void Invoke(object parameter)
    {
      if (Command == default) ResolveCommand();

      var eventArgsConverter = EventArgsConverter;
      if (eventArgsConverter != default) parameter = eventArgsConverter.Convert(parameter, default, default, default);

      if (Command?.CanExecute(parameter) == true)
      {
        Command.Execute(parameter);
      }
    }

    private void ResolveCommand()
    {
      if (AssociatedObject is FrameworkElement frameworkElement)
      {
        if (frameworkElement.DataContext != null)
        {
          var commandPropertyInfo = frameworkElement.DataContext
            .GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(
              p =>
              typeof(ICommand).IsAssignableFrom(p.PropertyType) &&
              string.Equals(p.Name, CommandName, StringComparison.Ordinal)
            );

          if (commandPropertyInfo != null)
          {
            Command = (ICommand)commandPropertyInfo.GetValue(frameworkElement.DataContext, null).AssertNotNull();
          }
        }
      }
    }

    private string? _commandName;
  }
}
