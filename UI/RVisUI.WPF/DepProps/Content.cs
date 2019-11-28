using LanguageExt;
using RVis.Base.Extensions;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace RVisUI.Wpf
{
  public static class Content
  {
    public static Arr<string> GetTextBlockRuns(DependencyObject textBlock) =>
      (Arr<string>)textBlock.GetValue(TextBlockRunsProperty);

    public static void SetTextBlockRuns(DependencyObject textBlock, Arr<string> value) =>
      textBlock.SetValue(TextBlockRunsProperty, value);

    public static readonly DependencyProperty TextBlockRunsProperty = DependencyProperty.RegisterAttached(
      "TextBlockRuns",
      typeof(Arr<string>),
      typeof(Content),
      new FrameworkPropertyMetadata(
        Arr<string>.Empty,
        FrameworkPropertyMetadataOptions.AffectsMeasure,
        TextBlockRunsPropertyChanged
        )
      );

    private static void TextBlockRunsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (!(d is TextBlock textBlock)) return;
      if (!(e.NewValue is Arr<string> runs)) return;

      textBlock.Inlines.Clear();
      var separator = textBlock.Tag?.ToString();
      const string escapePrefix = @"\u";

      if (separator?.StartsWith(escapePrefix) == true)
      {
        separator = separator.Substring(escapePrefix.Length);
        var unicodeChar = int.Parse(separator, NumberStyles.HexNumber);
        separator = new string((char)unicodeChar, 1);
      }

      runs.Iter((i, s) =>
      {
        if (i > 0 && separator.IsAString())
        {
          textBlock.Inlines.Add(new Run(separator));
        }

        textBlock.Inlines.Add(new Run(s));
      });
    }

    public static object GetResource(DependencyObject o) =>
      o.GetValue(ResourceProperty);

    public static void SetResource(DependencyObject o, object value) =>
      o.SetValue(ResourceProperty, value);

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

    public static DataPipeCollection GetDataPipes(DependencyObject o) =>
      (DataPipeCollection)o.GetValue(DataPipesProperty);

    public static void SetDataPipes(DependencyObject o, DataPipeCollection value) =>
      o.SetValue(DataPipesProperty, value);

    public static readonly DependencyProperty DataPipesProperty =
      DependencyProperty.RegisterAttached(
        "DataPipes",
        typeof(DataPipeCollection),
        typeof(Content),
        new UIPropertyMetadata(default)
        );
  }
}
