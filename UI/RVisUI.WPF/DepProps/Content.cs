using System.Windows;

namespace RVisUI.Wpf
{
  public static class Content
  {
    public static object GetResource(DependencyObject o)
    {
      return o.GetValue(ResourceProperty);
    }

    public static void SetResource(DependencyObject o, object value)
    {
      o.SetValue(ResourceProperty, value);
    }

    public static readonly DependencyProperty ResourceProperty =
      DependencyProperty.RegisterAttached(
        "Resource",
        typeof(object),
        typeof(Content),
        new UIPropertyMetadata(null, OnResourceChanged)
        );

    private static void OnResourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
    }

    public static readonly DependencyProperty DataPipesProperty =
      DependencyProperty.RegisterAttached(
        "DataPipes",
        typeof(DataPipeCollection),
        typeof(Content),
        new UIPropertyMetadata(default)
        );

    public static void SetDataPipes(DependencyObject o, DataPipeCollection value) => 
      o.SetValue(DataPipesProperty, value);

    public static DataPipeCollection GetDataPipes(DependencyObject o) => 
      (DataPipeCollection)o.GetValue(DataPipesProperty);
  }
}
