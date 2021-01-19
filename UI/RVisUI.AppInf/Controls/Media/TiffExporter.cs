using OxyPlot;
using OxyPlot.Wpf;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RVisUI.AppInf
{
  /// <summary>
  /// Provides functionality to export plots to tiff (adaptation of OxyPlot PngExporter).
  /// </summary>
  public sealed class TiffExporter : IExporter
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="TiffExporter" /> class.
    /// </summary>
    public TiffExporter()
    {
      Width = 700;
      Height = 400;
      Resolution = 96;
      Background = OxyColors.White;
    }

    /// <summary>
    /// Gets or sets the width of the output image.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the output image.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Gets or sets the resolution of the output image.
    /// </summary>
    /// <value>The resolution in dots per inch (dpi).</value>
    public int Resolution { get; set; }

    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public OxyColor Background { get; set; }

    /// <summary>
    /// Exports the specified plot model to a file.
    /// </summary>
    /// <param name="plotModel">The model to export.</param>
    /// <param name="fileName">The file name.</param>
    /// <param name="width">The width of the output bitmap.</param>
    /// <param name="height">The height of the output bitmap.</param>
    /// <param name="background">The background color. The default value is <c>null</c>.</param>
    /// <param name="resolution">The resolution (resolution). The default value is 96.</param>
    public static void Export(IPlotModel plotModel, string fileName, int width, int height, OxyColor background, int resolution = 96)
    {
      using var stream = File.Create(fileName);
      Export(plotModel, stream, width, height, background, resolution);
    }

    /// <summary>
    /// Exports the specified plot model to a stream.
    /// </summary>
    /// <param name="plotModel">The model to export.</param>
    /// <param name="stream">The stream.</param>
    /// <param name="width">The width of the output bitmap.</param>
    /// <param name="height">The height of the output bitmap.</param>
    /// <param name="background">The background color. The default value is <c>null</c>.</param>
    /// <param name="resolution">The resolution (resolution). The default value is 96.</param>
    public static void Export(IPlotModel plotModel, Stream stream, int width, int height, OxyColor background, int resolution = 96)
    {
      var exporter = new TiffExporter { Width = width, Height = height, Background = background, Resolution = resolution };
      exporter.Export(plotModel, stream);
    }

    /// <summary>
    /// Exports the specified plot model to a bitmap.
    /// </summary>
    /// <param name="plotModel">The plot model.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <param name="background">The background.</param>
    /// <param name="resolution">The resolution (dpi).</param>
    /// <returns>A bitmap.</returns>
    public static BitmapSource ExportToBitmap(
      IPlotModel plotModel,
      int width,
      int height,
      OxyColor background,
      int resolution = 96
      )
    {
      var exporter = new TiffExporter { Width = width, Height = height, Background = background, Resolution = resolution };
      return exporter.ExportToBitmap(plotModel);
    }

    /// <summary>
    /// Exports the specified <see cref="PlotModel" /> to the specified <see cref="Stream" />.
    /// </summary>
    /// <param name="plotModel">The model.</param>
    /// <param name="stream">The output stream.</param>
    public void Export(IPlotModel plotModel, Stream stream)
    {
      var bitmapSource = ExportToBitmap(plotModel);
      var encoder = new TiffBitmapEncoder();
      encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
      encoder.Save(stream);
    }

    /// <summary>
    /// Exports the specified plot model to a bitmap.
    /// </summary>
    /// <param name="plotModel">The model to export.</param>
    /// <returns>A bitmap.</returns>
    public BitmapSource ExportToBitmap(IPlotModel plotModel)
    {
      var scale = 96d / Resolution;
      var canvas = new Canvas { Width = Width * scale, Height = Height * scale, Background = Background.ToBrush() };
      canvas.Measure(new Size(canvas.Width, canvas.Height));
      canvas.Arrange(new Rect(0, 0, canvas.Width, canvas.Height));

      var context = new CanvasRenderContext(canvas)
      {
        RendersToScreen = false,
        TextFormattingMode = TextFormattingMode.Ideal
      };

      plotModel.Update(true);
      plotModel.Render(context, new OxyRect(0, 0, canvas.Width, canvas.Height));

      canvas.UpdateLayout();

      var renderTargetBitmap = new RenderTargetBitmap(Width, Height, Resolution, Resolution, PixelFormats.Pbgra32);
      renderTargetBitmap.Render(canvas);
      return renderTargetBitmap;

      // alternative implementation:
      // http://msdn.microsoft.com/en-us/library/system.windows.media.imaging.rendertargetbitmap.aspx
      // var dv = new DrawingVisual();
      // using (var ctx = dv.RenderOpen())
      // {
      //    var vb = new VisualBrush(canvas);
      //    ctx.DrawRectangle(vb, null, new Rect(new Point(), new Size(width, height)));
      // }
      // bmp.Render(dv);
    }
  }
}
