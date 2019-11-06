using System.Windows;
using static RVis.Base.Check;

namespace RVisUI.Wpf
{
  public class DataPipe : Freezable
  {
    public object Source
    {
      get => GetValue(SourceProperty);
      set => SetValue(SourceProperty, value);
    }

    public static readonly DependencyProperty SourceProperty =
      DependencyProperty.Register(
        nameof(Source), 
        typeof(object), 
        typeof(DataPipe),
        new FrameworkPropertyMetadata(default, new PropertyChangedCallback(OnSourceChanged))
        );

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      var dataPipe = RequireInstanceOf<DataPipe>(d);
      dataPipe.OnSourceChanged(e);
    }

    protected virtual void OnSourceChanged(DependencyPropertyChangedEventArgs e) => 
      Target = e.NewValue;

    public object Target
    {
      get => GetValue(TargetProperty);
      set => SetValue(TargetProperty, value);
    }

    public static readonly DependencyProperty TargetProperty =
      DependencyProperty.Register(
        nameof(Target), 
        typeof(object), 
        typeof(DataPipe),
        new FrameworkPropertyMetadata(default, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );

    protected override Freezable CreateInstanceCore() => 
      new DataPipe();
  }
}
