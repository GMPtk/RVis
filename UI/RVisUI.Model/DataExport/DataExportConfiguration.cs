using LanguageExt;

namespace RVisUI.Model
{
  public readonly struct DataExportConfiguration
  {
    public DataExportConfiguration(
      string title,
      string rootExportDirectory,
      string exportDirectoryName,
      bool openAfterExport,
      Arr<(string Name, bool IncludeInExport)> outputs
      )
    {
      Title = title;
      RootExportDirectory = rootExportDirectory;
      ExportDirectoryName = exportDirectoryName;
      OpenAfterExport = openAfterExport;
      Outputs = outputs;
    }

    public string Title { get; }
    public string RootExportDirectory { get; }
    public string ExportDirectoryName { get; }
    public bool OpenAfterExport { get; }
    public Arr<(string Name, bool IncludeInExport)> Outputs { get; }
  }
}
